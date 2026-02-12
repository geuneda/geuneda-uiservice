# UI Service 샘플

이 폴더에는 UI Service의 **피처 조합** 패턴을 보여주는 예제 구현이 포함되어 있습니다.

---

## 빠른 시작 (설정 불필요!)

1. **Window > Package Manager**를 엽니다
2. 목록에서 **UiService**를 선택합니다
3. **Samples** 섹션을 펼칩니다
4. 원하는 샘플 옆의 **Import**를 클릭합니다
5. `Assets/Samples/UiService/{version}/{SampleName}/`에서 샘플 씬을 엽니다
6. **Play를 누르면** - 바로 작동합니다!

Addressables 설정, 프리팹 생성, 설정 구성이 필요 없습니다.

---

## 아키텍처 개요

모든 샘플은 자체 완결형 피처 패턴을 사용합니다:

- **기본:** `UiPresenter` 또는 `UiPresenter<T>`
- **피처:** 자체 완결형 컴포넌트 (`TimeDelayFeature`, `AnimationDelayFeature`, `UiToolkitPresenterFeature`)
- **조합:** `[RequireComponent]`를 사용하여 필요에 따라 피처를 조합

### 서비스 인터페이스 (AI 어시스턴트를 위한 중요 사항)

UI Service는 서로 다른 목적을 가진 **두 가지 인터페이스**를 사용합니다:

| 인터페이스 | 목적 | `Init()` 포함 | `Dispose()` 포함 |
|-----------|------|--------------|-----------------|
| `IUiService` | **소비** - UI를 열기/닫기/쿼리만 할 때 사용 | 없음 | 없음 |
| `IUiServiceInit` | **초기화** - 서비스를 생성하고 초기화할 때 사용 | 있음 | 있음 |

**주의:** `Init(UiConfigs)` 메서드는 `IUiService`가 아닌 **`IUiServiceInit`에서만 사용 가능**합니다.

**올바른 초기화 패턴:**
```csharp
// 올바른 예 - Init()을 호출해야 할 때 IUiServiceInit 사용
private IUiServiceInit _uiService;

void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);  // 작동!
}

void OnDestroy()
{
    _uiService?.Dispose();  // IUiServiceInit에서도 사용 가능
}
```

**흔한 실수 (CS1061 오류 발생):**
```csharp
// 잘못된 예 - IUiService에는 Init()이 없음
private IUiService _uiService;

void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);  // 오류: CS1061
}
```

---

## 포함된 샘플

| # | 샘플 | 주제 |
|---|------|------|
| 1 | [BasicUiFlow](#1-basicuiflow) | 핵심 생명주기 |
| 2 | [DataPresenter](#2-datapresenter) | 데이터 기반 UI |
| 3 | [DelayedPresenter](#3-delayedpresenter) | 시간 및 애니메이션 딜레이 |
| 4 | [UiToolkit](#4-uitoolkit) | UI Toolkit 통합 |
| 5 | [DelayedUiToolkit](#5-delayeduitoolkit) | 다중 피처 조합 |
| 6 | [Analytics](#6-analytics) | 성능 추적 |
| 7 | [UiSets](#7-uisets) | HUD 관리 |
| 8 | [MultiInstance](#8-multiinstance) | 팝업 스택 |
| 9 | [CustomFeatures](#9-customfeatures) | 커스텀 피처 만들기 |
| 10 | [AssetLoadingStrategies](#10-assetloadingstrategies) | 로딩 전략 비교 |

---

### 1. BasicUiFlow

**파일:**
- `BasicUiExamplePresenter.cs` - 피처 없는 간단한 프레젠터
- `BasicUiFlowExample.cs` - 씬 설정 및 UI 서비스 사용

**시연 내용:**
- 기본 UI 프레젠터 생명주기
- UI 로딩, 열기, 닫기, 언로딩
- 간단한 버튼 상호작용

**패턴:**
```csharp
public class BasicUiExamplePresenter : UiPresenter
{
    protected override void OnInitialized() { }
    protected override void OnOpened() { }
    protected override void OnClosed() { }
}
```

---

### 2. DataPresenter

**파일:**
- `DataUiExamplePresenter.cs` - 타입이 지정된 데이터를 가진 프레젠터
- `DataPresenterExample.cs` - 데이터 기반 UI 예제

**시연 내용:**
- 데이터 기반 UI를 위한 `UiPresenter<T>` 사용
- 프레젠터 데이터 설정 및 접근
- `OnSetData()` 생명주기 훅

**패턴:**
```csharp
public struct PlayerData
{
    public string PlayerName;
    public int Level;
}

public class DataUiExamplePresenter : UiPresenter<PlayerData>
{
    protected override void OnSetData()
    {
        // Data.PlayerName, Data.Level로 데이터 접근
    }
}

// 데이터와 함께 열기
_uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(playerData);
```

---

### 3. DelayedPresenter

**파일:**
- `DelayedUiExamplePresenter.cs` - 시간 기반 딜레이
- `AnimatedUiExamplePresenter.cs` - 애니메이션 기반 딜레이
- `DelayedPresenterExample.cs` - 씬 설정

**시연 내용:**
- 시간 기반 딜레이를 위한 `TimeDelayFeature` 사용
- 애니메이션 동기화 딜레이를 위한 `AnimationDelayFeature` 사용
- 프레젠터 생명주기 훅을 통한 딜레이 완료 반응

**패턴 (Time Delay):**
```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class DelayedUiExamplePresenter : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;

    protected override void OnOpened()
    {
        base.OnOpened();
        // UI가 보이고, 딜레이가 시작 중...
    }

    protected override void OnOpenTransitionCompleted()
    {
        // 딜레이 완료 후 호출 - UI가 상호작용 준비 완료
        Debug.Log($"Opened after {_delayFeature.OpenDelayInSeconds}s delay!");
    }

    protected override void OnCloseTransitionCompleted()
    {
        // 닫기 딜레이 완료 후 호출
        Debug.Log("Closing transition completed!");
    }
}
```

**패턴 (Animation Delay):**
```csharp
[RequireComponent(typeof(AnimationDelayFeature))]
public class AnimatedUiExamplePresenter : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;

    protected override void OnOpenTransitionCompleted()
    {
        // 인트로 애니메이션 완료 후 호출
        Debug.Log("Intro animation completed!");
    }
}
```

**피처 설정:**

| 피처 | 인스펙터 설정 |
|------|-------------|
| `TimeDelayFeature` | 열기/닫기 딜레이 (초) |
| `AnimationDelayFeature` | Animation 컴포넌트, 인트로/아웃트로 클립 |

---

### 4. UiToolkit

**파일:**
- `UiToolkitExamplePresenter.cs` - UI Toolkit 프레젠터
- `UiToolkitExample.cs` - 씬 설정
- `UiToolkitExample.uxml` - UI Toolkit 레이아웃

**시연 내용:**
- UI Toolkit 통합을 위한 `UiToolkitPresenterFeature` 사용
- 루트에서 VisualElements 쿼리
- UI Toolkit 이벤트 바인딩

**패턴:**
```csharp
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class UiToolkitExamplePresenter : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var root = _toolkitFeature.Root;
        var button = root.Q<Button>("MyButton");
        button.clicked += OnButtonClicked;
    }
}
```

---

### 5. DelayedUiToolkit

**파일:**
- `TimeDelayedUiToolkitPresenter.cs` - 시간 딜레이 + UI Toolkit
- `AnimationDelayedUiToolkitPresenter.cs` - 애니메이션 딜레이 + UI Toolkit + 데이터
- `DelayedUiToolkitExample.cs` - 씬 설정
- `DelayedUiToolkitExample.uxml` - UI Toolkit 레이아웃

**시연 내용:**
- 여러 피처를 함께 조합
- 딜레이 피처와 UI Toolkit 결합
- 여러 피처와 함께 데이터 사용

**패턴:**
```csharp
[RequireComponent(typeof(TimeDelayFeature))]
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class TimeDelayedUiToolkitPresenter : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

    protected override void OnOpened()
    {
        base.OnOpened();
        _toolkitFeature.Root.SetEnabled(false);
    }

    protected override void OnOpenTransitionCompleted()
    {
        // 딜레이 완료 후 UI 활성화
        _toolkitFeature.Root.SetEnabled(true);
    }
}
```

---

### 6. Analytics

**파일:**
- `AnalyticsCallbackExample.cs` - 분석 통합 예제

**시연 내용:**
- `UiAnalytics` 인스턴스 생성
- 커스텀 분석 콜백 설정
- UI 생명주기 이벤트 구독
- 성능 메트릭 확인

**패턴:**
```csharp
// 분석 인스턴스 생성
var analytics = new UiAnalytics();

// 커스텀 콜백 설정
analytics.SetCallback(new CustomAnalyticsCallback());

// UiService 생성자에 전달
IUiServiceInit uiService = new UiService(new AddressablesUiAssetLoader(), analytics);
uiService.Init(_uiConfigs);

// UnityEvents 구독
analytics.OnUiOpened.AddListener(data => Debug.Log($"Opened: {data.UiName}"));

// 메트릭 접근
var metrics = analytics.GetMetrics(typeof(MyPresenter));
Debug.Log($"Opens: {metrics.OpenCount}, Load time: {metrics.LoadDuration}s");

// 요약 로그
analytics.LogPerformanceSummary();
```

**커스텀 콜백:**
```csharp
public class CustomAnalyticsCallback : IUiAnalyticsCallback
{
    public void OnUiLoaded(UiEventData data) { }
    public void OnUiOpened(UiEventData data) { }
    public void OnUiClosed(UiEventData data) { }
    public void OnUiUnloaded(UiEventData data) { }
    public void OnPerformanceMetricsUpdated(UiPerformanceMetrics metrics) { }
}
```

---

### 7. UiSets

**파일:**
- `UiSetsExample.cs` - 씬 설정 및 UI 세트 관리
- `HudHealthBarPresenter.cs` - HUD 요소 예제 (체력바)
- `HudCurrencyPresenter.cs` - HUD 요소 예제 (재화 표시)

**시연 내용:**
- 동시 관리를 위한 여러 UI 그룹핑
- 일반적인 HUD 패턴 (체력, 재화, 미니맵 등)
- 부드러운 전환을 위한 UI 세트 사전 로드

**패턴:**
```csharp
// 타입 안전성을 위해 세트 ID를 enum으로 정의
public enum UiSetId { GameHud = 0, PauseMenu = 1 }

// 세트의 모든 UI 로드 (표시하지 않고 사전 로드)
var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
await UniTask.WhenAll(loadTasks);

// 세트의 모든 UI 닫기 (숨기고 메모리에 유지)
_uiService.CloseAllUiSet((int)UiSetId.GameHud);

// 세트의 모든 UI 언로드 (파괴)
_uiService.UnloadUiSet((int)UiSetId.GameHud);

// 설정된 세트 목록
foreach (var kvp in _uiService.UiSets)
{
    Debug.Log($"Set {kvp.Key}: {kvp.Value.UiInstanceIds.Length} UIs");
}
```

**설정:**
1. HUD 요소용 UI 프레젠터 생성
2. 적절한 레이어로 `UiConfigs`에 설정
3. 모든 HUD 프레젠터를 포함하는 UI 세트를 `UiConfigs`에 생성
4. `LoadUiSetAsync`로 한 번에 모두 사전 로드

---

### 8. MultiInstance

**파일:**
- `MultiInstanceExample.cs` - 씬 설정 및 인스턴스 관리
- `NotificationPopupPresenter.cs` - 다중 인스턴스를 지원하는 팝업

**시연 내용:**
- 같은 UI 타입의 여러 인스턴스 생성
- 고유 식별을 위한 인스턴스 주소 사용
- 팝업 스택 및 알림 관리

**패턴:**
```csharp
// 고유 인스턴스 주소 생성
var instanceAddress = $"popup_{_counter}";

// 인스턴스 주소로 로드
await _uiService.LoadUiAsync(typeof(MyPopup), instanceAddress, openAfter: false);

// 특정 인스턴스 열기
await _uiService.OpenUiAsync(typeof(MyPopup), instanceAddress);

// 특정 인스턴스의 가시성 확인
bool visible = _uiService.IsVisible<MyPopup>(instanceAddress);

// 특정 인스턴스 닫기
_uiService.CloseUi(typeof(MyPopup), instanceAddress, destroy: true);

// 특정 인스턴스 언로드
_uiService.UnloadUi(typeof(MyPopup), instanceAddress);
```

**핵심 개념:**

| 개념 | 설명 |
|------|------|
| `UiInstanceId` | 고유 식별을 위해 Type + InstanceAddress를 결합 |
| Instance Address | 인스턴스를 구분하는 문자열 (예: `"popup_1"`, `"popup_2"`) |
| Default Instance | instanceAddress가 `null`이거나 비어 있을 때 (싱글톤 동작) |

---

### 9. CustomFeatures

**파일:**
- `CustomFeaturesExample.cs` - 커스텀 피처를 시연하는 씬 설정
- `FadeFeature.cs` - 페이드 인/아웃 효과를 위한 커스텀 피처
- `ScaleFeature.cs` - 커브를 사용한 스케일 인/아웃 커스텀 피처
- `SoundFeature.cs` - 열기/닫기 사운드를 위한 커스텀 피처
- `FadingPresenter.cs` - FadeFeature를 사용하는 프레젠터
- `ScalingPresenter.cs` - ScaleFeature를 사용하는 프레젠터
- `FullFeaturedPresenter.cs` - 세 가지 피처를 모두 결합한 프레젠터

**시연 내용:**
- 커스텀 프레젠터 피처 생성
- `PresenterFeatureBase` 확장
- 생명주기 훅 구현
- 피처 조합 (하나의 프레젠터에 여러 피처)

**패턴 (ITransitionFeature를 사용한 커스텀 전환 피처):**
```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameLovers.UiService;

[RequireComponent(typeof(CanvasGroup))]
public class FadeFeature : PresenterFeatureBase, ITransitionFeature
{
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private CanvasGroup _canvasGroup;

    private UniTaskCompletionSource _openTransitionCompletion;
    private UniTaskCompletionSource _closeTransitionCompletion;

    // ITransitionFeature 구현 - 프레젠터가 이를 대기합니다
    public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;
    public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

    private void OnValidate()
    {
        _canvasGroup = _canvasGroup ?? GetComponent<CanvasGroup>();
    }

    public override void OnPresenterOpening()
    {
        _canvasGroup.alpha = 0f;
    }

    public override void OnPresenterOpened()
    {
        FadeInAsync().Forget();
    }

    private async UniTask FadeInAsync()
    {
        _openTransitionCompletion = new UniTaskCompletionSource();

        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            _canvasGroup.alpha = elapsed / _fadeInDuration;
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        _canvasGroup.alpha = 1f;

        // 전환 완료 신호 - 프레젠터가 이를 대기합니다
        _openTransitionCompletion.TrySetResult();
    }
}
```

**패턴 (커스텀 피처 사용):**
```csharp
[RequireComponent(typeof(FadeFeature))]
[RequireComponent(typeof(ScaleFeature))]
[RequireComponent(typeof(SoundFeature))]
public class FullFeaturedPresenter : UiPresenter
{
    protected override void OnOpenTransitionCompleted()
    {
        // 모든 ITransitionFeature 태스크가 완료된 후 호출
        Debug.Log("All animations complete - UI is ready!");
    }
}
```

**생명주기 훅:**

| 훅 | 호출 시점 |
|----|----------|
| `OnPresenterInitialized(presenter)` | 프레젠터 생성 시 한 번 |
| `OnPresenterOpening()` | 프레젠터가 보이기 전 |
| `OnPresenterOpened()` | 프레젠터가 보인 후 |
| `OnPresenterClosing()` | 프레젠터가 숨겨지기 전 |
| `OnPresenterClosed()` | 프레젠터가 숨겨진 후 |

---

## 모범 사례

### 1. 전환에는 프레젠터 생명주기 훅 사용

피처 전환에 반응하기 위해 `OnOpenTransitionCompleted()`와 `OnCloseTransitionCompleted()`를 오버라이드하세요:

```csharp
protected override void OnOpenTransitionCompleted()
{
    // 딜레이/애니메이션 피처가 열기 전환을 완료한 후 호출
    Debug.Log("UI is fully ready for interaction!");
}

protected override void OnCloseTransitionCompleted()
{
    // 딜레이/애니메이션 피처가 닫기 전환을 완료한 후 호출
    Debug.Log("UI closing transition finished!");
}
```

### 2. 자동 할당에 OnValidate 사용

Unity가 에디터에서 컴포넌트 참조를 자동 할당하도록 합니다:

```csharp
private void OnValidate()
{
    _canvasGroup = _canvasGroup ?? GetComponent<CanvasGroup>();
}
```

### 3. 인스펙터에서 설정

`[SerializeField]`를 사용하여 디자이너에게 설정을 노출합니다:

```csharp
[SerializeField] private float _fadeDuration = 0.3f;
[SerializeField] private AnimationCurve _easeCurve;
```

### 4. 자유롭게 조합

상속 충돌 걱정 없이 피처를 자유롭게 조합합니다:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
[RequireComponent(typeof(UiToolkitPresenterFeature))]
[RequireComponent(typeof(SoundFeature))]
public class MyPresenter : UiPresenter { }
```

---

## 아키텍처 장점

| 장점 | 설명 |
|------|------|
| 자체 완결형 | 각 피처가 완전한 로직을 소유 |
| 조합 가능 | 모든 피처를 자유롭게 조합 |
| 설정 가능 | 모든 설정이 Unity 인스펙터에 |
| 명확함 | 하나의 컴포넌트 = 하나의 기능 |
| 확장성 | 기존 코드 수정 없이 새 피처 추가 |

---

### 10. AssetLoadingStrategies

**파일:**
- `ExamplePresenter.cs` - 시연용 간단한 프레젠터
- `AssetLoadingExample.cs` - 드롭다운 UI를 통한 런타임 전략 전환
- `PrefabAssetLoadingConfigs.asset` - PrefabRegistry 전략 설정
- `ResourcesAssetLoadingConfigs.asset` - Resources 전략 설정
- `AddressablesAssetLoadingConfigs.asset` - Addressables 전략 설정
- `Resources/ExamplePresenter.prefab` - Resources 로딩용 프리팹 복사본

**시연 내용:**
- 다양한 에셋 로딩 전략 사용 (PrefabRegistry, Addressables, Resources)
- 드롭다운을 통한 런타임 전략 전환
- `IUiAssetLoader` 추상화를 통한 로더 전환
- 각 전략의 설정 요구사항

**전략 사용 가능 여부:**

| 전략 | 설정 필요 | 임포트 후 바로 작동 |
|------|----------|-------------------|
| **PrefabRegistry** | 없음 | 예 |
| **Resources** | 없음 | 예 |
| **Addressables** | 필요 (아래 참조) | 아니오 |

**Addressables 설정 (해당 전략에 필요):**
1. **Window > Asset Management > Addressables > Groups**를 엽니다
2. 그룹이 없으면 **Create Addressables Settings**를 클릭합니다
3. 샘플 폴더에서 `ExamplePresenter.prefab`을 찾습니다
4. 프리팹 Inspector에서 **Addressable** 체크박스를 선택합니다
5. 주소를 `ExamplePresenter`로 설정합니다 (설정과 일치해야 함)
6. 테스트용: **Window > Asset Management > Addressables > Groups > Play Mode Script** → **Use Asset Database (fastest)** 선택
7. 빌드용: 플레이어 빌드 전에 Addressables 카탈로그를 빌드합니다

**패턴:**
```csharp
// 전략에 따라 로더 생성
IUiAssetLoader loader = _strategy switch
{
    LoadingStrategy.PrefabRegistry => new PrefabRegistryUiAssetLoader(_prefabRegistryConfigs),
    LoadingStrategy.Addressables => new AddressablesUiAssetLoader(),
    LoadingStrategy.Resources => new ResourcesUiAssetLoader(),
    _ => throw new ArgumentOutOfRangeException()
};

// 로더와 해당 설정으로 서비스 초기화
_uiService = new UiService(loader);
_uiService.Init(configs);
```

**런타임 전략 전환:**
```csharp
// 전환 전 현재 서비스 해제
_uiService?.Dispose();
_uiService = null;

// 새 전략으로 재초기화
InitializeService(newStrategy);
```

---

## 샘플 씬 설정

모든 샘플은 특정 입력 시스템(레거시 vs 새 시스템)에 대한 의존성을 피하기 위해 **UI 버튼**을 입력에 사용합니다. 프로젝트의 입력 설정에 관계없이 샘플이 작동하도록 보장합니다.

### 프리팹 구조

샘플 씬을 설정할 때, 제어 버튼이 있는 UI Canvas를 생성합니다. 예시 구조:

```
Scene
├── EventSystem (StandaloneInputModule 또는 InputSystemUIInputModule 포함)
├── SampleControlsCanvas (Screen Space - Overlay)
│   └── VerticalLayoutGroup
│       ├── HeaderText ("Sample Name")
│       ├── Button_Action1 ("Load UI")
│       ├── Button_Action2 ("Open UI")
│       ├── Button_Action3 ("Close UI")
│       └── ... 필요한 만큼 버튼 추가
├── SampleExample (MonoBehaviour)
│   ├── UiConfigs 참조
│   └── Button 참조 (직렬화된 필드)
└── (UI 프레젠터 프리팹은 런타임에 UiService가 인스턴스화)
```

### 버튼 연결

각 샘플 MonoBehaviour는 인스펙터에서 연결해야 하는 버튼 필드를 노출합니다:

```csharp
[Header("UI Buttons")]
[SerializeField] private Button _loadButton;
[SerializeField] private Button _openButton;
[SerializeField] private Button _closeButton;
```

샘플은:
1. `Start()`에서 버튼 클릭 이벤트를 구독합니다
2. `OnDestroy()`에서 메모리 누수 방지를 위해 구독을 해제합니다
3. 코드에서도 호출할 수 있는 public 메서드를 노출합니다

### 입력 시스템 호환성

| 프로젝트 설정 | 작동 여부 |
|--------------|----------|
| Legacy Input Manager | 버튼이 `StandaloneInputModule`을 통해 작동 |
| New Input System | 버튼이 `InputSystemUIInputModule`을 통해 작동 |
| 둘 다 | 두 모듈 모두 작동 |

---

## 시작하기

1. 사용 사례에 맞는 **샘플을 선택**합니다
2. Package Manager를 통해 **임포트**합니다
3. 제어 Canvas와 버튼이 있는 **씬을 생성**합니다 (위 구조 참조)
4. 샘플 MonoBehaviour 인스펙터에서 **버튼 참조를 연결**합니다
5. **패턴을 복사**하여 자신의 프레젠터에 적용합니다
6. `[RequireComponent]`를 통해 **필요한 피처를 추가**합니다
7. **인스펙터에서 설정** - 딜레이, 애니메이션 등
8. 피처 완료에 반응하기 위해 **전환 훅을 오버라이드**합니다 (`OnOpenTransitionCompleted`, `OnCloseTransitionCompleted`)

---

## 문서

전체 문서는 다음을 참조하세요:

- **[docs/README.md](../docs/README.md)** - 문서 목차
- **[docs/getting-started.md](../docs/getting-started.md)** - 빠른 시작 가이드
- **[docs/core-concepts.md](../docs/core-concepts.md)** - 핵심 개념
- **[docs/api-reference.md](../docs/api-reference.md)** - API 레퍼런스
- **[docs/advanced.md](../docs/advanced.md)** - 고급 주제
- **[docs/troubleshooting.md](../docs/troubleshooting.md)** - 문제 해결

---

**모든 샘플은 최대한의 유연성과 재사용성을 위해 피처 조합 패턴을 사용합니다.**
