# Geuneda UI Service

Unity UI 프레젠터의 로드, 열기, 닫기, 언로드를 한 서비스에서 관리하는 패키지입니다. 레이어 정렬, UI 세트, 다중 인스턴스, 피처 조합, UI Toolkit, 교체 가능한 에셋 로더(Addressables, Resources, 프리팹 직접 참조)를 지원합니다. 뒤로가기, 우선순위 팝업 큐, 모달 dim, 입력 차단도 제공합니다.

## 설치

core 와 UniTask 는 Unity 레지스트리에 없어서 의존성으로 자동 설치되지 않습니다. `Packages/manifest.json` 에 core, UniTask 와 이 패키지를 함께 추가합니다.

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.geuneda.core": "https://github.com/geuneda/geuneda-core.git",
    "com.geuneda.uiservice": "https://github.com/geuneda/geuneda-uiservice.git"
  }
}
```

Package Manager 의 `+` > `Add package from git URL` 에 위 URL 을 넣어도 됩니다. 특정 버전에 고정하려면 URL 끝에 `#<태그>` 를 붙입니다(레포에 실제로 있는 태그만 쓸 수 있습니다). Addressables 와 uGUI 는 Unity 레지스트리에서 자동으로 설치됩니다.

## 빠른 시작

1. 프레젠터 스크립트를 만듭니다.

   ```csharp
   using Geuneda.UiService;
   using UnityEngine;
   using UnityEngine.UI;

   public class MainMenuPresenter : UiPresenter
   {
       [SerializeField] private Button _playButton;

       protected override void OnInitialized()
       {
           _playButton.onClick.AddListener(() => Close(destroy: false));
       }

       protected override void OnOpened()
       {
           Debug.Log("메인 메뉴 열림");
       }
   }
   ```

2. 루트 GameObject 에 `Canvas` 와 `MainMenuPresenter` 를 붙인 프리팹을 만들고 Addressable 로 표시합니다.
3. `Create > ScriptableObjects > Configs > UiConfigs > Addressables` 로 `AddressablesUiConfigs` 에셋을 만듭니다(또는 `Tools > UI Service > Select UiConfigs`). 인스펙터를 열면 Addressable 그룹에서 `UiPresenter` 가 붙은 프리팹을 찾아 설정 목록을 자동으로 채웁니다.
4. 서비스를 초기화하고 UI 를 엽니다.

   ```csharp
   using Cysharp.Threading.Tasks;
   using Geuneda.UiService;
   using UnityEngine;

   public class GameBootstrap : MonoBehaviour
   {
       [SerializeField] private UiConfigs _uiConfigs;
       private IUiServiceInit _uiService;

       private void Start()
       {
           _uiService = new UiService();      // 기본 로더: AddressablesUiAssetLoader
           _uiService.Init(_uiConfigs);
           OpenMainMenuAsync().Forget();
       }

       private async UniTaskVoid OpenMainMenuAsync()
       {
           await _uiService.OpenUiAsync<MainMenuPresenter>();
       }

       private void OnDestroy()
       {
           _uiService?.Dispose();
       }
   }
   ```

자세한 단계는 [시작하기](docs/getting-started.md)를 참고하세요.

## 핵심 개념

| 개념 | 요약 |
|------|------|
| `IUiService` / `IUiServiceInit` | UI 를 다루는 공개 API 와, 여기에 `Init(UiConfigs)` 와 `Dispose()` 를 더한 소유자용 인터페이스 |
| `UiPresenter`, `UiPresenter<T>` | 모든 UI 의 기본 클래스. `OnInitialized`, `OnOpened`, `OnClosed` 등 생명주기 훅과 struct 데이터 바인딩 제공 |
| 레이어 | `UiConfig.Layer` 값이 프리팹 루트의 `Canvas`, `UIDocument`, `PanelRenderer`(Unity 6.5+) `sortingOrder` 로 설정됨 |
| UI 세트 | 여러 프레젠터를 묶어 `OpenUiSetAsync`, `CloseAllUiSet`, `UnloadUiSet` 으로 일괄 처리 |
| 다중 인스턴스 | `UiInstanceId`(타입 + 인스턴스 주소)로 같은 프레젠터 타입을 여러 개 띄움 |
| 피처 | `PresenterFeatureBase` 컴포넌트를 프리팹에 붙여 동작을 확장. `TimeDelayFeature`, `AnimationDelayFeature`, `UiToolkitPresenterFeature`, `UiToolkitPanelRendererFeature`(Unity 6.5+) 기본 제공 |
| 에셋 로더 | `IUiAssetLoader` 구현을 `UiService` 생성자에 주입. `AddressablesUiAssetLoader`(기본), `ResourcesUiAssetLoader`, `PrefabRegistryUiAssetLoader` 기본 제공 |
| 헬퍼 뷰 | `Geuneda.UiService.Views` 의 `SafeAreaHelperView`, `NonDrawingView`, `AdjustScreenSizeFitterView`, `InteractableTextView` |
| 에디터 도구 | `Tools > UI Service` 메뉴의 `Select UiConfigs`, `Layer Visualizer`, `Presenter Manager` |
| 뒤로가기 | `HandleBack()` 이 핸들러 → 화면 맨 위 `HandlesBack` 프레젠터 → `BackUnhandled` 순서로 처리. `ReadsBackInput` 으로 Escape/Android 뒤로가기를 직접 읽음 |
| 팝업 큐 | `UiPopupQueue` 가 팝업을 우선순위 순서로 하나씩 띄움. 키 중복 방지, `Hold` 토큰, 큐 밖 팝업이 닫힐 때까지 대기 |
| 모달 dim, 입력 차단 | `IsModal` 프레젠터 중 맨 위 하나 뒤에 공용 dim. `BlockInput(reason)` 토큰으로 모든 UI 입력 차단 |

## 문서

| 문서 | 내용 |
|------|------|
| [시작하기](docs/getting-started.md) | 설치, 설정 에셋, 프리팹 준비, 첫 프레젠터 |
| [핵심 개념](docs/core-concepts.md) | 생명주기, 피처, 레이어, 세트, 다중 인스턴스, 설정, 에디터 도구 |
| [API 레퍼런스](docs/api-reference.md) | 공개 API 시그니처와 동작 |
| [고급 주제](docs/advanced.md) | 헬퍼 뷰, 커스텀 로더, 로딩 전략 |
| [뒤로가기, 팝업 큐, 모달 dim, 입력 차단](docs/navigation.md) | 화면 맨 위 규칙, 뒤로가기 처리 순서, 큐 진행 조건, dim·차단막 배치 |
| [문제 해결](docs/troubleshooting.md) | 자주 겪는 문제와 해결 방법 |
| [문서 색인](docs/README.md) | 아키텍처 개요와 패키지 구조 |

변경 이력은 [CHANGELOG.md](CHANGELOG.md)에 있습니다.

## 요구 사항과 의존성

- Unity 6000.0 이상 (`UiToolkitPanelRendererFeature` 는 Unity 6000.5 이상에서만 컴파일됩니다)
- `com.geuneda.core` 0.4.0(로거 `ModuleLogger`)
- `com.unity.addressables` 2.6.0
- `com.cysharp.unitask` 2.5.10
- `com.unity.ugui` 2.0.0. 헬퍼 뷰가 uGUI(`UnityEngine.UI`)와 TextMeshPro(`TMPro`)를 사용하며, Unity 6 에서는 TextMeshPro 가 `com.unity.ugui` 에 포함되어 있습니다.
- 선택: `com.unity.inputsystem`. 있으면 `ReadsBackInput` 이 Input System 으로 Escape 를 읽습니다(`versionDefines` 로 감지). 없으면 레거시 Input Manager 를 씁니다.

## 라이선스

MIT License. 자세한 내용은 [LICENSE.md](LICENSE.md)를 참고하세요.

원본 저작권: Miguel Tomas (GameLovers)
