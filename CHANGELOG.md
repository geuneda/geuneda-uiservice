# 변경 이력
이 패키지의 주요 변경 사항은 이 파일에 기록됩니다.

형식은 [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)를 기반으로 하며
이 프로젝트는 [Semantic Versioning](http://semver.org/spec/v2.0.0.html)을 준수합니다.

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
- 세 개의 에디터 윈도우를 추가했습니다: `UiAnalyticsWindow`, `UiServiceHierarchyWindow`
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
