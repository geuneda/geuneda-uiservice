namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 기본 테스트를 위한 간단한 테스트 프레젠터.
	/// 프리팹에 추가할 수 있도록 런타임 호환 어셈블리에 위치한 클래스입니다.
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
}

