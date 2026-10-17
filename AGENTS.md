# Geuneda.UiService - AI 에이전트 가이드

이 파일은 패키지 자체를 수정하는 기여자와 에이전트를 위한 문서입니다. 사용자 문서는 [README.md](README.md)와 [docs/](docs/README.md)가 기준입니다.

## 1. 패키지 개요

- 패키지: `com.geuneda.uiservice` (버전은 `package.json` 기준)
- Unity: 6000.0+ (`UiToolkitPanelRendererFeature` 는 6000.5+)
- 의존성(`package.json`): `com.geuneda.core` 0.4.0(로거 `ModuleLogger`, 클래스마다 `Geuneda.UiService.*` 모듈), `com.unity.addressables` 2.6.0, `com.cysharp.unitask` 2.5.10, `com.unity.ugui` 2.0.0
- 선택 의존성: `com.unity.inputsystem`. 런타임 asmdef 가 `Unity.InputSystem` 을 참조하고 `versionDefines` 로 `GEUNEDA_INPUT_SYSTEM` 을 정의합니다. 패키지가 없으면 참조는 무시되고 레거시 입력 분기가 컴파일됩니다(2.0.0, Input System 없는 빈 프로젝트에서 확인).
- 런타임 asmdef 는 TextMeshPro 와 uGUI 도 사용합니다(`Views/`). Unity 6 에서는 둘 다 `com.unity.ugui` 에 포함됩니다.
- 어셈블리: `Runtime/Geuneda.UiService.asmdef`, `Editor/Geuneda.UiService.Editor.asmdef`, `Tests/EditMode/Geuneda.UiService.Tests.asmdef`, `Tests/PlayMode/Geuneda.UiService.Tests.PlayMode.asmdef`, `Tests/PlayMode/InputSystem/Geuneda.UiService.Tests.PlayMode.InputSystem.asmdef`(`GEUNEDA_INPUT_SYSTEM` 일 때만, `InputTestFixture` 사용)
- 네임스페이스: 런타임 `Geuneda.UiService`, 헬퍼 뷰 `Geuneda.UiService.Views`, 에디터 `GeunedaEditor.UiService`
- `Runtime/AssemblyInfo.cs` 가 internal 멤버를 `Geuneda.UiService.Editor` 와 `Geuneda.UiService.Tests.PlayMode` 에 노출합니다.

## 2. 런타임 구조

- `Runtime/UiService.cs`, `Runtime/UiService.Visibility.cs`, `Runtime/UiService.Navigation.cs` (`partial class UiService : IUiServiceInit`)
  - `UiService.cs` 는 로드·열기·닫기·세트, `UiService.Visibility.cs` 는 표시 목록 변경과 알림(디스패처, 제거, `CloseAllUi` 스냅샷, UI Toolkit 정렬, dim 갱신, 이벤트 발생), `UiService.Navigation.cs` 는 이벤트 선언, `TryGetUi`, 뒤로가기(`HandleBack`, `AddBackHandler`, `BackUnhandled`), 입력 차단(`BlockInput`), 모달 dim 설정(`DimColor`), `ReadsBackInput` 을 담당합니다.
  - 표시 목록을 바꾸는 경로는 모두 `OpenVisible`, `TryCloseVisible`, `RemoveRegistration` 을 거칩니다. '준비'(목록 변경, `presenter.PrepareOpen/PrepareClose` 로 세대 증가와 전환 태스크 생성)는 즉시 하고, '알림과 실행'(`ApplyToolkitOrder`, `RefreshModalDim`, 이벤트, `RunOpen/RunClose` 사용자 훅)은 `Publish` 의 큐에서 한 번에 하나씩 처리합니다. 알림이나 훅 도중에 생긴 변경은 준비만 하고 앞의 것이 끝난 뒤 처리되므로, 모든 구독자가 같은 순서를 받고 열기·닫기 훅(동기 구간)이 다른 훅 도중에 끼어들지 않습니다(전환 완료 훅은 이 순서 밖). 열기 실행 전에는 프레젠터가 아직 같은 객체로 등록돼 있는지 확인합니다. 정렬·dim 갱신 예외는 기록하고 계속합니다. `Dispose` 중에는 알림을 하지 않고, 처리 중 Dispose 하면 `RunPendingChangesForDispose` 로 알림 중이라 아직 실행하지 않은 변경(`_notifying`)과 대기 중인 닫기를 언로드 전에 실행합니다.
  - `RemoveRegistration` 은 빠진 프레젠터의 `DetachLifecycle()`(세대 증가, 그 시점 `_inFlight` 묶음을 떼어 반환)을 알림보다 먼저 부르고, `RemoveUi`/`UnloadUi` 는 그 묶음만 `CompleteDetached` 로 완료합니다(언로드는 파괴 뒤, 로더가 던져도 `finally` 로 완료). 제거 알림에서 다시 등록해 준비한 새 단계는 묶음에 없으므로 완료되지 않습니다.
  - `CloseAllUi` 는 (ID, 프레젠터 객체, 세대) 스냅샷을 돌며 같은 객체이고 세대가 같은 것만 닫습니다.
  - `ApplyToolkitOrder` 는 표시 중이거나 아직 켜진(닫기 전환 중) UIDocument 프레젠터를 (레이어, 연 순번 `OpenSequence`) 순으로 정렬해 `sortingOrder` 를 `레이어 + 순위 × UiModalDim.ToolkitOrderStep(1/1024)` 로 매깁니다. uGUI 는 같은 `sortingOrder` 캔버스의 앞뒤를 정할 수 없어(형제 순서·재활성화로 바뀌지 않음, 실험) 모달이 다른 uGUI 와 레이어를 같이 쓰면 타입마다 한 번 경고합니다.
  - 입력 차단: 첫 토큰과 마지막 해제 때 `OnInputBlockChanged` 가 `UiInputBlockWatcher`(UI 루트, 막힌 동안만 켜짐)를 켜고 끕니다. `ReapplyInputBlock` 은 `EventSystem.current` 와 켜진 UIDocument 프레젠터 패널에 `UiFocusBlocker` 를 적용하고, 감시자가 매 프레임, 열기 실행 직후에도 다시 부릅니다(이미 막은 대상은 건너뜀). 풀 때 내비게이션만 되돌리고 선택은 복구하지 않습니다(복구 콜백의 재차단과 엇갈림).
  - 설정(`Dictionary<Type, UiConfig>`, 타입당 하나), 표시 목록(`List<UiInstanceId>`), 세트, 로드된 인스턴스(`Dictionary<Type, IList<UiInstance>>`)를 소유합니다.
  - `Init()` 이 `"Ui"` 루트 GameObject 를 만들고 `DontDestroyOnLoad` 와 `UiServiceMonoComponent` 를 붙입니다. 프레젠터는 레이어 컨테이너 없이 이 루트 바로 아래에 인스턴스화됩니다.
  - `internal static UiService CurrentService` 는 가장 최근에 생성된 서비스입니다. 에디터 창(`UiPresenterManagerWindow`)이 사용합니다.
- `Runtime/IUiService.cs`: `IUiService`, `IUiServiceInit`, `UiInstance`
  - 주소를 받는 오버로드 중 `CloseUi(Type, string, bool)`, `UnloadUi(Type, string)` 은 인터페이스에 있고, `GetUi<T>(string)`, `IsVisible<T>(string)`, `AddUi<T>(T, int, string, bool)`, `RemoveUi(Type, string)`, `LoadUiAsync(Type, string, ...)`, `OpenUiAsync(Type, string, ...)`, `OpenUiAsync<TData>(Type, string, TData, ...)` 은 `UiService` 에만 있습니다.
- `Runtime/UiConfigs.cs`: `UiConfig`, `abstract UiConfigs`. 파생 타입은 `AddressablesUiConfigs`, `ResourcesUiConfigs`, `PrefabRegistryUiConfigs`(각각 `CreateAssetMenu` 보유)
- `Runtime/UiSetConfig.cs`: `UiSetConfig`(런타임), `UiSetEntry`/`UiSetConfigSerializable`(직렬화)
- `Runtime/UiPresenter.cs`: `UiPresenter`, `UiPresenter<T>`, `IUiPresenterData`
  - 가시성(`SetActive`)은 `UiPresenter` 만 바꿉니다. 피처는 `ITransitionFeature` 태스크로 타이밍만 제공합니다.
  - 열기·닫기 준비마다 `_lifecycleVersion` 이 오르고 `LifecycleStep`(세대, 완료 소스)을 돌려주며 완료 소스를 `_inFlight` 에 넣습니다. 과정은 `Complete(step)` 으로 끝냅니다. 훅 예외는 과정 전체의 catch 에서 `Fail(step, e)` 로 처리합니다: 태스크를 예외로 실패시킨 뒤 그 자리에서 한 번 `GetResult` 로 관찰하고 `Geuneda.UiService.Presenter` 로거로 한 번 기록합니다(Error 줄 + 예외)(관찰하지 않으면 UniTask `ExceptionHolder` 파이널라이저가 GC 시점에 다시 기록, 테스트에서 다른 픽스처로 새어 나와 확인). 닫기 실패는 먼저 그 단계를 `_inFlight` 에서 빼고(언로드가 성공으로 완료하지 않게) 숨김·요청된 언로드를 한 뒤 `finally` 에서 `Fail` 합니다(정리 예외는 Forget 이 기록). 피처 전환은 단계마다 고정합니다: 전환을 시작하는 알림(열기는 `OnPresenterOpened`, 닫기는 `OnPresenterClosing`)을 과정 안에서 직접 돌며 알린 수(`notified`)를 세고, 단계 시작 때 `SnapshotTransitions` 로 전환 피처들의 태스크 값을 기억하고(상태는 읽지 않음), 알린 피처 중 그 값에서 바뀐 태스크만 `CaptureTransitions` 로 배열에 담습니다(UniTask 값 비교). 바뀌지 않은 태스크는 이전 단계 것이라(이미 관찰했거나 소비돼 상태를 읽을 수 없을 수 있음) 읽지 않습니다. 콜백 안에서 던졌으면 던진 피처도 같은 기준으로 담습니다. 새 태스크의 상태 읽기가 던지면(예: `UniTask.Defer` 팩토리) 그 태스크는 담지 않고 나머지 수집을 계속한 뒤, 담은 전환은 관찰로 넘기고 첫 오류로 단계를 실패시킵니다(다른 예외로 끝나는 경로의 상태 읽기 오류는 따로 기록). catch 안의 관찰이 실패해도 정리와 `Fail` 은 반드시 실행됩니다. 정상 대기(`UiTransitionTasks.WaitForTransitionsAsync`, 모두 기다리고 첫 예외를 던지며 나머지는 기록), 일찍 끝내기(`ObserveTransitions`, Forget 으로 관찰 넘김), 닫기 시작 훅 실패(`HasFaulted`/`ThrowFirstFault`, `OnClosed` 전에 단계 실패), 예외 종료가 모두 이 배열과 지역 `observed` 하나로 관찰을 한 번만 합니다. 이전 단계의 태스크(이미 관찰한 실패 포함)는 다시 읽지 않습니다. 기본 피처는 전환을 시작하지 않을 때 이전 완료 소스를 비웁니다. 기본 전환 피처는 시작·완료 훅 예외를 자기 완료 소스로 넘겨 프레젠터의 `Fail` 로 이어지게 합니다. 각 사용자 훅·피처 콜백·활성 상태 변경 뒤와 전환 대기 뒤에 `IsCurrent`(살아 있고 세대 동일)를 확인해, 바뀌었으면 이후 단계를 건너뛰고 이번 태스크만 완료합니다. 피처 순회 중 바뀌면 남은 피처에도 알리지 않습니다. `destroy` 닫기는 숨김과 완료 훅 뒤 세대를 다시 확인하고 언로드한 다음 완료 소스를 완료합니다(완료 대기자가 같은 인스턴스를 다시 열 수 있음). `TimeDelayFeature`, `AnimationDelayFeature` 도 전환마다 완료 소스를 지역 변수로 잡고, 지연·클립 유무와 관계없이 열기·닫기 알림마다 올리는 `_transitionVersion` 으로 늦은 완료 훅을 건너뜁니다. 새 전환 피처도 같은 규칙을 따라야 합니다.
  - 뒤로가기·모달 훅(`HandlesBack`, `OnBackPressed`, `IsModal`, `OnDimClicked`)은 `protected virtual` 이고 서비스는 `Internal*` 래퍼로 부릅니다. `protected internal` 로 바꾸면 InternalsVisibleTo 를 받은 테스트 어셈블리에서 재정의 규칙이 달라져(CS0507) 사용자와 테스트가 다른 접근자를 써야 합니다.
- `Runtime/Navigation/`: `UiInputBlock`(public 토큰), `UiInputBlocker`(토큰 수, uGUI 차단 캔버스, UI Toolkit 차단 패널), `UiFocusBlocker`(선택·포커스 입력 차단), `UiInputBlockWatcher`(막힌 동안 매 프레임 재적용), `UiModalDim`(공용 dim 배치), `UiDimClickView`, `UiBackInputReader`, `UiStyleUtils`. `UiInputBlock` 외에는 internal 입니다.
- `Runtime/Queue/`: `IUiPopupQueue`, `UiPopupQueue`, `UiPopupQueueHold`. 큐는 `IUiService` 만 쓰고(`PresenterClosed`, `VisiblePresenters`, `TryGetUi`), 같은 어셈블리라 `UiPresenter.InternalHandlesBack`, `InstanceAddress` 를 읽습니다. 진행은 `Advance` 의 반복 루프(동기 건너뛰기에 재귀 없음)이고, 꺼낸 팝업은 `WaitUntilClosedAsync` 가 실제로 닫힐 때까지(닫기 중 재열기는 계속 추적, 새 닫기는 다시 대기) 지켜봅니다. 여러 큐가 동시에 로드 중인 팩토리끼리는 서로 기다리지 않습니다.
- `Runtime/Features/`: `PresenterFeatureBase`, `ITransitionFeature`, `TimeDelayFeature`, `AnimationDelayFeature`, `UiToolkitPresenterFeature`, `UiToolkitPanelRendererFeature`(`#if UNITY_6000_5_OR_NEWER`)
- `Runtime/Loaders/`: `IUiAssetLoader`, `AddressablesUiAssetLoader`(기본), `ResourcesUiAssetLoader`, `PrefabRegistryUiAssetLoader`
  - 로더는 `UiService` 생성자로만 정해집니다. 설정 에셋 타입이 로더를 고르지 않습니다.
- `Runtime/Views/`: `SafeAreaHelperView`(같은 파일에 `#if UNITY_EDITOR` 인스펙터 포함), `NonDrawingView`, `AdjustScreenSizeFitterView`, `InteractableTextView`

## 3. 에디터 구조

- `UiConfigsEditorBase<TSet>`: 레이어 시각화, 설정 목록, 세트 편집 공통 로직. `OnEnable` 에서 `SyncConfigs()` 로 설정 목록을 다시 만들고 `SetSetsSize(enum 길이)` 를 호출합니다.
- `AddressablesUiConfigsEditor<TSet>`, `ResourcesUiConfigsEditor<TSet>`, `PrefabRegistryUiConfigsEditor<TSet>`: 각 소스와 동기화
- `DefaultUiConfigsEditor.cs`: `DefaultUiSetId` enum 과 `DefaultAddressablesUiConfigsEditor`, `DefaultResourcesUiConfigsEditor`, `DefaultPrefabRegistryUiConfigsEditor`
- `UiConfigsMenuItems`: `Tools/UI Service/Select UiConfigs`, `Tools/UI Service/Layer Visualizer`
- `UiPresenterManagerWindow`: `Tools/UI Service/Presenter Manager`
- `UiPresenterEditor`: 플레이 모드 열기/닫기 버튼. `InternalOpen()`/`InternalClose()` 를 직접 호출하므로 서비스의 표시 목록을 거치지 않습니다.
- `NonDrawingViewEditor`

## 4. 동작 주의 사항

- 인스턴스 주소
  - `UiInstanceId` 는 `null`/빈 문자열을 `string.Empty` 로 정규화합니다.
  - 주소 없는 `LoadUiAsync`/`OpenUiAsync` 는 `UiConfig.Address` 를 인스턴스 주소로 씁니다(테스트 `PresenterInstanceAddress_DefaultIsConfigAddress`). `AddUi` 의 기본 주소는 `string.Empty` 입니다.
  - 에디터 세트 항목은 `InstanceAddress` 를 설정 주소로 채우므로 세트와 기본 인스턴스가 같은 인스턴스를 가리킵니다.
  - 주소 없는 Get/IsVisible/Close/Unload/Remove 는 내부 `ResolveInstanceAddress(type)` 을 씁니다. 인스턴스가 여러 개면 경고 후 첫 번째를 고릅니다.
- `UiPresenter.Close(destroy)` 는 자신의 `InstanceAddress` 로 `CloseUi` 를 호출하고, 닫기 전환이 끝난 뒤 `UnloadUi(GetType(), InstanceAddress)` 를 호출합니다.
- 레이어: `AddUi` 시 프레젠터 루트의 `Canvas.sortingOrder`, `UIDocument.sortingOrder`, `PanelRenderer.sortingOrder`(`#if UNITY_6000_5_OR_NEWER`, 1.3.2) 를 설정합니다. 설정 에디터는 `UiConfigsEditorBase.TryGetSortingOrder` 로 같은 세 컴포넌트에서 읽으며, 정렬 컴포넌트가 없을 때만 기존 레이어나 0 을 씁니다(음수 정렬값도 그대로 읽음).
- `LoadSynchronously`: `UiConfigSerializable` 에 저장됩니다(1.3.1). 필드 추가 전에 저장된 에셋은 `false` 로 읽히며, 설정 인스펙터를 열어 동기화하면 `LoadSynchronouslyAttribute` 값이 저장됩니다. `LoadSynchronouslyAttribute` 는 에디터 동기화 때만 읽힙니다.
- `CloseAllUi(int)` 는 설정이 있으면 `UiConfig.Layer` 를, 없으면 `AddUi` 에 넘긴 레이어(내부 `_uiLayers`)를 씁니다. `UnloadUi` 는 설정을 조회하지 않으므로 설정 없이 `AddUi` 로만 넣은 타입도 언로드할 수 있습니다.
- `LoadUiSetAsync` 는 세트가 없으면 빈 목록을 반환합니다. `UnloadUiSet`, `CloseAllUiSet` 은 인덱서를 써서 예외가 납니다.
- 정적 이벤트 `UiService.OnResolutionChanged`/`OnOrientationChanged` 는 `UiServiceMonoComponent` 가 발생시키며, `Dispose()` 는 리스너를 지우지 않습니다.
- `Dispose()`: 표시 중인 UI 닫기, 모든 인스턴스 언로드 시도(실패는 경고), 컬렉션 정리, 루트 파괴. `CurrentService` 가 자신이면 `null` 로 되돌립니다.
- 화면 맨 위: 표시 목록(연 순서)과 `GetSortingLayer`(AddUi 에 넘긴 레이어, 없으면 설정 레이어)로 고릅니다. 레이어가 높을수록, 같으면 목록 뒤쪽이 위입니다. 뒤로가기 대상과 dim 위치가 이 규칙을 씁니다.
- 런타임 UIDocument 생성: 켜진 GameObject 에 붙인 뒤 `panelSettings` 를 넣으면 플레이어 빌드에서 `rootVisualElement` 가 생기지 않습니다(Android 에서 확인). 끈 채로 붙이고 설정한 뒤 켭니다. 테마 없는 런타임 `PanelSettings` 는 경고를 남기므로 빈 `ThemeStyleSheet` 를 넣습니다. UIDocument 루트는 기본 높이가 0 이라 화면을 덮으려면 절대 위치로 채웁니다(`UiStyleUtils.FillParent`).
- 뒤로가기 키: Input System 분기는 `Keyboard.current` 를 읽지 않고 `<Keyboard>/escape` 액션을 씁니다. Android 뒤로가기가 다른 키보드 장치로 한 프레임 안에 눌렸다 떼어지면 `Keyboard.current.escapeKey.wasPressedThisFrame` 이 놓칩니다(에뮬레이터에서 재현, 회귀 테스트 `EscapeTappedWithinOneFrameOnAnotherKeyboard_CallsHandleBack`).
- UI Toolkit: 프레젠터가 비활성화/재활성화되면 `UIDocument` 가 요소를 다시 만듭니다. `UiToolkitPresenterFeature` 는 `OnPresenterInitialized`, `OnPresenterOpened`, `AttachToPanelEvent` 에서 리스너를 호출하므로 한 번 열 때 여러 번 호출될 수 있습니다.

## 5. 코딩 규칙

- C# 9.0, 전역 `using` 없음, 명시적 네임스페이스
- 런타임 코드는 `UnityEditor` 를 참조하지 않습니다. 불가피하면 `#if UNITY_EDITOR` 로 감쌉니다.
- 비동기는 UniTask 를 사용하고 가능한 경로로 `CancellationToken` 을 전달합니다.
- 프로퍼티 접근에서 할당을 만들지 않습니다(`VisiblePresenters`, `UiSets` 는 생성자에서 만든 읽기 전용 래퍼).

## 6. 외부 패키지 소스

API 확인이 필요하면 `Library/PackageCache/com.unity.addressables@*/`, `Library/PackageCache/com.cysharp.unitask@*/` 를 참고합니다.

## 7. 자주 하는 변경

- 프레젠터 추가: 루트에 `UiPresenter` 파생 컴포넌트와 `Canvas`/`UIDocument` 를 둔 프리팹을 만들고 Addressable 로 표시한 뒤 `UiConfigs` 인스펙터를 열어 동기화합니다(`Tools/UI Service/Select UiConfigs`).
- 세트 ID 변경: 0부터 연속인 enum 과 `[CustomEditor(typeof(AddressablesUiConfigs))] class X : AddressablesUiConfigsEditor<TEnum>` 을 만듭니다. 같은 타입에 패키지 기본 에디터도 등록되어 있으므로 적용 여부를 확인합니다.
- 피처 추가: `PresenterFeatureBase` 를 상속하고, 전환이 있으면 `ITransitionFeature` 도 구현합니다. 태스크는 `OnPresenterOpened`/`OnPresenterClosing` 에서 만들어야 프레젠터가 기다릴 수 있습니다.
- 로더 추가: `IUiAssetLoader` 를 구현합니다.
- 뒤로가기·모달 프레젠터: `HandlesBack`/`IsModal` 을 재정의하고 필요하면 `OnBackPressed`/`OnDimClicked` 를 재정의합니다. 테스트 헬퍼는 `Tests/PlayMode/Helpers/TestPopupPresenter*.cs`, UI Toolkit 모달은 `TestToolkitPopupPresenter.CreatePrefab(panelSettings)`(Canvas 없음)입니다.
- 공개 API, 생명주기, 설정 형식, 에디터 직렬화 방식을 바꾸면 `docs/`, 이 파일, 메타 레포(geuneda-frameworks)의 `claude-skills/geuneda-uiservice/` 스킬 문서를 함께 갱신합니다.
- 패키지에는 현재 `Samples~/` 가 없습니다.
