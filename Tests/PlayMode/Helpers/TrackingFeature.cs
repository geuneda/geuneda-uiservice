using Cysharp.Threading.Tasks;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 생명주기를 추적하고 전환을 시뮬레이션할 수 있는 모의 기능.
	/// 테스트에서 전환 완료 시점을 제어할 수 있도록 ITransitionFeature를 구현합니다.
	/// </summary>
	public class TrackingFeature : PresenterFeatureBase, ITransitionFeature
	{
		private static int _globalOpenCounter;
		
		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;
		
		public bool WasInitialized { get; private set; }
		public bool WasOpening { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosing { get; private set; }
		public bool WasClosed { get; private set; }
		public int OpenOrder { get; private set; }
		
		/// <summary>
		/// true일 때 이 기능은 수동으로 완료해야 하는 전환을 시작합니다.
		/// false일 때 전환은 즉시 완료됩니다.
		/// </summary>
		public bool SimulateDelayedTransitions { get; set; }

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			WasInitialized = true;
		}

		public override void OnPresenterOpening()
		{
			WasOpening = true;
		}

		public override void OnPresenterOpened()
		{
			WasOpened = true;
			OpenOrder = _globalOpenCounter++;
			
			if (SimulateDelayedTransitions)
			{
				_openTransitionCompletion = new UniTaskCompletionSource();
			}
		}

		public override void OnPresenterClosing()
		{
			WasClosing = true;
			
			if (SimulateDelayedTransitions)
			{
				_closeTransitionCompletion = new UniTaskCompletionSource();
			}
		}

		public override void OnPresenterClosed()
		{
			WasClosed = true;
		}

		/// <summary>
		/// 열기 전환의 완료를 시뮬레이션합니다.
		/// SimulateDelayedTransitions가 true일 때만 유효합니다.
		/// </summary>
		public void SimulateOpenTransitionComplete()
		{
			_openTransitionCompletion?.TrySetResult();
		}

		/// <summary>
		/// 닫기 전환의 완료를 시뮬레이션합니다.
		/// SimulateDelayedTransitions가 true일 때만 유효합니다.
		/// </summary>
		public void SimulateCloseTransitionComplete()
		{
			_closeTransitionCompletion?.TrySetResult();
		}

		public void Reset()
		{
			WasInitialized = false;
			WasOpening = false;
			WasOpened = false;
			WasClosing = false;
			WasClosed = false;
			_openTransitionCompletion = null;
			_closeTransitionCompletion = null;
		}
	}
}
