# API 레퍼런스

UI Service의 전체 API 문서입니다.

## 목차

- [IUiService 인터페이스](#iuiservice-인터페이스)
- [로딩 및 언로딩](#로딩-및-언로딩)
- [열기 및 닫기](#열기-및-닫기)
- [UI 세트 작업](#ui-세트-작업)
- [쿼리 메서드](#쿼리-메서드)
- [비동기 작업](#비동기-작업)
- [런타임 설정](#런타임-설정)

---

## IUiService 인터페이스

모든 UI 작업을 위한 메인 인터페이스입니다.

### 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|---------|------|------|
| `VisiblePresenters` | `IReadOnlyList<UiInstanceId>` | 현재 보이는 모든 프레젠터 인스턴스 |
| `UiSets` | `IReadOnlyDictionary<int, UiSetConfig>` | 등록된 모든 UI 세트 |

### 메서드 개요

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `GetLoadedPresenters()` | `List<UiInstance>` | 현재 메모리에 있는 모든 프레젠터 |
| `GetUi<T>()` | `T` | 타입으로 로드된 프레젠터 가져오기 |
| `IsVisible<T>()` | `bool` | 프레젠터가 보이는지 확인 |
| `LoadUiAsync<T>()` | `UniTask<T>` | 프레젠터를 메모리에 로드 |
| `OpenUiAsync<T>()` | `UniTask<T>` | 프레젠터 열기 (필요 시 로드) |
| `CloseUi<T>()` | `void` | 프레젠터 닫기 |
| `UnloadUi<T>()` | `void` | 메모리에서 프레젠터 언로드 |

---

## 로딩 및 언로딩

### LoadUiAsync

UI 프레젠터를 열지 않고 메모리에 로드합니다.

```csharp
// 메모리에 로드 (숨김 유지)
var inventory = await _uiService.LoadUiAsync<InventoryPresenter>();

// 로드 후 즉시 열기
var inventory = await _uiService.LoadUiAsync<InventoryPresenter>(openAfter: true);

// 취소 지원
var cts = new CancellationTokenSource();
var inventory = await _uiService.LoadUiAsync<InventoryPresenter>(
    openAfter: false,
    cancellationToken: cts.Token
);
```

**시그니처:**
```csharp
UniTask<T> LoadUiAsync<T>(bool openAfter = false, CancellationToken cancellationToken = default)
    where T : UiPresenter;

UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false, CancellationToken cancellationToken = default);
```

### UnloadUi

메모리에서 프레젠터를 언로드합니다 (Addressables 참조 해제).

```csharp
// 타입으로 언로드
_uiService.UnloadUi<InventoryPresenter>();

// 인스턴스로 언로드
_uiService.UnloadUi(inventoryPresenter);

// Type 객체로 언로드
_uiService.UnloadUi(typeof(InventoryPresenter));
```

**참고:** 보이는 프레젠터를 언로드하면 먼저 닫힙니다.

---

## 열기 및 닫기

### OpenUiAsync

프레젠터를 열고, 필요하면 먼저 로드합니다.

```csharp
// 기본 열기
var shop = await _uiService.OpenUiAsync<ShopPresenter>();

// 초기 데이터와 함께 열기 - 자동으로 OnSetData() 트리거
var questData = new QuestData { QuestId = 101, Title = "Dragon Slayer" };
var quest = await _uiService.OpenUiAsync<QuestPresenter, QuestData>(questData);

// 언제든지 데이터 업데이트 - 역시 OnSetData() 트리거
quest.Data = new QuestData { QuestId = 102, Title = "Updated Quest" };

// 취소 지원
var cts = new CancellationTokenSource();
try
{
    var ui = await _uiService.OpenUiAsync<LoadingPresenter>(cts.Token);
}
catch (OperationCanceledException)
{
    Debug.Log("UI loading was cancelled");
}
```

**시그니처:**
```csharp
UniTask<T> OpenUiAsync<T>(CancellationToken cancellationToken = default)
    where T : UiPresenter;

UniTask<T> OpenUiAsync<T, TData>(TData initialData, CancellationToken cancellationToken = default)
    where T : class, IUiPresenterData
    where TData : struct;
```

### CloseUi

보이는 프레젠터를 닫습니다.

```csharp
// 닫기 (메모리에 유지, 빠른 재열기)
_uiService.CloseUi<ShopPresenter>();
_uiService.CloseUi<ShopPresenter>(destroy: false);

// 닫기 및 메모리에서 언로드
_uiService.CloseUi<ShopPresenter>(destroy: true);

// 인스턴스로 닫기
_uiService.CloseUi(shopPresenter);
_uiService.CloseUi(shopPresenter, destroy: true);
```

**전환 동작:**
- 프레젠터에 `ITransitionFeature` 컴포넌트(예: `TimeDelayFeature`, `AnimationDelayFeature`)가 있으면, 닫기는 모든 전환이 완료된 후 GameObject를 숨깁니다.
- `presenter.CloseTransitionTask`를 사용하여 전환을 포함한 전체 닫기 프로세스를 대기할 수 있습니다.

```csharp
// 닫기 전환 완료 대기
_uiService.CloseUi<AnimatedPopup>();
await presenter.CloseTransitionTask;
Debug.Log("Popup fully closed with animation");
```

### CloseAllUi

여러 프레젠터를 한 번에 닫습니다.

```csharp
// 보이는 모든 UI 닫기
_uiService.CloseAllUi();

// 특정 레이어의 모든 UI 닫기
_uiService.CloseAllUi(layer: 2);
```

---

## UI 세트 작업

### LoadUiSetAsync

세트의 모든 프레젠터를 로드합니다.

```csharp
// 태스크 배열 반환 - 병렬로 로드
IList<UniTask<UiPresenter>> loadTasks = _uiService.LoadUiSetAsync(setId: 1);

// 모두 완료될 때까지 대기
var presenters = await UniTask.WhenAll(loadTasks);

// 또는 완료되는 대로 처리
foreach (var task in loadTasks)
{
    var presenter = await task;
    Debug.Log($"Loaded: {presenter.name}");
}
```

### OpenUiSetAsync

세트의 모든 프레젠터를 열고, 필요하면 로드합니다.

```csharp
// 모든 UI를 병렬로 열고, 모두 열릴 때까지 대기
UiPresenter[] presenters = await _uiService.OpenUiSetAsync(setId: 1);

// 취소 지원
var cts = new CancellationTokenSource();
var presenters = await _uiService.OpenUiSetAsync(setId: 1, cts.Token);
```

**시그니처:**
```csharp
UniTask<UiPresenter[]> OpenUiSetAsync(int setId, CancellationToken cancellationToken = default);
```

**참고:** 이 메서드는 적절한 주소 처리를 보장하여 이후 `CloseAllUiSet`과 `UnloadUiSet`이 올바르게 작동합니다.

### CloseAllUiSet

세트의 모든 프레젠터를 닫습니다.

```csharp
_uiService.CloseAllUiSet(setId: 1);
```

### UnloadUiSet

메모리에서 세트의 모든 프레젠터를 언로드합니다.

```csharp
_uiService.UnloadUiSet(setId: 1);
```

### RemoveUiSet

세트의 모든 프레젠터를 제거하고 반환합니다.

```csharp
List<UiPresenter> removed = _uiService.RemoveUiSet(setId: 2);

// 필요한 경우 수동으로 정리
foreach (var presenter in removed)
{
    Destroy(presenter.gameObject);
}
```

---

## 쿼리 메서드

### GetUi

타입으로 로드된 프레젠터를 가져옵니다.

```csharp
var hud = _uiService.GetUi<GameHudPresenter>();

if (hud != null)
{
    hud.UpdateScore(newScore);
}
```

**예외:** 로드되지 않은 경우 `KeyNotFoundException` 발생.

### IsVisible

프레젠터가 현재 보이는지 확인합니다.

```csharp
if (_uiService.IsVisible<MainMenuPresenter>())
{
    Debug.Log("Main menu is showing");
}

// 중복을 피하기 위해 열기 전에 확인
if (!_uiService.IsVisible<PauseMenu>())
{
    await _uiService.OpenUiAsync<PauseMenu>();
}
```

### GetLoadedPresenters

현재 메모리에 로드된 모든 프레젠터를 가져옵니다.

```csharp
List<UiInstance> loaded = _uiService.GetLoadedPresenters();

foreach (var instance in loaded)
{
    Debug.Log($"Loaded: {instance.Type.Name} [{instance.Address}]");
}

// 특정 타입이 로드되었는지 확인
bool isLoaded = loaded.Any(p => p.Type == typeof(InventoryPresenter));
```

### VisiblePresenters

현재 보이는 모든 프레젠터를 가져옵니다.

```csharp
IReadOnlyList<UiInstanceId> visible = _uiService.VisiblePresenters;

Debug.Log($"Visible count: {visible.Count}");

foreach (var id in visible)
{
    Debug.Log($"Visible: {id}");
}
```

---

## 비동기 작업

모든 비동기 작업은 더 나은 성능과 WebGL 호환성을 위해 UniTask를 사용합니다.

### 순차 로딩

```csharp
// 하나씩 로드
var menu = await _uiService.OpenUiAsync<MainMenuPresenter>();
var settings = await _uiService.OpenUiAsync<SettingsPresenter>();
```

### 병렬 로딩

```csharp
// 여러 UI를 동시에 로드 (더 빠름)
var menuTask = _uiService.OpenUiAsync<MainMenuPresenter>();
var hudTask = _uiService.OpenUiAsync<GameHudPresenter>();
var chatTask = _uiService.OpenUiAsync<ChatPresenter>();

await UniTask.WhenAll(menuTask, hudTask, chatTask);

// 결과 접근
var menu = menuTask.GetAwaiter().GetResult();
```

### 취소

```csharp
var cts = new CancellationTokenSource();

// 로딩 시작
var loadTask = _uiService.OpenUiAsync<HeavyPresenter>(cts.Token);

// 타임아웃 후 취소
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    var presenter = await loadTask;
}
catch (OperationCanceledException)
{
    Debug.Log("Loading was cancelled");
}

// 또는 수동으로 취소
cts.Cancel();
```

### Fire-and-Forget

```csharp
// await가 필요 없을 때
_uiService.OpenUiAsync<NotificationPresenter>().Forget();
```

---

## 런타임 설정

### AddUiConfig

런타임에 UI 설정을 추가합니다.

```csharp
// 참고: 실제 UiConfig 생성자는 다를 수 있습니다
var config = new UiConfig
{
    PresenterType = typeof(DynamicPopup),
    AddressableAddress = "UI/DynamicPopup",
    Layer = 3,
    LoadSynchronously = false
};

_uiService.AddUiConfig(config);
```

### AddUiSet

런타임에 UI 세트 설정을 추가합니다.

```csharp
var setConfig = new UiSetConfig(setId: 10);
_uiService.AddUiSet(setConfig);
```

### AddUi

이미 인스턴스화된 프레젠터를 서비스에 추가합니다.

```csharp
// 수동으로 인스턴스화
var dynamicUi = Instantiate(uiPrefab).GetComponent<UiPresenter>();

// 서비스에 추가
_uiService.AddUi(dynamicUi, layer: 3, openAfter: true);
```

### RemoveUi

언로드하지 않고 서비스에서 프레젠터를 제거합니다.

```csharp
// 타입으로 제거
bool removed = _uiService.RemoveUi<DynamicPopup>();

// 인스턴스로 제거
bool removed = _uiService.RemoveUi(dynamicPresenter);

// Type 객체로 제거
bool removed = _uiService.RemoveUi(typeof(DynamicPopup));
```

---

## IUiServiceInit 인터페이스

초기화와 해제를 위한 확장 인터페이스입니다.

### Init

설정으로 서비스를 초기화합니다.

```csharp
IUiServiceInit uiService = new UiService();
uiService.Init(uiConfigs);

// 또는 커스텀 로더 사용
var loader = new AddressablesUiAssetLoader();
IUiServiceInit uiService = new UiService(loader);
uiService.Init(uiConfigs);

// 다른 내장 로더 사용
var prefabLoader = new PrefabRegistryUiAssetLoader();
var resourcesLoader = new ResourcesUiAssetLoader();
```

### IUiAssetLoader 인터페이스

이 인터페이스를 사용하여 커스텀 에셋 로딩 전략을 구현할 수 있습니다.

```csharp
public interface IUiAssetLoader
{
    UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken ct = default);
    void UnloadAsset(GameObject asset);
}
```

### Dispose

모든 리소스를 정리합니다.

```csharp
// 모든 UI를 닫고, 에셋을 언로드하고, 루트 GameObject를 파괴합니다
uiService.Dispose();
```

**참고:** 서비스 사용이 끝나면 항상 `Dispose()`를 호출하세요 (예: `OnDestroy`에서).
