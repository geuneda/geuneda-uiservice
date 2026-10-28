using System.Runtime.CompilerServices;

// Geuneda.UiService.Urp 어셈블리 정보
//
// internal 멤버(UiBackdropBlurPass.ShouldInject, UiBackdropBlurRendererFeature 의 설정 읽기와 재질, UiCameraStackFeature.ConfigureForTest)를
// URP 테스트 어셈블리에 노출합니다. 공개 API 로 만들지 않고 테스트에서만 씁니다.
[assembly: InternalsVisibleTo("Geuneda.UiService.Tests.Urp")]
[assembly: InternalsVisibleTo("Geuneda.UiService.Tests.PlayMode.Urp")]
