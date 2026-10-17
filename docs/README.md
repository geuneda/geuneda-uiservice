# UI Service 문서

설치와 최소 예제는 패키지 [README](../README.md)에 있습니다. 이 폴더는 상세 문서입니다.

| 문서 | 내용 |
|------|------|
| [시작하기](getting-started.md) | 설치, 설정 에셋, 프리팹 준비, 첫 프레젠터, 데이터 프레젠터 |
| [핵심 개념](core-concepts.md) | 서비스 인터페이스, 생명주기, 피처, 레이어, 세트, 다중 인스턴스, 설정, 에디터 도구 |
| [API 레퍼런스](api-reference.md) | 공개 API 시그니처와 동작 |
| [고급 주제](advanced.md) | 헬퍼 뷰, 커스텀 로더, 로딩 전략 |
| [뒤로가기, 팝업 큐, 모달 dim, 입력 차단](navigation.md) | 화면 맨 위 규칙, 뒤로가기 처리 순서, 큐 진행 조건, dim·차단막 배치 |
| [문제 해결](troubleshooting.md) | 자주 겪는 문제와 해결 방법 |

## 아키텍처

```
게임 코드
   │  IUiService (열기/닫기/로드/언로드/조회)
   ▼
UiService : IUiServiceInit (Init(UiConfigs), Dispose())
   ├── 표시 목록 변경과 알림 (UiService.Visibility.cs)
   ├── 표시 상태 이벤트, 뒤로가기, 입력 차단, 모달 dim (UiService.Navigation.cs)
   ├── UiConfigs (ScriptableObject: UiConfig 목록 + UI 세트)
   ├── IUiAssetLoader (Addressables / Resources / PrefabRegistry)
   └── UiPresenter (프리팹 루트 컴포넌트)
          └── PresenterFeatureBase 컴포넌트들 (전환, UI Toolkit 등)

UiPopupQueue : IUiPopupQueue (게임이 장면마다 만듦)
   └── IUiService 로 팝업을 열고 PresenterClosed 로 다음 팝업을 띄움
```

- `Init()` 은 `"Ui"` 라는 `DontDestroyOnLoad` 루트 GameObject 를 만들고, 로드된 프레젠터는 모두 이 루트 바로 아래에 인스턴스화됩니다.
- 루트에는 `UiServiceMonoComponent` 가 붙어 해상도와 기기 방향 변화를 감지해 `UiService.OnResolutionChanged`, `UiService.OnOrientationChanged` 정적 이벤트를 발생시킵니다.
- 루트 아래에 입력 차단막(`UiInputBlocker`, UI Toolkit 프레젠터가 있으면 `UiInputBlockerPanel`)과 쉬고 있는 dim 이 있고, `ReadsBackInput` 이 켜지면 `UiBackInputReader` 가 붙습니다.

## 패키지 구조

```
Runtime/                              (어셈블리 Geuneda.UiService)
├── IUiService.cs                     IUiService, IUiServiceInit, UiInstance
├── UiService.cs                      서비스 구현 (로드, 열기, 닫기, 세트)
├── UiService.Visibility.cs           표시 목록 변경과 알림(디스패처), UI Toolkit·uGUI 정렬, dim 갱신
├── UiService.Navigation.cs           표시 상태 이벤트 선언, 뒤로가기, 입력 차단, 모달 dim 설정
├── UiPresenter.cs                    UiPresenter, UiPresenter<T>, IUiPresenterData
├── UiConfigs.cs                      UiConfig, UiConfigs(abstract)
├── AddressablesUiConfigs.cs          Addressables 용 설정 에셋
├── ResourcesUiConfigs.cs             Resources 용 설정 에셋
├── PrefabRegistryUiConfigs.cs        프리팹 직접 참조용 설정 에셋
├── UiSetConfig.cs                    UiSetConfig, UiSetEntry, UiSetConfigSerializable
├── UiInstanceId.cs                   다중 인스턴스 식별자
├── UiServiceMonoComponent.cs         해상도/방향 감지
├── LoadSynchronouslyAttribute.cs     동기 로드 표시 어트리뷰트
├── Navigation/                       UiInputBlock, 차단막, 모달 dim, 뒤로가기 키 리더 (UiInputBlock 외 internal)
├── Queue/                            IUiPopupQueue, UiPopupQueue, UiPopupQueueHold, 큐 사이 팩토리 조정(internal UiPopupQueueGate)
├── Loaders/                          IUiAssetLoader 와 구현 3종
├── Features/                         PresenterFeatureBase, ITransitionFeature, 기본 피처
└── Views/                            헬퍼 뷰 (네임스페이스 Geuneda.UiService.Views)

Editor/                               (어셈블리 Geuneda.UiService.Editor, 네임스페이스 GeunedaEditor.UiService)
├── UiConfigsEditorBase.cs            설정 인스펙터 공통 기반 (레이어 시각화, 세트 편집)
├── AddressablesUiConfigsEditor.cs    Addressables 그룹과 동기화
├── ResourcesUiConfigsEditor.cs       Resources 폴더와 동기화
├── PrefabRegistryUiConfigsEditor.cs  프리팹 레지스트리 편집
├── DefaultUiConfigsEditor.cs         DefaultUiSetId 와 기본 인스펙터 3종
├── UiConfigsMenuItems.cs             Tools > UI Service 메뉴
├── UiPresenterManagerWindow.cs       플레이 모드 프레젠터 관리 창
├── UiPresenterEditor.cs              프레젠터 인스펙터 (열기/닫기 버튼)
└── NonDrawingViewEditor.cs           NonDrawingView 인스펙터

Tests/EditMode, Tests/PlayMode        단위/통합/성능/스모크 테스트
```
