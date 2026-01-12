# Geuneda UI Service

[![Unity Version](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unity 게임 UI를 효율적으로 관리하는 서비스 패키지입니다.

## 왜 이 패키지를 사용해야 하나요?

| 문제점 | 해결책 |
|--------|--------|
| 분산된 UI 로직 | 중앙 집중식 서비스로 UI 생명주기 관리 |
| 메모리 관리 복잡성 | Addressables 통합으로 자동 로드/언로드 |
| 복잡한 비동기 로딩 | UniTask 기반 비동기 작업 지원 |
| 중복된 보일러플레이트 | 피처 조합 시스템으로 확장 |

## 주요 기능

- **UI MVP 패턴**: UI 로직의 깔끔한 분리
- **UI Toolkit 지원**: uGUI와 UI Toolkit 모두 호환
- **피처 조합**: 모듈식 피처 시스템
- **비동기 로딩**: UniTask 기반 비동기 에셋 로딩
- **UI 그룹**: 레이어별 UI 구성 및 일괄 작업

## 설치 방법

### Unity Package Manager (Git URL)

1. **Window → Package Manager** 열기
2. **+** 버튼 → **Add package from git URL...**
3. URL 입력:
```
https://github.com/geuneda/geuneda-uiservice.git
```

또는 `Packages/manifest.json`에 직접 추가:
```json
{
  "dependencies": {
    "com.geuneda.uiservice": "https://github.com/geuneda/geuneda-uiservice.git#v1.0.0"
  }
}
```

## 요구 사항

- Unity 6000.0 이상
- Addressables 2.6.0+
- UniTask 2.5.10+

## 빠른 시작

### 1. UiConfigs 생성

Project View에서 우클릭 → **Create → ScriptableObjects → Configs → UiConfigs**

### 2. UiPresenter 구현

```csharp
using Geuneda.UiService;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPresenter : UiPresenter
{
    [SerializeField] private Button playButton;

    protected override void OnOpen()
    {
        playButton.onClick.AddListener(OnPlayClicked);
    }

    protected override void OnClose()
    {
        playButton.onClick.RemoveListener(OnPlayClicked);
    }

    private void OnPlayClicked()
    {
        // 게임 시작
    }
}
```

### 3. 데이터가 있는 UI

```csharp
public class PlayerInfoPresenter : UiPresenter<PlayerData>
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;

    protected override void OnDataChanged(PlayerData data)
    {
        nameText.text = data.Name;
        levelText.text = $"Lv.{data.Level}";
    }
}

// 사용
var playerData = new PlayerData { Name = "영웅", Level = 10 };
await UiService.OpenUiAsync<PlayerInfoPresenter, PlayerData>(playerData);
```

### 4. UI Set 사용

```csharp
// 여러 UI를 세트로 관리 (HUD: 체력바, 미니맵 등)
await UiService.OpenUiSetAsync(hudSet);
await UiService.CloseUiSetAsync(hudSet);
```

## 피처 시스템

내장 피처:
- **TimeDelayFeature**: 시간 기반 지연
- **AnimationDelayFeature**: 애니메이션 기반 지연
- **UiToolkitPresenterFeature**: UI Toolkit 통합

커스텀 피처:
```csharp
public class FadeFeature : PresenterFeatureBase
{
    public override async UniTask OnOpenAsync()
    {
        // 페이드 인 로직
    }
}
```

## 에셋 로딩 전략

| 전략 | 설명 |
|------|------|
| **PrefabRegistry** | 직접 참조, 항상 로드 |
| **Addressables** | 주소 기반, 번들링 |
| **Resources** | 경로 기반, 간단한 프로젝트 |

## 라이센스

MIT License

원본 저작권: Miguel Tomas (GameLovers)
