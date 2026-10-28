# 변경 이력
이 패키지의 주요 변경 사항은 이 파일에 기록됩니다.

형식은 [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)를 기반으로 하며
이 프로젝트는 [Semantic Versioning](http://semver.org/spec/v2.0.0.html)을 준수합니다.

## [3.3.0] - 2026-10-28

**추가**:
- URP 프레젠터 피처 2종을 별도 어셈블리 `Geuneda.UiService.Urp`(`Runtime/Rendering/`, 네임스페이스 `Geuneda.UiService.Rendering`)로 추가했습니다. `com.unity.render-pipelines.universal` 17.0.1 이상이 있을 때만(`versionDefines` 로 `GEUNEDA_URP` 정의) 컴파일되고, 없으면 어셈블리와 테스트가 빠지므로 패키지는 URP 전용이 아닙니다. URP 가 없는 프로젝트에서는 `Runtime/Rendering/Shaders/UiBackdropBlur.shader` 가 URP 셰이더 라이브러리를 찾지 못해 셰이더 임포트 오류 로그가 한 번 남습니다(컴파일과 실행에는 영향 없음).
- `UiCameraStackFeature`: 프레젠터의 `Canvas` 를 Screen Space - Camera 로 바꾸고 오버레이 카메라를 기본 카메라(`Camera.main`, `BaseCameraResolver` 로 교체)의 URP 카메라 스택에 열 때 넣고 비활성화될 때 뺍니다. 스택 안 순서는 캔버스 `sortingOrder`(또는 직렬화한 `_stackPriority`)의 오름차순이고 같은 값은 뒤에 붙습니다(`InsertIndex`). 캔버스 아래에 둔 카메라는 캔버스 밖으로 꺼내고 프레젠터가 파괴될 때 함께 파괴합니다. Overlay 캔버스와 섞이면 `UiConfig.Layer` 가 전체 순서가 아닙니다([docs/urp-rendering.md](docs/urp-rendering.md)).
- `UiBackdropBlurRendererFeature`(URP Renderer 에셋에 추가) + `UiBackdropBlurPresenterFeature`(프레젠터에 추가): 프레젠터가 열려 있는 동안 뒤 배경을 흐립니다. 모양(다운샘플, 반복 1~8, 퍼짐, 틴트)은 렌더러 피처에서 정하고, 런타임에는 정적 `UiBackdropBlurRendererFeature.IterationsOverride`(0 이하면 해제, `EffectiveIterations` 로 확인)로 반복 횟수만 바꿉니다. Game 카메라이고 오버레이 카메라나 렌더 텍스처 카메라가 아니며 흐림 프레젠터가 하나라도 열려 있을 때만 패스를 넣습니다. 렌더러 피처 없이 프레젠터 피처만 열면 설정 방법을 적은 Error 로그를 남깁니다.
- `Tests/EditMode/Urp/`, `Tests/PlayMode/Urp/`(각각 `Geuneda.UiService.Tests.Urp`, `Geuneda.UiService.Tests.PlayMode.Urp`, URP 가 있을 때만)와 `InteractableTextView` PlayMode 테스트를 추가했습니다.

**수정**:
- `InteractableTextView` 가 링크 판정에 항상 `null` 카메라를 넘겨 Screen Space - Camera 와 World Space 캔버스에서 클릭 위치가 어긋나던 문제를 수정했습니다. 캔버스의 `worldCamera` 를 넘기고, Screen Space - Overlay 와 카메라 없는 Screen Space - Camera 캔버스, 부모 Canvas 가 없는 경우에는 그대로 `null` 을 넘깁니다.

## [3.2.0] - 2026-10-18

**추가**:
- `UiService.CanvasOrderStep`(기본 1): 같은 레이어에서 나중에 연 uGUI 프레젠터의 루트 `Canvas.sortingOrder` 에 더하는 간격입니다. 프레젠터 안의 자식 Canvas 를 루트 기준 오프셋(+1, +2 ...)으로 그리는 프로젝트가 10 처럼 크게 잡으면, 아래 프레젠터의 자식이 위 프레젠터를 덮지 않습니다. 값은 `레이어 + 순위 × 간격` 이고 상한(다음 설정 레이어 - 1, 없으면 32766)과 간격이 모자랄 때 먼저 연 캔버스가 시작 값을 같이 쓰는 규칙은 같습니다. 1 보다 작으면 `ArgumentOutOfRangeException`. 다음 열기·닫기부터 적용됩니다. 기본값이 1 이라 기존 동작은 바뀌지 않습니다.

## [3.1.0] - 2026-10-17

공개 API 는 그대로이고 아래 두 동작이 바뀝니다.

**동작 변경**:
- uGUI 프레젠터의 루트 `Canvas.sortingOrder` 를 열고 닫을 때마다 `레이어 + 그 레이어 안의 순위` 로 다시 매깁니다. 순위는 그 레이어에서 마지막으로 연 순서로 0 부터 1 씩 오르고, 닫기 전환 중이라 아직 그려지는 캔버스도 함께 매깁니다(UIDocument 와 같은 규칙과 시점). 같은 레이어에서도 나중에 연 팝업이 위에 그려지고 레이캐스트도 먼저 받습니다. 이전에는 모두 레이어 값 그대로라 앞뒤를 Unity 가 정했고, 닫기 전환 중에 다시 연 팝업처럼 꺼진 적 없는 캔버스는 나중에 열어도 아래에 남았습니다.
  - 값은 `UiConfigs` 에서 다음으로 높은 레이어보다 작게(다음 레이어가 없으면 입력 차단막 32767 보다 작게) 씁니다. Canvas 범위(-32768~32766) 밖의 레이어는 경계 값부터 매기고 레이어마다 한 번 경고합니다(이전에는 32767 이상이면 차단막과 같은 값). 간격보다 많이 띄우면 먼저 연 캔버스들이 레이어 값을 같이 쓰고(그 사이 앞뒤는 정해지지 않음) 레이어마다 한 번 경고합니다. 나중에 연 캔버스는 계속 위입니다.
  - 프리팹 루트의 Canvas 만 바꾸고 자식 Canvas 는 그대로 둡니다. 루트 `Canvas.sortingOrder` 가 레이어 값과 같다고 가정한 코드나, 레이어 바로 위 값(레이어 + 1 등)에 서비스 밖 캔버스를 둔 경우는 확인이 필요합니다. 레이어는 동시에 띄울 uGUI 수만큼 띄워 둡니다(예: 0, 10, 20).
  - 모달이 다른 uGUI 와 레이어를 같이 쓸 때 남기던 경고를 없앴습니다. 나중에 연 모달의 캔버스가 위로 가므로 dim(그 캔버스의 첫 자식)이 먼저 연 캔버스를 덮습니다.
- 같은 `UiService` 로 만든 `UiPopupQueue` 끼리, 다른 큐의 팩토리가 로드 중이면 기다립니다(먼저 시작한 큐가 이김). 이전에는 표시된 팝업만 보고 기다려 두 큐가 동시에 팩토리를 시작하면 팝업 두 개가 함께 뜰 수 있었습니다.
  - 팩토리가 끝나면(실패, 취소, null 포함) 기다리던 큐 중 그때 진행할 수 있는 큐가 기다리기 시작한 순서대로, 로드를 마친 큐의 다음 항목보다 먼저 진행합니다. 팝업이 떴으면 그 뒤로는 기존 규칙(큐 밖 `HandlesBack` 프레젠터가 닫힐 때까지 대기)을 따르고, 그 팝업이 닫히면 큐들은 만든 순서대로 다시 확인합니다(기다린 순서는 이어지지 않음).
  - 기다리던 큐를 진행시키는 도중 그 큐(예: `Drained` 구독자)가 던지면 기록하고, 남은 큐와 로드를 마친 큐는 계속 진행합니다.
  - 로드 중인 큐를 Dispose 하면 기다리던 큐가 바로 진행합니다. `IUiService` 를 직접 구현한 서비스로 만든 큐는 서로 기다리지 않습니다.

## [3.0.0] - 2026-10-17

**호환성 주의**:
- `com.geuneda.core` 0.4.0 에 의존합니다. git URL 로 설치한 프로젝트는 `Packages/manifest.json` 에 `"com.geuneda.core": "https://github.com/geuneda/geuneda-core.git"` 를 추가해야 합니다. core 를 넣지 않을 기존 프로젝트는 `#v2.0.0` 에 고정하세요.
- 예외만 남기던 곳은 무엇이 실패했는지 적은 Error 줄을 먼저 남기고 예외를 남깁니다. `LogAssert.Expect(LogType.Exception, ...)` 로 이 로그를 기대하던 테스트는 그 앞에 `LogType.Error` 기대를 추가해야 합니다. 해당 위치: 프레젠터 열기·닫기 단계 실패와 전환 관찰 실패, 다른 피처 전환의 추가 실패, 표시 상태 이벤트 구독자 예외, 정렬·dim 갱신 예외, 팝업 팩토리 예외.

**변경**:
- 런타임 로그를 core 의 `ModuleLogger` 로 옮겼습니다. 메시지 앞에 모듈 이름이 붙습니다: `[Geuneda.UiService]`(`UiService`), `[Geuneda.UiService.Presenter]`(`UiPresenter`), `[Geuneda.UiService.Transition]`(피처 전환 관찰), `[Geuneda.UiService.ModalDim]`, `[Geuneda.UiService.InputBlocker]`, `[Geuneda.UiService.PopupQueue]`, `[Geuneda.UiService.UiSet]`. 메시지에 직접 붙이던 `[UiService]`, `[UiPopupQueue]` 는 뺐습니다. 에디터 도구의 로그는 그대로입니다.

## [2.0.0] - 2026-10-14

**호환성 주의**:
- `IUiService` 에 멤버를 추가했습니다(`PresenterOpened`, `PresenterClosed`, `TryGetUi`, `HandleBack`, `AddBackHandler`, `BackUnhandled`, `IsInputBlocked`, `InputBlockReasons`, `BlockInput`). `IUiService` 를 직접 구현한 클래스는 새 멤버를 구현해야 합니다. 기존 프로젝트는 `#v1.3.2` 로 고정합니다.
- `OpenUi` 가 프레젠터를 표시 목록에 먼저 넣고 엽니다. `OnOpened` 안에서 다른 UI 를 열면 표시 목록 순서가 연 순서와 같아지고, `OnOpened` 안에서 `IsVisible` 이 true 입니다.
- 프레젠터가 닫기 전환 태스크를 `OnPresenterClosing()` 직후(`OnClosed()` 전)에 읽습니다(이전에는 `OnPresenterClosed()` 직후). `OnPresenterClosed()` 에서 닫기 전환을 시작하는 커스텀 피처는 `OnPresenterClosing()` 으로 옮겨야 프레젠터가 기다립니다.
- `CloseAllUi()`, `CloseAllUi(int)` 가 UI 마다 표시 목록에서 빼고 닫습니다. 닫는 도중 콜백에서 연(닫았다 다시 연) UI 는 닫지 않습니다.
- 서비스가 UIDocument 프레젠터의 `sortingOrder` 를 열고 닫을 때마다 `레이어 + 순위 × 1/1024` 로 다시 매깁니다(마지막으로 연 순서, 닫기 전환 중인 문서 포함). 같은 레이어 안에서도 나중에 연 문서가 위에 그려집니다.
- 표시 상태가 바뀌는 도중(구독자, 훅)에 연 UI·닫은 UI 의 훅은 지금 처리 중인 알림·훅이 끝난 뒤에 실행됩니다. `IsVisible` 은 바로 바뀝니다.
- `RemoveUi`/`UnloadUi` 가 진행 중이던 열기·닫기 전환 태스크를 완료합니다.
- `destroy: true` 닫기는 언로드를 마친 뒤 `CloseTransitionTask` 를 완료합니다. 완료를 기다리던 코드가 같은 인스턴스를 다시 열어도 언로드되지 않습니다.

**신규**:
- 뒤로가기: `UiPresenter.HandlesBack`/`OnBackPressed` 재정의, `IUiService.HandleBack()`(입력 차단 → 핸들러 → 화면 맨 위 대상 → `BackUnhandled`), `AddBackHandler(handler, priority)`. 화면 맨 위는 레이어가 높을수록, 같으면 나중에 연 프레젠터입니다.
- `UiService.ReadsBackInput`: Escape(Android 뒤로가기 포함)를 직접 읽어 `HandleBack()` 을 부릅니다. Input System 패키지가 있으면 `<Keyboard>/escape` 액션을, 없으면 레거시 Input Manager 를 씁니다(선택 의존성, `versionDefines`).
- 모달 dim: `UiPresenter.IsModal`/`OnDimClicked`, `UiService.DimColor`. 표시 중인 모달 중 맨 위 하나 뒤에 공용 dim 을 깝니다. uGUI 는 프레젠터 캔버스의 첫 자식, UI Toolkit 은 같은 패널에 모달 문서보다 반 단계(1/2048) 낮은 정렬값의 dim 문서입니다.
- 입력 차단: `IUiService.BlockInput(reason)` 이 할당 없는 토큰 `UiInputBlock` 을 돌려줍니다. 막힌 동안 정렬값 32767 의 uGUI 차단 캔버스와(UI Toolkit 프레젠터가 있으면) UI Toolkit 차단 패널을 켜고 뒤로가기를 무시합니다. 선택·포커스로 들어오는 키보드·게임패드 입력도 막습니다(EventSystem 선택 해제와 내비게이션 끄기, UIDocument 패널의 키·내비게이션 이벤트 차단). 막힌 동안 새로 생긴 EventSystem 과 새로 붙은 패널에도 적용하고, 풀 때 내비게이션은 되돌리지만 선택은 복구하지 않습니다. 같은 토큰을 두 번 풀면 경고합니다.
- 팝업 큐: `IUiPopupQueue`/`UiPopupQueue`. 우선순위(큰 값 먼저, 같으면 넣은 순서), 키 중복 방지, 띄울 때 만드는 팩토리(null 이면 건너뜀, 예외는 기록 후 계속), 닫기 전환까지 기다린 뒤 다음 팝업, `Hold(reason)` 토큰, 큐 밖에서 연 `HandlesBack` 프레젠터가 닫힐 때까지 대기, `Drained` 이벤트.
- 표시 상태 이벤트 `PresenterOpened`/`PresenterClosed` 와 `TryGetUi(UiInstanceId, out UiPresenter)`. 열기·닫기 훅보다 먼저 발생합니다. 알림과 훅 실행은 하나씩 차례로 처리해, 구독자나 훅 안에서 연 UI·닫은 UI 의 알림과 훅은 지금 처리 중인 것이 끝난 뒤에 실행됩니다(모든 구독자가 같은 순서를 받음). 구독자 예외는 기록만 하고 다른 구독자에게 계속 전달합니다.

**수정**:
- 닫기 전환 중에 다시 열면 전환이 끝날 때 프레젠터가 숨겨지고(`destroy: true` 면 언로드되고) 표시 목록과 어긋나던 문제를 수정했습니다. 늦게 끝난 닫기는 숨김·언로드·`OnCloseTransitionCompleted` 를 건너뜁니다. 열기 전환 중에 닫으면 `OnOpenTransitionCompleted` 를 부르지 않습니다. 전환 중 다시 열고 닫을 때 이전 과정이 새 전환 태스크를 먼저 완료하던 문제도 함께 고쳤습니다.
- `CloseAllUi()` 도중 닫기 콜백에서 UI 를 열면 컬렉션 변경 예외가 나던 문제를 수정했습니다.
- 열기·닫기 훅(`OnOpened`, `OnClosed`, 피처 콜백, `TimeDelayFeature`/`AnimationDelayFeature` 의 시작·완료 훅)이 예외를 던지면 `OpenTransitionTask`/`CloseTransitionTask` 가 끝나지 않던 문제를 수정했습니다. 태스크는 같은 예외로 실패하고 예외는 한 번만 기록됩니다(관찰되지 않은 실패로 GC 때 다시 기록되지 않음). 닫기가 실패하면 숨김과 요청된 언로드를 마칩니다. 피처 닫기 시작 훅의 실패는 `OnClosed` 전에 단계를 실패시키고, 여러 피처의 실패는 각각 한 번씩 기록됩니다. `destroy: true` 닫기의 언로드가 실패하면 닫기 태스크도 그 예외로 실패합니다. `UnloadUi` 의 로더가 던져도 진행 중이던 전환 태스크는 완료됩니다.
- `TimeDelayFeature`, `AnimationDelayFeature` 가 지연 중에 다시 열고 닫으면 이전 전환이 새 전환 태스크를 먼저 완료하고 자신의 태스크는 끝내 완료하지 않던 문제를 수정했습니다(프레젠터의 닫기가 영원히 끝나지 않음). 각 전환이 자신의 태스크를 완료하고, 지연 중에 반대 알림(지연·클립이 없는 전환 포함)이 오면 늦게 끝난 전환은 완료 훅을 건너뜁니다.
- `AddressablesUiAssetLoader` 로 로드를 취소하면 인스턴스화가 계속돼 추적되지 않는 GameObject 가 남던 문제를 수정했습니다. 취소되면 인스턴스화가 끝나는 대로 해제합니다(에디터에서 수정 전 누수와 수정 후 해제를 확인).

## [1.3.2] - 2026-10-12

**수정**:
- `UiType` 이 null 인 `UiConfig` 를 `UiConfigSerializable` 로 변환(설정 저장)할 때 `NullReferenceException` 이 나던 문제를 수정했습니다. 타입이 없으면 null 로 저장하고, 이런 설정은 `Init` 에서 `ArgumentException` 으로 알려 줍니다.

**신규**:
- Unity 6.5+ 에서 레이어 정렬이 프리팹 루트의 `PanelRenderer.sortingOrder` 에도 적용됩니다(`#if UNITY_6000_5_OR_NEWER`). 설정 에디터의 레이어 읽기/쓰기도 `PanelRenderer` 를 지원합니다.

**변경**:
- 설정 에디터가 정렬 컴포넌트가 없는 경우와 음수 정렬값을 구분하지 못해, 음수 `sortingOrder` 를 가진 프리팹의 레이어를 0 이나 이전 값으로 저장하던 문제를 수정했습니다. 세 설정 에디터의 판독 로직을 `UiConfigsEditorBase.TryGetSortingOrder` 로 모았습니다.

## [1.3.1] - 2026-10-12

**수정**:
- `UiConfigs` 에셋이 `LoadSynchronously` 를 저장하지 않아 `[LoadSynchronously]` 어트리뷰트가 런타임에 효과가 없던 문제를 수정했습니다. `UiConfigSerializable` 에 `LoadSynchronously` 필드를 추가했으며, 기존 에셋은 `false` 로 읽힙니다. 설정 인스펙터를 열어 동기화하면 어트리뷰트 값이 저장됩니다.
- 설정 없이 `AddUi` 로 추가한 프레젠터에서 `UnloadUi`, `Close(destroy: true)`, `CloseAllUi(int)` 가 `KeyNotFoundException` 을 던지던 문제를 수정했습니다. `CloseAllUi(int)` 는 설정이 없으면 `AddUi` 에 전달된 레이어를 사용합니다.
- `AddressablesUiAssetLoader.UnloadAsset` 이 Addressables 로 만들지 않은 인스턴스(`Instantiate` 후 `AddUi` 한 프레젠터)를 파괴하지 않던 문제를 수정했습니다. `Addressables.ReleaseInstance` 가 `false` 를 돌려주면 `Object.Destroy` 로 파괴합니다.
- `Init` 이 주소가 비어 있고 타입도 null 인 설정을 만나면 `ArgumentException` 대신 `NullReferenceException` 이 나던 문제를 수정했습니다.
- `package.json` 에 런타임 어셈블리가 사용하는 `com.unity.ugui`(uGUI, TextMeshPro) 의존성을 추가했습니다.
- 실제 동작과 맞지 않던 XML 주석을 정정했습니다(`AssemblyInfo`, `UiConfig`, `IUiServiceInit.Init` 생성 메뉴 경로, `LoadUiSetAsync`, `CloseUi`, `InteractableTextView`).

## [1.3.0] - 2026-07-08

**신규**:
- Unity 6.5(6000.5)+ 에서 `UIDocument`를 대체하는 `PanelRenderer` 기반 `UiToolkitPanelRendererFeature`를 추가했습니다. 기존 `UiToolkitPresenterFeature`(UIDocument)와 동일한 `AddVisualTreeAttachedListener(callback)` API 를 노출하므로 프레젠터 코드는 피처 타입만 교체하면 됩니다. PanelRenderer 는 `rootVisualElement` 프로퍼티가 없어 `RegisterUIReloadCallback` 콜백으로 루트 `VisualElement`를 공급합니다. `#if UNITY_6000_5_OR_NEWER`로 가드되어 6.5 미만 버전에는 영향을 주지 않습니다.

## [1.2.0] - 2026-01-07

**신규**:
- 플레이 모드에서 UI 프레젠터를 관리하기 위한 `UiPresenterManagerWindow` 통합 에디터 도구를 추가했습니다.

**변경**:
- **호환성 주의**: `IUiAnalytics`, `UiAnalytics`, `NullAnalytics` 및 모든 관련 분석 타입을 제거했습니다.
- 레거시 `UiAnalyticsWindow` 및 `UiServiceHierarchyWindow` 에디터 도구를 제거했습니다.

## [1.1.0] - 2026-01-06

**신규**:
- 적절한 주소 처리를 통해 세트 내 모든 UI 프레젠터를 여는 `OpenUiSetAsync(int setId, CancellationToken)` 메서드를 `IUiService`에 추가하여 `CloseAllUiSet` 및 `UnloadUiSet`과의 호환성을 보장합니다.
- 모든 전환 애니메이션/딜레이가 완료된 후 반응하기 위한 `OnOpenTransitionCompleted()` 및 `OnCloseTransitionCompleted()` 생명주기 훅을 `UiPresenter`에 추가했습니다.
- 종합 테스트 스위트를 추가했습니다:
  - `UiAnalytics`, `UiConfig`, `UiInstanceId`, `UiServiceCore`, `UiSetConfig`에 대한 단위 테스트
  - 멀티 인스턴스, 로딩, 열기/닫기, UI 세트 관리에 대한 PlayMode 통합 테스트
  - 성능 및 스모크 테스트
  - `AnimationDelayFeature`, `TimeDelayFeature`, `PresenterFeatureBase`에 대한 피처별 테스트
- 열기/닫기 전환 딜레이를 제공하는 피처를 위한 `ITransitionFeature` 인터페이스를 추가했습니다.
- 외부에서 전환 완료를 대기하기 위한 `OpenTransitionTask` 및 `CloseTransitionTask` 공개 프로퍼티를 `UiPresenter`에 추가했습니다.
- AI 코딩 에이전트를 위한 `AGENTS.md` 문서를 추가했습니다.
- 시작하기, 핵심 개념, API 레퍼런스, 고급 주제, 문제 해결을 위한 별도 페이지가 포함된 구조화된 문서를 `docs/` 폴더 아래에 추가했습니다.
- 패키지 라이브러리에 여러 새 샘플을 추가했습니다.
- 다양한 에셋 로딩 시나리오를 지원하기 위해 여러 `IUiAssetLoader` 구현을 추가했습니다:
  - `AddressablesUiAssetLoader` (기본): Unity Addressables 통합.
  - `PrefabRegistryUiAssetLoader`: 직접 프리팹 참조를 위한 간단한 로더 (테스트 및 샘플에 유용).
  - `ResourcesUiAssetLoader`: Unity `Resources` 폴더에서의 로딩 지원.
- UI 설정 관리를 위한 `AddressablesUiConfigs`, `ResourcesUiConfigs`, `PrefabRegistryUiConfigs`, `ResourcesUiConfigsEditor`, `AddressablesUiConfigsEditor` 및 `PrefabRegistryUiConfigsEditor`를 추가했습니다.

**변경**:
- **호환성 주의**: 특수 서브클래스(`AddressablesUiConfigs`, `ResourcesUiConfigs`, `PrefabRegistryUiConfigs`)의 사용을 강제하고 잘못된 설정으로 인한 런타임 오류를 방지하기 위해 `UiConfigs` 클래스를 `abstract`로 변경했습니다.
- **호환성 주의**: `IPresenterFeature` 인터페이스를 제거했습니다; 피처는 이제 `PresenterFeatureBase`를 직접 확장합니다.
- **호환성 주의**: 특정 로딩 메커니즘을 반영하기 위해 `UiAssetLoader`를 `AddressablesUiAssetLoader`로 이름을 변경했습니다.
- **호환성 주의**: 로더 비의존성을 위해 `UiConfig.AddressableAddress`를 `UiConfig.Address`로 이름을 변경했습니다.
- `UiPresenter<T>.Data` 프로퍼티를 할당 시 자동으로 `OnSetData()`를 트리거하는 공개 setter로 변경했습니다.
- `TimeDelayFeature`와 `AnimationDelayFeature`가 더 이상 `gameObject.SetActive(false)`를 직접 호출하지 않도록 리팩토링했습니다; 가시성은 이제 `UiPresenter`에 의해서만 제어됩니다.
- `UiPresenter.InternalOpen()`과 `InternalClose()`를 `ITransitionFeature` 태스크를 대기하는 내부 비동기 프로세스를 사용하도록 리팩토링했습니다.
- `AnimationDelayFeature`와 `TimeDelayFeature`를 내부 이벤트 대신 `Presenter.NotifyOpenTransitionCompleted()`와 `Presenter.NotifyCloseTransitionCompleted()`를 사용하도록 리팩토링했습니다.
- 딜레이 피처에서 `OnOpenCompletedEvent`와 `OnCloseCompletedEvent` 내부 이벤트를 제거했습니다.
- 더 나은 프로젝트 호환성을 위해 모든 샘플을 입력 시스템 의존성 대신 UI 버튼을 사용하도록 업데이트했습니다.

**수정**:
- `AnimationDelayFeature` 애니메이션 재생 로직 수정 - `!_introAnimationClip` 대신 `_introAnimationClip != null`을 잘못 확인하고 있었습니다.
- `UiPresenterEditor` 플레이 모드 버튼이 `gameObject.SetActive()` 토글 대신 `InternalOpen()`과 `InternalClose()`를 올바르게 호출하도록 수정했습니다.
- 테스트를 함께 실행할 때 딜레이 피처가 올바르게 작동하도록 수정했습니다 (UniTaskCompletionSource 생명주기).
- Unity 오브젝트 호환성을 위해 null 조건 연산자 대신 명시적 null 비교를 사용하도록 딜레이 피처의 null 체크를 수정했습니다.
- `OnOpenTransitionCompleted`/`OnCloseTransitionCompleted`가 피처가 존재할 때만 호출되는 일관성 없는 생명주기를 수정했습니다.
- `UiPresenter`와 피처 모두 `SetActive(false)`를 호출할 수 있었던 가시성 제어의 분리된 책임을 수정하여, 이제 모든 시나리오에서 프레젠터를 올바르게 닫을 수 있습니다.
- `LoadUiAsync` 가시성 상태 불일치를 수정했습니다. 이미 보이는 프레젠터에서 `openAfter=false`로 호출하면 GameObject를 비활성화하지만 `VisiblePresenters`를 업데이트하지 않아, 이후 `OpenUiAsync` 호출이 조용히 실패하는 문제를 해결했습니다.
- 프레젠터 내에서 `Close(destroy: true)`를 호출할 때 잘못된 인스턴스를 언로드할 가능성이 있던 멀티 인스턴스 모호성을 수정했습니다. 이제 올바르게 특정 인스턴스를 언로드합니다.
- `OnInitialized()`에서 비주얼 트리가 아직 패널에 연결되지 않아 요소 쿼리가 실패하던 UI Toolkit 타이밍 문제를 수정했습니다.

## [1.0.0] - 2025-11-04

**신규**:
- 성능 추적을 위한 `IUiAnalytics` 인터페이스와 `UiAnalytics` 구현을 추가했습니다.
- 에디터 윈도우를 추가했습니다: `UiAnalyticsWindow`, `UiServiceHierarchyWindow`
- `UiConfigsEditor` 인스펙터에 새로운 "UI 레이어 계층 구조 시각화" 섹션을 추가했습니다.
- Scene 뷰에서 시각적 디버깅을 위한 `UiPresenterSceneGizmos`를 추가했습니다.
- 빠른 열기/닫기 버튼이 있는 `UiPresenterEditor` 커스텀 인스펙터를 추가했습니다.
- `UiInstanceId` 구조체와 인스턴스 주소를 통한 UI 프레젠터 멀티 인스턴스 지원을 추가했습니다.
- 프레젠터 메타데이터(타입, 주소, 프레젠터 참조)를 캡슐화하는 `UiInstance` 구조체를 추가했습니다.
- `IPresenterFeature` 인터페이스를 사용한 피처 기반 프레젠터 조합 아키텍처를 추가했습니다.
- 조합 가능한 프레젠터 피처를 위한 `PresenterFeatureBase` 기본 클래스를 추가했습니다.
- UI 작업 지연을 위한 `AnimationDelayFeature`와 `TimeDelayFeature` 컴포넌트를 추가했습니다.
- UI Toolkit 통합을 위한 `UiToolkitPresenterFeature`를 추가했습니다.
- 커스텀 구현이 필요 없는 기본 UI 설정을 위한 `DefaultUiConfigsEditor`를 추가했습니다.

**변경**:
- 더 나은 성능과 WebGL 호환성을 위해 전체적으로 `Task.Delay`를 `UniTask.Delay`로 교체했습니다.
- 반복 중 컬렉션 수정을 방지하도록 `CloseAllUi`를 업데이트했습니다.
- 모든 프레젠터, 레이어, 에셋 로더의 적절한 정리로 `UiService.Dispose()`를 강화했습니다.
- `LoadUiAsync`, `OpenUiAsync` 메서드가 이제 선택적 `CancellationToken` 매개변수를 받습니다.
- 프로젝트의 완전한 정보로 README를 업데이트했습니다.
- 더 나은 캡슐화를 위해 `LoadedPresenters` 프로퍼티를 `GetLoadedPresenters()` 메서드로 교체했습니다.
- 모든 딜레이 기능을 `PresenterDelayerBase`에서 피처 기반 시스템(`AnimationDelayFeature`, `TimeDelayFeature`)으로 마이그레이션했습니다.
- 더 나은 성능과 최신 UI를 위해 모든 에디터 스크립트를 UI Toolkit을 사용하도록 변환했습니다.
- 향상된 시각과 드래그 앤 드롭 지원으로 UI Toolkit을 사용하도록 `UiConfigsEditor`를 리팩토링했습니다.
- `UiService`에서 더 나은 성능을 위해 컬렉션 타입(`Dictionary`, `List`)을 최적화했습니다.
- `UiService`에서 로딩 스피너를 제거했습니다 (초기화 간소화).

**수정**:
- **치명적**: 새 UI 로딩 시 `GetOrLoadUiAsync`가 null을 반환하는 문제를 수정했습니다 (이제 반환 값을 올바르게 할당).
- 적절한 `TryGetValue` 체크로 `UnloadUi`의 예외 처리를 수정했습니다.
- 적절한 `TryGetValue` 체크로 `RemoveUiSet`의 예외 처리를 수정했습니다.
- `CloseAllUi` 로직의 중복 작업을 수정했습니다.
- 에디터에서 UI 세트의 초기 값 처리를 수정했습니다.
- 에디터에서 프로퍼티 바인딩 전 직렬화 업데이트를 수정했습니다.
- 딜레이 프레젠터 구현의 스크립트 들여쓰기 문제를 수정했습니다.

## [0.13.1] - 2025-09-28

**신규**:
- UI Toolkit 기반 UI가 *UiPresenter<Data>*와 유사하게 작동할 수 있도록 *UiToolkitPresenter<Data>* 스크립트를 추가했습니다.

**변경**:
- *UiToolkitPresenter*를 리팩토링하여 구현 클래스에 루트 비주얼 요소를 전달하고 OnValidate에서 요소를 올바르게 할당하도록 했습니다.

## [0.13.0] - 2025-09-25

**신규**:
- UI Toolkit 기반 UI가 라이브러리와 함께 작동할 수 있도록 *UiToolkitPresenter* 스크립트를 추가했습니다.

**변경**:
- 프로젝트 구조를 반영하도록 *README*를 업데이트했습니다.
- UI Toolkit 기반 뷰를 처리하도록 에디터 도구와 *UiService*를 조정했습니다.

## [0.12.0] - 2025-01-08

**신규**:
- 텍스트 코드 실행을 링크할 수 있는 *InteractableTextView* 스크립트를 추가했습니다. 예: 브라우저에서 URL 열기

**변경**:
- 아키텍처 규칙에 맞게 *AdjustScreenSizeFitter*를 *AdjustScreenSizeFitterView*로 이름을 변경하여 뷰로 표시했습니다.
- 코드베이스를 적절히 구성하기 위해 *AdjustScreenSizeFitterView*, *NonDrawingView*, *SafeAreaHelperView*를 Views 폴더와 네임스페이스로 이동했습니다.

## [0.11.0] - 2025-01-05

**신규**:
- 앱 해상도 변경 시 트리거되는 새 정적 이벤트 호출과 화면 방향 변경 시 트리거되는 또 다른 이벤트를 UiService에 추가했습니다.
- Unity의 루프 또는 일반 *GameObjects* 의존 코드(예: 화면 해상도 변경 트리거)를 지원하기 위해 내부 목적으로 새 *UiServiceMonoComponent*를 프로젝트에 추가했습니다.
- Unity의 *ContentSizeFitter*의 UI 동작을 확장하여 *LayoutElement*가 Unity 인스펙터에 정의된 *LayoutElement.minSize*와 *LayoutElement.flexibleSize* 사이에서 맞추도록 하는 새 *AdjustScreenSizeFitter*를 추가했습니다.

**변경**:
- 덜 장황하게 하기 위해 *UiPresenterData<T>*를 *UiPresenter<T>*로 이름을 변경했습니다.

## [0.10.0] - 2024-11-13

**신규**:
- WebGL 플랫폼 지원을 위해 *UniTask* 의존성을 추가했습니다.

**변경**:
- 더 나은 성능과 WebGL 호환성을 위해 *IUiService* 비동기 메서드를 *Task* 대신 *UniTask*를 사용하도록 업데이트했습니다.

## [0.9.1] - 2024-11-04

**수정**:
- *GameObject*에 *CanvasRenderer*가 없을 때 *NonDrawingView*가 크래시되는 문제를 수정했습니다.

## [0.9.0] - 2024-11-01

**신규**:
- *IUiService*에 *GetUi<T>* 메서드를 추가했습니다. 제네릭 T를 직접 사용하여 *UiPresenter*를 요청합니다.
- *IUiService*에 *IsVisible<T>* 메서드를 추가했습니다. *UiPresenter*의 가시성 상태를 요청합니다.
- 외부 엔티티가 보이는 *UiPresenter* 목록에 접근할 수 있도록 *IUiService*에 IReadOnlyList 프로퍼티 *VisiblePresenters*를 추가했습니다.

**변경**:
- *GetAllVisibleUi()* 메서드를 제거했습니다. 대신 *IsVisible<T>* 메서드를 사용하세요.

## [0.8.0] - 2024-10-29

**신규**:
- 딜레이와 함께 열기/닫기하는 프레젠터를 지원하기 위해 새 *PresenterDelayerBase*, *AnimationDelayer*, *TimeDelayer*를 추가했습니다.
- *PresenterDelayerBase* 구현과 상호작용하여 딜레이와 함께 열기/닫기할 수 있는 새 *DelayUiPresenter*를 추가했습니다.
- *UiService*의 성능을 개선했습니다.

**변경**:
- *AnimatedUiPresenter*를 제거했습니다. 새 *DelayUiPresenter*와 *PresenterDelayerBase* 구현 중 하나를 사용하세요.
- *UiCloseActivePresenter*와 *UiCloseActivePresenterData*를 제거했습니다. 새 *DelayUiPresenter*와 *PresenterDelayerBase* 구현 중 하나를 사용하세요.
- *UiPresenter*에서 Canvas 의존성을 제거했습니다. 다양한 구조의 UI Unity 프로젝트 계층 구조가 *UiService*와 함께 작동할 수 있도록 합니다.
- *IUiService*에서 모든 Get 및 Has 메서드를 제거했습니다. 서비스에서 요청되는 모든 컬렉션에 대해 IReadOnlyDictionaries로 교체했습니다.
- 모든 OpenUi 메서드를 비동기로 변경했습니다. Ui를 열기 전에 항상 먼저 로드하는 예상 동작을 보장합니다.
- 모든 CloseUi 메서드를 동기로 변경했습니다. Ui 닫기는 이제 항상 원자적입니다. 닫기 딜레이를 얻으려면 *DelayUiPresenter*에서 직접 요청할 수 있습니다.
- *IUiAssetLoader*를 프리팹 인스턴스화를 단일 호출로 통합하도록 변경했습니다. 호출자가 동기/비동기 동작을 걱정할 필요가 없도록 메서드를 단순화합니다.
- *UiConfig*를 *UiPresenter*가 동기 또는 비동기로 로드되는지에 대한 정보를 포함하도록 변경했습니다.

## [0.7.2] - 2021-05-09

**수정**:
- 게임 오브젝트가 비활성화된 후에 *UiPresenter* 닫기가 호출되는 문제를 수정했습니다.

## [0.7.1] - 2021-05-03

**신규**:
- *SafeAreaHelpersView*가 안전 영역 밖에 배치되지 않은 경우 뷰를 동일한 위치에 유지할 수 있는 기능을 추가했습니다.

**수정**:
- 동일한 *UiPresenter*를 동시에 여러 번 로딩할 때 (하나가 완료되기 전에) 발생하는 중복 메모리 문제를 수정했습니다.

## [0.7.0] - 2021-03-12

**신규**:
- 추가 드로우 콜을 추가하지 않으면서 렌더러 없는 이미지를 가지기 위해 *NonDrawingView*를 추가했습니다.
- *RectTransform*이 화면 노치에 맞게 자체 조정할 수 있도록 *SafeAreaHelperView*를 추가했습니다.
- 진입 또는 닫기 시 애니메이션을 재생하는 *AnimatedUiPresenter*를 추가했습니다.
- *UiService*에 외부에서 *Layers*를 추가할 수 있는 기능을 추가했습니다.

**변경**:
- *Canvas*가 이제 *UiService* 외부에서 제어할 수 있는 단일 *GameObjects*가 되었습니다.

**수정**:
- *UiPresenterData*에서 데이터 설정 시 호출되지 않는 문제를 수정했습니다.

## [0.6.1] - 2020-09-24

**수정**:
- 의존성 패키지를 업데이트했습니다.

## [0.6.0] - 2020-09-24

**신규**:
- *IUiService*에 이미 열린/닫힌 *UiPresenters*를 열기/닫기를 허용하고, 그렇지 않으면 예외를 발생시키는 기능을 추가했습니다.
- UiPresenter의 현재 시각적 상태의 visible 프로퍼티를 추가했습니다. *UiService* 초기화를 위한 새 계약 인터페이스인 *IUiServiceInit*를 추가했습니다.

**수정**:
- 에셋 번들과 함께 *UiPresenter*를 올바르게 언로딩하지 못하는 문제를 수정했습니다.
- *UiPresenter*에 캔버스가 연결되어 있지 않을 때 발생하는 문제를 수정했습니다.
- 로딩 후 *UiPresenter*를 열려고 할 때 크래시되는 문제를 수정했습니다.

## [0.5.0] - 2020-07-13

**신규**:
- Ui 에셋을 메모리에 로드하기 위한 *UiAssetLoader*를 추가했습니다.

**변경**:
- *UiService*에서 *com.gamelovers.assetLoader* 의존성을 제거했습니다.

## [0.4.0] - 2020-07-13

**변경**:
- *UiService*에서 *com.unity.addressables* 의존성을 제거했습니다.
- *UiService*를 테스트 가능하고 다른 시스템에 주입 가능하도록 수정했습니다.

## [0.3.2] - 2020-04-18

**변경**:
- 코드 가독성을 향상시키기 위해 *IUiService* 인터페이스를 별도의 파일로 이동했습니다.

## [0.3.1] - 2020-02-15

**변경**:
- 의존성 패키지를 업데이트했습니다.

## [0.3.0] - 2020-02-11

**신규**:
- *UiPresenter*가 기본 데이터 값으로 초기화되어야 하는 경우를 위한 새 *UiPresenterData* 클래스를 추가했습니다.
- *UiPresenter*가 초기화된 후 호출되는 새 *OnInitialize* 메서드를 추가했습니다.

## [0.2.1] - 2020-02-09

**신규**:
- *UiService*에 추가하거나 로딩한 후 UI를 여는 기능을 추가했습니다.
- 주어진 레이어에 기반한 캔버스 참조 객체를 가져오는 기능을 추가했습니다.
- 참조만 전달하여 *UiPresenter*를 제거하고 언로드하는 기능을 추가했습니다.

**수정**:
- *UiService*가 *UiPresenter*를 올바르게 언로드하지 못하는 버그를 수정했습니다.

## [0.2.0] - 2020-01-19

**신규**:
- *UiConfigs.asset* 파일의 간편 선택 기능을 추가했습니다. *Tools > Select UiConfigs.asset*으로 이동하면 됩니다. *UiConfigs.asset*이 존재하지 않으면 Assets 폴더에 새로 생성됩니다.
- *UiService*를 호출하지 않고 *UiPresenter* 객체 파일에서 직접 *UiPresenter*를 닫을 수 있는 protected *Close()* 메서드를 추가했습니다. 또한 이제 *CloseUi<T>(T presenter)*를 호출하여 타입 참조 없이 객체를 직접 참조하여 서비스에서 Ui를 닫을 수 있습니다.
- *UnloadUi*와 *UnloadUiSet*이 이제 메모리에서 UI를 올바르게 언로드하고 서비스에서 제거합니다.
- 메모리에서 언로드하지 않고 서비스에서 UI를 제거할 수 있도록 *RemoveUi*와 *RemoveUiPresentersFromSet*을 추가했습니다.
- 문서를 개선했습니다.

**변경**:
- *UiPresenter*의 Refresh 메서드가 이제 public이며 접근 가능한 모든 객체에서 호출할 수 있습니다. *UiService*는 닫지 않고 동일한 *UiPresenter*를 두 번 열려고 할 때 더 이상 이 메서드를 호출하지 않습니다.
- *UiService.IsUiLoaded*가 *HasUiPresenter*로 변경되었습니다.
- *UiService.IsAllUiLoadedInSet*이 *HasAllUiPresentersInSet*으로 변경되었습니다.
- *AddUi* 메서드를 통합했습니다.

**수정**:
- *UiConfigs* 상태가 올바르게 저장되지 않는 경우가 있는 버그를 수정했습니다.

## [0.1.3] - 2020-01-09

**수정**:
- *UiConfigs* 상태가 올바르게 저장되지 않는 경우가 있는 버그를 수정했습니다.

## [0.1.2] - 2020-01-09

**수정**:
- 컴파일러 에러를 수정했습니다.

## [0.1.1] - 2020-01-09

**수정**:
- 로딩 시 *UiPresenter*의 상태를 수정했습니다. 이제 *UiPresenters*는 로딩 시 항상 비활성화됩니다.

## [0.1.0] - 2020-01-05

- 패키지 배포를 위한 초기 제출
