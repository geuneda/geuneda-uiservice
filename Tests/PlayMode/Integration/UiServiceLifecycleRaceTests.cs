using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 전환 중 다시 열기·닫기, 닫기 콜백 안에서 열기 같은 경쟁 상황 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiServiceLifecycleRaceTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private string _exceptionLogText;
		private int _exceptionLogCount;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestTimeDelayPresenter>("delay_presenter");
			_mockLoader.RegisterPrefab<TestPopupPresenter>("popup");
			_mockLoader.RegisterPrefab<TestPopupPresenterB>("popup_b");
			var callbackPrefab = TestHelpers.CreateTestPresenterPrefab<TestPopupPresenterC>();
			callbackPrefab.AddComponent<TestCallbackFeature>();
			_mockLoader.RegisterPrefab("popup_c", callbackPrefab);
			var countingPrefab = TestHelpers.CreateTestPresenterPrefab<TestLayerUiPresenter>();
			countingPrefab.AddComponent<TestCountingDelayFeature>();
			countingPrefab.AddComponent<TestCallbackFeature>();
			_mockLoader.RegisterPrefab("counting", countingPrefab);
			var multiPrefab = TestHelpers.CreateTestPresenterPrefab<TestDataUiPresenter>();
			multiPrefab.AddComponent<TestCountingDelayFeature>();
			multiPrefab.AddComponent<TestCountingDelayFeature>();
			_mockLoader.RegisterPrefab("multi", multiPrefab);
			var customPrefab = TestHelpers.CreateTestPresenterPrefab<TestZeroDelayPresenter>();
			customPrefab.AddComponent<TestCustomTransitionFeature>();
			_mockLoader.RegisterPrefab("custom", customPrefab);
			// 같은 두 피처를 순서만 바꿔 붙입니다(카운팅 먼저 / 커스텀 먼저)
			var countingFirstPrefab = TestHelpers.CreateTestPresenterPrefab<TestPlainPresenter>();
			countingFirstPrefab.AddComponent<TestCountingDelayFeature>();
			countingFirstPrefab.AddComponent<TestCustomTransitionFeature>();
			_mockLoader.RegisterPrefab("counting_first", countingFirstPrefab);
			var customFirstPrefab = TestHelpers.CreateTestPresenterPrefab<TestPlainPresenterB>();
			customFirstPrefab.AddComponent<TestCustomTransitionFeature>();
			customFirstPrefab.AddComponent<TestCountingDelayFeature>();
			_mockLoader.RegisterPrefab("custom_first", customFirstPrefab);
			var trackedPrefab = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>();
			trackedPrefab.AddComponent<TrackingFeature>();
			_mockLoader.RegisterPrefab("tracked", trackedPrefab);

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestTimeDelayPresenter), "delay_presenter", 0),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenter), "popup", 10),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterB), "popup_b", 10),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterC), "popup_c", 20),
				TestHelpers.CreateTestConfig(typeof(TestLayerUiPresenter), "counting", 0),
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "tracked", 0),
				TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "multi", 0),
				TestHelpers.CreateTestConfig(typeof(TestZeroDelayPresenter), "custom", 0),
				TestHelpers.CreateTestConfig(typeof(TestPlainPresenter), "counting_first", 0),
				TestHelpers.CreateTestConfig(typeof(TestPlainPresenterB), "custom_first", 0)));
		}

		[TearDown]
		public void TearDown()
		{
			Application.logMessageReceived -= OnLogMessage;
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest]
		public IEnumerator ReopenDuringCloseTransition_StaysOpenAndVisible()
		{
			var openTask = _service.OpenUiAsync<TestTimeDelayPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			_service.CloseUi<TestTimeDelayPresenter>();
			var closeTransition = presenter.CloseTransitionTask;
			yield return _service.OpenUiAsync<TestTimeDelayPresenter>().ToCoroutine();

			yield return closeTransition.ToCoroutine();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.That(presenter.gameObject.activeSelf, Is.True);
			Assert.That(_service.IsVisible<TestTimeDelayPresenter>(), Is.True);
			Assert.That(presenter.WasCloseTransitionCompleted, Is.False);
		}

		[UnityTest]
		public IEnumerator ReopenDuringDestroyingClose_DoesNotUnload()
		{
			var openTask = _service.OpenUiAsync<TestTimeDelayPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			_service.CloseUi<TestTimeDelayPresenter>(destroy: true);
			var closeTransition = presenter.CloseTransitionTask;
			yield return _service.OpenUiAsync<TestTimeDelayPresenter>().ToCoroutine();
			yield return closeTransition.ToCoroutine();

			Assert.That(_mockLoader.UnloadCallCount, Is.EqualTo(0));
			Assert.That(presenter != null, Is.True);
			Assert.That(_service.IsVisible<TestTimeDelayPresenter>(), Is.True);
		}

		[UnityTest]
		public IEnumerator CloseDuringOpenTransition_DoesNotReportOpenCompleted()
		{
			var openTask = _service.OpenUiAsync<TestTimeDelayPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			var openTransition = presenter.OpenTransitionTask;

			_service.CloseUi<TestTimeDelayPresenter>();
			yield return presenter.CloseTransitionTask.ToCoroutine();
			yield return openTransition.ToCoroutine();

			Assert.That(presenter.WasOpenTransitionCompleted, Is.False);
			Assert.That(presenter.WasCloseTransitionCompleted, Is.True);
			Assert.That(presenter.gameObject.activeSelf, Is.False);
		}

		[UnityTest]
		public IEnumerator CloseAllUi_CallbackOpensUi_KeepsNewUiOpen()
		{
			yield return _service.LoadUiAsync<TestPopupPresenterB>().ToCoroutine();
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var popup = openTask.GetAwaiter().GetResult();
			popup.ClosedCallback = () => _service.OpenUiAsync<TestPopupPresenterB>().Forget();

			Assert.DoesNotThrow(() => _service.CloseAllUi());

			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
			Assert.That(_service.IsVisible<TestPopupPresenterB>(), Is.True);
		}

		[UnityTest]
		public IEnumerator CloseAllUiLayer_CallbackOpensUi_KeepsNewUiOpen()
		{
			yield return _service.LoadUiAsync<TestPopupPresenterB>().ToCoroutine();
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var popup = openTask.GetAwaiter().GetResult();
			popup.ClosedCallback = () => _service.OpenUiAsync<TestPopupPresenterB>().Forget();

			Assert.DoesNotThrow(() => _service.CloseAllUi(10));

			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
			Assert.That(_service.IsVisible<TestPopupPresenterB>(), Is.True);
		}

		[UnityTest]
		public IEnumerator CloseReopenCloseWithinDelay_CompletesEveryCloseTask()
		{
			var openTask = _service.OpenUiAsync<TestTimeDelayPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			_service.CloseUi<TestTimeDelayPresenter>();
			var firstClose = presenter.CloseTransitionTask;
			_service.OpenUiAsync<TestTimeDelayPresenter>().Forget();
			_service.CloseUi<TestTimeDelayPresenter>();
			var secondClose = presenter.CloseTransitionTask;

			// 최신 닫기는 자신의 지연 전에 완료되면 안 됩니다
			Assert.That(secondClose.Status, Is.EqualTo(UniTaskStatus.Pending));

			yield return new WaitForSecondsRealtime(0.4f);

			Assert.That(firstClose.Status, Is.EqualTo(UniTaskStatus.Succeeded));
			Assert.That(secondClose.Status, Is.EqualTo(UniTaskStatus.Succeeded));
			Assert.That(presenter.gameObject.activeSelf, Is.False);
		}

		[UnityTest]
		public IEnumerator OpenedCallbackClosesSelf_EventsKeepOrder()
		{
			var events = RecordEvents();
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			popup.OpenedCallback = () => _service.CloseUi<TestPopupPresenter>();

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(events, Is.EqualTo(new[] { "open", "close" }));
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
		}

		[UnityTest]
		public IEnumerator ClosedCallbackReopensSelf_EventsKeepOrderAndStaysOpen()
		{
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var popup = openTask.GetAwaiter().GetResult();
			var events = RecordEvents();
			popup.ClosedCallback = () =>
			{
				popup.ClosedCallback = null;
				_service.OpenUiAsync<TestPopupPresenter>().Forget();
			};

			_service.CloseUi<TestPopupPresenter>();
			yield return popup.CloseTransitionTask.ToCoroutine();

			Assert.That(events, Is.EqualTo(new[] { "close", "open" }));
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);
			Assert.That(popup.gameObject.activeSelf, Is.True);
		}

		[UnityTest]
		public IEnumerator CloseAllUi_CallbackClosesAndReopensSameUi_KeepsItOpen()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPopupPresenterB>().ToCoroutine();
			var first = _service.GetUi<TestPopupPresenter>();
			first.ClosedCallback = () =>
			{
				_service.CloseUi<TestPopupPresenterB>();
				_service.OpenUiAsync<TestPopupPresenterB>().Forget();
			};

			_service.CloseAllUi();

			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
			Assert.That(_service.IsVisible<TestPopupPresenterB>(), Is.True);
		}

		[UnityTest]
		public IEnumerator CloseAllUiLayer_CallbackClosesAndReopensSameUi_KeepsItOpen()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPopupPresenterB>().ToCoroutine();
			// 레이어 오버로드는 뒤에서부터 닫으므로 B 의 콜백이 A 를 닫았다 다시 엽니다
			var last = _service.GetUi<TestPopupPresenterB>();
			last.ClosedCallback = () =>
			{
				_service.CloseUi<TestPopupPresenter>();
				_service.OpenUiAsync<TestPopupPresenter>().Forget();
			};

			_service.CloseAllUi(10);

			Assert.That(_service.IsVisible<TestPopupPresenterB>(), Is.False);
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);
		}

		[UnityTest]
		public IEnumerator FeatureClosesDuringOpening_EndsClosedAndHidden()
		{
			var loadTask = _service.LoadUiAsync<TestPopupPresenterC>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			popup.GetComponent<TestCallbackFeature>().Opening = () => _service.CloseUi<TestPopupPresenterC>();

			yield return _service.OpenUiAsync<TestPopupPresenterC>().ToCoroutine();
			yield return popup.OpenTransitionTask.ToCoroutine();
			yield return popup.CloseTransitionTask.ToCoroutine();

			Assert.That(_service.IsVisible<TestPopupPresenterC>(), Is.False);
			Assert.That(popup.gameObject.activeSelf, Is.False);
		}

		[UnityTest]
		public IEnumerator OpenedSubscriberUnloadsPresenter_CompletesOpenTaskWithoutErrors()
		{
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			_service.PresenterOpened += _ => _service.UnloadUi<TestPopupPresenter>();

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(popup.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Succeeded));
			Assert.That(_service.VisiblePresenters, Is.Empty);
			LogAssert.NoUnexpectedReceived();
		}

		[UnityTest]
		public IEnumerator OpenedSubscriberRemovesPresenter_DoesNotActivateIt()
		{
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			_service.PresenterOpened += _ => _service.RemoveUi<TestPopupPresenter>();

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(popup.gameObject.activeSelf, Is.False);
			Assert.That(popup.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Succeeded));
			Object.Destroy(popup.gameObject);
		}

		[UnityTest]
		public IEnumerator FeatureRemovesPresenterDuringOpening_DoesNotActivateIt()
		{
			var loadTask = _service.LoadUiAsync<TestPopupPresenterC>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			popup.GetComponent<TestCallbackFeature>().Opening = () => _service.RemoveUi(popup);

			yield return _service.OpenUiAsync<TestPopupPresenterC>().ToCoroutine();

			Assert.That(popup.gameObject.activeSelf, Is.False);
			Assert.That(popup.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Succeeded));
			Object.Destroy(popup.gameObject);
		}

		[UnityTest]
		public IEnumerator DisposeFromOpenedHook_RunsCloseHooksOfVisibleUiOnce()
		{
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var closedCount = 0;
			openTask.GetAwaiter().GetResult().ClosedCallback = () => closedCount++;
			var loadTask = _service.LoadUiAsync<TestPopupPresenterB>();
			yield return loadTask.ToCoroutine();
			loadTask.GetAwaiter().GetResult().OpenedCallback = () => _service.Dispose();

			yield return _service.OpenUiAsync<TestPopupPresenterB>().ToCoroutine();

			Assert.That(closedCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator ThrowingIsModal_DoesNotStallLaterChanges()
		{
			yield return _service.LoadUiAsync<TestPopupPresenterB>().ToCoroutine();
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var first = loadTask.GetAwaiter().GetResult();
			_service.GetUi<TestPopupPresenterB>().ThrowOnIsModal = true;
			var thirdTask = _service.LoadUiAsync<TestPopupPresenterC>();
			yield return thirdTask.ToCoroutine();
			var third = thirdTask.GetAwaiter().GetResult();
			first.OpenedCallback = () =>
			{
				_service.OpenUiAsync<TestPopupPresenterB>().Forget();
				_service.OpenUiAsync<TestPopupPresenterC>().Forget();
			};
			LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("modal boom"));
			LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("modal boom"));

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(_service.IsVisible<TestPopupPresenterC>(), Is.True);
			Assert.That(third.gameObject.activeSelf, Is.True);
			_service.GetUi<TestPopupPresenterB>().ThrowOnIsModal = false;
		}

		[UnityTest]
		public IEnumerator ThrowingOpenedHook_FaultsOpenTaskAndLogsOnce()
		{
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			loadTask = default;
			popup.OpenedCallback = () => throw new InvalidOperationException("hook boom");
			CountExceptionLogs("hook boom");
			LogAssert.Expect(LogType.Exception, new Regex("hook boom"));

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(popup.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(_exceptionLogCount, Is.EqualTo(1));

			// 실패한 태스크를 붙잡은 참조를 모두 놓은 뒤 거둬, GC 때 다시 기록되지 않는지 확인합니다
			var weak = new WeakReference(popup);
			popup = null;
			_service.UnloadUi<TestPopupPresenter>();
			yield return CollectGarbage();

			Assert.That(weak.IsAlive, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator ThrowingClosedHook_HidesPresenterFaultsTaskAndLogsOnce()
		{
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var popup = openTask.GetAwaiter().GetResult();
			openTask = default;
			popup.ClosedCallback = () => throw new InvalidOperationException("close boom");
			CountExceptionLogs("close boom");
			LogAssert.Expect(LogType.Exception, new Regex("close boom"));

			_service.CloseUi<TestPopupPresenter>(destroy: true);

			Assert.That(popup.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(_service.TryGetUi(new UiInstanceId(typeof(TestPopupPresenter), "popup"), out _), Is.False);

			var weak = new WeakReference(popup);
			popup = null;
			yield return CollectGarbage();

			Assert.That(weak.IsAlive, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator ThrowingClosedHookAndUnload_StillFaultsCloseTask()
		{
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var popup = openTask.GetAwaiter().GetResult();
			popup.ClosedCallback = () => throw new InvalidOperationException("close boom");
			_mockLoader.ThrowOnUnload = true;
			LogAssert.Expect(LogType.Exception, new Regex("close boom"));
			LogAssert.Expect(LogType.Exception, new Regex("unload boom"));

			_service.CloseUi<TestPopupPresenter>(destroy: true);
			yield return null;

			_mockLoader.ThrowOnUnload = false;
			Assert.That(popup.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
		}

		[UnityTest]
		public IEnumerator ThrowingFeatureCloseCompletedHook_HidesPresenterAndLogsOnce()
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			loadTask = default;
			var feature = presenter.GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0f, 0.05f);
			feature.ThrowOnClosedCompleted = true;
			feature = null;
			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();
			CountExceptionLogs("feature close boom");
			LogAssert.Expect(LogType.Exception, new Regex("feature close boom"));

			_service.CloseUi<TestLayerUiPresenter>();
			yield return new WaitForSecondsRealtime(0.2f);

			Assert.That(presenter.gameObject.activeSelf, Is.False);
			Assert.That(presenter.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(_exceptionLogCount, Is.EqualTo(1));

			var weak = new WeakReference(presenter);
			presenter = null;
			_service.UnloadUi<TestLayerUiPresenter>();
			yield return CollectGarbage();

			Assert.That(weak.IsAlive, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator ThrowingFeatureOpenStartedHook_FaultsOpenTaskAndLogsOnce()
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			loadTask = default;
			var feature = presenter.GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0.05f, 0f);
			feature.ThrowOnOpenStarted = true;
			feature = null;
			CountExceptionLogs("feature open boom");
			LogAssert.Expect(LogType.Exception, new Regex("feature open boom"));

			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();

			Assert.That(presenter.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(_exceptionLogCount, Is.EqualTo(1));

			var weak = new WeakReference(presenter);
			presenter = null;
			_service.UnloadUi<TestLayerUiPresenter>();
			yield return CollectGarbage();

			Assert.That(weak.IsAlive, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator FeatureCloseStartFailsThenReopen_LogsFailureImmediately()
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			var feature = presenter.GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0f, 0.05f);
			feature.ThrowOnCloseStarted = true;
			// 실패한 시작 훅 다음 피처가 다시 열어 이번 닫기는 대기 없이 일찍 끝납니다
			presenter.GetComponent<TestCallbackFeature>().Closing = () => _service.OpenUiAsync<TestLayerUiPresenter>().Forget();
			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();
			CountExceptionLogs("feature close start boom");
			LogAssert.Expect(LogType.Exception, new Regex("feature close start boom"));

			_service.CloseUi<TestLayerUiPresenter>();

			Assert.That(_exceptionLogCount, Is.EqualTo(1));
			Assert.That(_service.IsVisible<TestLayerUiPresenter>(), Is.True);

			// TearDown 의 닫기가 새로 실패하지 않게 끕니다
			feature.ThrowOnCloseStarted = false;
		}

		[UnityTest]
		public IEnumerator FeatureCloseStartFails_HidesAndFaultsClose()
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			var feature = presenter.GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0f, 0.05f);
			feature.ThrowOnCloseStarted = true;
			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();
			LogAssert.Expect(LogType.Exception, new Regex("feature close start boom"));

			_service.CloseUi<TestLayerUiPresenter>();

			Assert.That(presenter.gameObject.activeSelf, Is.False);
			Assert.That(presenter.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
		}

		[UnityTest]
		public IEnumerator OpenFeatureFailureThenRecovered_ReopenSucceeds([Values(0.05f, 0f)] float reopenDelay)
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			var feature = presenter.GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0.05f, 0f);
			feature.ThrowOnOpenStarted = true;
			LogAssert.Expect(LogType.Exception, new Regex("feature open boom"));
			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();
			Assert.That(presenter.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));

			// 원인을 없애고 닫았다 다시 열면, 이전 실패가 새 열기로 넘어오지 않아야 합니다
			feature.ThrowOnOpenStarted = false;
			feature.SetDelays(reopenDelay, 0f);
			_service.CloseUi<TestLayerUiPresenter>();
			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();
			yield return new WaitForSecondsRealtime(0.2f);

			Assert.That(presenter.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Succeeded));
			Assert.That(_service.IsVisible<TestLayerUiPresenter>(), Is.True);
			Assert.That(presenter.gameObject.activeSelf, Is.True);
			LogAssert.NoUnexpectedReceived();
		}

		[UnityTest]
		public IEnumerator FeatureStartFailureThenCallbackFailure_LogsBothOnce()
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			loadTask = default;
			var feature = presenter.GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0f, 0.05f);
			feature.ThrowOnCloseStarted = true;
			feature = null;
			presenter.GetComponent<TestCallbackFeature>().Closing = () => throw new InvalidOperationException("callback boom");
			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();
			CountExceptionLogs("boom");
			// 시작한 피처 전환의 실패를 먼저 관찰해 기록하고, 단계를 실패시킨 콜백 예외를 다음에 기록합니다
			LogAssert.Expect(LogType.Exception, new Regex("feature close start boom"));
			LogAssert.Expect(LogType.Exception, new Regex("callback boom"));

			_service.CloseUi<TestLayerUiPresenter>();

			Assert.That(_exceptionLogCount, Is.EqualTo(2));

			var weak = new WeakReference(presenter);
			presenter = null;
			_service.UnloadUi<TestLayerUiPresenter>();
			yield return CollectGarbage();

			Assert.That(weak.IsAlive, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(2));
		}

		[UnityTest]
		public IEnumerator MultipleFeatureCloseFailures_LogEachOnce()
		{
			var loadTask = _service.LoadUiAsync<TestDataUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			loadTask = default;
			foreach (var feature in presenter.GetComponents<TestCountingDelayFeature>())
			{
				feature.SetDelays(0f, 0.05f);
				feature.ThrowOnClosedCompleted = true;
			}

			yield return _service.OpenUiAsync<TestDataUiPresenter>().ToCoroutine();
			CountExceptionLogs("feature close boom");
			LogAssert.Expect(LogType.Exception, new Regex("feature close boom"));
			LogAssert.Expect(LogType.Exception, new Regex("feature close boom"));

			_service.CloseUi<TestDataUiPresenter>();
			yield return new WaitForSecondsRealtime(0.2f);

			Assert.That(presenter.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(presenter.gameObject.activeSelf, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(2));

			var weak = new WeakReference(presenter);
			presenter = null;
			_service.UnloadUi<TestDataUiPresenter>();
			yield return CollectGarbage();

			Assert.That(weak.IsAlive, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(2));
		}

		[UnityTest]
		public IEnumerator DestroyingCloseWithThrowingLoader_FaultsCloseTask()
		{
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var popup = openTask.GetAwaiter().GetResult();
			_mockLoader.ThrowOnUnload = true;
			CountExceptionLogs("unload boom");
			LogAssert.Expect(LogType.Exception, new Regex("unload boom"));

			_service.CloseUi<TestPopupPresenter>(destroy: true);
			yield return null;
			_mockLoader.ThrowOnUnload = false;

			Assert.That(popup.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(_exceptionLogCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator CustomFeatureThrowsBeforeNewTask_WithConsumedPooledTask_FaultsCloseOnly()
		{
			var openTask = _service.OpenUiAsync<TestZeroDelayPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			var feature = presenter.GetComponent<TestCustomTransitionFeature>();
			feature.UsePooledTask = true;

			// 첫 닫기에서 프레젠터가 일반 async UniTask 를 기다려 소비합니다
			_service.CloseUi<TestZeroDelayPresenter>();
			yield return new WaitForSecondsRealtime(0.1f);
			yield return _service.OpenUiAsync<TestZeroDelayPresenter>().ToCoroutine();
			yield return null;

			feature.ThrowBeforeAssign = true;
			CountExceptionLogs("boom");
			LogAssert.Expect(LogType.Exception, new Regex("custom closing boom"));

			_service.CloseUi<TestZeroDelayPresenter>();
			feature.ThrowBeforeAssign = false;

			Assert.That(presenter.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(presenter.gameObject.activeSelf, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(1));
			LogAssert.NoUnexpectedReceived();
		}

		[UnityTest]
		public IEnumerator CustomFeatureThrowsBeforeNewTask_WithPreviousFaultedSource_DoesNotRelogOldError()
		{
			var openTask = _service.OpenUiAsync<TestZeroDelayPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			var feature = presenter.GetComponent<TestCustomTransitionFeature>();
			feature.FailWith = new InvalidOperationException("custom transition boom");
			LogAssert.Expect(LogType.Exception, new Regex("custom transition boom"));

			_service.CloseUi<TestZeroDelayPresenter>();
			feature.FailWith = null;
			yield return _service.OpenUiAsync<TestZeroDelayPresenter>().ToCoroutine();

			feature.ThrowBeforeAssign = true;
			CountExceptionLogs("boom");
			LogAssert.Expect(LogType.Exception, new Regex("custom closing boom"));

			_service.CloseUi<TestZeroDelayPresenter>();
			feature.ThrowBeforeAssign = false;

			Assert.That(_exceptionLogCount, Is.EqualTo(1));
			Assert.That(presenter.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			LogAssert.NoUnexpectedReceived();
		}

		[UnityTest]
		public IEnumerator NewTransitionStatusThrows_FaultsCloseAndLogsOnce()
		{
			var openTask = _service.OpenUiAsync<TestZeroDelayPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			presenter.GetComponent<TestCustomTransitionFeature>().UseDeferredThrow = true;
			CountExceptionLogs("deferred close boom");
			LogAssert.Expect(LogType.Exception, new Regex("deferred close boom"));

			_service.CloseUi<TestZeroDelayPresenter>();

			Assert.That(presenter.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
			Assert.That(presenter.gameObject.activeSelf, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(1));
			LogAssert.NoUnexpectedReceived();
		}

		[UnityTest]
		public IEnumerator StatusErrorAndAnotherFeatureFailure_LogEachOnce([Values(true, false)] bool countingFirst)
		{
			var type = countingFirst ? typeof(TestPlainPresenter) : typeof(TestPlainPresenterB);
			// 기본 열기 지연(0.5초)이 GC 때까지 프레젠터를 붙잡지 않도록 지연을 먼저 정하고 엽니다
			var loadTask = _service.LoadUiAsync(type);
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			loadTask = default;
			var counting = presenter.GetComponent<TestCountingDelayFeature>();
			counting.SetDelays(0f, 0.05f);
			counting.ThrowOnCloseStarted = true;
			counting = null;
			presenter.GetComponent<TestCustomTransitionFeature>().UseAlwaysThrowingStatus = true;
			yield return _service.OpenUiAsync(type).ToCoroutine();
			CountExceptionLogs("boom");
			LogAssert.Expect(LogType.Exception, new Regex("feature close start boom"));
			LogAssert.Expect(LogType.Exception, new Regex("status boom"));

			_service.CloseUi(type);

			// 한 피처의 상태 읽기 오류가 다른 피처의 실패 관찰을 막지 않아야 합니다
			Assert.That(_exceptionLogCount, Is.EqualTo(2));
			Assert.That(presenter.CloseTransitionTask.Status, Is.EqualTo(UniTaskStatus.Faulted));

			var weak = new WeakReference(presenter);
			presenter = null;
			_service.UnloadUi(type);
			yield return CollectGarbage();

			Assert.That(weak.IsAlive, Is.False);
			Assert.That(_exceptionLogCount, Is.EqualTo(2));
		}

		[UnityTest]
		public IEnumerator DisposeFromClosedSubscriber_RunsThatCloseHookBeforeUnload()
		{
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var closedCount = 0;
			openTask.GetAwaiter().GetResult().ClosedCallback = () => closedCount++;
			_service.PresenterClosed += _ => _service.Dispose();

			_service.CloseUi<TestPopupPresenter>();

			Assert.That(closedCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator ReAddInRemovalNotification_KeepsNewOpenTaskPending()
		{
			var openTask = _service.OpenUiAsync<TestUiPresenter>();
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			var feature = presenter.GetComponent<TrackingFeature>();
			feature.SimulateDelayedTransitions = true;
			var reAdded = false;
			_service.PresenterClosed += _ =>
			{
				if (reAdded)
				{
					return;
				}

				reAdded = true;
				_service.AddUi(presenter, 0, openAfter: true);
			};

			_service.RemoveUi(presenter);

			// 새 등록의 열기는 피처 전환이 끝나기 전까지 끝나지 않아야 합니다
			Assert.That(presenter.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Pending));

			feature.SimulateOpenTransitionComplete();
			yield return null;
			Assert.That(presenter.OpenTransitionTask.Status, Is.EqualTo(UniTaskStatus.Succeeded));
		}

		private void CountExceptionLogs(string text)
		{
			_exceptionLogText = text;
			_exceptionLogCount = 0;
			Application.logMessageReceived += OnLogMessage;
		}

		private void OnLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Exception && condition.Contains(_exceptionLogText))
			{
				_exceptionLogCount++;
			}
		}

		private static IEnumerator CollectGarbage()
		{
			// 관찰되지 않은 UniTask 예외는 GC 파이널라이저에서 다시 기록되므로 강제로 거둬 확인합니다
			yield return null;
			System.GC.Collect();
			System.GC.WaitForPendingFinalizers();
			System.GC.Collect();
			yield return null;
			yield return null;
		}

		[UnityTest]
		public IEnumerator SubscriberClosesOnOpened_LaterSubscribersSeeSameOrder()
		{
			var events = new List<string>();
			_service.PresenterOpened += _ => _service.CloseUi<TestPopupPresenter>();
			_service.PresenterOpened += _ => events.Add("open");
			_service.PresenterClosed += _ => events.Add("close");

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(events, Is.EqualTo(new[] { "open", "close" }));
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
		}

		[UnityTest]
		public IEnumerator CloseAllUi_CallbackReloadsSameUi_KeepsNewInstanceOpen()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPopupPresenterB>().ToCoroutine();
			var oldB = _service.GetUi<TestPopupPresenterB>();
			_service.GetUi<TestPopupPresenter>().ClosedCallback = () =>
			{
				_service.UnloadUi<TestPopupPresenterB>();
				_service.OpenUiAsync<TestPopupPresenterB>().Forget();
			};

			_service.CloseAllUi();

			Assert.That(_service.IsVisible<TestPopupPresenterB>(), Is.True);
			Assert.That(_service.GetUi<TestPopupPresenterB>(), Is.Not.SameAs(oldB));
		}

		[UnityTest]
		public IEnumerator CloseAllUiLayer_CallbackReloadsSameUi_KeepsNewInstanceOpen()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPopupPresenterB>().ToCoroutine();
			var oldA = _service.GetUi<TestPopupPresenter>();
			_service.GetUi<TestPopupPresenterB>().ClosedCallback = () =>
			{
				_service.UnloadUi<TestPopupPresenter>();
				_service.OpenUiAsync<TestPopupPresenter>().Forget();
			};

			_service.CloseAllUi(10);

			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);
			Assert.That(_service.GetUi<TestPopupPresenter>(), Is.Not.SameAs(oldA));
		}

		[UnityTest]
		public IEnumerator InstantReopenDuringCloseDelay_SkipsStaleClosedHook()
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var feature = loadTask.GetAwaiter().GetResult().GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0f, 0.05f);
			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();

			_service.CloseUi<TestLayerUiPresenter>();
			_service.OpenUiAsync<TestLayerUiPresenter>().Forget();
			yield return new WaitForSecondsRealtime(0.2f);

			Assert.That(feature.ClosedCompletedCount, Is.EqualTo(0));
		}

		[UnityTest]
		public IEnumerator InstantCloseDuringOpenDelay_SkipsStaleOpenedHook()
		{
			var loadTask = _service.LoadUiAsync<TestLayerUiPresenter>();
			yield return loadTask.ToCoroutine();
			var feature = loadTask.GetAwaiter().GetResult().GetComponent<TestCountingDelayFeature>();
			feature.SetDelays(0.05f, 0f);

			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();
			_service.CloseUi<TestLayerUiPresenter>();
			yield return new WaitForSecondsRealtime(0.2f);

			Assert.That(feature.OpenedCompletedCount, Is.EqualTo(0));
		}

		private List<string> RecordEvents()
		{
			var events = new List<string>();
			_service.PresenterOpened += _ => events.Add("open");
			_service.PresenterClosed += _ => events.Add("close");
			return events;
		}

		[UnityTest]
		public IEnumerator OpenedCallbackOpensUi_VisibleOrderFollowsOpenOrder()
		{
			yield return _service.LoadUiAsync<TestPopupPresenterB>().ToCoroutine();
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			popup.OpenedCallback = () => _service.OpenUiAsync<TestPopupPresenterB>().Forget();

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(2));
			Assert.That(_service.VisiblePresenters[0].PresenterType, Is.EqualTo(typeof(TestPopupPresenter)));
			Assert.That(_service.VisiblePresenters[1].PresenterType, Is.EqualTo(typeof(TestPopupPresenterB)));
		}
	}
}
