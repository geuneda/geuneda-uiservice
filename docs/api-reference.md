# API 레퍼런스

런타임 네임스페이스는 `Geuneda.UiService`, 헬퍼 뷰는 `Geuneda.UiService.Views` 입니다. 비동기 메서드는 모두 UniTask(`Cysharp.Threading.Tasks`)를 반환합니다. 헬퍼 뷰는 [고급 주제](advanced.md#헬퍼-뷰)에 있습니다.

- [IUiService](#iuiservice)
- [IUiServiceInit](#iuiserviceinit)
- [UiService](#uiservice)
- [UiPresenter](#uipresenter)
- [팝업 큐](#팝업-큐)
- [피처](#피처)
- [설정 타입](#설정-타입)
- [에셋 로더](#에셋-로더)

---

## IUiService

### 프로퍼티와 조회

| 멤버 | 설명 |
|------|------|
| `IReadOnlyList<UiInstanceId> VisiblePresenters { get; }` | 표시 중인 인스턴스 목록 (할당 없는 읽기 전용 래퍼) |
| `IReadOnlyDictionary<int, UiSetConfig> UiSets { get; }` | 등록된 UI 세트 |
| `List<UiInstance> GetLoadedPresenters()` | 로드된 모든 인스턴스. 호출마다 새 리스트 생성 |
| `T GetUi<T>() where T : UiPresenter` | 로드된 프레젠터. 없으면 `KeyNotFoundException` |
| `bool IsVisible<T>() where T : UiPresenter` | 표시 목록에 있는지 여부 |

### 설정과 등록

| 시그니처 | 설명 |
|----------|------|
| `void AddUiConfig(UiConfig config)` | 설정 추가. 같은 타입이 이미 있으면 경고 후 무시 |
| `void AddUiSet(UiSetConfig uiSet)` | 세트 추가. 같은 ID 가 이미 있으면 경고 후 무시 |
| `void AddUi<T>(T ui, int layer, bool openAfter = false) where T : UiPresenter` | 이미 인스턴스화된 프레젠터를 빈 인스턴스 주소로 추가 |
| `bool RemoveUi<T>() where T : UiPresenter` | 언로드하지 않고 서비스에서 제거 |
| `bool RemoveUi<T>(T uiPresenter) where T : UiPresenter` | 해당 인스턴스를 제거 |
| `bool RemoveUi(Type type)` | 타입으로 제거 |
| `List<UiPresenter> RemoveUiSet(int setId)` | 세트의 로드된 프레젠터를 제거하고 반환. 세트가 없으면 `KeyNotFoundException` |

`RemoveUi` 계열은 표시 목록에서도 빼지만 닫기 생명주기를 실행하지 않고 GameObject 도 그대로 둡니다. 진행 중이던 열기·닫기 전환 태스크는 완료됩니다(`UnloadUi` 도 같음).

### 로드와 언로드

| 시그니처 | 설명 |
|----------|------|
| `UniTask<T> LoadUiAsync<T>(bool openAfter = false, CancellationToken cancellationToken = default) where T : UiPresenter` | 설정 주소를 인스턴스 주소로 로드 |
| `UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false, CancellationToken cancellationToken = default)` | 타입으로 로드 |
| `IList<UniTask<UiPresenter>> LoadUiSetAsync(int setId)` | 세트에서 아직 로드되지 않은 항목의 로드 태스크 목록. 세트가 없으면 빈 목록 |
| `void UnloadUi<T>() where T : UiPresenter` | 언로드 |
| `void UnloadUi<T>(T uiPresenter) where T : UiPresenter` | 해당 인스턴스 언로드 |
| `void UnloadUi(Type type)` | 타입으로 언로드 |
| `void UnloadUi(Type type, string instanceAddress)` | 특정 인스턴스 언로드 |
| `void UnloadUiSet(int setId)` | 세트의 로드된 항목을 언로드 |

- 설정이 없는 타입을 로드하면 `KeyNotFoundException` 이 납니다.
- 이미 로드된 인스턴스를 다시 로드하면 경고를 남기고 기존 인스턴스를 반환합니다.
- 로드되지 않은 인스턴스를 언로드하면 `KeyNotFoundException` 이 납니다. 언로드는 닫기 생명주기를 실행하지 않고 표시 목록에서 뺀 뒤 로더의 `UnloadAsset` 을 호출합니다. 훅이 필요하면 `CloseUi(..., destroy: true)` 를 사용하세요.

### 열기와 닫기

| 시그니처 | 설명 |
|----------|------|
| `UniTask<T> OpenUiAsync<T>(CancellationToken cancellationToken = default) where T : UiPresenter` | 필요하면 로드한 뒤 열기 |
| `UniTask<UiPresenter> OpenUiAsync(Type type, CancellationToken cancellationToken = default)` | 타입으로 열기 |
| `UniTask<T> OpenUiAsync<T, TData>(TData initialData, CancellationToken cancellationToken = default) where T : class, IUiPresenterData where TData : struct` | `Data` 를 설정한 뒤 열기 |
| `UniTask<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData, CancellationToken cancellationToken = default) where TData : struct` | 타입으로 데이터와 함께 열기 |
| `UniTask<UiPresenter[]> OpenUiSetAsync(int setId, CancellationToken cancellationToken = default)` | 세트 전체를 `UniTask.WhenAll` 로 병렬 열기. 세트가 없으면 `KeyNotFoundException` |
| `void CloseUi<T>(bool destroy = false) where T : UiPresenter` | 닫기. `destroy: true` 면 닫기 전환 후 언로드 |
| `void CloseUi<T>(T uiPresenter, bool destroy = false) where T : UiPresenter` | 해당 인스턴스 닫기 |
| `void CloseUi(Type type, bool destroy = false)` | 타입으로 닫기 |
| `void CloseUi(Type type, string instanceAddress, bool destroy = false)` | 특정 인스턴스 닫기 |
| `void CloseAllUi()` | 표시 중인 모든 UI 닫기. 닫는 도중 콜백에서 연 UI 는 닫지 않음 |
| `void CloseAllUi(int layer)` | 해당 레이어의 표시 중인 UI 닫기. 닫는 도중 콜백에서 연 UI 는 닫지 않음 |
| `void CloseAllUiSet(int setId)` | 세트의 항목 닫기. 세트가 없으면 `KeyNotFoundException` |

- 이미 열린 인스턴스를 열거나 열려 있지 않은 인스턴스를 닫으면 경고만 남깁니다. `CloseAllUiSet` 은 열리지 않은 항목마다 경고를 남깁니다.
- 열기 메서드는 열기를 시작한 직후 완료됩니다. 전환까지 기다리려면 반환된 프레젠터의 `OpenTransitionTask` 를 기다립니다.
- 닫기 전환 중에 다시 열면 그 닫기는 숨기거나 언로드하지 않고(`destroy: true` 여도) `OnCloseTransitionCompleted` 도 부르지 않습니다. 열기 전환 중에 닫으면 `OnOpenTransitionCompleted` 를 부르지 않습니다. `TimeDelayFeature`, `AnimationDelayFeature` 도 같은 규칙으로 늦게 끝난 전환의 완료 훅을 건너뛰고, 전환 태스크는 모두 완료됩니다.
- `destroy: true` 닫기는 언로드를 마친 뒤 `CloseTransitionTask` 를 완료합니다.
- 열고 닫을 때마다 UIDocument 프레젠터의 `sortingOrder` 는 `레이어 + 순번 × 1/1024` 로, uGUI 프레젠터의 루트 `Canvas.sortingOrder` 는 `레이어 + 순번`(다음 설정 레이어 값보다 작게, 3.1.0) 으로 다시 매겨집니다([화면 맨 위](navigation.md#화면-맨-위)).
- `AddressablesUiAssetLoader` 로 로드하다 취소하면, 인스턴스화가 끝나는 대로 그 인스턴스를 해제합니다.
- 데이터 오버로드에서 프레젠터가 `UiPresenter<TData>` 가 아니면 에러 로그를 남기고 **열지 않은 채** 프레젠터를 반환합니다.
- `CancellationToken` 은 `AddressablesUiAssetLoader` 의 비동기 인스턴스화 대기에만 전달됩니다. Resources/PrefabRegistry 로더는 동기로 인스턴스화하므로 토큰을 쓰지 않습니다.

```csharp
// 여러 UI 를 병렬로 열고 결과를 받기
var (hud, minimap) = await UniTask.WhenAll(
    _uiService.OpenUiAsync<HudPresenter>(),
    _uiService.OpenUiAsync<MinimapPresenter>());

// 닫기 전환이 끝날 때까지 기다리기
var popup = _uiService.GetUi<AnimatedPopup>();
_uiService.CloseUi(popup);
await popup.CloseTransitionTask;
```

UniTask 는 한 번만 `await` 할 수 있습니다. `UniTask.WhenAll` 로 기다린 태스크를 다시 `await` 하거나 `GetAwaiter().GetResult()` 로 읽지 마세요.

### 표시 상태 이벤트와 조회

| 멤버 | 설명 |
|------|------|
| `event Action<UiInstanceId> PresenterOpened` | 표시 목록에 들어간 직후, `OnOpened` 보다 먼저 |
| `event Action<UiInstanceId> PresenterClosed` | 표시 목록에서 빠진 직후, `OnClosed` 보다 먼저. 이때 `CloseTransitionTask` 는 이번 닫기의 태스크. 표시 중인 프레젠터를 `RemoveUi`/`UnloadUi` 해도 발생 |
| `bool TryGetUi(UiInstanceId instanceId, out UiPresenter presenter)` | 로드된 프레젠터를 인스턴스 ID 로 찾음 |

구독자 예외는 `Geuneda.UiService` 로거로 기록하고(Error 줄 + 예외) 다른 구독자에게 계속 전달합니다. `Dispose()` 중에는 발생하지 않습니다. 구독자나 훅 안에서 연 UI·닫은 UI 의 알림과 훅은 지금 처리 중인 알림이 끝난 뒤에 실행됩니다([표시 상태 이벤트](navigation.md#표시-상태-이벤트)).

### 뒤로가기

| 멤버 | 설명 |
|------|------|
| `bool HandleBack()` | 뒤로가기 처리. 막혀 있으면 false. 핸들러 → 화면 맨 위 `HandlesBack` 프레젠터의 `OnBackPressed` → `BackUnhandled` 순서 |
| `IDisposable AddBackHandler(Func<bool> handler, int priority = 0)` | 프레젠터보다 먼저 받을 핸들러. 큰 우선순위 먼저, 같으면 나중에 추가한 것 먼저. true 면 처리 끝. `handler` 가 null 이면 `ArgumentNullException` |
| `event Action BackUnhandled` | 처리할 핸들러와 프레젠터가 없을 때. 종료 확인 팝업용 |

### 입력 차단

| 멤버 | 설명 |
|------|------|
| `UiInputBlock BlockInput(string reason)` | 모든 UI 입력을 막는 토큰. 포인터는 차단막으로, 선택·포커스 입력은 EventSystem 선택 해제·내비게이션 끄기와 UIDocument 패널 키 이벤트 차단으로 막음(막힌 동안 새 EventSystem·패널 포함, 선택은 복구하지 않음). `reason` 이 null 이면 `ArgumentNullException` |
| `bool IsInputBlocked { get; }` | 토큰이 하나라도 남아 있는지 |
| `IReadOnlyList<string> InputBlockReasons { get; }` | 남은 토큰의 사유 |

`public readonly struct UiInputBlock : IDisposable` 은 할당 없는 토큰입니다. 두 번 Dispose 하면 경고만 남깁니다. `IUiService` 를 직접 구현할 때는 `new UiInputBlock(Action<int> release, int id)` 로 만듭니다.

동작과 예제는 [뒤로가기, 팝업 큐, 모달 dim, 입력 차단](navigation.md)을 보세요.

---

## IUiServiceInit

`public interface IUiServiceInit : IUiService, IDisposable`

| 시그니처 | 설명 |
|----------|------|
| `void Init(UiConfigs configs)` | 설정과 세트를 등록하고 `"Ui"` 루트 GameObject(`DontDestroyOnLoad`, `UiServiceMonoComponent` 부착)를 생성 |
| `void Dispose()` | 표시 중인 UI 를 닫고, 로드된 인스턴스를 모두 언로드하고, 이벤트 구독·뒤로가기 핸들러·차단막·dim 을 정리하고, 컬렉션을 비우고, 루트 GameObject 를 파괴 |

`Init` 예외: `configs` 가 `null` 이면 `ArgumentNullException`, 설정에 빈 주소나 찾을 수 없는 타입이 있으면 `ArgumentException`. 레이어가 0 미만 또는 1000 초과면 경고만 남깁니다.

---

## UiService

`public class UiService : IUiServiceInit`

### 생성자

| 시그니처 | 설명 |
|----------|------|
| `UiService()` | `AddressablesUiAssetLoader` 사용 |
| `UiService(IUiAssetLoader assetLoader)` | 로더 주입 |

### 프로퍼티

| 멤버 | 설명 |
|------|------|
| `Color DimColor { get; set; }` | 모달 dim 색. 기본 `(0, 0, 0, 0.6)` |
| `bool ReadsBackInput { get; set; }` | true 면 Escape(Android 뒤로가기 포함)를 읽어 `HandleBack()` 호출. 기본 false. Init 전후 언제든 변경 가능 |

### 정적 이벤트

| 멤버 | 설명 |
|------|------|
| `static readonly UnityEvent<DeviceOrientation, DeviceOrientation> OnOrientationChanged` | (이전, 현재) 기기 방향. `Unknown`, `FaceUp`, `FaceDown` 은 무시 |
| `static readonly UnityEvent<Vector2, Vector2> OnResolutionChanged` | (이전, 현재) `Screen.width/height` |

정적 이벤트이므로 `Dispose()` 가 리스너를 지우지 않습니다. 구독한 쪽에서 직접 해제하세요.

### 인스턴스 주소 오버로드 (UiService 전용)

| 시그니처 |
|----------|
| `T GetUi<T>(string instanceAddress) where T : UiPresenter` |
| `bool IsVisible<T>(string instanceAddress) where T : UiPresenter` |
| `void AddUi<T>(T ui, int layer, string instanceAddress, bool openAfter = false) where T : UiPresenter` |
| `bool RemoveUi(Type type, string instanceAddress)` |
| `UniTask<UiPresenter> LoadUiAsync(Type type, string instanceAddress, bool openAfter = false, CancellationToken cancellationToken = default)` |
| `UniTask<UiPresenter> OpenUiAsync(Type type, string instanceAddress, CancellationToken cancellationToken = default)` |
| `UniTask<UiPresenter> OpenUiAsync<TData>(Type type, string instanceAddress, TData initialData, CancellationToken cancellationToken = default) where TData : struct` |

`CloseUi(Type, string, bool)` 과 `UnloadUi(Type, string)` 은 `IUiService` 에도 있습니다. 주소 없는 오버로드의 인스턴스 선택 규칙은 [핵심 개념 - 다중 인스턴스](core-concepts.md#다중-인스턴스)를 보세요.

---

## UiPresenter

`public abstract class UiPresenter : MonoBehaviour`

| 멤버 | 설명 |
|------|------|
| `bool IsOpen { get; }` | `gameObject.activeSelf` |
| `UniTask OpenTransitionTask { get; }` | 최근 열기 과정(전환 포함)이 끝나면 완료 |
| `UniTask CloseTransitionTask { get; }` | 최근 닫기 과정(전환 포함)이 끝나면 완료 |
| `protected IUiService _uiService` | 이 프레젠터를 관리하는 서비스 |
| `protected void Close(bool destroy)` | 자신의 인스턴스를 닫음 |
| `protected virtual void OnInitialized()` | 서비스에 추가될 때 한 번 |
| `protected virtual void OnOpened()` | 열릴 때 (활성화 직후) |
| `protected virtual void OnClosed()` | 닫힐 때 (비활성화 전) |
| `protected virtual void OnOpenTransitionCompleted()` | 열기 전환이 모두 끝난 뒤 |
| `protected virtual void OnCloseTransitionCompleted()` | 닫기 전환이 끝나고 비활성화된 뒤 |
| `protected virtual bool HandlesBack { get; }` | 뒤로가기 대상인지. 기본 false |
| `protected virtual void OnBackPressed()` | 뒤로가기를 받았을 때. 기본 `Close(false)`. 닫을 수 없는 팝업은 재정의 |
| `protected virtual bool IsModal { get; }` | true 면 맨 위 모달일 때 공용 dim 이 바로 뒤에 깔림. 기본 false |
| `protected virtual void OnDimClicked()` | 뒤의 dim 을 눌렀을 때. 기본 동작 없음 |

`public abstract class UiPresenter<T> : UiPresenter, IUiPresenterData where T : struct`

| 멤버 | 설명 |
|------|------|
| `T Data { get; set; }` | 대입할 때마다 `OnSetData()` 호출 |
| `protected virtual void OnSetData()` | 데이터 반영 지점 |

`public interface IUiPresenterData` 는 `UiPresenter<T>` 가 구현하는 마커 인터페이스입니다.

---

## 팝업 큐

`public interface IUiPopupQueue : IDisposable`, `public sealed class UiPopupQueue : IUiPopupQueue`

| 멤버 | 설명 |
|------|------|
| `UiPopupQueue(IUiService uiService)` | 서비스의 `PresenterClosed` 를 구독. `uiService` 가 null 이면 `ArgumentNullException` |
| `bool Enqueue(string key, int priority, Func<CancellationToken, UniTask<UiPresenter>> factory)` | 예약. 큰 우선순위 먼저, 같으면 넣은 순서. 같은 키가 대기·표시 중이면 false. 팩토리가 null 을 돌려주면 건너뛰고, 예외는 기록 후 다음으로 |
| `bool Remove(string key)` | 대기 중인 항목 빼기 (표시 중인 항목은 빼지 않음) |
| `void Clear()` | 대기 중인 항목 모두 빼기 |
| `UiPopupQueueHold Hold(string reason)` | 멈춤 토큰. 모두 풀리면 진행 |
| `int Count { get; }` | 대기 중인 항목 수 |
| `bool IsShowing { get; }` | 꺼낸 팝업을 여는 중, 표시 중, 닫기 전환 대기 중 |
| `bool IsHeld { get; }`, `IReadOnlyList<string> HoldReasons { get; }` | 멈춤 상태와 사유 |
| `event Action Drained` | 큐가 띄운 팝업이 끝났고 대기 항목도 없을 때 |
| `void Dispose()` | 구독 해제, 대기 항목 버림, 여는 중인 팩토리의 토큰 취소. 띄운 팝업은 닫지 않음 |

- 팝업이 닫히고 닫기 전환까지 끝나야 다음 팝업을 띄웁니다. 닫기 전환 중에 다시 열리면 계속 현재 항목으로 추적합니다.
- `Hold` 가 남아 있거나, 큐 밖에서 연 `HandlesBack` 프레젠터가 표시 중이거나, 같은 서비스의 다른 큐가 팩토리를 돌리는 중이면 기다립니다. 큐 사이는 먼저 시작한 큐가 이기고, 팩토리가 끝나면(실패, 취소, null 포함) 기다리던 큐가 그 큐의 다음 항목보다 먼저 진행합니다(3.1.0, [여러 큐](navigation.md#여러-큐)).
- Dispose 뒤 `Enqueue`, `Hold` 는 `ObjectDisposedException` 을 던집니다.

`public readonly struct UiPopupQueueHold : IDisposable` 은 `UiInputBlock` 과 같은 규칙의 토큰입니다.

---

## 피처

`public abstract class PresenterFeatureBase : MonoBehaviour`

| 멤버 | 호출 시점 |
|------|-----------|
| `protected UiPresenter Presenter { get; }` | `OnPresenterInitialized` 에서 설정 |
| `public virtual void OnPresenterInitialized(UiPresenter presenter)` | 프레젠터 `OnInitialized()` 직전. 재정의 시 `base` 호출 필요(그래야 `Presenter` 가 설정됨) |
| `public virtual void OnPresenterOpening()` | 활성화 전 |
| `public virtual void OnPresenterOpened()` | `OnOpened()` 후 |
| `public virtual void OnPresenterClosing()` | `OnClosed()` 전 |
| `public virtual void OnPresenterClosed()` | `OnClosed()` 후, 닫기 전환 대기 전 |

`public interface ITransitionFeature`

| 멤버 | 설명 |
|------|------|
| `UniTask OpenTransitionTask { get; }` | 진행 중인 열기 전환. 없으면 `UniTask.CompletedTask` |
| `UniTask CloseTransitionTask { get; }` | 진행 중인 닫기 전환. 없으면 `UniTask.CompletedTask` |

### TimeDelayFeature : PresenterFeatureBase, ITransitionFeature

| 멤버 | 설명 |
|------|------|
| `float OpenDelayInSeconds { get; }` | 직렬화 필드 `_openDelayInSeconds`, 기본 0.5 |
| `float CloseDelayInSeconds { get; }` | 직렬화 필드 `_closeDelayInSeconds`, 기본 0.3 |
| `OpenTransitionTask`, `CloseTransitionTask` | `ITransitionFeature` 구현 |
| `protected virtual void OnOpenStarted()`, `OnOpenedCompleted()`, `OnCloseStarted()`, `OnClosedCompleted()` | 딜레이 시작/완료 훅 |

### AnimationDelayFeature : PresenterFeatureBase, ITransitionFeature

`[RequireComponent(typeof(Animation))]`

| 멤버 | 설명 |
|------|------|
| `Animation AnimationComponent { get; }` | 직렬화 필드 `_animation` (`OnValidate` 에서 자동 할당) |
| `AnimationClip IntroAnimationClip { get; }`, `AnimationClip OutroAnimationClip { get; }` | 인트로/아웃트로 클립 |
| `float OpenDelayInSeconds { get; }`, `float CloseDelayInSeconds { get; }` | 클립 길이, 클립이 없으면 0 |
| `OpenTransitionTask`, `CloseTransitionTask` | `ITransitionFeature` 구현 |
| `protected virtual void OnOpenStarted()`, `OnCloseStarted()` | 기본 구현이 해당 클립을 재생 |
| `protected virtual void OnOpenedCompleted()`, `OnClosedCompleted()` | 완료 훅 |

### UiToolkitPresenterFeature : PresenterFeatureBase

`[RequireComponent(typeof(UIDocument))]`

| 멤버 | 설명 |
|------|------|
| `UIDocument Document { get; }` | 부착된 `UIDocument` |
| `VisualElement Root { get; }` | `Document.rootVisualElement` |
| `void AddVisualTreeAttachedListener(UnityAction<VisualElement> callback)` | 트리가 준비되면 호출. 이미 준비돼 있으면 즉시 호출 |
| `void RemoveVisualTreeAttachedListener(UnityAction<VisualElement> callback)` | 등록 해제 |

### UiToolkitPanelRendererFeature : PresenterFeatureBase

Unity 6000.5 이상(`#if UNITY_6000_5_OR_NEWER`). `[RequireComponent(typeof(PanelRenderer))]`

| 멤버 | 설명 |
|------|------|
| `PanelRenderer Panel { get; }` | 부착된 `PanelRenderer` |
| `VisualElement Root { get; }` | 최근 리로드 콜백에서 받은 루트. 아직 없으면 `null` |
| `void AddVisualTreeAttachedListener(UnityAction<VisualElement> callback)` | `PanelRenderer` 리로드마다 호출. 루트가 있으면 즉시 호출 |
| `void RemoveVisualTreeAttachedListener(UnityAction<VisualElement> callback)` | 등록 해제 |

---

## 설정 타입

### UiConfig (struct)

| 필드 | 타입 |
|------|------|
| `Address` | `string` |
| `Layer` | `int` |
| `UiType` | `Type` |
| `LoadSynchronously` | `bool` |

### UiConfigs (abstract ScriptableObject)

| 멤버 | 설명 |
|------|------|
| `List<UiConfig> Configs { get; set; }` | 직렬화된 설정을 변환해 반환. 호출마다 새 리스트 |
| `List<UiSetConfig> Sets { get; }` | 직렬화된 세트를 변환해 반환. 타입을 찾지 못한 항목은 제외 |
| `void SetSetsSize(int size)` | 세트 수를 맞추고, 설정에 없는 타입을 가리키는 항목을 제거 |
| `struct UiConfigSerializable` | 직렬화 형태 (`Address`, `Layer`, 타입 이름 `UiType`, `LoadSynchronously`). `UiConfig` 와 암시적 변환 |

파생 타입과 생성 메뉴:

| 타입 | `Create` 메뉴 | 추가 멤버 |
|------|---------------|-----------|
| `AddressablesUiConfigs` | `ScriptableObjects/Configs/UiConfigs/Addressables` | |
| `ResourcesUiConfigs` | `ScriptableObjects/Configs/UiConfigs/Resources` | |
| `PrefabRegistryUiConfigs` | `ScriptableObjects/Configs/UiConfigs/PrefabRegistry` | `IReadOnlyList<PrefabEntry> PrefabEntries`, `struct PrefabEntry { string Address; GameObject Prefab; }` |

### UiSetConfig, UiSetEntry, UiSetConfigSerializable

| 타입 | 멤버 |
|------|------|
| `UiSetConfig` (struct) | `int SetId`, `UiInstanceId[] UiInstanceIds` |
| `UiSetEntry` (struct) | `string UiTypeName`(AssemblyQualifiedName), `string InstanceAddress`, `UiInstanceId ToUiInstanceId()`, `static UiSetEntry FromUiInstanceId(UiInstanceId)` |
| `UiSetConfigSerializable` (struct) | `int SetId`, `List<UiSetEntry> UiEntries`, `static UiSetConfig ToUiSetConfig(UiSetConfigSerializable)`, `static UiSetConfigSerializable FromUiSetConfig(UiSetConfig)` |

### UiInstanceId, UiInstance

| 타입 | 멤버 |
|------|------|
| `UiInstanceId` (readonly struct, `IEquatable<UiInstanceId>`) | `UiInstanceId(Type presenterType, string instanceAddress = null)`, `Type PresenterType`, `string InstanceAddress`, `bool IsDefault`, `static Default(Type)`, `static Named(Type, string)`, `==`, `!=`, `ToString()` |
| `UiInstance` (readonly struct) | `UiInstance(Type type, string address, UiPresenter presenter)`, `Type Type`, `string Address`, `UiPresenter Presenter` |

`UiInstanceId` 생성자는 `presenterType` 이 `null` 이면 `ArgumentNullException` 을 던지고, `null`/빈 주소를 `string.Empty` 로 바꿉니다.

### LoadSynchronouslyAttribute

프레젠터 클래스에 붙이는 어트리뷰트입니다. 설정 인스펙터가 동기화할 때 `UiConfig.LoadSynchronously` 를 채워 에셋에 저장하며, `AddressablesUiAssetLoader` 가 이 값으로 동기 로드합니다. 런타임에는 어트리뷰트를 직접 읽지 않으므로 어트리뷰트를 붙이거나 뗀 뒤에는 설정 인스펙터를 열어 다시 동기화해야 합니다. 자세한 내용은 [핵심 개념 - UI 설정](core-concepts.md#ui-설정).

---

## 에셋 로더

```csharp
public interface IUiAssetLoader
{
    UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default);
    void UnloadAsset(GameObject asset);
}
```

| 구현 | 인스턴스화 | 해제 |
|------|-----------|------|
| `AddressablesUiAssetLoader` | `Addressables.InstantiateAsync(config.Address, ...)`. `LoadSynchronously` 면 `WaitForCompletion()`. 실패 시 `OperationException` 을 던짐 | `Addressables.ReleaseInstance`. Addressables 인스턴스가 아니면 `Object.Destroy` |
| `ResourcesUiAssetLoader` | `Resources.Load<GameObject>(config.Address)` 결과를 캐시해 `Instantiate`. 없으면 `KeyNotFoundException` | `Object.DestroyImmediate` |
| `PrefabRegistryUiAssetLoader` | 등록된 프리팹을 `Instantiate`. 미등록 주소면 `KeyNotFoundException` | `Object.DestroyImmediate` |

`PrefabRegistryUiAssetLoader` 생성자와 메서드:

| 시그니처 | 설명 |
|----------|------|
| `PrefabRegistryUiAssetLoader()` | 빈 레지스트리 |
| `PrefabRegistryUiAssetLoader(PrefabRegistryUiConfigs configs)` | `configs.PrefabEntries` 를 등록 (`null` 이면 무시) |
| `void RegisterPrefab(string address, GameObject prefab)` | 주소에 프리팹 등록 (덮어쓰기) |

커스텀 로더 예제는 [고급 주제](advanced.md#커스텀-에셋-로더)에 있습니다.
