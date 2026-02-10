using System.Runtime.CompilerServices;

// Geuneda.UiService 어셈블리 정보
//
// 이 파일은 런타임 어셈블리의 internal 멤버를 이 패키지 내의 다른 어셈블리에 노출합니다.
// [InternalsVisibleTo] 어트리뷰트를 사용하여 internal API를 public으로 만들지 않고도 접근 권한을 부여합니다.

// internal 멤버를 Editor 어셈블리에 노출합니다.
// 이를 통해 에디터 도구(예: UiAnalyticsWindow)가 UiService.CurrentAnalytics 같은 internal API에
// 최종 사용자에게 public으로 공개하지 않고도 접근할 수 있습니다.
[assembly: InternalsVisibleTo("Geuneda.UiService.Editor")]

// internal 멤버를 PlayMode 테스트 어셈블리에 노출합니다.
// 이를 통해 테스트가 InternalOpen(), InternalClose() 및 internal 비동기 프로세스 메서드 등의
// internal API에 접근하여 프레젠터 생명주기 동작을 테스트할 수 있습니다.
[assembly: InternalsVisibleTo("Geuneda.UiService.Tests.PlayMode")]
