# 시작하기

이 가이드에서는 Unity 프로젝트에서 UI Service를 설정하고 첫 번째 UI 프레젠터를 만드는 방법을 안내합니다.

## 사전 요구사항

- **Unity** 6000.0 이상
- **Addressables** 2.6.0 이상
- **UniTask** 2.5.10 이상

## 설치

### Unity Package Manager를 통한 설치 (권장)

1. Unity Package Manager를 엽니다 (`Window` → `Package Manager`)
2. `+` 버튼을 클릭하고 `Add package from git URL`을 선택합니다
3. 다음 URL을 입력합니다:
   ```
   https://github.com/CoderGamester/com.gamelovers.uiservice.git
   ```

### manifest.json을 통한 설치

프로젝트의 `Packages/manifest.json`에 다음 줄을 추가합니다:

```json
{
  "dependencies": {
    "com.gamelovers.uiservice": "https://github.com/CoderGamester/com.gamelovers.uiservice.git"
  }
}
```

### OpenUPM을 통한 설치

```bash
openupm add com.gamelovers.uiservice
```

---

## 1단계: UI 설정 생성

UI Service는 UI 프레젠터에 대한 정보를 담은 설정 에셋이 필요합니다.

1. Project 뷰에서 마우스 오른쪽 클릭
2. `Create` → `ScriptableObjects` → `Configs` → `UiConfigs`로 이동
3. 이름을 지정합니다 (예: `GameUiConfigs`)

이 ScriptableObject는 다음을 저장합니다:
- UI 프레젠터 타입과 Addressable 주소
- 깊이 정렬을 위한 레이어 할당
- 일괄 작업을 위한 UI 세트 그룹핑

---

## 2단계: UI Service 초기화

UI Service를 설정하기 위한 게임 초기화 스크립트를 생성합니다:

```csharp
using UnityEngine;
using GameLovers.UiService;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;
    private IUiServiceInit _uiService;

    void Start()
    {
        // UI 서비스 생성 및 초기화
        _uiService = new UiService();
        _uiService.Init(_uiConfigs);

        // 서비스를 사용할 준비가 되었습니다
    }

    void OnDestroy()
    {
        // 완료 시 정리
        _uiService?.Dispose();
    }
}
```

### 에셋 로더 선택

UI Service는 기본 제공되는 여러 에셋 로딩 전략을 지원합니다:

| 로더 | 사용 시나리오 |
|------|-------------|
| `AddressablesUiAssetLoader` | **권장** - 비동기 로딩을 위한 Unity Addressables 시스템 사용. |
| `PrefabRegistryUiAssetLoader` | 샘플이나 게임 코드에서 프리팹을 직접 참조할 때 적합. |
| `ResourcesUiAssetLoader` | `Resources` 폴더에서 에셋을 로드 (전통적인 Unity 워크플로우). |

`ResourcesUiAssetLoader` 사용 예시:

```csharp
void Start()
{
    // Resources 로더를 사용하여 초기화
    _uiService = new UiService(new ResourcesUiAssetLoader());
    _uiService.Init(_uiConfigs);
}
```

---

## 3단계: 첫 번째 UI 프레젠터 만들기

### 3.1 프레젠터 스크립트 생성

```csharp
using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

public class MainMenuPresenter : UiPresenter
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;

    protected override void OnInitialized()
    {
        // 프레젠터가 처음 로드될 때 한 번 호출됩니다
        _playButton.onClick.AddListener(OnPlayClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
    }

    protected override void OnOpened()
    {
        // UI가 표시될 때마다 호출됩니다
        Debug.Log("Main menu opened!");
    }

    protected override void OnClosed()
    {
        // UI가 숨겨질 때 호출됩니다
        Debug.Log("Main menu closed!");
    }

    private void OnPlayClicked()
    {
        Close(destroy: false);
        // 게임플레이 UI 열기...
    }

    private void OnSettingsClicked()
    {
        // 설정 열기...
    }
}
```

### 3.2 UI 프리팹 생성

1. 씬에 새 Canvas를 생성합니다
2. UI 요소(버튼, 텍스트, 이미지)를 추가합니다
3. Canvas에 `MainMenuPresenter` 스크립트를 추가합니다
4. **중요:** 프리팹의 루트 GameObject를 **비활성화** 상태로 설정합니다 (서비스가 열릴 때 활성화합니다)
5. 프리팹으로 저장합니다

### 3.3 Addressable 설정

1. Project 뷰에서 프리팹을 선택합니다
2. Inspector에서 "Addressable" 체크박스를 선택합니다
3. 주소를 설정합니다 (예: `UI/MainMenu`)

### 3.4 UiConfigs에 등록

1. `UiConfigs` 에셋을 엽니다
2. 새 항목을 추가합니다:
   - **Type**: `MainMenuPresenter`
   - **Address**: `UI/MainMenu`
   - **Layer**: `1` (또는 원하는 레이어)

---

## 4단계: UI 열기 및 관리

```csharp
public class GameManager : MonoBehaviour
{
    private IUiService _uiService;

    async void Start()
    {
        // 메인 메뉴 열기
        var mainMenu = await _uiService.OpenUiAsync<MainMenuPresenter>();

        // UI가 보이는지 확인
        if (_uiService.IsVisible<MainMenuPresenter>())
        {
            Debug.Log("Main menu is currently visible");
        }

        // 특정 UI 닫기 (빠른 재열기를 위해 메모리에 유지)
        _uiService.CloseUi<MainMenuPresenter>();

        // 닫기 및 파괴 (메모리 해제)
        _uiService.CloseUi<MainMenuPresenter>(destroy: true);

        // 보이는 모든 UI 닫기
        _uiService.CloseAllUi();
    }
}
```

---

## 5단계: 데이터 기반 프레젠터 만들기

초기화 데이터가 필요한 UI의 경우:

```csharp
// 데이터 구조 정의
public struct PlayerProfileData
{
    public string PlayerName;
    public int Level;
    public Sprite Avatar;
}

// 데이터를 사용하는 프레젠터 생성
public class PlayerProfilePresenter : UiPresenter<PlayerProfileData>
{
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;
    [SerializeField] private Image _avatarImage;

    protected override void OnSetData()
    {
        // 데이터가 설정될 때 호출됨 - Data 프로퍼티 사용
        _nameText.text = Data.PlayerName;
        _levelText.text = $"Level {Data.Level}";
        _avatarImage.sprite = Data.Avatar;
    }
}

// 데이터와 함께 열기
var profileData = new PlayerProfileData
{
    PlayerName = "Hero",
    Level = 42,
    Avatar = avatarSprite
};

await _uiService.OpenUiAsync<PlayerProfilePresenter, PlayerProfileData>(profileData);
```

---

## 다음 단계

- [핵심 개념](core-concepts.md) - 레이어, 세트, 피처에 대해 알아보기
- [API 레퍼런스](api-reference.md) - 전체 API 문서
- [고급 주제](advanced.md) - 성능 최적화

---

## 샘플 프로젝트

패키지는 `Samples~` 폴더에 샘플 구현을 포함합니다:

1. Unity Package Manager를 엽니다 (`Window` → `Package Manager`)
2. "UI Service" 패키지를 선택합니다
3. "Samples" 탭으로 이동합니다
4. 원하는 샘플 옆의 "Import"를 클릭합니다

사용 가능한 샘플:
- **BasicUiFlow** - 기본 프레젠터 생명주기
- **DataPresenter** - 데이터 기반 UI
- **DelayedPresenter** - 시간 및 애니메이션 딜레이
- **UiToolkit** - UI Toolkit 통합
