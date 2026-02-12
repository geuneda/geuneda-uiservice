# 고급 주제

이 문서에서는 성능 최적화와 헬퍼 컴포넌트를 포함한 고급 기능을 다룹니다.

## 목차

- [헬퍼 뷰](#헬퍼-뷰)
- [성능 최적화](#성능-최적화)
- [알려진 제한사항](#알려진-제한사항)

---

## 헬퍼 뷰

일반적인 UI 요구사항을 위한 내장 컴포넌트입니다.

### SafeAreaHelperView

기기 안전 영역(노치, Dynamic Island, 둥근 모서리)에 맞게 UI를 자동으로 조정합니다.

```csharp
// 안전 영역을 고려해야 하는 모든 RectTransform에 추가
gameObject.AddComponent<SafeAreaHelperView>();
```

**용도:** 헤더 바, 하단 네비게이션, 가려지면 안 되는 전체 화면 콘텐츠.

### NonDrawingView

렌더링 없이 레이캐스트를 차단하는 보이지 않는 그래픽. 드로우 콜을 줄입니다.

```csharp
// 알파=0인 Image 대신 사용
gameObject.AddComponent<NonDrawingView>();
```

**용도:** 보이지 않는 터치 차단기, 시각적 요소가 필요 없는 모달 배경.

### AdjustScreenSizeFitterView

최소 및 최대 제약 사이에서 반응형 크기 조정.

```csharp
var fitter = gameObject.AddComponent<AdjustScreenSizeFitterView>();
// 인스펙터에서 설정: 최소/최대 너비 및 높이
```

**용도:** 화면 크기에 적응하면서 범위 내에 유지해야 하는 패널.

### InteractableTextView

TextMeshPro 텍스트를 클릭 가능한 링크로 인터랙티브하게 만듭니다.

```csharp
// TextMeshPro 컴포넌트에 추가
gameObject.AddComponent<InteractableTextView>();

// TMP 텍스트에서 <link> 태그 사용:
// "Visit our <link=https://example.com>website</link>!"
```

**용도:** 이용 약관, 클릭 가능한 URL, 게임 내 하이퍼링크.

---

## 성능 최적화

### 로딩 전략

| 전략 | 사용 시점 | 트레이드오프 |
|------|----------|------------|
| **온디맨드** | 드문 UI, 큰 에셋 | 필요 시 로드, 끊김 발생 가능 |
| **사전 로드** | 자주 쓰는 UI, 핵심 경로 | 즉시 표시, 메모리 사용 |
| **세트 사전 로드** | 씬별 UI 그룹 | 일괄 효율성, 메모리 오버헤드 |

```csharp
// 온디맨드 (지연)
await _uiService.OpenUiAsync<SettingsMenu>();

// 사전 로드 (즉시)
await _uiService.LoadUiAsync<GameHud>();
// 나중에: 즉시 열기
await _uiService.OpenUiAsync<GameHud>();

// 세트 사전 로드
var tasks = _uiService.LoadUiSetAsync(setId: 1);
await UniTask.WhenAll(tasks);
```

### 메모리 관리

```csharp
// 닫기 (메모리에 유지, 빠른 재열기)
_uiService.CloseUi<Shop>(destroy: false);

// 닫기 및 메모리 해제
_uiService.CloseUi<Shop>(destroy: true);

// 또는 명시적으로 언로드
_uiService.UnloadUi<Shop>();
```

**파괴해야 하는 경우:**
- 큰 에셋 (>5MB)
- 레벨 변경 시 레벨 전용 UI
- 일회성 튜토리얼
- 자주 다시 여는 UI는 파괴하지 않음 (설정, 일시정지)
- 작고 가벼운 프레젠터는 파괴하지 않음

### 병렬 로딩

```csharp
// 느림 - 순차 (각각 1초면 3초)
await _uiService.OpenUiAsync<Hud>();
await _uiService.OpenUiAsync<Minimap>();
await _uiService.OpenUiAsync<Chat>();

// 빠름 - 병렬 (총 1초)
await UniTask.WhenAll(
    _uiService.OpenUiAsync<Hud>(),
    _uiService.OpenUiAsync<Minimap>(),
    _uiService.OpenUiAsync<Chat>()
);
```

### 로딩 화면에서 사전 로드

```csharp
public async UniTask LoadLevel()
{
    await _uiService.OpenUiAsync<LoadingScreen>();

    // UI와 레벨을 병렬로 로드
    var uiTask = UniTask.WhenAll(
        _uiService.LoadUiAsync<GameHud>(),
        _uiService.LoadUiAsync<PauseMenu>()
    );
    var levelTask = SceneManager.LoadSceneAsync("GameLevel").ToUniTask();

    await UniTask.WhenAll(uiTask, levelTask);

    _uiService.CloseUi<LoadingScreen>();
}
```

### 반복 열기 방지

```csharp
// 잘못된 예 - 누르고 있으면 매 프레임마다 열림
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
        _uiService.OpenUiAsync<PauseMenu>().Forget();
}

// 올바른 예 - 먼저 확인
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape) && !_uiService.IsVisible<PauseMenu>())
        _uiService.OpenUiAsync<PauseMenu>().Forget();
}
```

### 씬 정리

```csharp
async void OnLevelComplete()
{
    // 게임플레이 레이어 닫기
    _uiService.CloseAllUi(layer: 1);

    // 게임플레이 UI 세트 언로드
    _uiService.UnloadUiSet(setId: 2);

    // 레벨 완료 UI 로드
    await _uiService.OpenUiAsync<LevelCompleteScreen>();
}
```

### 피처 성능

**간단한 딜레이에는 TimeDelayFeature를 선호** (더 가벼움):
```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class SimplePopup : UiPresenter { }
```

**실제 애니메이션에만 AnimationDelayFeature를 사용**:
```csharp
[RequireComponent(typeof(AnimationDelayFeature))]
public class ComplexAnimatedPopup : UiPresenter { }
```

**모범 사례:**
- 반응성을 위해 딜레이를 0.5초 이하로 유지
- UI가 닫혀 있을 때 Animator 컴포넌트를 비활성화
- 불필요한 피처를 피하기 - 각각 오버헤드 추가

### 모니터링

```csharp
// 로드된 수 확인
var loaded = _uiService.GetLoadedPresenters();
Debug.Log($"Loaded: {loaded.Count}");

// 보이는 수 확인
Debug.Log($"Visible: {_uiService.VisiblePresenters.Count}");
```

**경고 신호:**
- 10개 이상의 프레젠터가 동시에 로드됨
- 같은 UI가 초당 여러 번 로드됨
- `UnloadUi` 후 메모리가 줄어들지 않음

Unity Profiler로 모니터링할 항목:
- `Memory.Allocations` - 열기 시 GC 스파이크
- `Memory.Total` - 로드/언로드 후 메모리
- `Loading.AsyncLoad` - 느린 에셋 로딩

---

## 알려진 제한사항

| 제한사항 | 설명 | 우회 방법 |
|---------|------|----------|
| **레이어 범위** | 레이어는 0-1000이어야 함 | 이 범위 내의 값 사용 |
| **UI Toolkit 버전** | UI Toolkit 기능은 Unity 6000.0+ 필요 | 이전 버전에서는 uGUI 사용 |
| **WebGL Task.Delay** | 표준 `Task.Delay`는 WebGL에서 실패 | 패키지가 자동으로 UniTask 사용 |
| **동기 로딩** | `LoadSynchronously`는 메인 스레드를 차단 | 핵심 시작 UI에만 제한적으로 사용 |
