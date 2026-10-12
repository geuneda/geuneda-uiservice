# 시작하기

설치 방법과 요구 사항은 패키지 [README](../README.md#설치)를 먼저 보세요. 이 문서는 설치 이후 첫 UI 를 띄우기까지의 과정을 단계별로 설명합니다.

## 1. 로더와 설정 에셋 고르기

에셋 로더는 `UiService` 생성자로 정하고, 설정 에셋은 로더에 맞는 `UiConfigs` 파생 타입을 만듭니다. 설정 에셋 타입이 로더를 자동으로 고르지는 않으므로 둘을 직접 맞춰야 합니다.

| 로더 | 설정 에셋 | `UiConfig.Address` 의 의미 |
|------|-----------|----------------------------|
| `AddressablesUiAssetLoader` (기본, `new UiService()`) | `AddressablesUiConfigs` | Addressable 주소 |
| `ResourcesUiAssetLoader` | `ResourcesUiConfigs` | `Resources/` 기준 상대 경로(확장자 없음) |
| `PrefabRegistryUiAssetLoader` | `PrefabRegistryUiConfigs` | 등록된 프리팹 이름 |

`UiConfigs` 는 `abstract` 이므로 위 세 파생 타입 중 하나를 써야 합니다.

## 2. 프레젠터 스크립트 작성

```csharp
using Geuneda.UiService;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPresenter : UiPresenter
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;

    protected override void OnInitialized()
    {
        // 서비스에 추가될 때 한 번 호출됩니다
        _playButton.onClick.AddListener(OnPlayClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
    }

    protected override void OnOpened()
    {
        // 열릴 때마다 호출됩니다
    }

    protected override void OnClosed()
    {
        // 닫힐 때마다 호출됩니다 (GameObject 가 비활성화되기 전)
    }

    private void OnPlayClicked()
    {
        Close(destroy: false);
    }

    private void OnSettingsClicked()
    {
    }
}
```

## 3. 프리팹 준비

1. `UiPresenter` 파생 컴포넌트를 프리팹의 **루트** GameObject 에 붙입니다. 서비스와 설정 에디터는 루트에서 `UiPresenter` 를 찾습니다.
2. 레이어 정렬을 쓰려면 같은 루트에 `Canvas`(uGUI) 또는 `UIDocument`(UI Toolkit)를 둡니다. 자식에 있는 Canvas 는 정렬 대상이 아닙니다.
3. 로더에 맞게 등록합니다.
   - Addressables: 프리팹을 Addressable 로 표시하고 주소를 정합니다(예: `UI/MainMenu`).
   - Resources: 프리팹을 `Resources` 폴더 아래에 둡니다.
   - PrefabRegistry: 다음 단계에서 설정 에셋에 프리팹을 직접 끌어 놓습니다.

프리팹 루트를 비활성 상태로 저장할 필요는 없습니다. 서비스가 인스턴스화 직후 GameObject 를 비활성화하고 열 때 활성화합니다. 다만 활성 상태로 저장된 프리팹은 인스턴스화 시점에 `Awake`/`OnEnable` 이 먼저 실행됩니다.

## 4. 설정 에셋 만들기

`Create > ScriptableObjects > Configs > UiConfigs` 아래의 `Addressables`, `Resources`, `PrefabRegistry` 중 하나를 선택합니다. `Tools > UI Service > Select UiConfigs` 는 프로젝트의 첫 `UiConfigs` 에셋을 선택하고, 없으면 `Assets/AddressablesUiConfigs.asset` 을 만듭니다.

설정 목록은 인스펙터를 열 때 자동으로 다시 만들어집니다.

- `AddressablesUiConfigs`: 읽기 전용이 아닌 모든 Addressable 그룹에서 루트에 `UiPresenter` 가 있는 프리팹을 찾습니다.
- `ResourcesUiConfigs`: 경로에 `/Resources/` 가 들어간 모든 프리팹 중 루트에 `UiPresenter` 가 있는 것을 찾습니다.
- `PrefabRegistryUiConfigs`: 드롭 영역이나 목록에 프리팹을 넣으면 프리팹 이름이 주소가 됩니다.

각 항목의 레이어 값은 프리팹의 `Canvas`/`UIDocument` sortingOrder 에서 가져오며, 인스펙터에서 레이어를 바꾸면 프리팹의 sortingOrder 에도 기록됩니다. UI 세트 편집은 [핵심 개념 - UI 세트](core-concepts.md#ui-세트)를 보세요.

## 5. 서비스 초기화

```csharp
using Geuneda.UiService;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;
    private IUiServiceInit _uiService;

    private void Awake()
    {
        _uiService = new UiService();       // AddressablesUiAssetLoader 사용
        _uiService.Init(_uiConfigs);
    }

    private void OnDestroy()
    {
        _uiService?.Dispose();
    }
}
```

다른 로더를 쓸 때:

```csharp
// Resources
_uiService = new UiService(new ResourcesUiAssetLoader());
_uiService.Init(_resourcesUiConfigs);

// 프리팹 직접 참조: 설정 에셋의 PrefabEntries 를 로더에 등록합니다
_uiService = new UiService(new PrefabRegistryUiAssetLoader(_prefabRegistryUiConfigs));
_uiService.Init(_prefabRegistryUiConfigs);
```

`Init()` 은 주소가 비었거나 타입을 찾지 못한 설정이 있으면 `ArgumentException` 을 던지고, 레이어가 0 미만이거나 1000 초과이면 경고만 남깁니다.

## 6. UI 열기와 닫기

```csharp
using Cysharp.Threading.Tasks;
using Geuneda.UiService;

public class MenuFlow
{
    private readonly IUiService _uiService;

    public MenuFlow(IUiService uiService)
    {
        _uiService = uiService;
    }

    public async UniTask ShowMainMenuAsync()
    {
        var mainMenu = await _uiService.OpenUiAsync<MainMenuPresenter>();   // 필요하면 로드 후 열기

        if (_uiService.IsVisible<MainMenuPresenter>())
        {
            _uiService.CloseUi<MainMenuPresenter>();                        // 숨기기만, 메모리에 유지
        }

        // 닫으면서 메모리에서 해제하려면 destroy: true
        // _uiService.CloseUi<MainMenuPresenter>(destroy: true);
    }
}
```

`OpenUiAsync` 는 열기를 시작한 직후 완료됩니다. 전환 피처(딜레이, 애니메이션)까지 끝나기를 기다리려면 `await mainMenu.OpenTransitionTask` 를 사용합니다.

## 7. 데이터 프레젠터

열 때 데이터를 넘기려면 `UiPresenter<T>` 를 상속합니다. `T` 는 struct 여야 합니다.

```csharp
using Geuneda.UiService;
using UnityEngine;
using UnityEngine.UI;

public struct PlayerProfileData
{
    public string PlayerName;
    public int Level;
}

public class PlayerProfilePresenter : UiPresenter<PlayerProfileData>
{
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;

    protected override void OnSetData()
    {
        // Data 에 값을 대입할 때마다 호출됩니다
        _nameText.text = Data.PlayerName;
        _levelText.text = $"Lv. {Data.Level}";
    }
}
```

```csharp
var data = new PlayerProfileData { PlayerName = "Hero", Level = 42 };
var profile = await _uiService.OpenUiAsync<PlayerProfilePresenter, PlayerProfileData>(data);

// 연 뒤에도 Data 를 다시 대입하면 OnSetData() 가 호출됩니다
profile.Data = new PlayerProfileData { PlayerName = "Hero", Level = 43 };
```

## 다음 단계

- [핵심 개념](core-concepts.md): 생명주기 순서, 피처, 레이어, 세트, 다중 인스턴스
- [API 레퍼런스](api-reference.md): 전체 시그니처와 예외 동작
- [문제 해결](troubleshooting.md)
