using System.Reflection;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 완료 훅이 불린 횟수를 세는 TimeDelayFeature. 지연 값은 SetDelays 로 정합니다.
	/// </summary>
	public class TestCountingDelayFeature : TimeDelayFeature
	{
		public int OpenedCompletedCount { get; private set; }
		public int ClosedCompletedCount { get; private set; }
		public bool ThrowOnOpenStarted { get; set; }
		public bool ThrowOnClosedCompleted { get; set; }
		public bool ThrowOnCloseStarted { get; set; }

		public void SetDelays(float open, float close)
		{
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
			typeof(TimeDelayFeature).GetField("_openDelayInSeconds", flags)?.SetValue(this, open);
			typeof(TimeDelayFeature).GetField("_closeDelayInSeconds", flags)?.SetValue(this, close);
		}

		protected override void OnOpenStarted()
		{
			if (ThrowOnOpenStarted)
			{
				throw new System.InvalidOperationException("feature open boom");
			}
		}

		protected override void OnCloseStarted()
		{
			if (ThrowOnCloseStarted)
			{
				throw new System.InvalidOperationException("feature close start boom");
			}
		}

		protected override void OnOpenedCompleted()
		{
			OpenedCompletedCount++;
		}

		protected override void OnClosedCompleted()
		{
			ClosedCompletedCount++;

			if (ThrowOnClosedCompleted)
			{
				throw new System.InvalidOperationException("feature close boom");
			}
		}
	}
}
