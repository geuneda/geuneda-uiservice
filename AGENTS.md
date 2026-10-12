# Geuneda.UiService - AI 에이전트 가이드

이 파일은 패키지 자체를 수정하는 기여자와 에이전트를 위한 문서입니다. 사용자 문서는 [README.md](README.md)와 [docs/](docs/README.md)가 기준입니다.

## 1. 패키지 개요

- 패키지: `com.geuneda.uiservice` (버전은 `package.json` 기준)
- Unity: 6000.0+ (`UiToolkitPanelRendererFeature` 는 6000.5+)
- 의존성(`package.json`): `com.unity.addressables` 2.6.0, `com.cysharp.unitask` 2.5.10
- 런타임 asmdef 는 TextMeshPro 와 uGUI 도 참조합니다(`Views/`). 이 둘은 `package.json` 에 선언되어 있지 않습니다.
- 어셈블리: `Runtime/Geuneda.UiService.asmdef`, `Editor/Geuneda.UiService.Editor.asmdef`, `Tests/EditMode/Geuneda.UiService.Tests.asmdef`, `Tests/PlayMode/Geuneda.UiService.Tests.PlayMode.asmdef`
- 네임스페이스: 런타임 `Geuneda.UiService`, 헬퍼 뷰 `Geuneda.UiService.Views`, 에디터 `GeunedaEditor.UiService`
- `Runtime/AssemblyInfo.cs` 가 internal 멤버를 `Geuneda.UiService.Editor` 와 `Geuneda.UiService.Tests.PlayMode` 에 노출합니다.

## 2. 런타임 구조

- `Runtime/UiService.cs` (`UiService : IUiServiceInit`)
  - 설정(`Dictionary<Type, UiConfig>`, 타입당 하나), 표시 목록(`List<UiInstanceId>`), 세트, 로드된 인스턴스(`Dictionary<Type, IList<UiInstance>>`)를 소유합니다.
  - `Init()` 이 `"Ui"` 루트 GameObject 를 만들고 `DontDestroyOnLoad` 와 `UiServiceMonoComponent` 를 붙입니다. 프레젠터는 레이어 컨테이너 없이 이 루트 바로 아래에 인스턴스화됩니다.
  - `internal static UiService CurrentService` 는 가장 최근에 생성된 서비스입니다. 에디터 창(`UiPresenterManagerWindow`)이 사용합니다.
- `Runtime/IUiService.cs`: `IUiService`, `IUiServiceInit`, `UiInstance`
  - 주소를 받는 오버로드 중 `CloseUi(Type, string, bool)`, `UnloadUi(Type, string)` 은 인터페이스에 있고, `GetUi<T>(string)`, `IsVisible<T>(string)`, `AddUi<T>(T, int, string, bool)`, `RemoveUi(Type, string)`, `LoadUiAsync(Type, string, ...)`, `OpenUiAsync(Type, string, ...)`, `OpenUiAsync<TData>(Type, string, TData, ...)` 은 `UiService` 에만 있습니다.
- `Runtime/UiConfigs.cs`: `UiConfig`, `abstract UiConfigs`. 파생 타입은 `AddressablesUiConfigs`, `ResourcesUiConfigs`, `PrefabRegistryUiConfigs`(각각 `CreateAssetMenu` 보유)
- `Runtime/UiSetConfig.cs`: `UiSetConfig`(런타임), `UiSetEntry`/`UiSetConfigSerializable`(직렬화)
- `Runtime/UiPresenter.cs`: `UiPresenter`, `UiPresenter<T>`, `IUiPresenterData`
  - 가시성(`SetActive`)은 `UiPresenter` 만 바꿉니다. 피처는 `ITransitionFeature` 태스크로 타이밍만 제공합니다.
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
- 레이어: `AddUi` 시 프레젠터 루트의 `Canvas.sortingOrder` 또는 `UIDocument.sortingOrder` 를 설정합니다. `PanelRenderer` 는 처리하지 않습니다.
- `LoadSynchronously`: `UiConfig` 에는 있지만 `UiConfigSerializable` 이 저장하지 않으므로 `UiConfigs.Configs` 에서 읽으면 항상 `false` 입니다. `LoadSynchronouslyAttribute` 는 에디터 동기화 때만 읽힙니다.
- `UnloadUi(Type, string)` 와 `CloseAllUi(int)` 는 `_uiConfigs[type]` 인덱서를 쓰므로 설정 없이 `AddUi` 로만 넣은 타입에서 `KeyNotFoundException` 이 납니다.
- `LoadUiSetAsync` 는 세트가 없으면 빈 목록을 반환합니다(XML 주석의 예외 설명과 다름). `UnloadUiSet`, `CloseAllUiSet` 은 인덱서를 써서 예외가 납니다.
- 정적 이벤트 `UiService.OnResolutionChanged`/`OnOrientationChanged` 는 `UiServiceMonoComponent` 가 발생시키며, `Dispose()` 는 리스너를 지우지 않습니다.
- `Dispose()`: 표시 중인 UI 닫기, 모든 인스턴스 언로드 시도(실패는 경고), 컬렉션 정리, 루트 파괴. `CurrentService` 가 자신이면 `null` 로 되돌립니다.
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
- 공개 API, 생명주기, 설정 형식, 에디터 직렬화 방식을 바꾸면 `docs/`, 이 파일, 메타 레포(geuneda-frameworks)의 `claude-skills/geuneda-uiservice/` 스킬 문서를 함께 갱신합니다.
- 패키지에는 현재 `Samples~/` 가 없습니다.
