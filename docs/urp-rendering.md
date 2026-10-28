# URP 렌더링 피처

[목차로 돌아가기](README.md)

Universal Render Pipeline 프로젝트용 프레젠터 피처 두 가지입니다. `Runtime/Rendering/` 의 별도 어셈블리 `Geuneda.UiService.Urp`(네임스페이스 `Geuneda.UiService.Rendering`)에 있고, `com.unity.render-pipelines.universal` 17.0.1 이상이 설치된 프로젝트에서만 컴파일됩니다(`versionDefines` 로 `GEUNEDA_URP` 정의). URP 가 없으면 어셈블리가 빠지고 패키지는 그대로 동작합니다(블러 셰이더의 임포트 오류 로그만 한 번 남음).

이 문서는 인스펙터에서 알 수 없는 것만 다룹니다. 흐림에 필요한 설정 단계와 주의점입니다. 피처를 붙이는 일반적인 방법은 [핵심 개념](core-concepts.md#프레젠터-피처)을 보세요.

## `UiCameraStackFeature`

프레젠터의 `Canvas` 를 Screen Space - Camera 로 그리고 오버레이 카메라를 기본 카메라의 URP 카메라 스택에 넣어, UI 가 항상 맨 위에 합성되는 대신 3D 콘텐츠와 겹쳐 그려지게 합니다.

인스펙터에서 오버레이 **Camera** 와 프레젠터의 **Canvas** 를 지정합니다. 카메라를 프레젠터 프리팹의 자식으로 두어도 됩니다. Screen Space - Camera 캔버스는 `worldCamera` 에 맞춰 자기 트랜스폼을 움직이므로 카메라가 캔버스 아래에 있으면 캔버스 평면으로 끌려가 아무것도 보지 못합니다. 그래서 피처가 초기화 때 카메라를 캔버스 계층 밖으로 꺼내고, 그렇게 꺼낸 카메라는 프레젠터가 파괴될 때 함께 파괴합니다.

기본 카메라는 `Camera.main` 입니다. 카메라가 여러 개이거나 `MainCamera` 태그가 없거나 분할 화면이면 프레젠터를 열기 전에 `BaseCameraResolver`(`Func<Camera>`)를 넣습니다.

| 멤버 | 설명 |
|------|------|
| `IsStacked` | 오버레이 카메라가 기본 카메라 스택에 들어가 있는 동안 true |
| `StackPriority` | 스택 안 순서. 기본은 캔버스의 `sortingOrder`, 인스펙터에서 끄면 직렬화한 값. 낮을수록 아래 |
| `BaseCameraResolver` | 기본 카메라를 찾는 함수. null 을 넣으면 `Camera.main` |
| `InsertIndex(IReadOnlyList<int>, int)` | 오름차순 목록에서 넣을 위치. 같은 값은 기존 항목 뒤 |

런타임 피처인 이유: URP 카메라 스택은 씬에 있는 기본 카메라의 인스펙터 목록입니다. 프레젠터는 런타임에 프리팹에서 만들어지므로 그 목록에 미리 넣을 수 없어, 열 때 넣고 비활성화될 때 뺍니다. 이 피처가 아닌 곳에서 스택에 넣은 카메라는 우선순위 0 으로 봅니다.

## `UiBackdropBlurRendererFeature` + `UiBackdropBlurPresenterFeature`

프레젠터가 열려 있는 동안 UI 뒤의 모든 것을 흐립니다.

> ### 필수 설정: URP Renderer 에셋에 `UiBackdropBlurRendererFeature` 추가
>
> Universal Renderer 에셋(예: `Assets/Settings/URP-Mobile_Renderer.asset`. Pipeline 에셋이 아님)을 선택하고 **Add Renderer Feature** 에서 `UiBackdropBlurRendererFeature` 를 추가합니다. 이 단계 전에는 흐림이 아무것도 하지 않습니다. 프레젠터 피처를 열면 이 설정 방법을 적은 Error 로그가 남습니다.

모양(다운샘플 0~3, 반복 1~8, 퍼짐, 틴트)은 그 렌더러 피처에서 정합니다. 프로젝트 전체의 아트 결정 하나라 프레젠터마다 다르게 할 수 없고, 흐림을 쓰는 모든 프레젠터가 같은 모양을 씁니다. 반복은 가장 직관적인 강도 조절이지만 가장 비쌉니다(반복마다 전체 화면 한 번). `Downsample` 이 더 싸게 더 흐립니다.

런타임 조절(품질 단계, 접근성 슬라이더, 설정 메뉴)은 정적 `UiBackdropBlurRendererFeature.IterationsOverride` 로 합니다. `MinIterations`(1)~`MaxIterations`(8) 로 잘리고 0 이하는 해제이며 `EffectiveIterations` 가 실제 적용값, `AuthoredIterations` 가 에셋 값입니다. 에셋 필드에 쓰지 않는 이유는 피처가 ScriptableObject 라 런타임 쓰기가 에디터에서 디스크에 저장돼 프로젝트 전체의 모양을 바꾸기 때문입니다.

그다음 흐린 배경이 필요한 프레젠터에 `UiBackdropBlurPresenterFeature` 를 붙입니다. 설정은 없습니다. 프레젠터가 보이는 동안 흐림을 열어 두고, 그런 프레젠터가 마지막으로 닫힐 때까지 유지합니다.

패스는 다음이 모두 참일 때만 들어갑니다(`UiBackdropBlurPass.ShouldInject`).

- **Game** 카메라. 아니면 에디터의 Scene 뷰와 재질 미리보기도 흐려집니다
- `UiCameraStackFeature` 의 오버레이 카메라가 **아님**. 렌더러 피처는 스택의 카메라마다 도므로, 거기서 돌면 뒤의 월드가 아니라 UI 자체가 흐려집니다
- `RenderTexture` 대상이 **없음**. 있으면 캡처 중인 UI 가 흐려집니다
- `UiBackdropBlurPresenterFeature` 프레젠터가 하나라도 열려 있음

## UI 를 RenderTexture 에 그리기

Unity 6 에서는 코드 없이 작성만으로 되므로 피처가 없습니다.

| UI 시스템 | 설정 |
|---|---|
| **uGUI** | `Canvas` → Render Mode = Screen Space - Camera → 카메라 지정 → 그 **카메라의 Target Texture** 지정 |
| **UI Toolkit** | `PanelSettings` → **Render Texture** |

uGUI 경로에서는 깊이/스텐실이 필수입니다. `Mask`/`RectMask2D` 가 스텐실 버퍼를 쓰므로 `RenderTexture` 를 24비트 깊이(깊이+스텐실)로 만듭니다. Linear 색 공간 프로젝트에서 텍스처에 그린 UI 를 월드 재질로 샘플링하면 sRGB 변환이 두 번 되거나 빠질 수 있으니 눈으로 확인합니다.

월드 공간 UI Toolkit 은 텍스처 대신 `PanelSettings.renderMode = PanelRenderMode.WorldSpace` 를 쓰는 편이 낫습니다. Unity 가 패널을 월드 공간에 바로 그리고 `UIDocument.position`, `pivot`, `pivotReferenceSize`, `worldSpaceSize`, `worldSpaceSizeMode` 로 문서마다 배치합니다.

## 렌더 모드가 섞이면 `UiConfig.Layer` 가 전체 순서가 아님

Screen Space - Overlay 캔버스는 URP 렌더링이 **모두** 끝난 뒤 백버퍼에 바로 합성됩니다. 그래서 카메라 스택에 넣은 Screen Space - Camera 프레젠터는 `Layer` 가 아무리 높아도 같은 씬에 `Layer` 0 의 Overlay 프레젠터가 있는 순간 그 **아래**에 그려집니다.

두 렌더 모드가 합성되는 방식이라 런타임에 고칠 수 없습니다. Overlay 프레젠터와 카메라 스택 프레젠터를 설계에서 분리하거나(예: HUD 는 Overlay, 월드에 붙는 UI 는 스택), 두 그룹 사이의 고정된 상대 순서를 받아들이세요.

관련: `UiCameraStackFeature` 프레젠터의 카메라를 `RenderTexture` 에 그리지 마세요. URP 는 스택에 넣은 오버레이 카메라가 기본 카메라의 대상을 공유해야 하는데 렌더 텍스처 카메라는 자기 대상에 그립니다. 둘 중 하나만 고릅니다.

## `PanelSettings` 를 런타임에 바꾸지 않기

`PanelSettings` 에셋 하나가 패널 하나입니다. 같은 에셋을 가리키는 모든 `UIDocument` 가 패널 하나를 공유하고, 그래서 `UiService` 가 `UiConfig.Layer` 로 매기는 `UIDocument.sortingOrder` 의 프레젠터 간 순서와 배칭이 동작합니다. 에셋에 쓰면 같은 에셋을 쓰는 다른 프레젠터가 모두 바뀌고 에디터에서는 디스크에 저장됩니다. 다른 패널 설정이 필요하면 두 번째 `PanelSettings` 에셋을 만들어 그 `UIDocument` 가 가리키게 합니다. 패널 수는 가능한 한 적게 둡니다.

## 흐림 중 Metal 경고 두 줄

Metal 에서 URP 는 카메라 깊이 첨부를 memoryless 로 둡니다. 전체 화면 패스를 넣으면 네이티브 렌더 패스가 끊겨 백엔드가 다음을 남깁니다.

```
Ignoring depth surface load action as it is memoryless
Ignoring depth surface store action as it is memoryless
```

둘 다 이 패키지가 아니라 Unity 네이티브 렌더러가 세션에 한 번 남기는 것이고 Unity 가 그 동작을 무시했다는 뜻입니다. 흐림을 요청하는 프레젠터가 없어지면 멈춥니다. `requiresIntermediateTexture` 와 작업 텍스처의 깊이 제거로는 없어지지 않습니다.

## 확인 범위

자동화된 것: 패스 주입 조건(`ShouldInject`), 열림/닫힘 참조 수, 스택 삽입 순서와 카메라 스택 넣기·빼기, 에디터에서 패키지 셰이더로 재질을 만들고 지우는 것. 셰이더가 보기 좋은지는 자동으로 확인하지 않습니다. 다운샘플·반복·퍼짐 조합은 콘텐츠와 해상도에 따른 판단이라 렌더러 피처에서 조절하며 눈으로 봅니다. URP 2D Renderer(`Renderer2D`)에서의 동작은 아직 확인하지 않았습니다. 2D 프로젝트에 넣을 때는 카메라 스택과 블러 패스가 2D 렌더러에서도 기대대로 그리는지 먼저 봅니다.
