using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Geuneda.UiService
{
	/// <summary>
	/// <see cref="UiPresenter"/>에 시간 기반 지연 열기/닫기를 추가하는 기능입니다.
	/// 이 컴포넌트에서 직접 초 단위 딜레이를 설정할 수 있습니다.
	/// <see cref="ITransitionFeature"/>를 구현하여 프레젠터가 전환을 대기할 수 있게 합니다.
	/// </summary>
	public class TimeDelayFeature : PresenterFeatureBase, ITransitionFeature
	{
		[SerializeField, Range(0f, float.MaxValue)] private float _openDelayInSeconds = 0.5f;
		[SerializeField, Range(0f, float.MaxValue)] private float _closeDelayInSeconds = 0.3f;

		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;
		// 열기·닫기 알림마다 올라갑니다. 지연 중에 반대 알림이 왔으면 이전 전환의 완료 훅을 건너뜁니다
		private int _transitionVersion;

		/// <summary>
		/// 프레젠터를 열기 전 지연 시간(초)을 가져옵니다
		/// </summary>
		public float OpenDelayInSeconds => _openDelayInSeconds;

		/// <summary>
		/// 프레젠터를 닫기 전 지연 시간(초)을 가져옵니다
		/// </summary>
		public float CloseDelayInSeconds => _closeDelayInSeconds;

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			// 전환이 없더라도 세대를 올려, 지연 중이던 반대 전환의 완료 훅이 뒤늦게 불리지 않게 합니다
			var version = ++_transitionVersion;

			if (_openDelayInSeconds > 0)
			{
				OpenWithDelayAsync(version).Forget();
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

			if (_closeDelayInSeconds > 0 && Presenter && Presenter.gameObject)
			{
				CloseWithDelayAsync(version).Forget();
			}
			else
			{
				_closeTransitionCompletion = null;
			}
		}

		/// <summary>
		/// 프레젠터의 열기 딜레이가 시작될 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnOpenStarted() { }

		/// <summary>
		/// 프레젠터의 열기 딜레이가 완료되었을 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnOpenedCompleted() { }

		/// <summary>
		/// 프레젠터의 닫기 딜레이가 시작될 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnCloseStarted() { }

		/// <summary>
		/// 프레젠터의 닫기 딜레이가 완료되었을 때 호출됩니다.
		/// 파생 클래스에서 커스텀 동작을 추가하려면 재정의하세요.
		/// </summary>
		protected virtual void OnClosedCompleted() { }

		private async UniTask OpenWithDelayAsync(int version)
		{
			// 지연 중에 다시 열고 닫으면 필드가 새 소스로 바뀌므로 이번 전환의 소스를 따로 잡아 둡니다
			var completion = new UniTaskCompletionSource();
			_openTransitionCompletion = completion;

			try
			{
				OnOpenStarted();

				await UniTask.Delay(TimeSpan.FromSeconds(_openDelayInSeconds));

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

		private async UniTask CloseWithDelayAsync(int version)
		{
			// 지연 중에 다시 열고 닫으면 필드가 새 소스로 바뀌므로 이번 전환의 소스를 따로 잡아 둡니다
			var completion = new UniTaskCompletionSource();
			_closeTransitionCompletion = completion;

			try
			{
				OnCloseStarted();

				await UniTask.Delay(TimeSpan.FromSeconds(_closeDelayInSeconds));

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
