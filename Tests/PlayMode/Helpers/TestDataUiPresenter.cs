namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 테스트 프레젠터 데이터 구조체
	/// </summary>
	public struct TestPresenterData
	{
		public int Id;
		public string Name;
	}
    
	/// <summary>
	/// 데이터 지원이 있는 테스트 프레젠터.
	/// 프리팹에 추가할 수 있도록 런타임 호환 어셈블리에 위치한 클래스입니다.
	/// </summary>
	public class TestDataUiPresenter : UiPresenter<TestPresenterData>
	{
		public bool WasDataSet { get; private set; }
		public int OnSetDataCallCount { get; private set; }
		public TestPresenterData ReceivedData { get; private set; }

		protected override void OnSetData()
		{
			WasDataSet = true;
			OnSetDataCallCount++;
			ReceivedData = Data;
		}
	}
}

