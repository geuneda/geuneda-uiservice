using UnityEngine;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 클립 기반 지연 테스트를 위한 애니메이션 클립이 있는 테스트 프레젠터
	/// </summary>
	[RequireComponent(typeof(AnimationDelayFeature))]
	[RequireComponent(typeof(Animation))]
	public class TestAnimationDelayWithClipPresenter : UiPresenter
	{
		public AnimationDelayFeature AnimationFeature { get; private set; }

		private void Awake()
		{
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

			// 알려진 길이의 모의 애니메이션 클립 생성
			var introClip = new AnimationClip();
			introClip.legacy = true;
			
			// 길이를 부여하기 위한 더미 커브 추가
			introClip.SetCurve("", typeof(Transform), "localPosition.x", 
				AnimationCurve.Linear(0f, 0f, 0.1f, 1f));

			// 리플렉션을 통해 필드 설정
			var animField = typeof(AnimationDelayFeature).GetField("_animation",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var introField = typeof(AnimationDelayFeature).GetField("_introAnimationClip",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			
			animField?.SetValue(AnimationFeature, animation);
			introField?.SetValue(AnimationFeature, introClip);
		}
	}
}

