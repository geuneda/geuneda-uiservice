using UnityEngine;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 두 개의 기능을 가진 테스트 프레젠터
	/// </summary>
	public class TestDualFeaturePresenter : UiPresenter
	{
		public TrackingFeature FeatureA { get; private set; }
		public TrackingFeature FeatureB { get; private set; }
		public int OpenTransitionCount { get; private set; }
		public int CloseTransitionCount { get; private set; }

		private void Awake()
		{
			FeatureA = gameObject.AddComponent<TrackingFeature>();
			FeatureB = gameObject.AddComponent<TrackingFeature>();
		}

		protected override void OnOpenTransitionCompleted()
		{
			OpenTransitionCount++;
		}

		protected override void OnCloseTransitionCompleted()
		{
			CloseTransitionCount++;
		}
	}
}

