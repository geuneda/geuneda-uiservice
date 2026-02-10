using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 타입, 주소 및 프레젠터 참조를 가진 UI 프레젠터 인스턴스를 나타냅니다
	/// </summary>
	public readonly struct UiInstance
	{
		/// <summary>
		/// UI 프레젠터의 타입입니다
		/// </summary>
		public readonly Type Type;

		/// <summary>
		/// 인스턴스 주소입니다 (기본/싱글턴 인스턴스의 경우 빈 문자열)
		/// </summary>
		public readonly string Address;

		/// <summary>
		/// UI 프레젠터 참조입니다
		/// </summary>
		public readonly UiPresenter Presenter;

		public UiInstance(Type type, string address, UiPresenter presenter)
		{
			Type = type;
			Address = address;
			Presenter = presenter;
		}
	}

	/// <summary>
	/// 게임의 UI <seealso cref="UiPresenter"/>와 상호작용하기 위한 추상화 레이어를 제공하는 서비스입니다.
	/// UI 서비스는 레이어별로 구성됩니다. 레이어가 높을수록 카메라 뷰포트에 더 가깝습니다.
	/// UiInstanceId 시스템을 통해 동일한 UI 타입의 다중 인스턴스를 지원합니다.
	/// </summary>
	public interface IUiService
	{
		
		/// <summary>
		/// 현재 표시 중인 모든 프레젠터 인스턴스의 읽기 전용 목록을 가져옵니다.
		/// 각 항목은 Type과 인스턴스 이름을 포함하는 UiInstanceId입니다.
		/// </summary>
		IReadOnlyList<UiInstanceId> VisiblePresenters { get; }

		/// <summary>
		/// UI 서비스가 관리하는 'UI 세트'라 불리는 UI 컨테이너의 읽기 전용 딕셔너리를 가져옵니다.
		/// </summary>
		IReadOnlyDictionary<int, UiSetConfig> UiSets { get; }

		/// <summary>
		/// UI 서비스에 의해 현재 메모리에 로드된 모든 UI 프레젠터를 가져옵니다.
		/// </summary>
		/// <returns>로드된 모든 UI 인스턴스 목록</returns>
		List<UiInstance> GetLoadedPresenters();

		/// <summary>
		/// 주어진 <typeparamref name="T"/> 타입의 UI를 요청합니다
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// 서비스에 주어진 <typeparamref name="T"/>의 <see cref="UiPresenter"/>가 없는 경우 발생합니다
		/// </exception>
		/// <typeparam name="T">요청된 UI 프레젠터의 타입입니다.</typeparam>
		/// <returns>요청된 <typeparamref name="T"/> 타입의 UI</returns>
		T GetUi<T>() where T : UiPresenter;

		/// <summary>
		/// 주어진 <typeparamref name="T"/> UI 타입의 표시 상태를 요청합니다.
		/// </summary>
		/// <typeparam name="T">표시 여부를 확인할 UI 프레젠터의 타입입니다.</typeparam>
		/// <returns>UI가 표시 중이면 true, 그렇지 않으면 false</returns>
		bool IsVisible<T>() where T : UiPresenter;

		/// <summary>
		/// 서비스에 UI 구성을 추가합니다.
		/// </summary>
		/// <param name="config">추가할 UI 구성.</param>
		void AddUiConfig(UiConfig config);

		/// <summary>
		/// 서비스에 UI 세트 구성을 추가합니다.
		/// </summary>
		/// <param name="uiSet">추가할 UI 세트 구성.</param>
		void AddUiSet(UiSetConfig uiSet);

		/// <summary>
		/// UI 프레젠터를 서비스에 추가하고 지정된 레이어에 포함시킵니다.
		/// <paramref name="openAfter"/>가 true이면 서비스에 추가된 후 UI 프레젠터가 열립니다.
		/// </summary>
		/// <typeparam name="T">추가할 UI 프레젠터의 타입.</typeparam>
		/// <param name="ui">추가할 UI 프레젠터.</param>
		/// <param name="layer">UI 프레젠터를 포함할 레이어.</param>
		/// <param name="openAfter">서비스에 추가 후 UI 프레젠터를 열지 여부.</param>
		void AddUi<T>(T ui, int layer, bool openAfter = false) where T : UiPresenter;

		/// <summary>
		/// 지정된 타입의 UI를 언로드하지 않고 서비스에서 제거합니다.
		/// </summary>
		/// <typeparam name="T">제거할 UI의 타입.</typeparam>
		/// <returns>UI가 제거되었으면 true, 그렇지 않으면 false.</returns>
		bool RemoveUi<T>() where T : UiPresenter;

		/// <summary>
		/// 지정된 UI 프레젠터를 언로드하지 않고 서비스에서 제거합니다.
		/// </summary>
		/// <typeparam name="T">제거할 UI 프레젠터의 타입.</typeparam>
		/// <param name="uiPresenter">제거할 UI 프레젠터.</param>
		/// <returns>UI 프레젠터가 제거되었으면 true, 그렇지 않으면 false.</returns>
		bool RemoveUi<T>(T uiPresenter) where T : UiPresenter;

		/// <summary>
		/// 지정된 타입의 UI를 언로드하지 않고 서비스에서 제거합니다.
		/// </summary>
		/// <param name="type">제거할 UI의 타입.</param>
		/// <returns>UI가 제거되었으면 true, 그렇지 않으면 false.</returns>
		bool RemoveUi(Type type);

		/// <summary>
		/// 서비스에 여전히 존재하는 지정된 UI 세트의 모든 UI 프레젠터를 제거하고 반환합니다.
		/// </summary>
		/// <param name="setId">제거할 UI 세트의 ID.</param>
		/// <returns>제거된 UI 프레젠터 목록.</returns>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 ID의 UI 세트가 없는 경우 발생합니다.</exception>
		List<UiPresenter> RemoveUiSet(int setId);

		/// <summary>
		/// 지정된 타입의 UI를 비동기적으로 로드합니다.
		/// 이 메서드는 async 메서드에서 제어할 수 있으며 로드된 UI를 반환합니다.
		/// <paramref name="openAfter"/>가 true이면 로딩 후 UI가 열립니다.
		/// </summary>
		/// <typeparam name="T">로드할 UI의 타입.</typeparam>
		/// <param name="openAfter">로딩 후 UI를 열지 여부.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>로드된 UI로 완료되는 태스크.</returns>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 타입의 UI 구성이 없는 경우 발생합니다.</exception>
		UniTask<T> LoadUiAsync<T>(bool openAfter = false, CancellationToken cancellationToken = default) where T : UiPresenter;

		/// <summary>
		/// 지정된 타입의 UI를 비동기적으로 로드합니다.
		/// 이 메서드는 async 메서드에서 제어할 수 있으며 로드된 UI를 반환합니다.
		/// <paramref name="openAfter"/>가 true이면 로딩 후 UI가 열립니다.
		/// </summary>
		/// <param name="type">로드할 UI의 타입.</param>
		/// <param name="openAfter">로딩 후 UI를 열지 여부.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>로드된 UI로 완료되는 태스크.</returns>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 타입의 UI 구성이 없는 경우 발생합니다.</exception>
		UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// 지정된 UI 세트의 모든 UI 프레젠터를 비동기적으로 로드합니다.
		/// 이 메서드는 async 메서드에서 제어할 수 있으며 로드된 각 UI를 반환합니다.
		/// UI는 먼저 로드된 순서대로 반환됩니다.
		/// </summary>
		/// <param name="setId">로드할 UI 세트의 ID.</param>
		/// <returns>로드된 각 UI로 완료되는 태스크 배열.</returns>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 ID의 UI 세트가 없는 경우 발생합니다.</exception>
		IList<UniTask<UiPresenter>> LoadUiSetAsync(int setId);

		/// <summary>
		/// 지정된 타입의 UI를 언로드합니다.
		/// </summary>
		/// <typeparam name="T">언로드할 UI의 타입.</typeparam>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 타입의 UI가 없는 경우 발생합니다.</exception>
		void UnloadUi<T>() where T : UiPresenter;

		/// <summary>
		/// 지정된 UI 프레젠터를 언로드합니다.
		/// </summary>
		/// <typeparam name="T">언로드할 UI 프레젠터의 타입.</typeparam>
		/// <param name="uiPresenter">언로드할 UI 프레젠터.</param>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 UI 프레젠터가 없는 경우 발생합니다.</exception>
		void UnloadUi<T>(T uiPresenter) where T : UiPresenter;

		/// <summary>
		/// 지정된 타입의 UI를 언로드합니다.
		/// </summary>
		/// <param name="type">언로드할 UI의 타입.</param>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 타입의 UI가 없는 경우 발생합니다.</exception>
		void UnloadUi(Type type);

		/// <summary>
		/// 지정된 타입의 특정 프레젠터 인스턴스를 언로드합니다.
		/// </summary>
		/// <param name="type">언로드할 UI의 타입.</param>
		/// <param name="instanceAddress">인스턴스 주소 (기본/싱글턴 인스턴스의 경우 빈 문자열).</param>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 타입과 인스턴스 주소의 UI가 없는 경우 발생합니다.</exception>
		void UnloadUi(Type type, string instanceAddress);

		/// <summary>
		/// 지정된 UI 세트의 모든 UI 프레젠터를 언로드합니다.
		/// </summary>
		/// <param name="setId">언로드할 UI 세트의 ID.</param>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 ID의 UI 세트가 없는 경우 발생합니다.</exception>
		void UnloadUiSet(int setId);

		/// <summary>
		/// UI 프레젠터를 비동기적으로 열고, 필요한 경우 에셋을 로드합니다.
		/// </summary>
		/// <typeparam name="T">열 UI 프레젠터의 타입.</typeparam>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>UI 프레젠터가 열리면 완료되는 태스크.</returns>
		UniTask<T> OpenUiAsync<T>(CancellationToken cancellationToken = default) where T : UiPresenter;

		/// <summary>
		/// UI 프레젠터를 비동기적으로 열고, 필요한 경우 에셋을 로드합니다.
		/// </summary>
		/// <param name="type">열 UI 프레젠터의 타입.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>UI 프레젠터가 열리면 완료되는 태스크.</returns>
		UniTask<UiPresenter> OpenUiAsync(Type type, CancellationToken cancellationToken = default);

		/// <summary>
		/// UI 프레젠터를 비동기적으로 열고, 필요한 경우 에셋을 로드하며, 초기 데이터를 설정합니다.
		/// </summary>
		/// <typeparam name="T">열 UI 프레젠터의 타입.</typeparam>
		/// <typeparam name="TData">설정할 초기 데이터의 타입.</typeparam>
		/// <param name="initialData">설정할 초기 데이터.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>UI 프레젠터가 열리면 완료되는 태스크.</returns>
		UniTask<T> OpenUiAsync<T, TData>(TData initialData, CancellationToken cancellationToken = default)
			where T : class, IUiPresenterData
			where TData : struct;

		/// <summary>
		/// UI 프레젠터를 비동기적으로 열고, 필요한 경우 에셋을 로드하며, 초기 데이터를 설정합니다.
		/// </summary>
		/// <param name="type">열 UI 프레젠터의 타입.</param>
		/// <param name="initialData">설정할 초기 데이터.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>UI 프레젠터가 열리면 완료되는 태스크.</returns>
		UniTask<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData, CancellationToken cancellationToken = default) where TData : struct;

		/// <summary>
		/// 지정된 UI 세트의 모든 UI 프레젠터를 열고, 필요한 경우 로드합니다.
		/// 이 메서드는 UI 세트에 대한 올바른 주소 처리를 보장하므로
		/// <see cref="CloseAllUiSet"/> 및 <see cref="UnloadUiSet"/>와 함께 안전하게 사용할 수 있습니다.
		/// 세트의 모든 UI는 UniTask.WhenAll을 사용하여 병렬로 열립니다.
		/// </summary>
		/// <param name="setId">열 UI 세트의 ID.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>세트의 모든 UI가 열리면 열린 모든 UI 프레젠터 배열로 완료되는 태스크.</returns>
		/// <exception cref="KeyNotFoundException">서비스에 지정된 ID의 UI 세트가 없는 경우 발생합니다.</exception>
		UniTask<UiPresenter[]> OpenUiSetAsync(int setId, CancellationToken cancellationToken = default);

		/// <summary>
		/// UI 프레젠터를 닫고 선택적으로 에셋을 파괴합니다.
		/// </summary>
		/// <typeparam name="T">닫을 UI 프레젠터의 타입.</typeparam>
		/// <param name="destroy">UI 프레젠터의 에셋을 파괴할지 여부.</param>
		void CloseUi<T>(bool destroy = false) where T : UiPresenter;

		/// <summary>
		/// UI 프레젠터를 닫고 선택적으로 에셋을 파괴합니다.
		/// </summary>
		/// <param name="uiPresenter">닫을 UI 프레젠터.</param>
		/// <param name="destroy">UI 프레젠터의 에셋을 파괴할지 여부.</param>
		/// <returns>UI 프레젠터가 닫히면 완료되는 태스크.</returns>
		void CloseUi<T>(T uiPresenter, bool destroy = false) where T : UiPresenter;

		/// <summary>
		/// UI 프레젠터를 닫고 선택적으로 에셋을 파괴합니다.
		/// </summary>
		/// <param name="type">닫을 UI 프레젠터의 타입.</param>
		/// <param name="destroy">UI 프레젠터의 에셋을 파괴할지 여부.</param>
		/// <returns>UI 프레젠터가 닫히면 완료되는 태스크.</returns>
		void CloseUi(Type type, bool destroy = false);

		/// <summary>
		/// 특정 프레젠터 인스턴스를 닫고 선택적으로 에셋을 파괴합니다.
		/// </summary>
		/// <param name="type">닫을 UI 프레젠터의 타입.</param>
		/// <param name="instanceAddress">인스턴스 주소 (기본/싱글턴 인스턴스의 경우 빈 문자열).</param>
		/// <param name="destroy">UI 프레젠터의 에셋을 파괴할지 여부.</param>
		void CloseUi(Type type, string instanceAddress, bool destroy = false);

		/// <summary>
		/// 모든 표시 중인 UI 프레젠터를 닫습니다.
		/// </summary>
		void CloseAllUi();

		/// <summary>
		/// 지정된 레이어의 모든 표시 중인 UI 프레젠터를 닫습니다.
		/// </summary>
		/// <param name="layer">UI 프레젠터를 닫을 레이어.</param>
		void CloseAllUi(int layer);

		/// <summary>
		/// 지정된 UI 세트 구성에 속하는 모든 UI 프레젠터를 닫습니다.
		/// </summary>
		/// <param name="setId">닫을 UI 세트 구성의 ID.</param>
		void CloseAllUiSet(int setId);
	}

	/// <inheritdoc cref="IUiService" />
	/// <remarks>
	/// 이 인터페이스는 게임의 UI 구성으로 UI 서비스를 초기화하는 방법을 제공합니다.
	/// </remarks>
	public interface IUiServiceInit : IUiService, IDisposable
	{
		/// <summary>
		/// 주어진 UI 구성으로 UI 서비스를 초기화합니다.
		/// </summary>
		/// <param name="configs">서비스를 초기화할 UI 구성.</param>
		/// <remarks>
		/// 게임의 UI를 구성하려면 다음과 같이 UiConfigs ScriptableObject를 생성해야 합니다:
		/// - Project View에서 우클릭 > Create > ScriptableObjects > Configs > UiConfigs
		/// - 중복된 UI 구성이나 UI 세트는 경고를 기록하지만 예외를 발생시키지 않습니다
		/// - 0 미만 또는 1000 초과의 레이어 번호는 경고를 기록합니다
		/// - 빈 어드레서블 주소 또는 null UI 타입은 ArgumentException을 발생시킵니다
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="configs"/>가 null인 경우 발생합니다.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// UI 구성에 빈 어드레서블 주소 또는 null UI 타입이 있는 경우 발생합니다.
		/// </exception>
		void Init(UiConfigs configs);
	}
}