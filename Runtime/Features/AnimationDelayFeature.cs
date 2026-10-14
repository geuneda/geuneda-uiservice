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
		// 열기·닫기 알림마다 올라갑니다. 지연 중에 반대 알림이 왔으면 이전 전환의 완료 훅을 건너뜁니다
		private int _transitionVersion;

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
			// 전환이 없더라도 세대를 올려, 지연 중이던 반대 전환의 완료 훅이 뒤늦게 불리지 않게 합니다
			var version = ++_transitionVersion;

			if (_introAnimationClip != null)
			{
				OpenWithAnimationAsync(version).Forget();
			}
			else
			{
				// 이번에는 전환이 없으므로 이전 전환(실패했을 수 있음)의 태스크를 이번 열기에 넘기지 않습니다
				_openTransitionCompletion = null;
			}
		}

		/// <inheritdoc />
		public override void OnPresenterClosing()
		{
			var version = ++_transitionVersion;

			if (_outroAnimationClip != null && Presenter && Presenter.gameObject)
			{
				CloseWithAnimationAsync(version).Forget();
			}
			else
			{
				_closeTransitionCompletion = null;
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

		private async UniTask OpenWithAnimationAsync(int version)
		{
			// 지연 중에 다시 열고 닫으면 필드가 새 소스로 바뀌므로 이번 전환의 소스를 따로 잡아 둡니다
			var completion = new UniTaskCompletionSource();
			_openTransitionCompletion = completion;

			try
			{
				OnOpenStarted();

				await UniTask.Delay(TimeSpan.FromSeconds(OpenDelayInSeconds));

				if (this && gameObject && version == _transitionVersion)
				{
					OnOpenedCompleted();
				}

				completion.TrySetResult();
			}
			catch (Exception exception)
			{
				// 프레젠터가 이 태스크를 기다리므로 예외를 넘겨 그쪽에서 정리하고 한 번 기록하게 합니다
				completion.TrySetException(exception);
			}
		}

		private async UniTask CloseWithAnimationAsync(int version)
		{
			// 지연 중에 다시 열고 닫으면 필드가 새 소스로 바뀌므로 이번 전환의 소스를 따로 잡아 둡니다
			var completion = new UniTaskCompletionSource();
			_closeTransitionCompletion = completion;

			try
			{
				OnCloseStarted();

				await UniTask.Delay(TimeSpan.FromSeconds(CloseDelayInSeconds));

				if (this && gameObject && version == _transitionVersion)
				{
					// 참고: 가시성(SetActive)은 이제 모든 전환이 완료된 후 UiPresenter가 처리합니다
					OnClosedCompleted();
				}

				completion.TrySetResult();
			}
			catch (Exception exception)
			{
				// 프레젠터가 이 태스크를 기다리므로 예외를 넘겨 그쪽에서 정리하고 한 번 기록하게 합니다
				completion.TrySetException(exception);
			}
		}
	}
}
