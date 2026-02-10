using UnityEngine;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 세 개의 기능을 가진 테스트 프레젠터
	/// </summary>
	public class TestTripleFeaturePresenter : UiPresenter
	{
		public TrackingFeature FeatureA { get; private set; }
		public TrackingFeature FeatureB { get; private set; }
		public TrackingFeature FeatureC { get; private set; }
		public int OpenTransitionCount { get; private set; }

		private void Awake()
		{
			FeatureA = gameObject.AddComponent<TrackingFeature>();
			FeatureB = gameObject.AddComponent<TrackingFeature>();
			FeatureC = gameObject.AddComponent<TrackingFeature>();
		}

		protected override void OnOpenTransitionCompleted()
		{
			OpenTransitionCount++;
		}
	}
}

