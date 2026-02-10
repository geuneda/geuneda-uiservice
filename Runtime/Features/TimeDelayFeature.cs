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
			if (_openDelayInSeconds > 0)
			{
				OpenWithDelayAsync().Forget();
			}
		}

		/// <inheritdoc />
		public override void OnPresenterClosing()
		{
			if (_closeDelayInSeconds > 0 && Presenter && Presenter.gameObject)
			{
				CloseWithDelayAsync().Forget();
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

		private async UniTask OpenWithDelayAsync()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();
			
			OnOpenStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(_openDelayInSeconds));

			if (this && gameObject)
			{
				OnOpenedCompleted();
			}
			
			_openTransitionCompletion?.TrySetResult();
		}

		private async UniTask CloseWithDelayAsync()
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();
			
			OnCloseStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(_closeDelayInSeconds));

			if (this && gameObject)
			{
				// 참고: 가시성(SetActive)은 이제 모든 전환이 완료된 후 UiPresenter가 처리합니다
				OnClosedCompleted();
			}
			
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
