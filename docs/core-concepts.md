# 핵심 개념

이 문서에서는 UI Service의 기본 개념인 프레젠터, 레이어, 세트, 피처, 설정에 대해 다룹니다.

## 목차

- [에디터 윈도우](#에디터-윈도우)
- [서비스 인터페이스](#서비스-인터페이스)
- [UI 프레젠터](#ui-프레젠터)
- [프레젠터 피처](#프레젠터-피처)
- [UI 레이어](#ui-레이어)
- [UI 세트](#ui-세트)
- [멀티 인스턴스 지원](#멀티-인스턴스-지원)
- [UI 설정](#ui-설정)

---

## 에디터 윈도우

패키지에는 개발 및 디버깅을 위한 통합 도구가 포함되어 있습니다.

### Presenter Manager 윈도우

**메뉴:** `Tools → UI Service → Presenter Manager`

![UiConfigs Inspector](presenter-manager.png)

플레이 모드에서 활성 및 로드된 UI 프레젠터를 실시간으로 관리합니다:
- 모든 로드된 프레젠터와 현재 상태(열림/닫힘) 확인
- 인스턴스별 빠른 열기/닫기/언로드 작업
- 일괄 작업: 모두 닫기, 모두 언로드
- 상태 표시기: 🟢 열림, 🔴 닫힘

### UiConfigs 인스펙터

**메뉴:** `Tools → UI Service → Select Ui Configs`

![UiConfigs Inspector](uiconfigs-inspector.gif)

`UiConfigs` 에셋을 선택하면 향상된 인스펙터를 볼 수 있습니다:
- 시각적 레이어 계층 구조
- 색상 코딩된 레이어
- 드래그 앤 드롭 재정렬
- 통계 패널
- UI 세트 관리

---

## 서비스 인터페이스

UI Service는 서로 다른 사용 사례를 위해 **두 가지 인터페이스**를 노출합니다:

| 인터페이스 | 목적 | 주요 메서드 |
|-----------|------|-----------|
| `IUiService` | 서비스 **소비** | `OpenUiAsync`, `CloseUi`, `LoadUiAsync`, `UnloadUi`, `IsVisible`, `GetUi` |
| `IUiServiceInit` | 서비스 **초기화** | `IUiService` 상속 + `Init(UiConfigs)`, `Dispose()` |

### 각 인터페이스를 언제 사용할까

**`IUiServiceInit`을 사용할 때:**
- `UiService` 인스턴스를 직접 생성하고 소유할 때
- `Init(UiConfigs)`을 호출하여 초기화할 때
- `Dispose()`를 호출하여 정리할 때

**`IUiService`를 사용할 때:**
- 의존성 주입을 통해 서비스를 받을 때
- UI를 열기/닫기/쿼리만 할 때
- 서비스 생명주기를 관리하지 않을 때

### 올바른 초기화 패턴

```csharp
using UnityEngine;
using GameLovers.UiService;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;

    // IUiServiceInit 사용 - Init()과 Dispose()가 필요합니다
    private IUiServiceInit _uiService;

    void Start()
    {
        _uiService = new UiService();
        _uiService.Init(_uiConfigs);  // IUiServiceInit에서 사용 가능
    }

    void OnDestroy()
    {
        _uiService?.Dispose();  // IUiServiceInit에서 사용 가능
    }
}
```

### 흔한 실수

```csharp
// IUiService에는 Init()이 없습니다
private IUiService _uiService;

void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);  // CS1061: 'IUiService'에 'Init'이 포함되어 있지 않습니다
}
```

### 의존성 주입 패턴

```csharp
// 서비스 로케이터 또는 DI 컨테이너에서 IUiServiceInit으로 등록
public class ServiceLocator
{
    private IUiServiceInit _uiService;

    public void Initialize(UiConfigs configs)
    {
        _uiService = new UiService();
        _uiService.Init(configs);
    }

    // 소비자는 IUiService를 받습니다 (Init/Dispose 접근 불가)
    public IUiService GetUiService() => _uiService;

    public void Shutdown()
    {
        _uiService?.Dispose();
    }
}

// 소비자 클래스는 IUiService만 필요합니다
public class ShopController
{
    private readonly IUiService _uiService;

    public ShopController(IUiService uiService)
    {
        _uiService = uiService;
    }

    public async void OpenShop()
    {
        await _uiService.OpenUiAsync<ShopPresenter>();
    }
}
```

---

## UI 프레젠터

`UiPresenter`는 시스템 내 모든 UI 요소의 기본 클래스입니다. 생명주기 관리와 서비스 통합을 제공합니다.

### 생명주기 훅

| 메서드 | 호출 시점 | 용도 |
|--------|----------|------|
| `OnInitialized()` | 처음 로드될 때 한 번 | 설정, 이벤트 구독 |
| `OnOpened()` | UI가 표시될 때마다 | 애니메이션, 데이터 새로고침 |
| `OnClosed()` | UI가 숨겨질 때 | 정리, 상태 저장 |
| `OnOpenTransitionCompleted()` | 모든 전환 피처가 열기를 완료한 후 | 전환 후 로직, 상호작용 활성화 |
| `OnCloseTransitionCompleted()` | 모든 전환 피처가 닫기를 완료한 후 | 전환 후 정리 |

> **참고**: `OnOpenTransitionCompleted()`와 `OnCloseTransitionCompleted()`는 전환 피처가 없는 프레젠터에서도 **항상 호출**됩니다. 모든 프레젠터에 일관된 생명주기를 제공합니다.

### 전환 태스크

프레젠터는 외부 대기를 위한 공개 `UniTask` 프로퍼티를 노출합니다:

```csharp
// 프레젠터가 완전히 열릴 때까지 대기 (전환 포함)
await presenter.OpenTransitionTask;

// 프레젠터가 완전히 닫힐 때까지 대기 (전환 포함)
await presenter.CloseTransitionTask;
```

### 기본 프레젠터

```csharp
public class BasicPopup : UiPresenter
{
    protected override void OnInitialized()
    {
        // 프레젠터가 처음 로드될 때 호출됩니다
        // UI 요소 설정, 이벤트 구독
    }

    protected override void OnOpened()
    {
        // UI가 표시될 때마다 호출됩니다
        // 애니메이션 시작, 데이터 새로고침
    }

    protected override void OnClosed()
    {
        // UI가 숨겨질 때 호출됩니다
        // 애니메이션 중지, 상태 저장
    }
}
```

### 데이터 기반 프레젠터

초기화 데이터가 필요한 UI에는 `UiPresenter<T>`를 사용합니다:

```csharp
public struct QuestData
{
    public int QuestId;
    public string Title;
    public string Description;
}

public class QuestPresenter : UiPresenter<QuestData>
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _descriptionText;

    protected override void OnSetData()
    {
        // Data가 할당될 때마다 자동으로 호출됩니다
        _titleText.text = Data.Title;
        _descriptionText.text = Data.Description;
    }
}

// 사용법 - 열기 시 초기 데이터
var questData = new QuestData { QuestId = 1, Title = "Dragon Slayer", Description = "..." };
await _uiService.OpenUiAsync<QuestPresenter, QuestData>(questData);
```

### 동적 데이터 업데이트

`UiPresenter<T>`의 `Data` 프로퍼티는 할당 시 자동으로 `OnSetData()`를 트리거하는 **공개 setter**를 가지고 있습니다. 이를 통해 닫기와 다시 열기 없이 언제든지 UI 데이터를 업데이트할 수 있습니다:

```csharp
// 프레젠터를 가져와서 데이터를 직접 업데이트
var questPresenter = _uiService.GetUi<QuestPresenter>();

// OnSetData()가 자동으로 호출됩니다
questPresenter.Data = new QuestData
{
    QuestId = 2,
    Title = "Updated Quest",
    Description = "New description"
};
```

> **참고**: `Data` 설정은 `OpenUiAsync`를 통한 초기 열기 시든 이후 데이터 업데이트 시든 항상 `OnSetData()`를 호출합니다. 데이터 기반 프레젠터에 일관된 생명주기를 제공합니다.

### 내부에서 닫기

프레젠터는 스스로를 닫을 수 있습니다:

```csharp
public class ConfirmPopup : UiPresenter
{
    public void OnConfirmClicked()
    {
        // 닫기 (메모리에 유지)
        Close(destroy: false);
    }

    public void OnCancelClicked()
    {
        // 닫기 및 메모리에서 언로드
        // 멀티 인스턴스 프레젠터에서도 올바르게 작동합니다
        Close(destroy: true);
    }
}
```

---

## 프레젠터 피처

UI Service는 상속 복잡성 없이 프레젠터 동작을 확장하기 위해 **피처 기반 조합 시스템**을 사용합니다.

### 전환 피처

`ITransitionFeature`를 구현하는 피처는 열기/닫기 전환 딜레이를 제공합니다. 프레젠터는 다음 작업 전에 자동으로 모든 전환 피처를 대기합니다:
- `OnOpenTransitionCompleted()` 호출 (열기 후)
- GameObject 숨기기 및 `OnCloseTransitionCompleted()` 호출 (닫기 후)

이를 통해 가시성이 단일 지점(`UiPresenter`)에서 제어되고 전환이 적절히 조율됩니다.

### 내장 피처

#### TimeDelayFeature

UI 열기와 닫기에 시간 기반 딜레이를 추가합니다. `ITransitionFeature`를 구현합니다:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class DelayedPopup : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;

    protected override void OnOpened()
    {
        base.OnOpened();
        Debug.Log($"Opening with {_delayFeature.OpenDelayInSeconds}s delay...");
    }

    protected override void OnOpenTransitionCompleted()
    {
        Debug.Log("Opening delay completed - UI is ready!");
    }

    protected override void OnCloseTransitionCompleted()
    {
        Debug.Log("Closing delay completed!");
    }
}
```

**인스펙터 설정:**
- `Open Delay In Seconds` - 열기 후 대기 시간 (기본값: 0.5초)
- `Close Delay In Seconds` - 닫기 전 대기 시간 (기본값: 0.3초)

#### AnimationDelayFeature

UI 생명주기를 애니메이션 클립과 동기화합니다. `ITransitionFeature`를 구현합니다:

```csharp
[RequireComponent(typeof(AnimationDelayFeature))]
public class AnimatedPopup : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;

    protected override void OnOpenTransitionCompleted()
    {
        Debug.Log("Intro animation completed - UI is ready!");
    }

    protected override void OnCloseTransitionCompleted()
    {
        Debug.Log("Outro animation completed!");
    }
}
```

**인스펙터 설정:**
- `Animation Component` - 자동 감지 또는 수동 할당
- `Intro Animation Clip` - 열기 시 재생
- `Outro Animation Clip` - 닫기 시 재생

#### UiToolkitPresenterFeature

안전한 비주얼 트리 처리와 함께 UI Toolkit (UI Elements) 통합을 제공합니다.

> **주의:** UI Toolkit은 프레젠터 GameObject가 비활성화/재활성화될 때 **비주얼 요소를 재생성**합니다. `AddVisualTreeAttachedListener`를 통해 등록된 콜백은 이를 처리하기 위해 **매번 열기 시** 호출됩니다.

**프로퍼티:**
- `Document` - 연결된 `UIDocument`
- `Root` - 루트 `VisualElement` (패널 연결 전에는 null일 수 있음)

**메서드:**
- `AddVisualTreeAttachedListener(callback)` - 비주얼 트리가 준비되면 호출되는 콜백을 등록합니다. 요소 재생성을 처리하기 위해 매번 열기 시 호출됩니다.
- `RemoveVisualTreeAttachedListener(callback)` - 이전에 등록된 콜백을 제거합니다.

**권장 패턴:**

요소가 매번 열기 시 재생성될 수 있으므로, 새 요소를 쿼리하고 등록하기 전에 항상 이전 요소에서 등록을 해제하세요:

```csharp
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class UIToolkitMenu : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

    private Button _playButton;

    protected override void OnInitialized()
    {
        _toolkitFeature.AddVisualTreeAttachedListener(SetupUI);
    }

    private void SetupUI(VisualElement root)
    {
        // 1. 이전 요소에서 등록 해제 (닫기/재열기 후 오래된 것일 수 있음)
        _playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);

        // 2. 새 요소 쿼리
        _playButton = root.Q<Button>("play-button");

        // 3. 현재 요소에 등록
        _playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
    }

    private void OnPlayClicked(ClickEvent evt)
    {
        // 클릭 처리
    }

    private void OnDestroy()
    {
        _playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
    }
}
```

### 여러 피처 조합

피처는 자유롭게 조합할 수 있습니다:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class DelayedUiToolkitPresenter : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // 전환 완료까지 UI 비활성화
        _toolkitFeature.AddVisualTreeAttachedListener(root => root.SetEnabled(false));
    }

    protected override void OnOpenTransitionCompleted()
    {
        // 딜레이 완료 후 UI 활성화
        _toolkitFeature.Root?.SetEnabled(true);
    }
}
```

### 커스텀 피처 생성

기본 생명주기 훅에는 `PresenterFeatureBase`를 확장합니다. 전환 피처의 경우 `ITransitionFeature`도 구현합니다:

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeFeature : PresenterFeatureBase, ITransitionFeature
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.3f;

    private UniTaskCompletionSource _openTransitionCompletion;
    private UniTaskCompletionSource _closeTransitionCompletion;

    // ITransitionFeature 구현
    public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;
    public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

    public override void OnPresenterOpening()
    {
        _canvasGroup.alpha = 0f;
    }

    public override void OnPresenterOpened()
    {
        FadeInAsync().Forget();
    }

    public override void OnPresenterClosing()
    {
        FadeOutAsync().Forget();
    }

    private async UniTask FadeInAsync()
    {
        _openTransitionCompletion = new UniTaskCompletionSource();

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            _canvasGroup.alpha = elapsed / _fadeDuration;
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        _canvasGroup.alpha = 1f;

        _openTransitionCompletion.TrySetResult();
    }

    private async UniTask FadeOutAsync()
    {
        _closeTransitionCompletion = new UniTaskCompletionSource();

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            _canvasGroup.alpha = 1f - (elapsed / _fadeDuration);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        _canvasGroup.alpha = 0f;

        _closeTransitionCompletion.TrySetResult();
    }
}
```

**사용 가능한 생명주기 훅:**
- `OnPresenterInitialized(UiPresenter presenter)`
- `OnPresenterOpening()`
- `OnPresenterOpened()`
- `OnPresenterClosing()`
- `OnPresenterClosed()`

**전환 피처 생성:**
- 프레젠터가 대기해야 하는 피처에 `ITransitionFeature`를 구현합니다
- `OpenTransitionTask`와 `CloseTransitionTask`를 `UniTask` 프로퍼티로 노출합니다
- 전환이 완료되면 `UniTaskCompletionSource`를 사용하여 신호를 보냅니다
- 프레젠터는 생명주기를 완료하기 전에 모든 `ITransitionFeature` 태스크를 대기합니다

---

## UI 레이어

UI 요소는 레이어로 구성되며, 높은 레이어 번호가 위에(카메라에 더 가깝게) 표시됩니다.

### 레이어 구성

```csharp
// 권장 레이어 구조:
// 레이어 0: 배경 UI (스카이박스, 패럴랙스)
// 레이어 1: 게임 HUD (체력바, 미니맵)
// 레이어 2: 메뉴 (메인 메뉴, 설정)
// 레이어 3: 팝업 (확인, 보상)
// 레이어 4: 시스템 메시지 (오류, 로딩)
// 레이어 5: 디버그 오버레이
```

### 레이어 작업

```csharp
// 특정 레이어의 모든 UI 닫기
_uiService.CloseAllUi(layer: 2);

// 레이어는 UiConfigs에서 프레젠터별로 설정됩니다
```

### 레이어 작동 방식

- 각 프레젠터는 `UiConfigs`에서 레이어를 할당받습니다
- `Canvas.sortingOrder` (uGUI) 또는 `UIDocument.sortingOrder` (UI Toolkit)가 자동으로 설정됩니다
- 높은 레이어가 낮은 레이어 위에 렌더링됩니다

---

## UI 세트

관련 UI 요소를 일괄 작업을 위해 그룹화합니다.

### 세트 정의

세트는 `UiConfigs` 에셋에서 정의됩니다. 각 프레젠터는 선택적으로 세트 ID를 통해 세트에 속할 수 있습니다.

```
세트 0: 핵심 UI (항상 로드됨)
세트 1: 메인 메뉴 (로고, 메뉴 버튼, 배경)
세트 2: 게임플레이 (HUD, 미니맵, 채팅)
세트 3: 상점 (상점 윈도우, 인벤토리, 재화)
```

### 세트 작업

```csharp
// 세트의 모든 UI 로드 (태스크 배열 반환)
var loadTasks = _uiService.LoadUiSetAsync(setId: 1);
await UniTask.WhenAll(loadTasks);

// 세트의 모든 UI 닫기
_uiService.CloseAllUiSet(setId: 1);

// 메모리에서 세트 언로드
_uiService.UnloadUiSet(setId: 1);

// 세트를 제거하고 제거된 프레젠터 받기
var removed = _uiService.RemoveUiSet(setId: 2);
foreach (var presenter in removed)
{
    Destroy(presenter.gameObject);
}
```

### 권장 세트 구성

| 세트 ID 범위 | 용도 |
|-------------|------|
| 0 | 핵심/영구 UI (항상 로드됨) |
| 1-10 | 씬별 UI |
| 11-20 | 기능별 UI (상점, 인벤토리) |

---

## 멀티 인스턴스 지원

기본적으로 각 UI 프레젠터 타입은 싱글톤입니다. `UiInstanceId` 시스템은 동일 타입의 여러 인스턴스를 가능하게 합니다.

### 사용 사례

- 여러 개의 툴팁 윈도우
- 쌓인 알림 팝업
- 여러 플레이어 정보 패널 (멀티플레이어)
- 풀링된 UI 요소

### UiInstanceId

```csharp
// 기본/싱글톤 인스턴스
var defaultId = UiInstanceId.Default(typeof(TooltipPresenter));

// 이름이 지정된 인스턴스
var itemTooltipId = UiInstanceId.Named(typeof(TooltipPresenter), "item");
var skillTooltipId = UiInstanceId.Named(typeof(TooltipPresenter), "skill");

// 기본 인스턴스인지 확인
if (instanceId.IsDefault)
{
    Debug.Log("This is the singleton instance");
}
```

### 인스턴스 작업

```csharp
// 로드된 모든 프레젠터 가져오기
List<UiInstance> loaded = _uiService.GetLoadedPresenters();

foreach (var instance in loaded)
{
    Debug.Log($"Type: {instance.Type.Name}");
    Debug.Log($"Address: {instance.Address}"); // 기본값이면 비어 있음
    Debug.Log($"Presenter: {instance.Presenter.name}");
}

// 보이는 프레젠터 확인
IReadOnlyList<UiInstanceId> visible = _uiService.VisiblePresenters;
```

### UiInstance vs UiInstanceId

| 구조체 | 용도 | 포함 내용 |
|--------|------|----------|
| `UiInstanceId` | 참조를 위한 식별자 | `PresenterType`, `InstanceAddress` |
| `UiInstance` | 로드된 인스턴스의 전체 데이터 | `Type`, `Address`, `Presenter` |

---

## UI 설정

`UiConfigs` ScriptableObject는 모든 UI 설정을 저장합니다.

### UiConfigs 생성

1. Project 뷰에서 마우스 오른쪽 클릭
2. `Create` → `ScriptableObjects` → `Configs` → `UiConfigs`로 이동

### 설정 프로퍼티

| 프로퍼티 | 설명 |
|---------|------|
| **Type** | 프레젠터 클래스 타입 |
| **Addressable Address** | UI 프리팹의 Addressable 키 |
| **Layer** | 깊이 레이어 (높을수록 카메라에 가까움) |
| **Load Synchronously** | 로드 중 메인 스레드 차단 (주의하여 사용) |
| **UI Set ID** | 일괄 작업을 위한 선택적 그룹핑 |

### 런타임 설정

```csharp
// 런타임에 설정 추가
var config = new UiConfig(typeof(DynamicPopup), "UI/DynamicPopup", layer: 3);
_uiService.AddUiConfig(config);

// 런타임에 UI 세트 추가
var setConfig = new UiSetConfig(setId: 5, new[] { typeof(ShopUI), typeof(InventoryUI) });
_uiService.AddUiSet(setConfig);

// 인스턴스화된 UI 추가
var dynamicUi = Instantiate(uiPrefab);
_uiService.AddUi(dynamicUi, layer: 3, openAfter: true);
```
