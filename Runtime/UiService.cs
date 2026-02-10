using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <inheritdoc />
	public class UiService : IUiServiceInit
	{
		public static readonly UnityEvent<DeviceOrientation, DeviceOrientation> OnOrientationChanged = new ();
		public static readonly UnityEvent<Vector2, Vector2> OnResolutionChanged = new ();

		/// <summary>
		/// 가장 최근에 생성된 UiService 인스턴스에 대한 내부 정적 참조입니다.
		/// 에디터 도구가 플레이 모드 중에 활성 서비스에 접근하는 데 사용됩니다.
		/// 이 패키지 내의 에디터 코드에서만 접근 가능합니다.
		/// </summary>
		internal static UiService CurrentService { get; private set; }
		
		private readonly IUiAssetLoader _assetLoader;
		private readonly IDictionary<Type, UiConfig> _uiConfigs = new Dictionary<Type, UiConfig>();
		private readonly IList<UiInstanceId> _visibleUiList = new List<UiInstanceId>();
		private readonly IDictionary<int, UiSetConfig> _uiSets = new Dictionary<int, UiSetConfig>();
		private readonly IDictionary<Type, IList<UiInstance>> _uiPresenters = new Dictionary<Type, IList<UiInstance>>();

		private readonly IReadOnlyDictionary<int, UiSetConfig> _uiSetsReadOnly;
		private readonly IReadOnlyList<UiInstanceId> _visiblePresentersReadOnly;

		private Transform _uiParent;
		private bool _disposed;

		/// <inheritdoc />
		public IReadOnlyDictionary<int, UiSetConfig> UiSets => _uiSetsReadOnly;

		/// <inheritdoc />
		public IReadOnlyList<UiInstanceId> VisiblePresenters => _visiblePresentersReadOnly;

		public UiService() : this(new AddressablesUiAssetLoader()) { }

		public UiService(IUiAssetLoader assetLoader)
		{
			_assetLoader = assetLoader;
			
			// 에디터/디버깅 접근을 위한 정적 참조 설정
			CurrentService = this;
			
			// 프로퍼티 접근 시 할당을 방지하기 위해 읽기 전용 래퍼를 초기화합니다
			_uiSetsReadOnly = new ReadOnlyDictionary<int, UiSetConfig>(_uiSets);
			_visiblePresentersReadOnly = new ReadOnlyCollection<UiInstanceId>(_visibleUiList);
		}

		/// <inheritdoc />
		public void Init(UiConfigs configs)
		{
			if (configs == null)
			{
				throw new ArgumentNullException(nameof(configs), "UiConfigs cannot be null");
			}

			var uiConfigs = configs.Configs;
			var sets = configs.Sets;

			foreach (var uiConfig in uiConfigs)
			{
				if (string.IsNullOrEmpty(uiConfig.Address))
				{
					throw new ArgumentException($"UiConfig for type '{uiConfig.UiType.Name}' has empty address. This UI will fail to load.");
				}
				if (uiConfig.UiType == null)
				{
					throw new ArgumentException($"UiConfig with address '{uiConfig.Address}' has null UiType, skipping");
				}

				if (uiConfig.Layer < 0)
				{
					Debug.LogWarning($"UiConfig for type '{uiConfig.UiType.Name}' has negative layer number ({uiConfig.Layer}). This may cause unexpected behavior.");
				}
				if (uiConfig.Layer > 1000)
				{
					Debug.LogWarning($"UiConfig for type '{uiConfig.UiType.Name}' has very high layer number ({uiConfig.Layer}). Consider using lower values for better organization.");
				}

				AddUiConfig(uiConfig);
			}

			foreach (var set in sets)
			{
				AddUiSet(set);
			}

			_uiParent = new GameObject("Ui").transform;

			_uiParent.gameObject.AddComponent<UiServiceMonoComponent>();
			Object.DontDestroyOnLoad(_uiParent.gameObject);
		}

		/// <inheritdoc />
		public List<UiInstance> GetLoadedPresenters()
		{
			var list = new List<UiInstance>();
			
			foreach (var kvp in _uiPresenters)
			{
				list.AddRange(kvp.Value);
			}
			
			return list;
		}

		/// <inheritdoc />
		public T GetUi<T>() where T : UiPresenter
		{
			return GetUi<T>(ResolveInstanceAddress(typeof(T)));
		}
		
		/// <summary>
		/// 지정된 인스턴스 주소와 함께 <typeparamref name="T"/> 타입의 UI를 요청합니다
		/// </summary>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		/// <exception cref="KeyNotFoundException">
		/// 서비스에 지정된 타입과 인스턴스 주소의 <see cref="UiPresenter"/>가 없는 경우 발생합니다
		/// </exception>
		public T GetUi<T>(string instanceAddress) where T : UiPresenter
		{
			if (!TryFindPresenter(typeof(T), instanceAddress, out var presenter))
			{
				throw new KeyNotFoundException($"UI presenter of type {typeof(T).Name} with instance '{instanceAddress ?? "default"}' not found.");
			}
			return presenter as T;
		}

		/// <inheritdoc />
		public bool IsVisible<T>() where T : UiPresenter
		{
			return IsVisible<T>(ResolveInstanceAddress(typeof(T)));
		}
		
		/// <summary>
		/// 지정된 인스턴스 주소와 함께 <typeparamref name="T"/> UI 타입의 표시 상태를 요청합니다
		/// </summary>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		public bool IsVisible<T>(string instanceAddress) where T : UiPresenter
		{
			var instanceId = new UiInstanceId(typeof(T), instanceAddress);
			return _visibleUiList.Contains(instanceId);
		}

		/// <inheritdoc />
		public void AddUiConfig(UiConfig config)
		{
			if (!_uiConfigs.TryAdd(config.UiType, config))
			{
				Debug.LogWarning($"The UiConfig {config.Address} was already added");
			}
		}

		/// <inheritdoc />
		public void AddUiSet(UiSetConfig uiSet)
		{
			if (!_uiSets.TryAdd(uiSet.SetId, uiSet))
			{
				Debug.LogWarning($"The Ui Configuration with the id {uiSet.SetId.ToString()} was already added");
			}
		}

		/// <inheritdoc />
		public void AddUi<T>(T ui, int layer, bool openAfter = false) where T : UiPresenter
		{
			AddUi(ui, layer, string.Empty, openAfter);
		}
		
		/// <summary>
		/// 선택적 인스턴스 주소와 함께 UI 프레젠터를 서비스에 추가합니다
		/// </summary>
		/// <param name="ui">추가할 UI 프레젠터</param>
		/// <param name="layer">UI 프레젠터를 포함할 레이어</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		/// <param name="openAfter">추가 후 UI 프레젠터를 열지 여부</param>
		public void AddUi<T>(T ui, int layer, string instanceAddress, bool openAfter = false) where T : UiPresenter
		{
			var type = ui.GetType().UnderlyingSystemType;
			var instanceId = new UiInstanceId(type, instanceAddress);

			// 이미 존재하는지 확인합니다
			if (TryFindPresenter(type, instanceAddress, out _))
			{
				Debug.LogWarning($"The Ui {instanceId} was already added");
				return;
			}
			
			// 타입 인덱스 컬렉션에 추가합니다
			if (!_uiPresenters.TryGetValue(type, out var instanceList))
			{
				instanceList = new List<UiInstance>();
				_uiPresenters[type] = instanceList;
			}
			instanceList.Add(new UiInstance(type, instanceAddress, ui));
			
			// Canvas 정렬 순서가 레이어와 일치하는지 확인합니다
			EnsureCanvasSortingOrder(ui.gameObject, layer);

			ui.Init(this, instanceAddress);

			if (openAfter)
			{
				OpenUi(instanceId);
			}
		}

		/// <inheritdoc />
		public bool RemoveUi<T>() where T : UiPresenter
		{
			return RemoveUi(typeof(T));
		}

		/// <inheritdoc />
		public bool RemoveUi<T>(T uiPresenter) where T : UiPresenter
		{
			return RemoveUi(uiPresenter.GetType().UnderlyingSystemType, uiPresenter.InstanceAddress);
		}

		/// <inheritdoc />
		public bool RemoveUi(Type type)
		{
			return RemoveUi(type, ResolveInstanceAddress(type));
		}
		
		/// <summary>
		/// 지정된 타입과 인스턴스 주소의 UI를 언로드하지 않고 서비스에서 제거합니다
		/// </summary>
		/// <param name="type">제거할 UI의 타입</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		/// <returns>UI가 제거되었으면 true, 그렇지 않으면 false</returns>
		public bool RemoveUi(Type type, string instanceAddress)
		{
			var instanceId = new UiInstanceId(type, instanceAddress);
			_visibleUiList.Remove(instanceId);
			
			if (!_uiPresenters.TryGetValue(type, out var instanceList))
			{
				return false;
			}

			// 인스턴스를 찾아 제거합니다
			for (int i = 0; i < instanceList.Count; i++)
			{
				if (instanceList[i].Address == instanceAddress)
				{
					instanceList.RemoveAt(i);
					
					// 빈 타입 항목을 정리합니다
					if (instanceList.Count == 0)
					{
						_uiPresenters.Remove(type);
					}
					
					return true;
				}
			}
			
			return false;
		}

		/// <inheritdoc />
		public List<UiPresenter> RemoveUiSet(int setId)
		{
			if (!_uiSets.TryGetValue(setId, out var set))
			{
				throw new KeyNotFoundException($"UI Set with id {setId} not found.");
			}
			
			var list = new List<UiPresenter>();

			foreach (var instanceId in set.UiInstanceIds)
			{
				if (!TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var ui))
				{
					continue;
				}

				RemoveUi(instanceId.PresenterType, instanceId.InstanceAddress);

				list.Add(ui);
			}

			return list;
		}

		/// <inheritdoc />
		public async UniTask<T> LoadUiAsync<T>(bool openAfter = false, CancellationToken cancellationToken = default) where T : UiPresenter
		{
			return await LoadUiAsync(typeof(T), openAfter, cancellationToken) as T;
		}

		/// <inheritdoc />
		public async UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false, CancellationToken cancellationToken = default)
		{
			// UI 세트 작업과의 일관성을 보장하기 위해 config.Address를 기본/싱글턴 인스턴스 주소로 사용합니다. ResolveInstanceAddress는 기존 인스턴스에만 사용됩니다
			if (!_uiConfigs.TryGetValue(type, out var config))
			{
				throw new KeyNotFoundException($"The UiConfig of type {type} was not added to the service. Call {nameof(AddUiConfig)} first");
			}
			return await LoadUiAsync(type, config.Address, openAfter, cancellationToken);
		}
		
		/// <summary>
		/// 선택적 인스턴스 주소와 함께 지정된 타입의 UI를 비동기적으로 로드합니다
		/// </summary>
		/// <param name="type">로드할 UI의 타입</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		/// <param name="openAfter">로드 후 UI를 열지 여부</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰</param>
		/// <returns>로드된 UI로 완료되는 태스크</returns>
		public async UniTask<UiPresenter> LoadUiAsync(Type type, string instanceAddress, bool openAfter = false, CancellationToken cancellationToken = default)
		{
			if (!_uiConfigs.TryGetValue(type, out var config))
			{
				throw new KeyNotFoundException($"The UiConfig of type {type} was not added to the service. Call {nameof(AddUiConfig)} first");
			}

			var instanceId = new UiInstanceId(type, instanceAddress);
			
			if (TryFindPresenter(type, instanceAddress, out var existingUi))
			{
				Debug.LogWarning($"The Ui {instanceId} was already loaded");

				return existingUi;
			}

			// _uiParent에 직접 부모 설정 - 레이어 GameObject가 필요 없습니다
			var gameObject = await _assetLoader.InstantiatePrefab(config, _uiParent, cancellationToken);

			// 동일한 UiPresenter가 이미 로드되었는지 이중 확인합니다. 개발자가 LoadUiAsync를 반복 호출할 때 발생할 수 있습니다
			if (TryFindPresenter(type, instanceAddress, out var uiDouble))
			{
				_assetLoader.UnloadAsset(gameObject);

				return uiDouble;
			}

			var uiPresenter = gameObject.GetComponent<UiPresenter>();

			gameObject.SetActive(false);
			AddUi(uiPresenter, config.Layer, instanceAddress, openAfter);

			return uiPresenter;
		}

		/// <inheritdoc />
		public IList<UniTask<UiPresenter>> LoadUiSetAsync(int setId)
		{
			var uiTasks = new List<UniTask<UiPresenter>>();

			if (_uiSets.TryGetValue(setId, out var set))
			{
				foreach (var instanceId in set.UiInstanceIds)
				{
					if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out _))
					{
						continue;
					}

					uiTasks.Add(LoadUiAsync(instanceId.PresenterType, instanceId.InstanceAddress));
				}
			}

			return uiTasks;
		}

		/// <inheritdoc />
		public void UnloadUi<T>() where T : UiPresenter
		{
			UnloadUi(typeof(T));
		}

		/// <inheritdoc />
		public void UnloadUi<T>(T uiPresenter) where T : UiPresenter
		{
			UnloadUi(uiPresenter.GetType().UnderlyingSystemType, uiPresenter.InstanceAddress);
		}

		/// <inheritdoc />
		public void UnloadUi(Type type)
		{
			UnloadUi(type, ResolveInstanceAddress(type));
		}
		
		/// <summary>
		/// 지정된 타입과 인스턴스 주소의 UI를 언로드합니다
		/// </summary>
		/// <param name="type">언로드할 UI의 타입</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		public void UnloadUi(Type type, string instanceAddress)
		{
			var instanceId = new UiInstanceId(type, instanceAddress);
			
			if (!TryFindPresenter(type, instanceAddress, out var ui))
			{
				throw new KeyNotFoundException($"Cannot unload UI {instanceId}. It is not loaded.");
			}
			
			var config = _uiConfigs[type];
			
			RemoveUi(type, instanceAddress);

			_assetLoader.UnloadAsset(ui.gameObject);
		}

		/// <inheritdoc />
		public void UnloadUiSet(int setId)
		{
			var set = _uiSets[setId];

			foreach (var instanceId in set.UiInstanceIds)
			{
				if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out _))
				{
					UnloadUi(instanceId.PresenterType, instanceId.InstanceAddress);
				}
			}
		}

		/// <inheritdoc />
		public async UniTask<T> OpenUiAsync<T>(CancellationToken cancellationToken = default) where T : UiPresenter
		{
			return await OpenUiAsync(typeof(T), cancellationToken) as T;
		}

		/// <inheritdoc />
		public async UniTask<UiPresenter> OpenUiAsync(Type type, CancellationToken cancellationToken = default)
		{
			// UI 세트 작업과의 일관성을 보장하기 위해 config.Address를 기본/싱글턴 인스턴스 주소로 사용합니다. ResolveInstanceAddress는 기존 인스턴스에만 사용됩니다
			if (!_uiConfigs.TryGetValue(type, out var config))
			{
				throw new KeyNotFoundException($"The UiConfig of type {type} was not added to the service. Call {nameof(AddUiConfig)} first");
			}
			return await OpenUiAsync(type, config.Address, cancellationToken);
		}

		/// <summary>
		/// 선택적 인스턴스 주소와 함께 UI 프레젠터를 비동기적으로 엽니다
		/// </summary>
		/// <param name="type">열 UI 프레젠터의 타입</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰</param>
		public async UniTask<UiPresenter> OpenUiAsync(Type type, string instanceAddress, CancellationToken cancellationToken = default)
		{
			var ui = await GetOrLoadUiAsync(type, instanceAddress, cancellationToken);

			OpenUi(new UiInstanceId(type, instanceAddress));

			return ui;
		}

		/// <inheritdoc />
		public async UniTask<T> OpenUiAsync<T, TData>(TData initialData, CancellationToken cancellationToken = default) 
			where T : class, IUiPresenterData 
			where TData : struct
		{
			return await OpenUiAsync(typeof(T), initialData, cancellationToken) as T;
		}

		/// <inheritdoc />
		public async UniTask<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData, CancellationToken cancellationToken = default) where TData : struct
		{
			// UI 세트 작업과의 일관성을 보장하기 위해 config.Address를 기본/싱글턴 인스턴스 주소로 사용합니다. ResolveInstanceAddress는 기존 인스턴스에만 사용됩니다
			if (!_uiConfigs.TryGetValue(type, out var config))
			{
				throw new KeyNotFoundException($"The UiConfig of type {type} was not added to the service. Call {nameof(AddUiConfig)} first");
			}
			return await OpenUiAsync(type, config.Address, initialData, cancellationToken);
		}

		/// <summary>
		/// 초기 데이터와 선택적 인스턴스 주소와 함께 UI 프레젠터를 비동기적으로 엽니다
		/// </summary>
		/// <param name="type">열 UI 프레젠터의 타입</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		/// <param name="initialData">설정할 초기 데이터</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰</param>
		public async UniTask<UiPresenter> OpenUiAsync<TData>(Type type, string instanceAddress, TData initialData, CancellationToken cancellationToken = default) where TData : struct
		{
			var ui = await GetOrLoadUiAsync(type, instanceAddress, cancellationToken);

			if (ui is UiPresenter<TData> uiPresenter)
			{
				uiPresenter.Data = initialData;
			}
			else
			{
				Debug.LogError($"The UiPresenter {type} is not a {nameof(UiPresenter<TData>)} type. " +
							$"Implement UiPresenter<{typeof(TData).Name}> to allow it to open with initial defined data");
				return ui;
			}
			
			OpenUi(new UiInstanceId(type, instanceAddress));

			return ui;
		}

		/// <inheritdoc />
		public void CloseUi<T>(bool destroy = false) where T : UiPresenter
		{
			CloseUi(typeof(T), destroy);
		}

		/// <inheritdoc />
		public void CloseUi<T>(T uiPresenter, bool destroy = false) where T : UiPresenter
		{
			CloseUi(uiPresenter.GetType().UnderlyingSystemType, uiPresenter.InstanceAddress, destroy);
		}

		/// <inheritdoc />
		public void CloseUi(Type type, bool destroy = false)
		{
			CloseUi(type, ResolveInstanceAddress(type), destroy);
		}
		
		/// <summary>
		/// 선택적 인스턴스 주소와 함께 UI 프레젠터를 닫고 선택적으로 에셋을 파괴합니다
		/// </summary>
		/// <param name="type">닫을 UI 프레젠터의 타입</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 기본/싱글턴 인스턴스의 경우 null을 사용하세요.</param>
		/// <param name="destroy">UI 프레젠터의 에셋을 파괴할지 여부</param>
		public void CloseUi(Type type, string instanceAddress, bool destroy = false)
		{
			var instanceId = new UiInstanceId(type, instanceAddress);
			
			if (!_visibleUiList.Contains(instanceId))
			{
				Debug.LogWarning($"Is trying to close the {instanceId} ui but is not open");
				return;
			}

			_visibleUiList.Remove(instanceId);
			
			if (TryFindPresenter(type, instanceAddress, out var presenter))
			{
				presenter.InternalClose(destroy);
			}
		}

		/// <inheritdoc />
		public void CloseAllUi()
		{
			foreach (var instanceId in _visibleUiList)
			{
				if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter))
				{
					presenter.InternalClose(false);
				}
			}

			_visibleUiList.Clear();
		}

		/// <inheritdoc />
		public void CloseAllUi(int layer)
		{
			for (int i = _visibleUiList.Count - 1; i >= 0; i--)
			{
				var instanceId = _visibleUiList[i];

				if (_uiConfigs[instanceId.PresenterType].Layer == layer)
				{
					if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter))
					{
						presenter.InternalClose(false);
					}
					_visibleUiList.Remove(instanceId);
				}
			}
		}

		/// <inheritdoc />
		public void CloseAllUiSet(int setId)
		{
			var set = _uiSets[setId];

			foreach (var instanceId in set.UiInstanceIds)
			{
				CloseUi(instanceId.PresenterType, instanceId.InstanceAddress);
			}
		}

		/// <inheritdoc />
		public async UniTask<UiPresenter[]> OpenUiSetAsync(int setId, CancellationToken cancellationToken = default)
		{
			if (!_uiSets.TryGetValue(setId, out var set))
			{
				throw new KeyNotFoundException($"UI Set with id {setId} not found.");
			}

			var openTasks = new List<UniTask<UiPresenter>>(set.UiInstanceIds.Length);
			
			foreach (var instanceId in set.UiInstanceIds)
			{
				openTasks.Add(OpenUiAsync(instanceId.PresenterType, instanceId.InstanceAddress, cancellationToken));
			}

			return await UniTask.WhenAll(openTasks);
		}

		/// <summary>
		/// UI 서비스를 폐기하고, 모든 리소스를 정리하며 이벤트 구독을 해제합니다.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			// 정적 참조를 정리합니다
			if (CurrentService == this)
			{
				CurrentService = null;
			}

			// 모든 표시 중인 UI를 닫습니다
			CloseAllUi();

			// 모든 UI 프레젠터를 언로드합니다
			var presenterInstances = new List<UiInstanceId>();
			foreach (var kvp in _uiPresenters)
			{
				var type = kvp.Key;
				foreach (var instance in kvp.Value)
				{
					presenterInstances.Add(new UiInstanceId(type, instance.Address));
				}
			}
			
			foreach (var instanceId in presenterInstances)
			{
				try
				{
					UnloadUi(instanceId.PresenterType, instanceId.InstanceAddress);
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"Failed to unload UI {instanceId} during disposal: {ex.Message}");
				}
			}

			// 모든 컬렉션을 정리합니다
			_uiPresenters.Clear();
			_visibleUiList.Clear();
			_uiConfigs.Clear();
			_uiSets.Clear();

			// 정적 이벤트를 정리합니다
			// 참고: 정적 UnityEvent에 RemoveAllListeners를 호출하면 다른 인스턴스에 영향을 줄 수 있으므로 호출하지 않습니다

			// UI 부모 GameObject를 파괴합니다
			if (_uiParent != null)
			{
				Object.Destroy(_uiParent.gameObject);
				_uiParent = null;
			}
		}

		/// <summary>
		/// 타입과 주소로 프레젠터를 찾으려고 시도합니다
		/// </summary>
		/// <param name="type">찾을 UI 프레젠터의 타입</param>
		/// <param name="address">인스턴스 주소</param>
		/// <param name="presenter">찾은 프레젠터, 찾지 못하면 null</param>
		/// <returns>프레젠터를 찾았으면 true, 그렇지 않으면 false</returns>
		private bool TryFindPresenter(Type type, string address, out UiPresenter presenter)
		{
			if (_uiPresenters.TryGetValue(type, out var instances))
			{
				foreach (var instance in instances)
				{
					if (instance.Address == address)
					{
						presenter = instance.Presenter;
						return true;
					}
				}
			}

			presenter = null;
			return false;
		}

		private void EnsureCanvasSortingOrder(GameObject gameObject, int layer)
		{
			if (gameObject.TryGetComponent<Canvas>(out var canvas))
			{
				canvas.sortingOrder = layer;
			}
			else if (gameObject.TryGetComponent<UnityEngine.UIElements.UIDocument>(out var document))
			{
				document.sortingOrder = layer;
			}
		}

		private void OpenUi(UiInstanceId instanceId)
		{
			if (_visibleUiList.Contains(instanceId))
			{
				Debug.LogWarning($"Is trying to open the {instanceId} ui but is already open");
				return;
			}

			if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter))
			{
				presenter.InternalOpen();
				_visibleUiList.Add(instanceId);
			}
		}

		private async UniTask<UiPresenter> GetOrLoadUiAsync(Type type, string instanceAddress, CancellationToken cancellationToken = default)
		{
			if (!TryFindPresenter(type, instanceAddress, out var ui))
			{
				ui = await LoadUiAsync(type, instanceAddress, false, cancellationToken);
			}

			return ui;
		}

		/// <summary>
		/// 이미 로드된 프레젠터에 대해 작업할 때 주어진 타입의 인스턴스 주소를 확인합니다.
		/// 기존 인스턴스를 찾아야 하는 작업(GetUi, IsVisible, CloseUi 등)에서 사용됩니다.
		///
		/// 우선순위:
		/// 1. 인스턴스가 없으면 string.Empty를 반환합니다 (기본/싱글턴)
		/// 2. 인스턴스가 정확히 하나이면 해당 인스턴스의 주소를 반환합니다
		/// 3. 인스턴스가 여러 개이면 첫 번째를 반환합니다 (경고와 함께)
		///
		/// 참고: 이 메서드는 새 인스턴스를 생성하는 Load/Open 작업에 사용하면 안 됩니다.
		/// </summary>
		/// <param name="type">확인할 UI 프레젠터의 타입</param>
		/// <returns>확인된 인스턴스 주소</returns>
		private string ResolveInstanceAddress(Type type)
		{
			if (!_uiPresenters.TryGetValue(type, out var instances))
			{
				// 로드된 인스턴스가 없음 - 기본/싱글턴 인스턴스를 위해 빈 문자열 반환
				return string.Empty;
			}
			
			if (instances.Count == 1)
			{
				return instances[0].Address;
			}
			
			// 여러 인스턴스 발견 - 경고를 기록하고 첫 번째를 반환
			var instanceNames = new List<string>(instances.Count);
			foreach (var instance in instances)
			{
				instanceNames.Add(string.IsNullOrEmpty(instance.Address) ? "default" : instance.Address);
			}
			
			var firstMatch = instances[0];
			var selectedName = string.IsNullOrEmpty(firstMatch.Address) ? "default" : firstMatch.Address;
			Debug.LogWarning($"[UiService] Ambiguous call for {type.Name}: found {instances.Count} instances [{string.Join(", ", instanceNames)}]. " +
							$"Using '{selectedName}'. Specify instance address explicitly to avoid ambiguity.");
			
			return firstMatch.Address;
		}
	}
}