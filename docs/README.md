# UI Service 문서

GameLovers UI Service 문서에 오신 것을 환영합니다. 이 가이드는 Unity 프로젝트에서 UI Service를 효과적으로 사용하는 데 필요한 모든 내용을 다룹니다.

## 빠른 탐색

| 문서 | 설명 |
|------|------|
| [시작하기](getting-started.md) | 설치, 설정, 첫 번째 UI 프레젠터 |
| [핵심 개념](core-concepts.md) | 프레젠터, 레이어, 세트, 피처, 설정 |
| [API 레퍼런스](api-reference.md) | 예제가 포함된 전체 API 문서 |
| [고급 주제](advanced.md) | 성능 최적화, 헬퍼 뷰 |
| [문제 해결](troubleshooting.md) | 일반적인 문제와 해결 방법 |

## 개요

UI Service는 Unity 게임에서 UI를 관리하기 위한 중앙 집중식 시스템을 제공합니다. 주요 기능은 다음과 같습니다:

- **생명주기 관리** - UI 프레젠터의 로드, 열기, 닫기, 언로드
- **레이어 구성** - 설정 가능한 레이어를 사용한 깊이 정렬 UI
- **UI 세트** - 그룹화된 UI 요소에 대한 일괄 작업
- **비동기 로딩** - 커스터마이징 가능한 에셋 로딩 전략 (Addressables, Resources, Prefab Registry)
- **피처 조합** - 모듈식 피처로 프레젠터 동작 확장

## 아키텍처 개요

```
┌─────────────────────────────────────────────────────────┐
│                      게임 코드                           │
│                 (GameManager, Systems)                  │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│              IUiServiceInit : IUiService                │
│    ┌────────────────────────────────────────────────┐   │
│    │ IUiService (소비: open/close/load/unload)      │   │
│    └────────────────────────────────────────────────┘   │
│    + Init(UiConfigs) + Dispose()                        │
└─────────────────────┬───────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌───────────┐  ┌────────────┐  ┌─────────────────┐
│ UiConfigs │  │ UiPresenter│  │ IUiAssetLoader  │
│   (SO)    │  │  (Views)   │  │ (추상화)        │
└───────────┘  └─────┬──────┘  └─────────────────┘
                     │
                     ▼
              ┌──────────────┐
              │   Features   │
              │ (조합 가능)   │
              └──────────────┘
```

### 서비스 인터페이스

UI Service는 **두 가지 인터페이스**를 노출합니다:

| 인터페이스 | 목적 | 핵심 기능 |
|-----------|------|----------|
| `IUiService` | **소비** - UI 열기/닫기/쿼리 | 모든 UI 생명주기 작업 |
| `IUiServiceInit` | **초기화** - `IUiService` 확장 | `Init(UiConfigs)` + `Dispose()` |

> **주의:** `Init()`을 호출해야 할 때는 `IUiServiceInit`을 사용하세요. `Init()` 메서드는 `IUiService`에서는 사용할 수 **없습니다**. 자세한 내용은 [핵심 개념 - 서비스 인터페이스](core-concepts.md#서비스-인터페이스)를 참조하세요.

## 패키지 구조

```
Runtime/
├── IUiService.cs          # 공개 API 인터페이스
├── UiService.cs           # 핵심 구현
├── UiPresenter.cs         # 기본 프레젠터 클래스
├── UiConfigs.cs           # 설정 ScriptableObject
├── Loaders/
│   ├── IUiAssetLoader.cs      # 에셋 로딩 인터페이스
│   ├── AddressablesUiAssetLoader.cs # Addressables 구현
│   ├── PrefabRegistryUiAssetLoader.cs # 직접 프리팹 참조
│   └── ResourcesUiAssetLoader.cs # Resources.Load 구현
├── UiInstanceId.cs        # 멀티 인스턴스 지원
├── Features/              # 조합 가능한 피처
│   ├── TimeDelayFeature.cs
│   ├── AnimationDelayFeature.cs
│   └── UiToolkitPresenterFeature.cs
└── Views/                 # 헬퍼 컴포넌트
    ├── SafeAreaHelperView.cs
    ├── NonDrawingView.cs
    └── AdjustScreenSizeFitterView.cs

Editor/
├── UiConfigsEditor.cs     # 향상된 인스펙터
└── UiPresenterManagerWindow.cs  # 실시간 디버깅 및 관리
```

## 버전 이력

버전 이력과 릴리스 노트는 [CHANGELOG.md](../CHANGELOG.md)를 참조하세요.

## 기여하기

기여 가이드라인은 메인 [README.md](../README.md)를 참조하세요.
