# 핵심 개념

- [서비스 인터페이스](#서비스-인터페이스)
- [프레젠터와 생명주기](#프레젠터와-생명주기)
- [프레젠터 피처](#프레젠터-피처)
- [UI 레이어](#ui-레이어)
- [UI 세트](#ui-세트)
- [다중 인스턴스](#다중-인스턴스)
- [UI 설정](#ui-설정)
- [에디터 도구](#에디터-도구)

---

## 서비스 인터페이스

| 타입 | 용도 |
|------|------|
| `IUiService` | UI 를 소비하는 쪽. 로드/열기/닫기/언로드/조회 |
| `IUiServiceInit : IUiService, IDisposable` | 서비스를 소유하는 쪽. `Init(UiConfigs)` 와 `Dispose()` 추가 |
| `UiService : IUiServiceInit` | 구현. 인스턴스 주소를 받는 추가 오버로드 제공 |

서비스를 만든 곳은 `IUiServiceInit` 으로 들고 `Init()`/`Dispose()` 를 호출하고, 나머지 코드에는 `IUiService` 만 넘기는 구성을 권장합니다.

```csharp
using Geuneda.UiService;

public class UiInstaller
{
    private IUiServiceInit _uiService;

    public IUiService Service => _uiService;

    public void Install(UiConfigs configs)
    {
        _uiService = new UiService();
        _uiService.Init(configs);
    }

    public void Uninstall()
    {
        _uiService?.Dispose();
    }
}
```

`IUiService` 로 선언한 변수에는 `Init()` 이 없습니다. 인스턴스 주소를 받는 오버로드 중 `CloseUi(Type, string, bool)` 과 `UnloadUi(Type, string)` 만 `IUiService` 에 있고, 나머지(`GetUi<T>(string)`, `IsVisible<T>(string)`, `LoadUiAsync(Type, string, ...)`, `OpenUiAsync(Type, string, ...)`, `AddUi<T>(T, int, string, bool)`, `RemoveUi(Type, string)`)는 `UiService` 에만 있습니다.

---

## 프레젠터와 생명주기

`UiPresenter` 는 `MonoBehaviour` 이며 프리팹 루트에 붙입니다. 데이터가 필요하면 `UiPresenter<T>`(`T : struct`)를 상속하고, `Data` 에 값을 대입할 때마다 `OnSetData()` 가 호출됩니다.

### 생명주기 순서

서비스에 추가될 때(`LoadUiAsync`, `OpenUiAsync` 의 로드 단계, `AddUi`):

1. 인스턴스화 후 `SetActive(false)` (`AddUi` 는 인스턴스화와 비활성화를 하지 않음)
2. 레이어를 `Canvas`/`UIDocument` sortingOrder 에 반영
3. 루트의 `PresenterFeatureBase` 컴포넌트를 모아 각 피처의 `OnPresenterInitialized(presenter)`
4. `OnInitialized()`

열기:

1. 피처 `OnPresenterOpening()`
2. `gameObject.SetActive(true)`
3. `OnOpened()`
4. 피처 `OnPresenterOpened()`
5. 모든 `ITransitionFeature.OpenTransitionTask` 대기
6. `OnOpenTransitionCompleted()` (전환 피처가 없어도 호출)

닫기:

1. 피처 `OnPresenterClosing()`
2. `OnClosed()`
3. 피처 `OnPresenterClosed()`
4. 모든 `ITransitionFeature.CloseTransitionTask` 대기
5. `gameObject.SetActive(false)`
6. `OnCloseTransitionCompleted()` (전환 피처가 없어도 호출)
7. `destroy: true` 였다면 `UnloadUi(type, instanceAddress)`

주의할 점:

- `OpenUiAsync` 와 `CloseUi` 는 전환을 기다리지 않습니다. 전환 완료를 기다리려면 `presenter.OpenTransitionTask` / `presenter.CloseTransitionTask` 를 `await` 합니다.
- `IsVisible<T>()` 는 서비스의 표시 목록 기준이고, `presenter.IsOpen` 은 `gameObject.activeSelf` 기준입니다. 닫기 전환 중에는 `IsVisible` 이 이미 `false` 이고 `IsOpen` 은 아직 `true` 입니다.
- 프레젠터 내부에서는 `Close(bool destroy)` 로 자기 자신을 닫습니다. 자신의 인스턴스 주소를 사용하므로 다중 인스턴스에서도 해당 인스턴스만 닫힙니다.

```csharp
public class ConfirmPopup : UiPresenter
{
    public void OnConfirmClicked() => Close(destroy: false);  // 숨기기
    public void OnCancelClicked() => Close(destroy: true);    // 닫은 뒤 언로드
}
```

---

## 프레젠터 피처

피처는 프레젠터 프리팹의 **같은 GameObject** 에 붙인 `PresenterFeatureBase` 컴포넌트입니다(`GetComponents` 로 수집하므로 자식 오브젝트의 피처는 무시됩니다). `ITransitionFeature` 를 구현한 피처는 프레젠터가 전환 태스크를 기다립니다.

### 기본 제공 피처

| 피처 | 동작 | 설정 |
|------|------|------|
| `TimeDelayFeature` | 열린 뒤 `_openDelayInSeconds`(기본 0.5), 닫기 시작 시 `_closeDelayInSeconds`(기본 0.3)만큼 전환 대기 | 인스펙터의 두 딜레이 값. 파생 클래스에서 `OnOpenStarted`, `OnOpenedCompleted`, `OnCloseStarted`, `OnClosedCompleted` 재정의 가능 |
| `AnimationDelayFeature` | 열린 뒤 인트로 클립, 닫기 시작 시 아웃트로 클립을 `Animation` 컴포넌트로 재생하고 클립 길이만큼 대기 | `Animation` 컴포넌트 필요(`RequireComponent`), 인트로/아웃트로 클립. 클립이 없으면 해당 전환은 즉시 완료 |
| `UiToolkitPresenterFeature` | `UIDocument` 의 비주얼 트리가 준비되면 콜백 호출 | `UIDocument` 필요(`RequireComponent`) |
| `UiToolkitPanelRendererFeature` | Unity 6000.5 이상 전용. `PanelRenderer` 의 UI 리로드 콜백으로 루트를 받아 같은 API 로 전달 | `PanelRenderer` 필요. `#if UNITY_6000_5_OR_NEWER` 안에서만 컴파일 |

`AnimationDelayFeature` 는 레거시 `Animation` 컴포넌트를 사용합니다. `Animator` 는 지원하지 않습니다.

### UI Toolkit

`UIDocument` 는 GameObject 가 비활성화됐다가 다시 활성화되면 비주얼 요소를 새로 만듭니다. 그래서 `OnInitialized()` 에서 `rootVisualElement` 를 직접 쿼리하지 말고 `AddVisualTreeAttachedListener` 로 콜백을 등록합니다. 콜백은 트리가 이미 준비돼 있으면 즉시, 그리고 열 때마다 호출되며 한 번 여는 동안 여러 번 호출될 수 있습니다. 콜백은 여러 번 불려도 안전하게 작성합니다.

```csharp
using Geuneda.UiService;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class SettingsPresenter : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

    private Button _closeButton;

    protected override void OnInitialized()
    {
        _toolkitFeature.AddVisualTreeAttachedListener(SetupUi);
    }

    private void SetupUi(VisualElement root)
    {
        // 이전 요소의 등록을 먼저 해제하고 새 요소에 다시 등록합니다
        if (_closeButton != null)
        {
            _closeButton.clicked -= OnCloseClicked;
        }

        _closeButton = root.Q<Button>("close-button");

        if (_closeButton != null)
        {
            _closeButton.clicked += OnCloseClicked;
        }
    }

    private void OnCloseClicked() => Close(destroy: false);
}
```

`UiToolkitPanelRendererFeature` 는 `Document` 대신 `Panel`(`PanelRenderer`)을 노출하는 점만 다르고 `Root`, `AddVisualTreeAttachedListener`, `RemoveVisualTreeAttachedListener` 사용법은 같습니다. 서비스의 레이어 정렬은 `Canvas` 와 `UIDocument` 에만 적용되고 `PanelRenderer` 에는 적용되지 않습니다.

### 커스텀 피처

```csharp
using Cysharp.Threading.Tasks;
using Geuneda.UiService;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeFeature : PresenterFeatureBase, ITransitionFeature
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _duration = 0.3f;

    private UniTaskCompletionSource _openCompletion;
    private UniTaskCompletionSource _closeCompletion;

    public UniTask OpenTransitionTask => _openCompletion?.Task ?? UniTask.CompletedTask;
    public UniTask CloseTransitionTask => _closeCompletion?.Task ?? UniTask.CompletedTask;

    public override void OnPresenterOpening()
    {
        _canvasGroup.alpha = 0f;
    }

    public override void OnPresenterOpened()
    {
        // 프레젠터가 태스크를 읽기 전에 CompletionSource 를 만들어 둡니다
        _openCompletion = new UniTaskCompletionSource();
        FadeAsync(0f, 1f, _openCompletion).Forget();
    }

    public override void OnPresenterClosing()
    {
        _closeCompletion = new UniTaskCompletionSource();
        FadeAsync(1f, 0f, _closeCompletion).Forget();
    }

    private async UniTaskVoid FadeAsync(float from, float to, UniTaskCompletionSource completion)
    {
        var elapsed = 0f;
        while (elapsed < _duration)
        {
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _duration);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        _canvasGroup.alpha = to;
        completion.TrySetResult();
    }
}
```

프레젠터는 `OnPresenterOpened()` 직후, `OnPresenterClosed()` 직후에 전환 태스크를 읽습니다. 그 시점까지 `UniTaskCompletionSource` 를 만들어 두지 않으면 이전 태스크나 완료된 태스크를 읽게 됩니다.

---

## UI 레이어

- 각 `UiConfig.Layer` 값이 프리팹 루트의 `Canvas.sortingOrder` 또는 `UIDocument.sortingOrder` 로 설정됩니다. 값이 클수록 위에 그려집니다.
- 레이어별 컨테이너 GameObject 는 없습니다. 모든 프레젠터는 `"Ui"` 루트 바로 아래에 생성됩니다.
- `CloseAllUi(int layer)` 로 한 레이어의 표시 중인 UI 를 모두 닫습니다.
- `Init()` 은 0 미만 또는 1000 초과 레이어에 경고를 남기지만 막지는 않습니다.

---

## UI 세트

UI 세트는 함께 다루는 프레젠터 인스턴스 묶음입니다(`UiSetConfig`: `SetId` + `UiInstanceId[]`).

```csharp
UiPresenter[] opened = await _uiService.OpenUiSetAsync(setId: 2);   // 병렬로 로드 후 열기
_uiService.CloseAllUiSet(setId: 2);
_uiService.UnloadUiSet(setId: 2);

// 열지 않고 미리 로드 (이미 로드된 항목은 건너뜀)
await UniTask.WhenAll(_uiService.LoadUiSetAsync(setId: 2));

// 언로드하지 않고 서비스에서만 제거한 뒤 프레젠터 목록을 돌려받음
List<UiPresenter> removed = _uiService.RemoveUiSet(setId: 2);
```

### 에디터에서 세트 편집

`UiConfigs` 인스펙터에는 enum 값마다 세트 목록이 하나씩 표시되고, 목록 순서가 세트 ID(0부터)입니다. 기본 enum 은 `GeunedaEditor.UiService.DefaultUiSetId` 입니다.

| ID | 이름 |
|----|------|
| 0 | `InitialLoading` |
| 1 | `MainMenu` |
| 2 | `Gameplay` |
| 3 | `Settings` |
| 4 | `Overlays` |
| 5 | `Popups` |

세트 항목은 드롭다운에서 설정의 주소를 고르며, 프레젠터 타입 이름(`AssemblyQualifiedName`)과 인스턴스 주소(= 고른 주소)가 저장됩니다. 인스턴스 주소가 설정 주소와 같으므로 세트로 연 UI 와 `OpenUiAsync<T>()` 로 연 UI 는 같은 인스턴스입니다.

세트 이름을 바꾸려면 Editor 폴더에 자체 enum 과 커스텀 에디터를 만듭니다. enum 값은 0부터 연속으로 둡니다.

```csharp
using Geuneda.UiService;
using GeunedaEditor.UiService;
using UnityEditor;

public enum GameUiSetId
{
    Hud = 0,
    Shop = 1,
}

[CustomEditor(typeof(AddressablesUiConfigs))]
public class GameUiConfigsEditor : AddressablesUiConfigsEditor<GameUiSetId>
{
}
```

Resources 와 PrefabRegistry 는 각각 `ResourcesUiConfigsEditor<TSet>`, `PrefabRegistryUiConfigsEditor<TSet>` 를 상속합니다. 패키지에도 같은 타입을 대상으로 하는 기본 에디터(`DefaultAddressablesUiConfigsEditor` 등)가 있으므로, 적용 후 인스펙터에 새 세트 이름이 보이는지 확인하세요.

### 런타임에 세트 추가

```csharp
_uiService.AddUiSet(new UiSetConfig
{
    SetId = 10,
    UiInstanceIds = new[]
    {
        new UiInstanceId(typeof(ShopPresenter), "UI/Shop"),            // 설정 주소와 같게 두면 기본 인스턴스와 공유
        new UiInstanceId(typeof(InventoryPresenter), "UI/Inventory"),
    }
});
```

---

## 다중 인스턴스

`UiInstanceId` 는 프레젠터 타입과 인스턴스 주소의 조합입니다. `null` 과 빈 문자열은 모두 `string.Empty` 로 정규화됩니다.

- 설정(`UiConfig`)은 **타입당 하나**입니다. 같은 타입의 모든 인스턴스는 같은 프리팹 주소와 레이어를 사용하고, 인스턴스 주소로만 구분됩니다.
- 주소 없이 `LoadUiAsync`/`OpenUiAsync` 를 호출하면 인스턴스 주소로 **설정의 주소**(`UiConfig.Address`)가 사용됩니다. 주소 없이 `AddUi` 로 추가하면 빈 문자열이 사용됩니다. 따라서 `UiInstanceId.IsDefault`(빈 주소 여부)는 설정으로 로드한 기본 인스턴스에서 `false` 입니다.
- 주소 없이 호출하는 `GetUi<T>()`, `IsVisible<T>()`, `CloseUi<T>()`, `UnloadUi<T>()`, `RemoveUi<T>()` 는 로드된 인스턴스가 하나면 그것을, 여러 개면 경고를 남기고 첫 번째를 대상으로 합니다.

```csharp
// 주소 오버로드 대부분은 UiService 에만 있으므로 구체 타입으로 보관합니다
var uiService = new UiService();
uiService.Init(_uiConfigs);

await uiService.OpenUiAsync(typeof(TooltipPresenter), "item");
await uiService.OpenUiAsync(typeof(TooltipPresenter), "skill");

var itemTooltip = uiService.GetUi<TooltipPresenter>("item");
bool skillVisible = uiService.IsVisible<TooltipPresenter>("skill");

uiService.CloseUi(typeof(TooltipPresenter), "item");               // IUiService 에도 있음
uiService.UnloadUi(typeof(TooltipPresenter), "skill");             // IUiService 에도 있음
```

| 구조체 | 내용 |
|--------|------|
| `UiInstanceId` | `PresenterType`, `InstanceAddress`, `IsDefault`, `Default(Type)`, `Named(Type, string)` |
| `UiInstance` | 로드된 인스턴스 정보. `Type`, `Address`, `Presenter` (`GetLoadedPresenters()` 의 항목) |

---

## UI 설정

`UiConfig` 필드:

| 필드 | 설명 |
|------|------|
| `Address` | 로더가 프리팹을 찾는 키 (로더별 의미는 [시작하기](getting-started.md#1-로더와-설정-에셋-고르기)) |
| `Layer` | 정렬 레이어 |
| `UiType` | 프레젠터 타입 |
| `LoadSynchronously` | `AddressablesUiAssetLoader` 가 `WaitForCompletion()` 으로 동기 로드할지 여부 |

`UiConfigs` 에셋에는 `Address`, `Layer`, 타입 이름(`AssemblyQualifiedName`)만 저장됩니다. `LoadSynchronously` 는 저장되지 않아 에셋에서 읽은 설정은 항상 `false` 입니다. `[LoadSynchronously]` 어트리뷰트는 설정 인스펙터의 표시(`[SYNC]`)와 통계에만 반영됩니다. 동기 로드가 필요하면 `AddUiConfig` 로 `LoadSynchronously = true` 인 설정을 직접 등록합니다.

### 런타임 설정

```csharp
_uiService.AddUiConfig(new UiConfig
{
    Address = "UI/DynamicPopup",
    Layer = 3,
    UiType = typeof(DynamicPopupPresenter),
    LoadSynchronously = false,
});

// 이미 인스턴스화된 프레젠터를 서비스에 추가
var popup = Instantiate(_popupPrefab).GetComponent<DynamicPopupPresenter>();
_uiService.AddUi(popup, layer: 3, openAfter: true);
```

- 같은 타입의 설정을 다시 추가하면 경고만 남고 무시됩니다.
- `AddUi` 로 추가한 프레젠터도 `UnloadUi`, `CloseAllUi(int layer)`, `Close(destroy: true)` 를 쓰려면 같은 타입의 `UiConfig` 가 등록되어 있어야 합니다. 서비스가 이 메서드들에서 타입의 설정을 조회하기 때문입니다.
- `AddUi` 의 인스턴스 주소는 빈 문자열이고 `OpenUiAsync<T>()` 는 설정 주소를 사용하므로, `AddUi` 로 넣은 프레젠터를 나중에 열려면 `openAfter: true` 를 쓰거나 `UiService.OpenUiAsync(typeof(T), string.Empty)` 로 엽니다.

---

## 에디터 도구

| 메뉴 / 위치 | 기능 |
|-------------|------|
| `Tools > UI Service > Select UiConfigs` | 프로젝트의 첫 `UiConfigs` 에셋을 선택. 없으면 `Assets/AddressablesUiConfigs.asset` 생성 |
| `Tools > UI Service > Layer Visualizer` | 위 에셋을 선택하고 레이어 시각화 섹션을 펼침 |
| `Tools > UI Service > Presenter Manager` | 플레이 모드에서 로드된 프레젠터와 열림/닫힘 상태 표시, 인스턴스별 열기/닫기/언로드, 모두 닫기, 모두 언로드 |
| `UiConfigs` 인스펙터 | 레이어별 시각화(검색, 통계), 설정 목록과 레이어 편집, UI 세트 편집 |
| `UiPresenter` 인스펙터 | 플레이 모드에서 `UI 열기`, `UI 닫기`, `닫기 및 제거` 버튼 |

`UiPresenter` 인스펙터의 버튼은 서비스를 거치지 않고 프레젠터를 직접 열고 닫으므로 `VisiblePresenters` 에 반영되지 않습니다. 서비스 상태까지 맞춰야 하면 Presenter Manager 를 사용하세요.
