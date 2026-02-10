using Geuneda.UiService;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// 기본 테스트를 위한 간단한 테스트 프레젠터
	/// </summary>
	public class TestUiPresenter : UiPresenter
	{
		public bool WasInitialized { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosed { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }
		public int OpenCount { get; private set; }
		public int CloseCount { get; private set; }
		public int OpenTransitionCompletedCount { get; private set; }
		public int CloseTransitionCompletedCount { get; private set; }

		protected override void OnInitialized()
		{
			WasInitialized = true;
		}

		protected override void OnOpened()
		{
			WasOpened = true;
			OpenCount++;
		}

		protected override void OnClosed()
		{
			WasClosed = true;
			CloseCount++;
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
			OpenTransitionCompletedCount++;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
			CloseTransitionCompletedCount++;
		}
	}
	
	/// <summary>
	/// 데이터 지원이 있는 테스트 프레젠터
	/// </summary>
	public struct TestPresenterData
	{
		public int Id;
		public string Name;
	}

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

