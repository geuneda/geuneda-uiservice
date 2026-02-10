using UnityEngine;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 엣지 케이스 테스트를 위한 지연 시간 0인 테스트 프레젠터
	/// </summary>
	[RequireComponent(typeof(TimeDelayFeature))]
	public class TestZeroDelayPresenter : UiPresenter
	{
		public TimeDelayFeature DelayFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }

		private void Awake()
		{
			DelayFeature = GetComponent<TimeDelayFeature>();
			if (DelayFeature == null)
			{
				DelayFeature = gameObject.AddComponent<TimeDelayFeature>();
			}
			
			// 지연 시간 0으로 설정
			var openField = typeof(TimeDelayFeature).GetField("_openDelayInSeconds", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var closeField = typeof(TimeDelayFeature).GetField("_closeDelayInSeconds", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			openField?.SetValue(DelayFeature, 0f);
			closeField?.SetValue(DelayFeature, 0f);
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
		}
	}
}

