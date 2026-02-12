# 문제 해결

일반적인 문제와 해결 방법입니다.

## 목차

- [UiPresenter<TData>에서 데이터가 표시되지 않음](#uipresentertdata에서-데이터가-표시되지-않음)
- [애니메이션이 재생되지 않음](#애니메이션이-재생되지-않음)
- [UiConfig 미등록 오류](#uiconfig-미등록-오류)
- [열기 시 UI 깜빡임](#열기-시-ui-깜빡임)
- [취소가 작동하지 않음](#취소가-작동하지-않음)
- [도움 받기](#도움-받기)

---

## UiPresenter<TData>에서 데이터가 표시되지 않음

**증상:** UI가 열리지만 데이터 필드가 비어 있거나 기본값을 표시합니다.

### 원인 1: 제네릭 열기 메서드를 사용하지 않음

```csharp
// 잘못된 예 - 데이터가 전달되지 않음
await _uiService.OpenUiAsync<PlayerProfile>();

// 올바른 예 - 데이터가 포함된 제네릭 오버로드 사용
var data = new PlayerData { Name = "Hero", Level = 10 };
await _uiService.OpenUiAsync<PlayerProfile, PlayerData>(data);
```

### 원인 2: OnSetData가 구현되지 않음

```csharp
public class PlayerProfile : UiPresenter<PlayerData>
{
    // 잘못됨 - OnSetData를 반드시 오버라이드해야 함

    // 올바른 예
    protected override void OnSetData()
    {
        nameText.text = Data.Name;
        levelText.text = Data.Level.ToString();
    }
}
```

### 원인 3: 잘못된 기본 클래스

```csharp
// 잘못된 예 - <T> 제네릭 누락
public class PlayerProfile : UiPresenter

// 올바른 예
public class PlayerProfile : UiPresenter<PlayerData>
```

---

## 애니메이션이 재생되지 않음

**증상:** `AnimationDelayFeature`가 있는 UI가 애니메이션 없이 즉시 열리거나 닫힙니다.

### 원인 1: 피처 컴포넌트가 부착되지 않음

```csharp
// 잘못된 예 - 피처 컴포넌트 없음
public class AnimatedPopup : UiPresenter
{
}

// 올바른 예 - 피처를 요구하고 참조
[RequireComponent(typeof(AnimationDelayFeature))]
public class AnimatedPopup : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;
}
```

### 원인 2: 애니메이션 클립이 할당되지 않음

인스펙터에서 확인하세요:
- `Animation Component`가 할당됨
- `Intro Animation Clip`이 할당됨
- `Outro Animation Clip`이 할당됨

**대안:** 애니메이션 없이 간단한 딜레이에는 `TimeDelayFeature`를 사용하세요:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class SimpleDelayedPopup : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;
    // 인스펙터에서 딜레이 시간 설정
}
```

### 원인 3: Animation 컴포넌트 누락

`AnimationDelayFeature`는 GameObject에 `Animation` 컴포넌트가 필요합니다.

1. 프레젠터 GameObject에 `Animation` 컴포넌트를 추가합니다
2. 또는 `AnimationClip` 에셋과 함께 `Animator`를 사용합니다

---

## UiConfig 미등록 오류

**오류:** `KeyNotFoundException: The UiConfig of type X was not added to the service`

**원인:** UI 타입이 `UiConfigs` 에셋에 등록되지 않았습니다.

### 해결 방법

1. `UiConfigs` ScriptableObject 에셋을 엽니다
2. 프레젠터 타입에 대한 새 항목을 추가합니다:
   - **Type**: 프레젠터 클래스 선택
   - **Addressable Address**: Addressable 키 설정 (예: `UI/MyPresenter`)
   - **Layer**: 깊이 레이어 번호 설정
3. 에셋을 저장합니다

**검증:**
```csharp
// UiConfigs 인스펙터에서:
// Type: MyNewPresenter
// Addressable: Assets/UI/MyNewPresenter.prefab
// Layer: 2
```

---

## 열기 시 UI 깜빡임

**증상:** UI가 올바른 표시 전에 잘못된 위치나 상태로 잠깐 나타납니다.

**원인:** 초기화 중에 프리팹의 루트 GameObject가 활성화 상태입니다.

### 해결 방법

1. UI 프리팹을 엽니다
2. 루트 GameObject를 **비활성화**합니다 (인스펙터 상단의 체크박스 해제)
3. 프리팹을 저장합니다

UI Service가 표시 준비가 되면 활성화합니다.

**참고:** UI를 동적으로 생성하는 경우:
```csharp
var go = Instantiate(prefab);
go.SetActive(false); // 비활성화 확인
_uiService.AddUi(go.GetComponent<UiPresenter>(), layer: 3, openAfter: true);
```

---

## 취소가 작동하지 않음

**증상:** `CancellationToken`이 UI 로딩을 중지하지 않습니다.

### 원인 1: 토큰을 전달하지 않음

```csharp
// 잘못된 예 - 토큰 미사용
await _uiService.OpenUiAsync<Shop>();

// 올바른 예 - 토큰 전달
var cts = new CancellationTokenSource();
await _uiService.OpenUiAsync<Shop>(cancellationToken: cts.Token);
```

### 원인 2: 너무 늦게 취소

취소는 비동기 로드 단계에서만 작동합니다:

```csharp
var cts = new CancellationTokenSource();

// 로딩 시작
var task = _uiService.OpenUiAsync<Shop>(cts.Token);

// await 완료 전에 취소
cts.Cancel(); // 아직 로딩 중이면 작동

await task; // 이미 완료됨
cts.Cancel(); // 너무 늦음 - 효과 없음
```

### 원인 3: 예외를 처리하지 않음

```csharp
var cts = new CancellationTokenSource();

try
{
    await _uiService.OpenUiAsync<Shop>(cts.Token);
}
catch (OperationCanceledException)
{
    // 취소 처리
    Debug.Log("Loading was cancelled");
}
```

---

## 자주 하는 실수

### 같은 UI를 여러 번 열기

```csharp
// 잘못된 예 - 중복으로 열릴 수 있음
void Update()
{
    if (Input.GetKeyDown(KeyCode.I))
        _uiService.OpenUiAsync<Inventory>().Forget();
}

// 올바른 예 - 가시성 먼저 확인
void Update()
{
    if (Input.GetKeyDown(KeyCode.I) && !_uiService.IsVisible<Inventory>())
        _uiService.OpenUiAsync<Inventory>().Forget();
}
```

### 초기화 누락

```csharp
// 잘못된 예 - 서비스 미초기화
private IUiServiceInit _uiService = new UiService();

async void Start()
{
    await _uiService.OpenUiAsync<Menu>(); // 예외 발생!
}

// 올바른 예 - 먼저 Init 호출
void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);
}
```

### Dispose 미호출

```csharp
// 잘못된 예 - 메모리 누수
void OnDestroy()
{
    // 서비스가 여전히 참조를 보유
}

// 올바른 예 - 정리
void OnDestroy()
{
    _uiService?.Dispose();
}
```

---

## 도움 받기

### 보고 전 확인사항

1. **Unity Console 확인** - UiService의 경고/오류 메시지 확인
2. **의존성 확인** - UniTask와 Addressables가 올바르게 설치되었는지 확인
3. **CHANGELOG 검토** - 최근 버전에서 호환성 변경 사항이 있는지 확인
4. **이슈 검색** - 이미 GitHub에 보고된 문제인지 확인

### 버그 보고

[GitHub Issues](https://github.com/CoderGamester/com.gamelovers.uiservice/issues)에서 이슈를 생성하세요:

- **Unity 버전** (예: 6000.0.5f1)
- **패키지 버전** (`package.json`에서 확인)
- **재현 코드** 샘플
- **오류 로그** (전체 스택 트레이스)
- **재현 단계**

### 기능 요청

[GitHub Discussions](https://github.com/CoderGamester/com.gamelovers.uiservice/discussions)에서:
- 새 기능 제안
- 질문하기
- 패키지 사용 방법 공유

---

## 디버그 체크리스트

문제가 발생하면 다음 순서로 확인하세요:

- [ ] UI Service가 `Init(uiConfigs)`로 초기화됨
- [ ] 프레젠터 타입이 `UiConfigs`에 등록됨
- [ ] 프리팹이 Addressable로 표시됨
- [ ] Addressable 주소가 `UiConfigs` 항목과 일치
- [ ] 프리팹 루트 GameObject가 비활성화 상태
- [ ] UniTask와 Addressables 패키지가 설치됨
- [ ] 프로젝트에 컴파일 오류 없음
- [ ] 피처 컴포넌트(있는 경우)가 부착되고 설정됨
