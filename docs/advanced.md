# 고급 주제

- [헬퍼 뷰](#헬퍼-뷰)
- [커스텀 에셋 로더](#커스텀-에셋-로더)
- [로딩과 메모리 전략](#로딩과-메모리-전략)
- [알려진 제한 사항](#알려진-제한-사항)

---

## 헬퍼 뷰

네임스페이스 `Geuneda.UiService.Views`. 모두 uGUI 컴포넌트이며 프리팹에 붙여 인스펙터에서 설정합니다.

### SafeAreaHelperView

기기 안전 영역(노치 등)에 맞게 `RectTransform` 의 위치와 크기를 조정합니다. `OnEnable` 에서 한 번 적용하고, `_onUpdate` 를 켜면 매 프레임 적용합니다.

| 필드 | 설명 |
|------|------|
| `_rectTransform` | 대상. 비워 두면 자신의 `RectTransform` |
| `_refResolution` | 기준 해상도. 에디터 `OnValidate` 에서 `transform.root` 의 `CanvasScaler.referenceResolution` 으로 채워짐 |
| `_checkAreaBounds` | 켜면 앵커 위치가 이미 안전 영역 안에 있을 때 이동하지 않음 |
| `_ignoreHeight`, `_ignoreWidth` | 해당 축 조정 생략 |
| `_onUpdate` | 매 프레임 갱신 |

- 네 방향으로 늘어난 앵커(`anchorMin = (0,0)`, `anchorMax = (1,1)`)는 위치와 크기를, 위/아래 또는 왼쪽/오른쪽에 고정된 앵커는 위치를 조정합니다.
- 인스펙터의 `Update Anchored Data` 버튼은 초기 위치/크기와 기준 해상도를 다시 읽고, `Update Anchored View` 버튼은 즉시 적용합니다.
- 기준 해상도는 프리팹 루트의 `CanvasScaler` 에서 가져오므로, 루트에 `CanvasScaler` 가 없으면 `_refResolution` 을 직접 입력해야 합니다.

### NonDrawingView

아무것도 그리지 않는 `Graphic` 입니다. 투명한 `Image` 대신 레이캐스트 대상(터치 차단, 모달 배경 클릭 영역)으로 씁니다. `CanvasRenderer` 가 필요하며, 인스펙터에는 레이캐스트 설정만 표시됩니다.

### AdjustScreenSizeFitterView

부모 `Canvas` 크기에서 `_padding` 을 뺀 영역에 맞춰 `RectTransform` 크기를 조정하는 레이아웃 컨트롤러입니다. `LayoutElement` 가 필요하고(`RequireComponent`), 에디트 모드에서도 동작합니다(`ExecuteAlways`). 컴포넌트 메뉴는 `Layout/Adjust Size Fitter` 입니다.

- `LayoutElement` 의 Min Width/Height 를 최소 크기, Flexible Width/Height 를 최대 픽셀 크기로 사용합니다.
- 사용 가능한 영역이 최소보다 작으면 최소 크기, 최대 이상이면 최대 크기로 맞추고, 그 사이에서는 현재 크기가 영역보다 클 때만 영역 크기로 줄입니다.
- 캔버스 크기가 바뀌면 `LateUpdate` 에서 다시 계산합니다.

### InteractableTextView

`TMP_Text` 의 `<link>` 태그 클릭을 감지해 `OnLinkedInfoClicked`(`UnityEvent<TMP_LinkInfo>`)를 발생시킵니다. URL 을 여는 등의 동작은 직접 연결해야 합니다.

| 멤버 | 설명 |
|------|------|
| `OnLinkedInfoClicked` | 클릭된 링크 정보 이벤트 |
| `InteractableType` | `IntersectingLink`(클릭 위치와 겹치는 링크) 또는 `NearestLink`(가장 가까운 링크) |
| `Text` | 대상 `TMP_Text` (`OnValidate` 에서 자동 할당) |

```csharp
using Geuneda.UiService.Views;
using TMPro;
using UnityEngine;

public class TermsLinkHandler : MonoBehaviour
{
    [SerializeField] private InteractableTextView _textView;

    private void Awake()
    {
        _textView.OnLinkedInfoClicked.AddListener(OnLinkClicked);
    }

    private void OnDestroy()
    {
        _textView.OnLinkedInfoClicked.RemoveListener(OnLinkClicked);
    }

    private void OnLinkClicked(TMP_LinkInfo info)
    {
        // <link="https://example.com">이용 약관</link>
        Application.OpenURL(info.GetLinkID());
    }
}
```

링크 판정에는 부모 `Canvas` 의 `worldCamera` 를 넘기므로 Screen Space - Camera 와 World Space 캔버스에서도 클릭 위치가 맞습니다(3.3.0. 이전에는 항상 `null` 을 넘겨 Overlay 에서만 맞았음). Screen Space - Overlay 캔버스, 카메라가 없는 Screen Space - Camera 캔버스, 부모 Canvas 가 없는 텍스트는 `null` 로 판정합니다. 레이캐스트를 받으려면 텍스트의 `Raycast Target` 이 켜져 있고 씬에 `EventSystem` 이 있어야 합니다.

---

## 커스텀 에셋 로더

`IUiAssetLoader` 를 구현해 `UiService` 생성자에 넘깁니다. 서비스는 반환된 GameObject 를 `"Ui"` 루트 아래에 둔 상태로 받아 비활성화한 뒤 사용하므로, 로더는 인스턴스화만 책임지면 됩니다.

```csharp
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Geuneda.UiService;
using UnityEngine;

public class AsyncResourcesUiAssetLoader : IUiAssetLoader
{
    public async UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default)
    {
        var request = Resources.LoadAsync<GameObject>(config.Address);
        await request.ToUniTask(cancellationToken: cancellationToken);

        if (request.asset is not GameObject prefab)
        {
            throw new KeyNotFoundException($"Resources 에서 프리팹을 찾을 수 없습니다: {config.Address}");
        }

        return Object.Instantiate(prefab, parent);
    }

    public void UnloadAsset(GameObject asset)
    {
        if (asset != null)
        {
            Object.Destroy(asset);
        }
    }
}
```

```csharp
var uiService = new UiService(new AsyncResourcesUiAssetLoader());
uiService.Init(_resourcesUiConfigs);
```

테스트에서는 `PrefabRegistryUiAssetLoader` 에 `RegisterPrefab(address, prefab)` 으로 프리팹을 등록해 Addressables 없이 서비스를 구성할 수 있습니다.

---

## 로딩과 메모리 전략

| 전략 | 방법 | 특징 |
|------|------|------|
| 필요할 때 로드 | `OpenUiAsync<T>()` | 메모리를 아끼지만 처음 열 때 로딩 시간이 생김 |
| 미리 로드 | `LoadUiAsync<T>()` 후 나중에 `OpenUiAsync<T>()` | 이미 로드된 인스턴스는 바로 열림 |
| 세트 단위 미리 로드 | `await UniTask.WhenAll(LoadUiSetAsync(setId))` | 화면 전환 단위로 한 번에 로드 |
| 숨기기 | `CloseUi<T>()` | 메모리에 남아 다시 열 때 빠름 |
| 해제 | `CloseUi<T>(destroy: true)` 또는 `UnloadUi<T>()` | 메모리 해제. `UnloadUi` 는 닫기 훅을 실행하지 않음 |

로딩 화면 동안 다음 화면의 UI 를 미리 로드하는 예:

```csharp
using Cysharp.Threading.Tasks;
using Geuneda.UiService;
using UnityEngine.SceneManagement;

public class LevelLoader
{
    private readonly IUiService _uiService;

    public LevelLoader(IUiService uiService)
    {
        _uiService = uiService;
    }

    public async UniTask LoadLevelAsync(string sceneName)
    {
        await _uiService.OpenUiAsync<LoadingScreenPresenter>();

        await UniTask.WhenAll(
            _uiService.LoadUiAsync<GameHudPresenter>().AsUniTask(),
            _uiService.LoadUiAsync<PauseMenuPresenter>().AsUniTask(),
            SceneManager.LoadSceneAsync(sceneName).ToUniTask());

        _uiService.CloseUi<LoadingScreenPresenter>();
    }
}
```

같은 키 입력으로 열기를 반복 호출하면 이미 열린 경우 경고가 남습니다. 토글 입력은 `IsVisible<T>()` 로 먼저 확인하세요.

---

## 알려진 제한 사항

| 항목 | 내용 |
|------|------|
| 설정은 타입당 하나 | 같은 프레젠터 타입을 다른 프리팹으로 나눠 등록할 수 없음. 다중 인스턴스는 같은 프리팹을 인스턴스 주소로 구분 |
| `AddUi` 와 언로드 | `AddressablesUiAssetLoader` 를 쓸 때 Addressables 로 인스턴스화하지 않고 `AddUi` 로 넣은 프레젠터는 `Addressables.ReleaseInstance` 가 처리하지 못하므로 로더가 `Object.Destroy` 로 파괴함(v1.3.1). GameObject 를 남기려면 `RemoveUi` 를 씀 |
| `PanelRenderer` 레이어 | Unity 6.5+ 에서 `PanelRenderer.sortingOrder`(int, `Renderer` 상속)에도 적용(v1.3.2) |
| 정적 이벤트 | `OnResolutionChanged`, `OnOrientationChanged` 는 `Dispose()` 후에도 리스너가 남음 |
| 프레젠터 인스펙터 버튼 | 서비스를 거치지 않아 `VisiblePresenters` 와 어긋날 수 있음 |
| 레이어 범위 | 0 미만, 1000 초과는 경고만 남김 |
| 차단막 정렬값 | 입력 차단막은 정렬값 32767 을 씀. 이보다 높은 `sortingOrder`/`PanelSettings.sortingOrder` 를 가진 UI 는 막지 못함 |
| `PanelRenderer` dim | 루트가 `PanelRenderer` 인 모달에는 dim 을 깔지 않고 경고만 남김 |
| `UiPopupQueue` 와 서비스 수명 | 서비스를 Dispose 하는 동안에는 `PresenterClosed` 가 오지 않으므로, 큐를 먼저 Dispose 함 |
| 같은 레이어의 uGUI | 서비스가 루트 `Canvas.sortingOrder` 를 `레이어 + 연 순위 × CanvasOrderStep`(기본 1, 3.2.0) 으로 매기되 다음 설정 레이어 값보다 작게만 씀(3.1.0). 레이어를 띄워 둠. 간격이 모자라면 먼저 연 캔버스끼리 정렬값을 같이 쓰고(앞뒤 미정) 경고. 자식 Canvas 는 그대로 |
| 여러 팝업 큐 | 같은 서비스의 큐끼리는 로드 중인 팩토리도 기다림(먼저 시작한 큐가 이김, 3.1.0). 큐 사이는 우선순위 값이 아니라 시작 순서를 따르므로 우선순위로 순서를 정하려면 한 큐에 넣음. `IUiService` 를 직접 구현한 서비스에서는 조정하지 않음 |
| 렌더 모드가 섞인 레이어 | `UiCameraStackFeature` 를 붙인 Screen Space - Camera 프레젠터는 레이어가 아무리 높아도 Screen Space - Overlay 프레젠터 아래에 그려짐. [URP 렌더링 피처](urp-rendering.md) |
