# GameLovers.UiService - AI 에이전트 가이드

## 1. 패키지 개요
- **패키지**: `com.gamelovers.uiservice`
- **Unity**: 6000.0+
- **의존성** (`package.json` 참조)
  - `com.unity.addressables` (2.6.0)
  - `com.cysharp.unitask` (2.5.10)

이 패키지는 프레젠터의 **로드/열기/닫기/언로드**를 조율하고, **레이어링**, **UI 세트**, **멀티 인스턴스** 프레젠터를 지원하며, **Addressables** + **UniTask**와 통합되는 중앙 집중식 UI 관리 서비스를 제공합니다.

사용자 대상 문서는 `docs/README.md` (및 연결된 페이지)를 기본 문서 세트로 취급하세요. 이 파일은 패키지 자체를 작업하는 기여자/에이전트를 위한 것입니다.

## 2. 런타임 아키텍처 (고수준)
- **서비스 핵심**: `Runtime/UiService.cs` (`UiService : IUiServiceInit`)
  - 설정, 로드된 프레젠터 인스턴스, 표시 목록, UI 세트 설정을 소유합니다.
  - `"Ui"`라는 이름의 `DontDestroyOnLoad` 부모 GameObject를 생성하고 해상도/방향 추적을 위해 `UiServiceMonoComponent`를 연결합니다.
  - 프레젠터를 **인스턴스**로 추적합니다: `Dictionary<Type, IList<UiInstance>>` 여기서 각 `UiInstance`는 `(Type, Address, UiPresenter)`를 저장합니다.
  - **에디터 지원**: `UiService.CurrentService`는 에디터 윈도우에서 플레이 모드의 활성 서비스에 접근하기 위해 사용하는 **내부** 정적 참조입니다.
- **공개 API 표면**: `Runtime/IUiService.cs`
  - 생명주기 작업(로드/열기/닫기/언로드)과 읽기 전용 뷰를 노출합니다:
    - `VisiblePresenters : IReadOnlyList<UiInstanceId>`
    - `UiSets : IReadOnlyDictionary<int, UiSetConfig>`
    - `GetLoadedPresenters() : List<UiInstance>`
  - 참고: **멀티 인스턴스 오버로드** (명시적 `instanceAddress`)는 `IUiService`가 아닌 `UiService` (구체 타입)에 존재합니다.
- **설정**: `Runtime/UiConfigs.cs` (`ScriptableObject`)
  - UI 설정을 `UiConfigs.UiConfigSerializable` (주소 + 레이어 + 타입 이름)로 저장하고 UI 세트를 `UiSetEntry` 항목을 포함하는 `UiSetConfigSerializable`로 저장합니다.
  - 특수 서브클래스를 사용하세요 (각각 `CreateAssetMenu`가 있습니다): `AddressablesUiConfigs` (기본), `ResourcesUiConfigs`, `PrefabRegistryUiConfigs` (내장 프리팹 레지스트리).
  - 참고: `UiConfigs`는 직접 사용을 방지하기 위해 `abstract`입니다 - 항상 특수 서브클래스 중 하나를 사용하세요.
- **UI 세트**: `Runtime/UiSetConfig.cs`
  - `UiSetEntry`는 다음을 저장합니다:
    - 프레젠터 타입을 `AssemblyQualifiedName` 문자열로
    - 선택적 `InstanceAddress` (빈 문자열은 기본 인스턴스를 의미)
  - `UiSetConfig`는 런타임 형태입니다: `SetId` + `UiInstanceId[]`.
- **프레젠터 패턴**: `Runtime/UiPresenter.cs`
  - 생명주기 훅: `OnInitialized`, `OnOpened`, `OnClosed`, `OnOpenTransitionCompleted`, `OnCloseTransitionCompleted`.
  - 타입이 지정된 프레젠터: `UiPresenter<T>`는 할당 시 `OnSetData()`를 트리거하는 `Data` 프로퍼티를 가집니다 (`OpenUiAsync(..., initialData, ...)` 또는 이후 업데이트 시 동작).
  - 프레젠터 피처는 초기화 시 `GetComponents(_features)`를 통해 검색되고 열기/닫기 생명주기에서 알림을 받습니다.
  - **전환 태스크**: `OpenTransitionTask`와 `CloseTransitionTask`는 모든 전환 피처가 완료될 때 완료되는 공개 `UniTask` 프로퍼티입니다.
  - **가시성 제어**: `UiPresenter`는 닫기 시 `SetActive(false)`의 단일 책임 지점입니다; 숨기기 전에 모든 `ITransitionFeature` 태스크를 대기합니다.
- **조합 가능한 피처**: `Runtime/Features/*`
  - `PresenterFeatureBase`를 사용하면 프레젠터 프리팹에 컴포넌트를 추가하여 생명주기에 연결할 수 있습니다.
  - `ITransitionFeature` 인터페이스: 열기/닫기 전환 지연을 제공하는 피처용 (프레젠터가 이를 대기).
  - 내장 전환 피처: `TimeDelayFeature`, `AnimationDelayFeature`.
  - UI Toolkit 지원: `UiToolkitPresenterFeature` (`UIDocument`를 통해)는 안전한 요소 쿼리를 위한 `AddVisualTreeAttachedListener(callback)`을 제공합니다. 콜백은 UI Toolkit이 프레젠터가 비활성화/재활성화될 때 요소를 재생성하기 때문에 매번 열기 시 호출됩니다.
- **헬퍼 뷰**: `Runtime/Views/*` (`GameLovers.UiService.Views`)
  - `SafeAreaHelperView`: 안전 영역(노치)에 따라 앵커/크기를 조정합니다.
  - `NonDrawingView`: 렌더링 없이 레이캐스트를 대상으로 합니다 (`Graphic` 확장).
  - `AdjustScreenSizeFitterView`: 최소/유연 크기 사이에서 클램핑하는 레이아웃 피터.
  - `InteractableTextView`: TMP 링크 클릭 처리.
- **에셋 로딩**: `Runtime/Loaders/IUiAssetLoader.cs`
  - 여러 구현이 있는 `IUiAssetLoader` 추상화: `Runtime/Loaders/`:
    - `AddressablesUiAssetLoader` (기본): `Addressables.InstantiateAsync`와 `Addressables.ReleaseInstance`를 사용.
    - `PrefabRegistryUiAssetLoader`: 직접 프리팹 참조를 사용 (샘플/테스트에 유용). 생성자에서 `PrefabRegistryUiConfigs`로 초기화 가능.
    - `ResourcesUiAssetLoader`: `Resources.Load`를 사용.
  - `UiConfig.LoadSynchronously`를 통한 선택적 동기 인스턴스화를 지원합니다 (Addressables 로더에서).

## 3. 주요 디렉토리 / 파일
- **문서 (사용자 대상)**: `docs/`
  - `docs/README.md` - 문서 진입점.
- **런타임**: `Runtime/`
  - 진입점: `IUiService.cs`, `UiService.cs`, `UiPresenter.cs`, `UiConfigs.cs`, `UiSetConfig.cs`, `UiInstanceId.cs`.
  - 통합/확장 지점 (동작이 예상과 다를 때 여기서 시작):
    - `Loaders/*` - **프레젠터 프리팹이 인스턴스화/해제되는 방식**.
      - UI 로드/언로드에 실패하면 `Loaders/IUiAssetLoader.cs`와 활성 로더(`AddressablesUiAssetLoader`, `ResourcesUiAssetLoader`, `PrefabRegistryUiAssetLoader`)에서 시작하세요.
      - 로더 선택은 일반적으로 사용하는 `UiConfigs` 서브클래스(`AddressablesUiConfigs` / `ResourcesUiConfigs` / `PrefabRegistryUiConfigs`)에 의해 결정됩니다.
    - `Features/*` - **프레젠터 조합** (프레젠터 프리팹에 부착된 컴포넌트).
      - 생명주기 훅은 `PresenterFeatureBase`에 있으며; 피처는 프레젠터 초기화 중에 검색됩니다.
      - 전환 타이밍 문제(UI가 예상대로 표시/숨겨지지 않음)는 보통 `ITransitionFeature` 구현(예: `TimeDelayFeature`, `AnimationDelayFeature`)과 관련됩니다.
      - UI Toolkit 프레젠터는 `UiToolkitPresenterFeature`에 의존합니다; `OnInitialized()` 중에 `UIDocument.rootVisualElement`를 쿼리하지 마세요 - `AddVisualTreeAttachedListener(...)`를 사용하세요.
    - `Views/*` - **프레젠터 프리팹이 사용하는 선택적 헬퍼 컴포넌트** (안전 영역, 레이캐스트, 레이아웃 피터, TMP 링크 클릭).
      - 상호작용/레이아웃이 올바르지 않지만 서비스 부기가 정확해 보이면 `UiService`를 변경하기 전에 여기를 확인하세요.
- **에디터**: `Editor/` (어셈블리: `Editor/GameLovers.UiService.Editor.asmdef`)
  - 설정 에디터: `UiConfigsEditorBase.cs`, `*UiConfigsEditor.cs`, `DefaultUiConfigsEditor.cs`.
  - 디버깅: `UiPresenterManagerWindow.cs`, `UiPresenterEditor.cs`.
- **샘플**: `Samples~/`
  - 기본 흐름, 데이터 프레젠터, 딜레이 피처, UI Toolkit 통합을 시연합니다.
- **테스트**: `Tests/`
  - `Tests/EditMode/*` - 단위 테스트 (설정, 세트, 로더, 핵심 서비스 동작)
  - `Tests/PlayMode/*` - 통합/성능/스모크 테스트

## 4. 중요 동작 / 주의사항
- **인스턴스 주소 정규화**
  - `UiInstanceId`는 `null/""`을 `string.Empty`로 정규화합니다.
  - 기본/싱글톤 인스턴스 식별자로 **`string.Empty`**를 선호하세요.
- **모호한 "기본 인스턴스" 호출**
  - `UiService`는 명시적 `instanceAddress` 없이 API가 호출될 때 내부 `ResolveInstanceAddress(type)`을 사용합니다.
  - **여러 인스턴스**가 존재하면 경고를 기록하고 **첫 번째** 인스턴스를 선택합니다. 멀티 인스턴스 사용 시 `instanceAddress`를 포함하는 `UiService` 오버로드를 호출하는 것을 선호하세요.
- **프레젠터 자체 닫기 + 멀티 인스턴스에서의 파괴**
  - `UiPresenter.Close(destroy: true)`는 이제 올바른 인스턴스를 언로드하기 위해 프레젠터에 저장된 `InstanceAddress`를 정확하게 사용합니다.
  - 싱글톤과 멀티 인스턴스 프레젠터 모두에서 원활하게 작동합니다.
- **레이어링**
  - `UiService`는 추가/로딩 시 `Canvas.sortingOrder` 또는 `UIDocument.sortingOrder`를 설정 레이어로 설정하여 정렬을 강제합니다.
  - 로드된 프레젠터는 `"Ui"` 루트 아래에 직접 인스턴스화됩니다 (레이어별 컨테이너 GameObject 없음).
- **UI 세트는 주소가 아닌 타입을 저장합니다**
  - UI 세트는 `UiSetEntry` (타입 이름 + 인스턴스 주소)로 직렬화됩니다. 기본 에디터는 고유성을 위해 `InstanceAddress`를 **Addressable 주소**로 채웁니다.
- **`LoadSynchronously` 영속성**
  - `UiConfig.LoadSynchronously`가 존재하며 `AddressablesUiAssetLoader`에 의해 존중됩니다.
  - **그러나**: `UiConfigs.UiConfigSerializable`는 현재 `LoadSynchronously`를 직렬화하지 **않으므로**, `UiConfigs` 에셋에서 로드된 설정은 `UiConfigs.Configs`에서 `LoadSynchronously = false`를 생성합니다.
- **정적 이벤트**
  - `UiService.OnResolutionChanged` / `UiService.OnOrientationChanged`는 `UiServiceMonoComponent`에 의해 발생하는 정적 `UnityEvent`입니다.
  - 서비스는 리스너를 초기화하지 않습니다; 소비자는 적절히 구독 해제해야 합니다.
- **해제**
  - `UiService.Dispose()`는 모든 보이는 UI를 닫고, 모든 로드된 인스턴스를 언로드 시도하고, 컬렉션을 초기화하고, `"Ui"` 루트 GameObject를 파괴합니다.
- **에디터 디버깅 도구**
  - 일부 에디터 윈도우는 편의를 위해 `presenter.gameObject.SetActive(...)`를 직접 토글합니다; 이는 `UiService` 부기를 우회하므로 `IUiService.VisiblePresenters`에 반영되지 않을 수 있습니다.
- **UI Toolkit 비주얼 트리 타이밍과 요소 재생성**
  - `UIDocument.rootVisualElement`는 프레젠터에서 `OnInitialized()`가 호출될 때 준비되지 않았을 수 있습니다.
  - UI Toolkit은 프레젠터 GameObject가 비활성화/재활성화(닫기/재열기 주기)될 때 **비주얼 요소를 재생성**합니다. `AddVisualTreeAttachedListener(callback)`은 요소 재생성을 처리하기 위해 **매번 열기 시** 호출됩니다.

## 5. 코딩 표준 (Unity 6 / C# 9.0)
- **C#**: C# 9.0 구문; 전역 `using`s 없음; **명시적 네임스페이스** 유지.
- **어셈블리**
  - 런타임 코드는 `UnityEditor` 참조를 피해야 합니다; 에디터 전용 도구는 `Editor/`와 `GameLovers.UiService.Editor.asmdef` 아래에 배치하세요.
  - 런타임 타입 근처에 에디터 전용 코드를 추가해야 하면 `#if UNITY_EDITOR`로 보호하고 최소한으로 유지하세요.
- **비동기**
  - `UniTask`를 사용하세요; 가능한 경우 비동기 API를 통해 `CancellationToken`을 전달하세요.
- **메모리 / 할당**
  - 프레임 단위 할당을 피하세요; API 프로퍼티를 할당 없이 유지하세요 (`VisiblePresenters`와 `UiSets`를 위한 `UiService` 읽기 전용 래퍼 참조).

## 6. 외부 패키지 소스 (API 조회용)
서드파티 소스/문서가 필요할 때, 로컬 캐시된 UPM 패키지를 선호하세요:
- Addressables: `Library/PackageCache/com.unity.addressables@*/`
- UniTask: `Library/PackageCache/com.cysharp.unitask@*/`

## 7. 개발 워크플로우 (일반적인 변경)
- **새 프레젠터 추가**
  - `UiPresenter` (또는 `UiPresenter<T>`)를 상속하는 컴포넌트가 포함된 프리팹을 생성합니다.
  - 레이어 정렬을 적용하려면 `Canvas` 또는 `UIDocument`가 있는지 확인합니다.
  - 프리팹을 Addressable로 표시하고 주소를 설정합니다.
  - `UiConfigs`에서 항목을 추가/업데이트합니다 (메뉴: `Tools/UI Service/Select UiConfigs`).
- **UI 세트 추가 / 업데이트**
  - 기본 `UiConfigs` 인스펙터는 `DefaultUiSetId`를 사용합니다 (기본 제공).
  - 세트 ID를 커스터마이징하려면 고유한 enum과 `[CustomEditor(typeof(UiConfigs))] : UiConfigsEditor<TEnum>`을 생성하세요.
- **멀티 인스턴스 흐름 추가**
  - 인스턴스를 외부에서 추적해야 할 때 `UiInstanceId` (기본값 = `string.Empty`)를 사용합니다.
  - 프레젠터는 내부 `InstanceAddress` 프로퍼티를 통해 자체 인스턴스 주소를 알고 있습니다; `Close(destroy: true)`는 올바른 인스턴스를 언로드합니다.
- **프레젠터 피처 추가**
  - `PresenterFeatureBase`를 확장하고 프레젠터 프리팹에 부착합니다.
  - 피처는 초기화 시 `GetComponents`를 통해 검색되고 열기/닫기 중에 알림을 받습니다.
  - 전환이 있는 피처(애니메이션, 딜레이)의 경우: 프레젠터가 `OpenTransitionTask` / `CloseTransitionTask`를 대기할 수 있도록 `ITransitionFeature`를 구현하세요.
- **로딩 전략 변경**
  - 내장 로더(`AddressablesUiAssetLoader`, `PrefabRegistryUiAssetLoader`, `ResourcesUiAssetLoader`) 중 하나를 사용하거나 커스텀 필요에 맞게 `IUiAssetLoader`를 확장하는 것을 선호하세요.
- **문서/샘플 업데이트**
  - 사용자 대상 문서는 `docs/`에 있으며 동작/API 변경 시 업데이트해야 합니다.
  - 새 핵심 기능을 추가하면 `Samples~/` 아래에 샘플을 추가/조정하는 것을 고려하세요.

## 8. 업데이트 정책
다음 경우에 이 파일을 업데이트하세요:
- 공개 API 변경 (`IUiService`, `IUiServiceInit`, 프레젠터 생명주기, 설정 형식)
- 핵심 런타임 시스템/기능이 도입/제거될 때 (피처, 뷰, 멀티 인스턴스)
- 에디터 도구가 설정이나 세트를 생성/직렬화하는 방식이 변경될 때
