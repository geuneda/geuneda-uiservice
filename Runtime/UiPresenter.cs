using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// <see cref="UiPresenter"/>를 <see cref="UiPresenterData{T}"/>로 태그하여
	/// <see cref="UiService"/>를 통해 UI를 열 때 특정 상태를 정의할 수 있게 합니다
	/// </summary>
	public interface IUiPresenterData
	{
	}

	/// <summary>
	/// <seealso cref="IUiService"/>의 UI 프레젠터 최상위 기본 클래스입니다.
	/// 올바른 UI 생명주기를 실행하려면 이 추상 클래스를 구현하세요
	/// </summary>
	public abstract class UiPresenter : MonoBehaviour
	{
		protected IUiService _uiService;
		private List<PresenterFeatureBase> _features;
		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		/// <summary>
		/// 이 프레젠터 인스턴스를 고유하게 식별하는 인스턴스 주소입니다.
		/// 기본/싱글턴 인스턴스의 경우 빈 문자열입니다.
		/// </summary>
		internal string InstanceAddress { get; private set; } = string.Empty;

		/// <summary>
		/// <see cref="UiPresenter"/>의 열림 상태를 요청합니다
		/// </summary>
		public bool IsOpen => gameObject.activeSelf;

		/// <summary>
		/// 열기 전환이 완료되면 완료되는 태스크입니다.
		/// UI가 완전히 준비될 때까지 대기해야 하는 외부 코드에 유용합니다.
		/// </summary>
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <summary>
		/// 닫기 전환이 완료되면 완료되는 태스크입니다.
		/// UI가 완전히 닫힐 때까지 대기해야 하는 외부 코드에 유용합니다.
		/// </summary>
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <summary>
		/// UI 프레젠터 구현체가 서비스를 직접 호출하지 않고도 UI 프레젠터를 직접 닫을 수 있게 합니다
		/// </summary>
		protected void Close(bool destroy)
		{
			_uiService.CloseUi(GetType(), InstanceAddress, destroy);
		}

		/// <summary>
		/// UI 프레젠터 구현체가 초기화될 때 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnInitialized() {}

		/// <summary>
		/// UI 프레젠터 구현체가 열릴 때 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnOpened() {}

		/// <summary>
		/// UI 프레젠터 구현체가 닫힐 때 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnClosed() {}

		/// <summary>
		/// 모든 열기 전환(애니메이션, 딜레이)이 완료된 후 호출됩니다.
		/// UI가 완전히 보이고 상호작용할 준비가 되었을 때 반응하려면 이 메서드를 재정의하세요.
		/// 전환 기능이 없는 프레젠터에서도 항상 호출됩니다.
		/// </summary>
		protected virtual void OnOpenTransitionCompleted() {}

		/// <summary>
		/// 모든 닫기 전환(애니메이션, 딜레이)이 완료된 후 호출됩니다.
		/// UI가 닫기 전환을 완료했을 때 반응하려면 이 메서드를 재정의하세요.
		/// 전환 기능이 없는 프레젠터에서도 항상 호출됩니다.
		/// </summary>
		protected virtual void OnCloseTransitionCompleted() {}

		internal void Init(IUiService uiService, string instanceAddress)
		{
			_uiService = uiService;
			InstanceAddress = instanceAddress ?? string.Empty;
			InitializeFeatures();
			OnInitialized();
		}

		internal void InternalOpen()
		{
			InternalOpenProcessAsync().Forget();
		}

		internal void InternalClose(bool destroy)
		{
			InternalCloseProcessAsync(destroy).Forget();
		}

		private async UniTask InternalOpenProcessAsync()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();

			NotifyFeaturesOpening();
			gameObject.SetActive(true);
			OnOpened();
			NotifyFeaturesOpened();

			// 모든 기능의 열기 전환을 대기합니다
			await WaitForOpenTransitionsAsync();

			// 대기 중에 이 MonoBehaviour가 파괴되었는지 확인합니다 (예: Dispose 중)
			if (!this)
			{
				_openTransitionCompletion?.TrySetResult();
				return;
			}

			// 항상 알림 - 모든 프레젠터에 대해 일관된 생명주기
			OnOpenTransitionCompleted();

			_openTransitionCompletion.TrySetResult();
		}

		private async UniTask InternalCloseProcessAsync(bool destroy)
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();

			NotifyFeaturesClosing();
			OnClosed();
			NotifyFeaturesClosed();

			// 이 MonoBehaviour가 파괴되었는지 확인합니다 (예: Dispose 중)
			// 파괴된 오브젝트에도 안전한 암시적 bool 변환을 사용합니다
			if (!this)
			{
				_closeTransitionCompletion.TrySetResult();
				return;
			}

			// 모든 기능의 닫기 전환을 대기합니다
			await WaitForCloseTransitionsAsync();

			// 대기 후 다시 확인 - 대기 중에 오브젝트가 파괴되었을 수 있습니다
			if (!this)
			{
				_closeTransitionCompletion?.TrySetResult();
				return;
			}

			// 여기서 항상 숨김 - 단일 책임 지점
			gameObject.SetActive(false);

			// 항상 알림 - 모든 프레젠터에 대해 일관된 생명주기
			OnCloseTransitionCompleted();

			_closeTransitionCompletion.TrySetResult();

			if (destroy)
			{
				// UI 서비스가 Addressables.UnloadAsset을 호출하여 메모리에서 에셋을 언로드하고 게임 오브젝트를 파괴합니다
				_uiService.UnloadUi(GetType(), InstanceAddress);
			}
		}

		private UniTask WaitForOpenTransitionsAsync()
		{
			if (_features == null || _features.Count == 0)
			{
				return UniTask.CompletedTask;
			}

			// ITransitionFeature를 구현하는 기능들로부터 모든 열기 전환 태스크를 수집합니다
			List<UniTask> tasks = null;
			foreach (var feature in _features)
			{
				if (feature is ITransitionFeature transitionFeature)
				{
					var task = transitionFeature.OpenTransitionTask;
					if (task.Status != UniTaskStatus.Succeeded)
					{
						tasks ??= new List<UniTask>();
						tasks.Add(task);
					}
				}
			}

			return tasks != null && tasks.Count > 0 ? UniTask.WhenAll(tasks) : UniTask.CompletedTask;
		}

		private UniTask WaitForCloseTransitionsAsync()
		{
			if (_features == null || _features.Count == 0)
			{
				return UniTask.CompletedTask;
			}

			// ITransitionFeature를 구현하는 기능들로부터 모든 닫기 전환 태스크를 수집합니다
			List<UniTask> tasks = null;
			foreach (var feature in _features)
			{
				if (feature is ITransitionFeature transitionFeature)
				{
					var task = transitionFeature.CloseTransitionTask;
					if (task.Status != UniTaskStatus.Succeeded)
					{
						tasks ??= new List<UniTask>();
						tasks.Add(task);
					}
				}

			}

			return tasks != null && tasks.Count > 0 ? UniTask.WhenAll(tasks) : UniTask.CompletedTask;
		}

		private void InitializeFeatures()
		{
			_features = new List<PresenterFeatureBase>();
			GetComponents(_features);

			foreach (var feature in _features)
			{
				feature.OnPresenterInitialized(this);
			}
		}

		private void NotifyFeaturesOpening()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterOpening();
			}
		}

		private void NotifyFeaturesOpened()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterOpened();
			}
		}

		private void NotifyFeaturesClosing()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterClosing();
			}
		}

		private void NotifyFeaturesClosed()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterClosed();
			}
		}
	}

	/// <inheritdoc cref="UiPresenter"/>
	/// <remarks>
	/// <see cref="UiPresenter"/> 동작을 확장하여 <typeparamref name="T"/> 타입의 데이터를 보유합니다
	/// </remarks>
	public abstract class UiPresenter<T> : UiPresenter, IUiPresenterData where T : struct
	{
		private T _data;

		/// <summary>
		/// <see cref="UiService"/>를 통해 열릴 때 정의되는 UI 데이터입니다
		/// </summary>
		public T Data
		{
			get => _data;
			set
			{
				_data = value;
				OnSetData();
			}
		}

		/// <summary>
		/// 프레젠터에 정의된 데이터가 설정될 때 UI 프레젠터 구현체가 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnSetData() {}
	}
}

