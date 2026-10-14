# 뒤로가기, 팝업 큐, 모달 dim, 입력 차단

2.0.0 에서 추가된 기능입니다. 모두 `UiService` 가 관리하는 표시 목록(연 순서)과 레이어를 기준으로 동작합니다.

- [화면 맨 위](#화면-맨-위)
- [뒤로가기](#뒤로가기)
- [모달 dim](#모달-dim)
- [입력 차단](#입력-차단)
- [팝업 큐](#팝업-큐)
- [표시 상태 이벤트](#표시-상태-이벤트)

---

## 화면 맨 위

뒤로가기 대상과 dim 위치는 "표시 중인 프레젠터 중 화면 맨 위"로 정합니다.

- 레이어(`UiConfig.Layer`, 설정 없이 `AddUi` 했으면 넘긴 레이어)가 높을수록 위입니다.
- 같은 레이어면 나중에 연 프레젠터가 위입니다. 닫았다가 다시 열면 맨 뒤로 갑니다.
- `OnOpened` 안에서 다른 UI 를 열어도 연 순서가 지켜집니다(2.0.0 부터 표시 목록에 먼저 넣고 엽니다).
- UI Toolkit(`UIDocument`) 프레젠터는 화면에서도 이 순서로 그려지도록 서비스가 열고 닫을 때마다 `sortingOrder` 를 `레이어 + 순위 × 1/1024` 로 다시 매깁니다. 순위는 마지막으로 연 순서이고, 닫기 전환 중이라 아직 그려지는 문서도 함께 매겨 새로 연 문서보다 위에 남지 않습니다. 같은 레이어에 1023 개를 넘게 띄우지 않는 한 다음 레이어를 넘지 않습니다.
- uGUI 는 같은 `sortingOrder` 캔버스끼리의 앞뒤를 Unity 가 정하고 형제 순서로는 바뀌지 않아(실험으로 확인) 서비스가 맞출 수 없습니다. 같은 레이어에 겹치는 uGUI 를 띄우면 뒤로가기는 나중에 연 것을 닫지만 화면에서는 그것이 아래에 있을 수 있습니다. 겹치는 팝업은 레이어를 나눕니다. 모달이 다른 uGUI 와 레이어를 같이 쓰면 타입마다 한 번 경고를 남깁니다.

---

## 뒤로가기

프레젠터가 뒤로가기에 참여하려면 `HandlesBack` 을 재정의합니다. 기본값은 false 라 HUD 같은 화면은 뒤로가기로 닫히지 않습니다.

```csharp
public class ShopPopup : UiPresenter
{
    protected override bool HandlesBack => true;   // 뒤로가기로 닫힘(기본 동작 Close(false))
}

public class ForcedUpdatePopup : UiPresenter
{
    protected override bool HandlesBack => true;

    // 닫을 수 없는 팝업: 재정의해서 닫지 않습니다. 뒤로가기는 여기서 끝납니다
    protected override void OnBackPressed()
    {
        ShowToast("업데이트가 필요합니다");
    }
}
```

`IUiService.HandleBack()` 은 다음 순서로 처리합니다.

1. 입력이 막혀 있으면([입력 차단](#입력-차단)) 아무것도 하지 않고 false 를 돌려줍니다.
2. `AddBackHandler` 로 추가한 핸들러를 우선순위가 높은 것부터, 같으면 나중에 추가한 것부터 부릅니다. 하나가 true 를 돌려주면 끝납니다.
3. 화면 맨 위 뒤로가기 대상의 `OnBackPressed()` 를 부릅니다.
4. 대상이 없으면 `BackUnhandled` 를 발생시키고 false 를 돌려줍니다. 게임 종료 확인 팝업을 여기에 연결합니다.

```csharp
// 서비스가 관리하지 않는 화면(네이티브 웹뷰 등)이 먼저 뒤로가기를 받게 합니다
var registration = uiService.AddBackHandler(() =>
{
    if (!webView.IsOpen) return false;
    webView.Close();
    return true;
}, priority: 100);

// 화면을 떠날 때
registration.Dispose();

// 닫을 것이 없을 때
uiService.BackUnhandled += () => uiService.OpenUiAsync<QuitConfirmPopup>().Forget();
```

### 키 입력

`UiService.ReadsBackInput = true` 로 두면 서비스가 매 프레임 Escape 키(Android 뒤로가기 포함)를 읽어 `HandleBack()` 을 부릅니다. 기본값은 false 입니다.

- Input System 패키지(`com.unity.inputsystem`)가 있고 Active Input Handling 에 켜져 있으면 `<Keyboard>/escape` 에 바인딩한 `InputAction` 을 씁니다. Android 뒤로가기는 `Keyboard.current` 가 아닌 키보드 장치로 한 프레임 안에 눌렸다 떼어질 수 있어, `Keyboard.current.escapeKey.wasPressedThisFrame` 로는 놓칩니다(에뮬레이터에서 확인).
- 그렇지 않으면 레거시 `Input.GetKeyDown(KeyCode.Escape)` 를 씁니다. 둘 다 없으면 아무것도 읽지 않습니다.
- 게임이 직접 입력을 받는다면 끄고 두고 입력 처리 코드에서 `HandleBack()` 을 부릅니다.

```csharp
var uiService = new UiService { ReadsBackInput = true };
uiService.Init(uiConfigs);
```

---

## 모달 dim

`IsModal` 을 true 로 재정의하면 그 프레젠터가 화면 맨 위 모달일 때 공용 dim 이 바로 뒤에 깔립니다. 모달이 여러 개 떠 있어도 dim 은 맨 위 모달 하나에만 깔리므로 아래쪽이 겹쳐 어두워지지 않습니다. dim 은 그 아래 UI 의 입력도 막습니다.

```csharp
public class RewardPopup : UiPresenter
{
    protected override bool HandlesBack => true;
    protected override bool IsModal => true;

    // 바깥(dim)을 누르면 닫기. 기본 동작은 없습니다
    protected override void OnDimClicked()
    {
        Close(false);
    }
}

// 색 바꾸기 (기본 검정 60%)
uiService.DimColor = new Color(0f, 0f, 0f, 0.75f);
```

| 프레젠터 루트 | dim 위치 |
|---------------|----------|
| `Canvas` | dim GameObject(`UiDim`, `Image` + 클릭 전달)를 프레젠터의 첫 자식으로 넣고 화면을 채움 |
| `UIDocument` | 같은 `PanelSettings` 에 모달 문서보다 반 단계(1/2048) 낮은 정렬값의 dim 문서(`UiDimDocument`)를 둠. 같은 레이어의 먼저 연 문서는 dim 아래로 감. UIDocument 루트는 기본 높이가 0 이라 프레젠터 안에 넣지 않음 |
| 그 밖(`PanelRenderer` 등) | dim 을 깔지 않고 경고를 남김 |

- 모달이 닫히기 시작하면 dim 은 곧바로 다음 모달로 옮겨 가거나 숨겨집니다(닫기 전환을 기다리지 않음).
- uGUI 클릭은 씬에 `EventSystem` 이 있어야 받습니다.
- uGUI 모달은 같은 레이어의 다른 uGUI 와 dim 의 앞뒤를 보장하지 않습니다([화면 맨 위](#화면-맨-위)). 모달은 다른 UI 보다 높은 레이어에 둡니다.

---

## 입력 차단

로딩, 결제, 서버 응답 대기처럼 잠시 모든 UI 입력을 막을 때 씁니다.

```csharp
using (uiService.BlockInput("purchase"))
{
    await purchaseService.BuyAsync(productId);
}
```

- 토큰이 하나라도 남아 있으면 막힌 상태입니다. 순서와 관계없이 풀 수 있습니다.
- 같은 토큰을 두 번 Dispose 하면 차단 수를 바꾸지 않고 경고만 남깁니다. `default(UiInputBlock)` 은 아무것도 하지 않습니다.
- 차단이 풀리지 않으면 `InputBlockReasons` 로 남은 사유를 확인합니다.
- 막힌 동안에는 `HandleBack()` 도 무시합니다.

차단막은 포인터 입력을 막습니다. 이미 선택되거나 포커스된 대상으로 가는 키보드·게임패드 입력도 막기 위해, 첫 토큰을 받을 때 다음을 하고 마지막 토큰이 풀리면 되돌립니다.

- `EventSystem` 의 선택을 풀고 `sendNavigationEvents` 를 끕니다(Submit, Move, Cancel 차단). 풀 때 원래 값으로 돌립니다. 선택은 복구하지 않으므로 게임패드 UI 는 차단이 풀린 뒤 선택을 다시 지정합니다.
- 켜진 UIDocument 프레젠터의 패널마다 포커스를 풀고, 패널로 오는 `KeyDownEvent` 와 내비게이션 이벤트를 루트에서 멈춥니다.
- 막힌 동안에는 매 프레임(그리고 UI 를 열 때마다) 새로 생긴 `EventSystem` 과 새로 붙은 패널에도 같은 처리를 하고, 그사이 생긴 선택(입력 필드 자동 선택, 선택 콜백 도중이라 풀지 못한 선택)도 풉니다. 이 확인은 막힌 동안에만 돕니다.

차단막은 정렬값 32767 로 모든 프레젠터 위에 깔립니다.

| 차단막 | 막는 입력 |
|--------|-----------|
| uGUI 오버레이 캔버스(`UiInputBlocker`, 그리지 않는 `NonDrawingView`) | `EventSystem` 으로 입력을 받는 uGUI 와 UI Toolkit |
| UI Toolkit 패널(`UiInputBlockerPanel`) | EventSystem 없이 입력을 받는 UI Toolkit. UIDocument 프레젠터가 처음 추가될 때 만듦 |

---

## 팝업 큐

로비에 들어올 때 공지, 이벤트, 출석 보상 팝업을 하나씩 띄우는 용도입니다. 장면마다 `UiPopupQueue` 를 만들고, 떠날 때 Dispose 합니다. 서비스보다 먼저 Dispose 해야 합니다.

```csharp
_queue = new UiPopupQueue(uiService);

// 우선순위가 높은 것부터, 같으면 넣은 순서대로
_queue.Enqueue("daily-reward", 100, ct => uiService.OpenUiAsync(typeof(DailyRewardPopup), ct));

// 띄울 시점의 상태로 판단해 null 을 돌려주면 건너뜁니다
_queue.Enqueue("notice:42", 50, async ct =>
{
    if (!noticeService.HasUnread(42)) return null;
    return await uiService.OpenUiAsync<NoticePopup, NoticeData>(new NoticeData(42), ct);
});

_queue.Drained += () => tutorial.StartIfNeeded();
```

- 팝업 하나가 닫히고 닫기 전환까지 끝나야 다음 팝업을 띄웁니다. 닫기 전환 중에 다시 열리면 계속 그 항목을 표시 중으로 보고(키도 유지), 다시 닫힐 때까지 기다립니다. 팩토리가 돌려주기 전에 닫힌 팝업도 그 닫기 전환을 기다립니다.
- `key` 가 같은 항목이 대기 중이거나 표시 중이면 `Enqueue` 가 false 를 돌려줍니다. 서버 알림이 여러 번 와도 한 번만 뜹니다.
- 팩토리가 예외를 던지면 기록하고 다음 항목으로 넘어갑니다.
- `Drained` 는 큐가 띄운 팝업이 끝났을 때(건너뛰거나 실패한 경우 포함) 대기 항목이 없으면 발생합니다.

### 멈춤과 진행 조건

큐는 다음 경우에 다음 팝업을 띄우지 않고 기다립니다. 조건은 이벤트로 다시 확인하므로 매 프레임 확인하지 않습니다.

- `Hold` 토큰이 하나라도 남아 있을 때. 튜토리얼이나 연출 동안 멈춥니다.
- 큐 밖에서 연 뒤로가기 대상(`HandlesBack`)이 표시 중일 때. 사용자가 연 상점 팝업이나 다른 큐의 팝업이 닫힐 때까지 기다립니다.

```csharp
using (_queue.Hold("unit-card-reveal"))
{
    await PlayRevealAsync();
}
```

큐가 멈춰 있으면 `HoldReasons` 와 표시 중인 뒤로가기 대상을 확인합니다. 이미 띄운 팝업은 `Hold` 나 `Clear` 로 닫히지 않습니다.

한 서비스에 큐를 여러 개 두면, 큐끼리는 이미 표시된 팝업만 보고 기다립니다. 두 큐가 동시에 팩토리를 시작하면(로드 중) 서로를 기다리지 않아 두 팝업이 함께 뜰 수 있습니다. 순서가 중요한 팝업은 한 큐에 넣습니다.

---

## 표시 상태 이벤트

`PresenterOpened`, `PresenterClosed` 는 프레젠터가 표시 목록에 들어가거나 빠진 직후, 그 프레젠터의 열기·닫기 훅(`OnOpened`, `OnClosed`)보다 먼저 발생합니다. `PresenterClosed` 시점에 `CloseTransitionTask` 는 이미 이번 닫기의 태스크입니다.

- 열기·닫기는 표시 목록과 세대를 즉시 바꾸고, 알림과 훅 실행은 하나씩 차례로 처리합니다. 구독자나 훅 안에서 다른 UI 를 열고 닫으면 `IsVisible` 은 바로 바뀌지만, 그 알림과 훅은 지금 처리 중인 것이 끝난 뒤에 실행됩니다. 그래서 모든 구독자가 같은 순서로 알림을 받고, 열기·닫기 훅(`OnOpened`, `OnClosed`, 피처의 시작·종료 콜백)이 다른 열기·닫기 훅 도중에 끼어들지 않습니다. 전환을 기다린 뒤 부르는 `OnOpenTransitionCompleted`, `OnCloseTransitionCompleted` 는 이 순서 밖에서 실행되므로, 그 안에서 연 UI 의 훅은 곧바로 실행됩니다.
- 먼저 시작한 열기·닫기가 실행되기 전에 반대 동작이 준비되면, 먼저 것은 훅을 부르지 않고 태스크만 완료합니다. 훅이나 피처 콜백 도중에 반대 동작이 준비되면 그 뒤 단계는 진행하지 않습니다.
- 표시 중이든 아니든 `RemoveUi`/`UnloadUi` 로 빠진 프레젠터는 실행 중인 열기·닫기를 멈추고, 그 등록에서 진행 중이던 열기·닫기 태스크(이미 새 단계로 교체된 이전 단계 포함)가 모두 완료됩니다. 빠진 뒤(제거 알림 안 포함) 다시 등록해 연 태스크는 영향을 받지 않습니다. 알림 도중에 빠진 프레젠터는 열지 않습니다.
- 열기·닫기 훅(`TimeDelayFeature`, `AnimationDelayFeature` 의 시작·완료 훅 포함)이 예외를 던지면 그 단계는 멈추고, 예외는 한 번 기록되며 전환 태스크는 같은 예외로 실패합니다(기다리는 쪽도 예외를 받음). 닫기가 실패하면 화면에 남지 않게 숨기고 요청된 언로드를 마친 뒤 실패를 알립니다(정리 중 예외가 나도 실패는 알리고, 정리 예외도 따로 기록). 피처의 닫기 시작 훅이 실패하면 `OnClosed` 로 넘어가기 전에 그 단계가 실패하고, 반대 동작이나 다른 예외로 대기 없이 끝난 단계에서 시작된 피처 전환의 실패도 한 번씩 기록됩니다. 여러 피처가 실패하면 첫 예외로 단계가 실패하고 나머지도 각각 한 번 기록됩니다. 이전에 실패한 전환이 다음 열기·닫기에 넘어오지 않습니다.
- `UnloadUi` 의 로더가 예외를 던져도 빠진 등록의 진행 중 태스크는 완료되고, 예외는 호출자에게 전달됩니다. 정렬과 dim 갱신 중 예외는 기록하고 남은 변경을 계속 처리합니다.
- 알림이나 훅 안에서 `Dispose` 하면, 알림 중이던 변경과 대기 중인 닫기를 먼저 실행해 표시 중이던(또는 닫히던) UI 의 닫기 훅이 언로드 전에 한 번 불립니다.

- 표시 중인 프레젠터를 `RemoveUi`/`UnloadUi` 로 빼도 `PresenterClosed` 가 발생합니다.
- 구독자 예외는 기록만 하고 다른 구독자에게 계속 전달합니다. `Dispose()` 중에는 발생하지 않습니다.
- 인스턴스 ID 로 프레젠터를 찾으려면 `TryGetUi(UiInstanceId, out UiPresenter)` 를 씁니다.
