namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 닫기 전환이 있는 UI Toolkit 모달. 닫기 전환 중인 문서의 정렬을 시험합니다.
	/// </summary>
	public class TestToolkitDelayPopupPresenter : TestToolkitPopupPresenter
	{
		private void Awake()
		{
			gameObject.AddComponent<TestCountingDelayFeature>().SetDelays(0f, 0.2f);
		}
	}
}
