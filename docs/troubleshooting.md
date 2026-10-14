# 문제 해결

- [`The UiConfig of type X was not added to the service`](#the-uiconfig-of-type-x-was-not-added-to-the-service)
- [`Init()` 에서 `ArgumentException`](#init-에서-argumentexception)
- [UiConfigs 인스펙터에 프레젠터가 나타나지 않음](#uiconfigs-인스펙터에-프레젠터가-나타나지-않음)
- [데이터가 표시되지 않거나 UI 가 열리지 않음](#데이터가-표시되지-않거나-ui-가-열리지-않음)
- [전환 애니메이션이 재생되지 않음](#전환-애니메이션이-재생되지-않음)
- [UI Toolkit 요소를 찾지 못함](#ui-toolkit-요소를-찾지-못함)
- [`Ambiguous call` 경고](#ambiguous-call-경고)
- [같은 UI 가 두 개 로드됨](#같은-ui-가-두-개-로드됨)
- [취소가 동작하지 않음](#취소가-동작하지-않음)
- [레이어 순서가 적용되지 않음](#레이어-순서가-적용되지-않음)
- [뒤로가기가 동작하지 않음](#뒤로가기가-동작하지-않음)
- [팝업 큐가 다음 팝업을 띄우지 않음](#팝업-큐가-다음-팝업을-띄우지-않음)
- [입력이 계속 막혀 있음](#입력이-계속-막혀-있음)
- [dim 이 보이지 않음](#dim-이-보이지-않음)
- [점검 목록](#점검-목록)

---

## `The UiConfig of type X was not added to the service`

`KeyNotFoundException` 입니다. 해당 타입의 `UiConfig` 가 서비스에 없습니다.

- `Init(uiConfigs)` 를 호출했는지, 올바른 `UiConfigs` 에셋을 넘겼는지 확인합니다.
- `UiConfigs` 인스펙터를 열어 목록에 프레젠터가 있는지 확인합니다. 목록은 인스펙터를 열 때 다시 만들어집니다.
- 코드로 등록한다면 `AddUiConfig` 를 먼저 호출합니다.

## `Init()` 에서 `ArgumentException`

- `has empty address`: 주소가 빈 설정이 있습니다.
- `has null UiType`: 저장된 타입 이름(`AssemblyQualifiedName`)으로 타입을 찾지 못했습니다. 프레젠터 클래스 이름, 네임스페이스, 어셈블리를 바꾼 경우에 생깁니다. `UiConfigs` 인스펙터를 다시 열어 동기화한 뒤 저장합니다.

## UiConfigs 인스펙터에 프레젠터가 나타나지 않음

- `UiPresenter` 파생 컴포넌트가 프리팹 **루트**에 있어야 합니다.
- `AddressablesUiConfigs`: 프리팹이 읽기 전용이 아닌 Addressable 그룹에 있어야 합니다.
- `ResourcesUiConfigs`: 프리팹 경로에 `/Resources/` 가 있어야 합니다.
- `PrefabRegistryUiConfigs`: 프리팹을 레지스트리 목록이나 드롭 영역에 넣어야 합니다.
- 같은 타입의 프리팹이 여러 개면 인스펙터에는 모두 보이지만 런타임에는 첫 설정만 등록되고 나머지는 `was already added` 경고와 함께 무시됩니다.

## 데이터가 표시되지 않거나 UI 가 열리지 않음

- 데이터가 필요한 프레젠터는 `UiPresenter<TData>` 를 상속하고 `OnSetData()` 를 재정의합니다.
- 데이터를 넘길 때는 `OpenUiAsync<TPresenter, TData>(data)` 를 사용합니다. 데이터 없는 `OpenUiAsync<T>()` 는 `Data` 를 설정하지 않습니다.
- 데이터 오버로드에 `UiPresenter<TData>` 가 아닌 프레젠터를 넘기면 `is not a UiPresenter...` 에러를 남기고 **열지 않습니다**.

## 전환 애니메이션이 재생되지 않음

- 피처 컴포넌트는 프레젠터와 **같은 GameObject** 에 있어야 합니다. 자식 오브젝트의 피처는 수집되지 않습니다.
- `AnimationDelayFeature` 는 레거시 `Animation` 컴포넌트를 사용합니다. `Animator` 로는 동작하지 않으며, 인트로/아웃트로 클립이 지정되어 있어야 합니다. 클립이 레거시 `Animation` 에서 재생 가능한 클립인지도 확인합니다.
- 애니메이션 없이 시간만 늦추려면 `TimeDelayFeature` 를 사용합니다.

## UI Toolkit 요소를 찾지 못함

서비스가 로드한 프레젠터는 `OnInitialized()` 시점에 비활성 상태라 `UIDocument` 의 비주얼 트리가 패널에 붙어 있지 않습니다. `UiToolkitPresenterFeature.AddVisualTreeAttachedListener` 로 콜백을 등록하고, 콜백 안에서 요소를 쿼리합니다. 다시 열 때 요소가 새로 만들어지므로 이전 요소의 이벤트를 해제하고 새 요소에 다시 등록합니다. 예제는 [핵심 개념 - UI Toolkit](core-concepts.md#ui-toolkit).

## `Ambiguous call` 경고

`[UiService] Ambiguous call for X: found N instances` 는 같은 타입의 인스턴스가 여러 개인데 주소 없는 오버로드(`GetUi<T>()`, `CloseUi<T>()` 등)를 호출했다는 뜻입니다. 첫 번째 인스턴스가 대상이 됩니다. 인스턴스 주소를 받는 `UiService` 오버로드를 사용하세요.

## 같은 UI 가 두 개 로드됨

주소 없는 `LoadUiAsync`/`OpenUiAsync` 는 설정 주소를 인스턴스 주소로 씁니다. 다음 경우에 같은 타입의 인스턴스가 하나 더 생깁니다.

- `AddUi` 로 추가한 프레젠터(인스턴스 주소가 빈 문자열)를 `OpenUiAsync<T>()` 로 열 때
- 런타임 `UiSetConfig` 에 설정 주소와 다른 인스턴스 주소(빈 문자열 포함)를 넣었을 때

인스턴스 주소를 일치시키거나, `UiService.OpenUiAsync(type, instanceAddress)` 로 명시해서 엽니다.

## 취소가 동작하지 않음

- 토큰은 `AddressablesUiAssetLoader` 의 비동기 인스턴스화 대기에만 전달됩니다. `ResourcesUiAssetLoader`, `PrefabRegistryUiAssetLoader` 는 동기로 인스턴스화하므로 취소할 구간이 없습니다.
- 이미 로드된 UI 를 여는 경우에도 기다리는 구간이 없습니다.
- 취소되면 `OperationCanceledException` 이 발생하므로 `try/catch` 로 처리합니다.

```csharp
using System;
using System.Threading;

var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await _uiService.OpenUiAsync<ShopPresenter>(cts.Token);
}
catch (OperationCanceledException)
{
    // 취소 처리
}
```

## 레이어 순서가 적용되지 않음

- 서비스는 프리팹 **루트**의 `Canvas`, `UIDocument`, `PanelRenderer`(Unity 6.5+) sortingOrder 만 설정합니다. 자식 Canvas 에는 적용되지 않습니다.
- uGUI 에서 sortingOrder 는 같은 Sorting Layer 안에서만 비교되므로 Canvas 의 Sorting Layer 도 확인합니다.

## 뒤로가기가 동작하지 않음

- 키 입력을 서비스가 읽게 하려면 `UiService.ReadsBackInput = true` 로 둡니다. 게임이 직접 입력을 받는다면 그 코드에서 `HandleBack()` 을 부릅니다.
- 프레젠터가 `HandlesBack` 을 true 로 재정의했는지 확인합니다. 기본값은 false 입니다.
- 입력 차단 토큰이 남아 있으면(`IsInputBlocked`) 뒤로가기를 무시합니다.
- 우선순위가 높은 `AddBackHandler` 핸들러가 true 를 돌려주고 있지 않은지 확인합니다.
- Input System 만 켠 프로젝트에서는 `com.unity.inputsystem` 패키지가 있어야 Escape 를 읽습니다.

## 팝업 큐가 다음 팝업을 띄우지 않음

- `HoldReasons` 에 남은 멈춤 토큰이 있는지 확인합니다.
- 큐 밖에서 연 `HandlesBack` 프레젠터(다른 큐의 팝업 포함)가 표시 중이면 닫힐 때까지 기다립니다. `VisiblePresenters` 를 확인합니다.
- 큐가 띄운 팝업은 닫기 전환이 끝나야 다음으로 넘어갑니다. `CloseUi` 를 불렀는지, 전환 피처의 태스크가 끝나는지 확인합니다.
- 같은 `key` 가 이미 대기 중이거나 표시 중이면 `Enqueue` 가 false 를 돌려주고 넣지 않습니다.

## 입력이 계속 막혀 있음

- `InputBlockReasons` 에 남은 사유를 확인합니다. `BlockInput` 토큰을 `using` 이나 `try/finally` 로 풀어야 예외가 나도 풀립니다.
- 같은 토큰을 두 번 풀면 경고가 남지만 다른 토큰은 그대로 남습니다.

## dim 이 보이지 않음

- `IsModal` 을 true 로 재정의했는지 확인합니다. dim 은 표시 중인 모달 중 화면 맨 위 하나에만 깔립니다.
- 프리팹 루트에 `Canvas` 나 `PanelSettings` 가 지정된 `UIDocument` 가 있어야 합니다. 없으면 경고를 남기고 깔지 않습니다.
- uGUI dim 클릭(`OnDimClicked`)은 씬에 `EventSystem` 이 있어야 받습니다.

---

## 점검 목록

- [ ] `Init(uiConfigs)` 를 호출했고 `UiConfigs` 파생 에셋(`AddressablesUiConfigs` 등)을 넘겼다
- [ ] 로더와 설정 에셋 종류가 맞다 (예: `ResourcesUiAssetLoader` 와 `ResourcesUiConfigs`)
- [ ] `UiPresenter` 와 피처, `Canvas`/`UIDocument` 가 프리팹 루트에 있다
- [ ] 설정 인스펙터에 프레젠터가 보이고 주소가 맞다
- [ ] UniTask 와 Addressables 가 설치되어 있고 컴파일 오류가 없다
- [ ] 서비스를 다 쓰면 `Dispose()` 를 호출한다

문제가 계속되면 Unity 버전, 패키지 버전(`package.json`), 재현 코드, 전체 스택 트레이스를 정리해 [geuneda-uiservice](https://github.com/geuneda/geuneda-uiservice) 레포에 공유하세요.
