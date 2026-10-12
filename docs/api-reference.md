# API 레퍼런스

런타임 네임스페이스는 `Geuneda.UiService`, 헬퍼 뷰는 `Geuneda.UiService.Views` 입니다. 비동기 메서드는 모두 UniTask(`Cysharp.Threading.Tasks`)를 반환합니다. 헬퍼 뷰는 [고급 주제](advanced.md#헬퍼-뷰)에 있습니다.

- [IUiService](#iuiservice)
- [IUiServiceInit](#iuiserviceinit)
- [UiService](#uiservice)
- [UiPresenter](#uipresenter)
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

`RemoveUi` 계열은 표시 목록에서도 빼지만 닫기 생명주기를 실행하지 않고 GameObject 도 그대로 둡니다.

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
| `void CloseAllUi()` | 표시 중인 모든 UI 닫기 |
| `void CloseAllUi(int layer)` | 해당 레이어의 표시 중인 UI 닫기 |
| `void CloseAllUiSet(int setId)` | 세트의 항목 닫기. 세트가 없으면 `KeyNotFoundException` |

- 이미 열린 인스턴스를 열거나 열려 있지 않은 인스턴스를 닫으면 경고만 남깁니다. `CloseAllUiSet` 은 열리지 않은 항목마다 경고를 남깁니다.
- 열기 메서드는 열기를 시작한 직후 완료됩니다. 전환까지 기다리려면 반환된 프레젠터의 `OpenTransitionTask` 를 기다립니다.
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

---

## IUiServiceInit

`public interface IUiServiceInit : IUiService, IDisposable`

| 시그니처 | 설명 |
|----------|------|
| `void Init(UiConfigs configs)` | 설정과 세트를 등록하고 `"Ui"` 루트 GameObject(`DontDestroyOnLoad`, `UiServiceMonoComponent` 부착)를 생성 |
| `void Dispose()` | 표시 중인 UI 를 닫고, 로드된 인스턴스를 모두 언로드하고, 컬렉션을 비우고, 루트 GameObject 를 파괴 |

`Init` 예외: `configs` 가 `null` 이면 `ArgumentNullException`, 설정에 빈 주소나 찾을 수 없는 타입이 있으면 `ArgumentException`. 레이어가 0 미만 또는 1000 초과면 경고만 남깁니다.

---

## UiService

`public class UiService : IUiServiceInit`

### 생성자

| 시그니처 | 설명 |
|----------|------|
| `UiService()` | `AddressablesUiAssetLoader` 사용 |
| `UiService(IUiAssetLoader assetLoader)` | 로더 주입 |

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

`public abstract class UiPresenter<T> : UiPresenter, IUiPresenterData where T : struct`

| 멤버 | 설명 |
|------|------|
| `T Data { get; set; }` | 대입할 때마다 `OnSetData()` 호출 |
| `protected virtual void OnSetData()` | 데이터 반영 지점 |

`public interface IUiPresenterData` 는 `UiPresenter<T>` 가 구현하는 마커 인터페이스입니다.

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
