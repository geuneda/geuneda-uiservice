using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Geuneda.UiService
{
	/// <summary>
	/// <see cref="UiPresenter"/>에 애니메이션 기반 지연 열기/닫기를 추가하는 기능입니다.
	/// 인트로/아웃트로 애니메이션을 재생하고 완료될 때까지 대기합니다.
	/// <see cref="ITransitionFeature"/>를 구현하여 프레젠터가 전환을 대기할 수 있게 합니다.
	/// </summary>
	[RequireComponent(typeof(Animation))]
	public class AnimationDelayFeature : PresenterFeatureBase, ITransitionFeature
	{
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _introAnimationClip;
		[SerializeField] private AnimationClip _outroAnimationClip;

		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		/// <summary>
		/// Animation 컴포넌트를 가져옵니다
		/// </summary>
		public Animation AnimationComponent => _animation;

		/// <summary>
		/// 인트로 애니메이션 클립을 가져옵니다
		/// </summary>
		public AnimationClip IntroAnimationClip => _introAnimationClip;

		/// <summary>
		/// 아웃트로 애니메이션 클립을 가져옵니다
		/// </summary>
		public AnimationClip OutroAnimationClip => _outroAnimationClip;

		/// <summary>
		/// 열기 지연 시간(초)을 가져옵니다 (인트로 애니메이션 길이 기반)
		/// </summary>
		public float OpenDelayInSeconds => _introAnimationClip == null ? 0f : _introAnimationClip.length;

		/// <summary>
		/// 닫기 지연 시간(초)을 가져옵니다 (아웃트로 애니메이션 길이 기반)
		/// </summary>
		public float CloseDelayInSeconds => _outroAnimationClip == null ? 0f : _outroAnimationClip.length;

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		private void OnValidate()
		{
			_animation = _animation ?? GetComponent<Animation>();
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			if (_introAnimationClip != null)
			{
				OpenWithAnimationAsync().Forget();
			}
		}

		/// <inheritdoc />
		public override void OnPresenterClosing()
		{
			if (_outroAnimationClip != null && Presenter && Presenter.gameObject)
			{
				CloseWithAnimationAsync().Forget();
			}
		}

		/// <summary>
		/// 프레젠터의 열기 애니메이션이 시작될 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnOpenStarted()
		{
			if (_introAnimationClip != null)
			{
				_animation.clip = _introAnimationClip;
				_animation.Play();
			}
		}

		/// <summary>
		/// 프레젠터의 열기 애니메이션이 완료되었을 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnOpenedCompleted() { }

		/// <summary>
		/// 프레젠터의 닫기 애니메이션이 시작될 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnCloseStarted()
		{
			if (_outroAnimationClip != null)
			{
				_animation.clip = _outroAnimationClip;
				_animation.Play();
			}
		}

		/// <summary>
		/// 프레젠터의 닫기 애니메이션이 완료되었을 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnClosedCompleted() { }

		private async UniTask OpenWithAnimationAsync()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();
			
			OnOpenStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(OpenDelayInSeconds));

			if (this && gameObject)
			{
				OnOpenedCompleted();
			}
			
			_openTransitionCompletion?.TrySetResult();
		}

		private async UniTask CloseWithAnimationAsync()
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();
			
			OnCloseStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(CloseDelayInSeconds));

			if (this && gameObject)
			{
				// 참고: 가시성(SetActive)은 이제 모든 전환이 완료된 후 UiPresenter가 처리합니다
				OnClosedCompleted();
			}
			
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
