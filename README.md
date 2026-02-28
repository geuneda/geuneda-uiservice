# Geuneda UI Service

[![Unity Version](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.2.0-green.svg)](CHANGELOG.md)

> **바로가기**: [설치](#설치-방법) | [빠른 시작](#빠른-시작) | [문서](docs/README.md) | [예제](#예제) | [문제 해결](docs/troubleshooting.md)

## 왜 이 패키지를 사용해야 하나요?

Unity 게임에서 UI를 관리하다 보면 직접 참조의 난맥, 분산된 열기/닫기 로직, 수동 생명주기 관리 등의 문제에 부딪힙니다. 이 **UI Service**는 이러한 문제를 해결합니다:

| 문제점 | 해결책 |
|--------|--------|
| **분산된 UI 로직** | 중앙 집중식 서비스로 UI 생명주기 관리 (로드 -> 열기 -> 닫기 -> 언로드) |
| **메모리 관리 복잡성** | Addressables 통합으로 자동 에셋 로드/언로드 |
| **경직된 UI 계층 구조** | 유연한 깊이 정렬이 가능한 레이어 기반 구성 |
| **중복 보일러플레이트** | 상속의 복잡성 없이 동작을 확장하는 피처 조합 시스템 |
| **복잡한 비동기 로딩** | 취소 지원이 포함된 UniTask 기반 비동기 작업 |
| **UI 상태 파악 불가** | 실시간 분석, 계층 구조 디버깅, 설정을 위한 에디터 윈도우 |
| **어려운 테스팅** | 주입 가능한 인터페이스(`IUiService`, `IUiAssetLoader`)와 내장 로더로 쉬운 모킹 |

**프로덕션 검증 완료:** WebGL, 모바일, 데스크톱을 지원하는 실제 게임에서 사용 중. 핫 패스에서 프레임당 할당 제로.

### 주요 기능

- **UI Model-View-Presenter 패턴** - 생명주기 관리가 포함된 깔끔한 UI 로직 분리
- **UI Toolkit 지원** - uGUI와 UI Toolkit 모두 호환
- **피처 조합** - 프레젠터 동작을 확장하는 모듈식 피처 시스템
- **비동기 로딩** - UniTask 지원으로 UI 에셋을 비동기적으로 로드
- **UI 그룹 구성** - 깊이 레이어별로 UI 요소를 구성하고 일괄 작업 수행
- **메모리 관리** - Unity의 Addressables 시스템으로 효율적인 UI 에셋 로드/언로드
- **분석 및 성능 추적** - 의존성 주입을 활용한 선택적 분석 시스템
- **에디터 도구** - 디버깅 및 모니터링을 위한 강력한 에디터 윈도우
- **반응형 디자인** - 기기 세이프 영역 내장 지원 (예: iPhone 다이나믹 아일랜드)

---

## 시스템 요구 사항

- **[Unity](https://unity.com/download)** (v6.0+) - 패키지 실행 환경
- **[Unity Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest)** (v2.6.0+) - 비동기 에셋 로딩
- **[UniTask](https://github.com/Cysharp/UniTask)** (v2.5.10+) - 효율적인 비동기 작업

Unity Package Manager를 통해 설치하면 종속성이 자동으로 해결됩니다.

### 호환성 매트릭스

| Unity 버전 | 상태 | 비고 |
|------------|------|------|
| 6000.3.x (Unity 6) | 완전 테스트 | 주요 개발 대상 |
| 6000.0.x (Unity 6) | 완전 테스트 | 완전 지원 |
| 2022.3 LTS | 미테스트 | 약간의 조정이 필요할 수 있음 |

| 플랫폼 | 상태 | 비고 |
|---------|------|------|
| Standalone (Windows/Mac/Linux) | 지원 | 모든 기능 지원 |
| WebGL | 지원 | UniTask 필요 (Task.Delay 사용 불가) |
| Mobile (iOS/Android) | 지원 | 모든 기능 지원 |
| Console | 미테스트 | Addressables 설정으로 작동할 것으로 예상 |

## 설치 방법

### Unity Package Manager (권장)

1. Unity Package Manager 열기 (`Window` -> `Package Manager`)
2. `+` 버튼 클릭 후 `Add package from git URL` 선택
3. 다음 URL 입력:
   ```
   https://github.com/geuneda/geuneda-uiservice.git
   ```

### manifest.json으로 설치

프로젝트의 `Packages/manifest.json`에 다음을 추가:

```json
{
  "dependencies": {
    "com.geuneda.uiservice": "https://github.com/geuneda/geuneda-uiservice.git"
  }
}
```

---

## 문서

| 문서 | 설명 |
|------|------|
| [시작하기](docs/getting-started.md) | 설치, 설정, 첫 프레젠터 만들기 |
| [핵심 개념](docs/core-concepts.md) | 프레젠터, 레이어, 세트, 피처 |
| [API 레퍼런스](docs/api-reference.md) | 전체 API 문서 |
| [고급 주제](docs/advanced.md) | 분석, 성능, 헬퍼 뷰 |
| [문제 해결](docs/troubleshooting.md) | 자주 발생하는 문제와 해결법 |

## 패키지 구조

```
Runtime/
├── Loaders/
│   ├── IUiAssetLoader.cs          # 에셋 로딩 인터페이스
│   ├── AddressablesUiAssetLoader.cs # Addressables 구현
│   ├── PrefabRegistryUiAssetLoader.cs # 프리팹 직접 참조
│   └── ResourcesUiAssetLoader.cs # Resources.Load 구현
├── IUiService.cs          # 공개 API 인터페이스
├── UiService.cs           # 핵심 구현
├── UiPresenter.cs         # 기본 프레젠터 클래스
├── UiConfigs.cs           # 설정용 ScriptableObject
├── Features/              # 조합 가능한 피처
│   ├── TimeDelayFeature.cs
│   ├── AnimationDelayFeature.cs
│   └── UiToolkitPresenterFeature.cs
└── Views/                 # 헬퍼 컴포넌트

Editor/
├── UiConfigsEditor.cs     # 향상된 인스펙터
├── UiAnalyticsWindow.cs   # 성능 모니터링
└── UiServiceHierarchyWindow.cs  # 실시간 디버깅
```

### 주요 파일

| 컴포넌트 | 역할 |
|----------|------|
| **IUiService** | 모든 UI 작업을 위한 공개 API |
| **UiService** | 생명주기, 레이어, 상태를 관리하는 핵심 구현 |
| **UiPresenter** | 생명주기 훅이 포함된 모든 UI 뷰의 기본 클래스 |
| **UiConfigs** | UI 설정과 세트를 저장하는 ScriptableObject |
| **PrefabRegistryConfig** | 주소 키를 UI 프리팹에 매핑하여 직접 참조 |
| **IUiAssetLoader** | 커스텀 에셋 로딩 전략을 위한 인터페이스 |
| **AddressablesUiAssetLoader** | 비동기 로딩을 위한 Addressables 통합 처리 |
| **PrefabRegistryUiAssetLoader** | 프리팹 직접 참조를 위한 간단한 로더 |
| **ResourcesUiAssetLoader** | Unity Resources 폴더에서 UI 로드 |
| **PresenterFeatureBase** | 조합 가능한 프레젠터 동작의 기본 클래스 |
| **UiInstanceId** | 같은 프레젠터 타입의 다중 인스턴스 지원 |

---

## 빠른 시작

### 1. UI 설정 생성

1. Project View에서 우클릭
2. `Create` -> `ScriptableObjects` -> `Configs` -> `UiConfigs` 이동
3. 생성된 에셋에서 UI 프레젠터 설정

### 2. UI Service 초기화

```csharp
using UnityEngine;
using Geuneda.UiService;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;
    private IUiServiceInit _uiService;

    void Start()
    {
        _uiService = new UiService();
        _uiService.Init(_uiConfigs);
    }
}
```

### 3. 첫 번째 UI Presenter 만들기

```csharp
using UnityEngine;
using Geuneda.UiService;

public class MainMenuPresenter : UiPresenter
{
    [SerializeField] private Button _playButton;

    protected override void OnInitialized()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
    }

    protected override void OnOpened()
    {
        Debug.Log("메인 메뉴가 열렸습니다!");
    }

    protected override void OnClosed()
    {
        Debug.Log("메인 메뉴가 닫혔습니다!");
    }

    private void OnPlayClicked()
    {
        Close(destroy: false);
    }
}
```

### 4. UI 열기 및 관리

```csharp
// UI 열기
var mainMenu = await _uiService.OpenUiAsync<MainMenuPresenter>();

// 표시 여부 확인
if (_uiService.IsVisible<MainMenuPresenter>())
{
    Debug.Log("메인 메뉴가 표시 중입니다");
}

// UI 닫기
_uiService.CloseUi<MainMenuPresenter>();
```

전체 설정 가이드는 [시작하기](docs/getting-started.md)를 참조하세요.

---

## 라이센스

이 프로젝트는 MIT License에 따라 라이센스됩니다. 자세한 내용은 [LICENSE.md](LICENSE.md) 파일을 참조하세요.

원본 저작권: Miguel Tomas (GameLovers)
