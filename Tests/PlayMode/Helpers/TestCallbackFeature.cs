using System;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 피처 콜백 도중에 서비스 동작을 끼워 넣기 위한 테스트 피처.
	/// </summary>
	public class TestCallbackFeature : PresenterFeatureBase
	{
		public Action Opening { get; set; }
		public Action Closing { get; set; }

		public override void OnPresenterOpening()
		{
			Opening?.Invoke();
		}

		public override void OnPresenterClosing()
		{
			Closing?.Invoke();
		}
	}
}
