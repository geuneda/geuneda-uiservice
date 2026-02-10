using UnityEngine;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 애니메이션 동작 테스트를 위한 AnimationDelayFeature가 있는 테스트 프레젠터
	/// </summary>
	[RequireComponent(typeof(AnimationDelayFeature))]
	[RequireComponent(typeof(Animation))]
	public class TestAnimationDelayPresenter : UiPresenter
	{
		public AnimationDelayFeature AnimationFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }

		private void Awake()
		{
			// Animation 컴포넌트가 존재하는지 확인
			var animation = GetComponent<Animation>();
			if (animation == null)
			{
				animation = gameObject.AddComponent<Animation>();
			}

			AnimationFeature = GetComponent<AnimationDelayFeature>();
			if (AnimationFeature == null)
			{
				AnimationFeature = gameObject.AddComponent<AnimationDelayFeature>();
			}

			// 리플렉션을 통해 애니메이션 참조 설정
			var animField = typeof(AnimationDelayFeature).GetField("_animation",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			animField?.SetValue(AnimationFeature, animation);
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
		}
	}
}

