using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// UiPopupQueue 의 순서, 중복 방지, 멈춤, 진행 조건 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiPopupQueueTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private UiPopupQueue _queue;
		private List<string> _shown;
		private int _drainedCount;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestPopupPresenter>("popup");
			_mockLoader.RegisterPrefab<TestPopupPresenterB>("popup_b");
			_mockLoader.RegisterPrefab<TestPopupPresenterC>("popup_c");
			_mockLoader.RegisterPrefab<TestTimeDelayPresenter>("delay_popup");
			_mockLoader.RegisterPrefab<TestUiPresenter>("hud");
			var trackedPrefab = TestHelpers.CreateTestPresenterPrefab<TestLayerUiPresenter>();
			trackedPrefab.AddComponent<TrackingFeature>();
			_mockLoader.RegisterPrefab("tracked", trackedPrefab);

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenter), "popup", 10),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterB), "popup_b", 10),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterC), "popup_c", 10),
				TestHelpers.CreateTestConfig(typeof(TestTimeDelayPresenter), "delay_popup", 10),
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "hud", 0),
				TestHelpers.CreateTestConfig(typeof(TestLayerUiPresenter), "tracked", 10)));

			_queue = new UiPopupQueue(_service);
			_shown = new List<string>();
			_drainedCount = 0;
			_queue.Drained += () => _drainedCount++;
		}

		[TearDown]
		public void TearDown()
		{
			_queue?.Dispose();
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest]
		public IEnumerator Enqueue_ShowsByPriorityThenInsertionOrder()
		{
			var hold = _queue.Hold("setup");
			_queue.Enqueue("low", 1, OpenFactory<TestPopupPresenter>("low"));
			_queue.Enqueue("high-first", 5, OpenFactory<TestPopupPresenterB>("high-first"));
			_queue.Enqueue("high-second", 5, OpenFactory<TestPopupPresenterC>("high-second"));
			hold.Dispose();
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "high-first" }));
			Assert.That(_queue.Count, Is.EqualTo(2));

			_service.CloseUi<TestPopupPresenterB>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "high-first", "high-second" }));

			_service.CloseUi<TestPopupPresenterC>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "high-first", "high-second", "low" }));
			Assert.That(_drainedCount, Is.EqualTo(0));

			_service.CloseUi<TestPopupPresenter>();
			yield return null;
			Assert.That(_drainedCount, Is.EqualTo(1));
			Assert.That(_queue.IsShowing, Is.False);
		}

		[UnityTest]
		public IEnumerator Enqueue_SameKeyWhilePendingOrShowing_IsRejected()
		{
			Assert.That(_queue.Enqueue("notice", 0, OpenFactory<TestPopupPresenter>("notice")), Is.True);
			yield return null;
			Assert.That(_queue.Enqueue("notice", 0, OpenFactory<TestPopupPresenterB>("dup-showing")), Is.False);

			Assert.That(_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenterB>("next")), Is.True);
			Assert.That(_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenterC>("dup-pending")), Is.False);
			Assert.That(_queue.Count, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator FactoryReturningNull_IsSkipped()
		{
			_queue.Enqueue("skip", 5, _ => UniTask.FromResult<UiPresenter>(null));
			_queue.Enqueue("show", 0, OpenFactory<TestPopupPresenter>("show"));
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "show" }));
		}

		[UnityTest]
		public IEnumerator FactoryThrowing_IsLoggedAndSkipped()
		{
			LogAssert.Expect(LogType.Error, new Regex(@"\[Geuneda\.UiService\.PopupQueue\] The popup factory threw \(key 'broken'\)"));
			LogAssert.Expect(LogType.Exception, new Regex("factory failed"));
			_queue.Enqueue("broken", 5, _ => throw new InvalidOperationException("factory failed"));
			_queue.Enqueue("show", 0, OpenFactory<TestPopupPresenter>("show"));
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "show" }));
		}

		[Test]
		public void OnlySkippedEntries_RaiseDrainedOnce()
		{
			_queue.Enqueue("a", 0, _ => UniTask.FromResult<UiPresenter>(null));

			Assert.That(_drainedCount, Is.EqualTo(1));

			var hold = _queue.Hold("batch");
			_queue.Enqueue("b", 0, _ => UniTask.FromResult<UiPresenter>(null));
			_queue.Enqueue("c", 0, _ => UniTask.FromResult<UiPresenter>(null));
			hold.Dispose();

			Assert.That(_drainedCount, Is.EqualTo(2));
		}

		[UnityTest]
		public IEnumerator NextPopup_WaitsForCloseTransition()
		{
			_queue.Enqueue("delayed", 5, OpenFactory<TestTimeDelayPresenter>("delayed"));
			_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenter>("next"));
			yield return null;
			var delayed = _service.GetUi<TestTimeDelayPresenter>();
			yield return delayed.OpenTransitionTask.ToCoroutine();

			_service.CloseUi<TestTimeDelayPresenter>();

			Assert.That(_shown, Is.EqualTo(new[] { "delayed" }));
			Assert.That(_queue.IsShowing, Is.True);

			yield return delayed.CloseTransitionTask.ToCoroutine();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "delayed", "next" }));
		}

		[UnityTest]
		public IEnumerator Hold_PausesUntilAllTokensReleased()
		{
			var first = _queue.Hold("tutorial");
			var second = _queue.Hold("cutscene");
			_queue.Enqueue("popup", 0, OpenFactory<TestPopupPresenter>("popup"));

			Assert.That(_queue.HoldReasons, Is.EqualTo(new[] { "tutorial", "cutscene" }));
			first.Dispose();
			yield return null;
			Assert.That(_shown, Is.Empty);

			second.Dispose();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "popup" }));
		}

		[Test]
		public void Hold_DoubleDispose_Warns()
		{
			var hold = _queue.Hold("once");
			hold.Dispose();
			LogAssert.Expect(LogType.Warning, new Regex("이미 풀린 멈춤 토큰"));

			hold.Dispose();

			Assert.That(_queue.IsHeld, Is.False);
		}

		[UnityTest]
		public IEnumerator OutsideBackTarget_DelaysQueueUntilClosed()
		{
			yield return _service.OpenUiAsync<TestPopupPresenterC>().ToCoroutine();

			_queue.Enqueue("popup", 0, OpenFactory<TestPopupPresenter>("popup"));
			yield return null;
			Assert.That(_shown, Is.Empty);

			_service.CloseUi<TestPopupPresenterC>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "popup" }));
		}

		[UnityTest]
		public IEnumerator OutsideNonBackPresenter_DoesNotDelayQueue()
		{
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();

			_queue.Enqueue("popup", 0, OpenFactory<TestPopupPresenter>("popup"));
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "popup" }));
		}

		[Test]
		public void RemoveAndClear_DropPendingEntries()
		{
			var hold = _queue.Hold("setup");
			_queue.Enqueue("a", 0, OpenFactory<TestPopupPresenter>("a"));
			_queue.Enqueue("b", 0, OpenFactory<TestPopupPresenterB>("b"));
			_queue.Enqueue("c", 0, OpenFactory<TestPopupPresenterC>("c"));

			Assert.That(_queue.Remove("b"), Is.True);
			Assert.That(_queue.Remove("missing"), Is.False);
			Assert.That(_queue.Count, Is.EqualTo(2));

			_queue.Clear();
			hold.Dispose();

			Assert.That(_queue.Count, Is.EqualTo(0));
			Assert.That(_shown, Is.Empty);
		}

		[UnityTest]
		public IEnumerator Dispose_CancelsFactoryToken_AndStopsQueue()
		{
			var token = default(CancellationToken);
			var gate = new UniTaskCompletionSource<UiPresenter>();
			_queue.Enqueue("pending-open", 5, ct =>
			{
				token = ct;
				return gate.Task;
			});
			_queue.Enqueue("never", 0, OpenFactory<TestPopupPresenter>("never"));

			_queue.Dispose();
			gate.TrySetCanceled();
			yield return null;

			Assert.That(token.IsCancellationRequested, Is.True);
			Assert.That(_shown, Is.Empty);
			Assert.Throws<ObjectDisposedException>(() => _queue.Enqueue("late", 0, OpenFactory<TestPopupPresenter>("late")));
		}

		[UnityTest]
		public IEnumerator DestroyingClose_NextEntryReopensSameType_IsNotUnloaded()
		{
			_queue.Enqueue("first", 5, OpenFactory<TestTimeDelayPresenter>("first"));
			_queue.Enqueue("second", 0, OpenFactory<TestTimeDelayPresenter>("second"));
			yield return null;
			yield return _service.GetUi<TestTimeDelayPresenter>().OpenTransitionTask.ToCoroutine();

			_service.CloseUi<TestTimeDelayPresenter>(destroy: true);
			yield return new WaitForSecondsRealtime(0.3f);

			Assert.That(_shown, Is.EqualTo(new[] { "first", "second" }));
			Assert.That(_service.IsVisible<TestTimeDelayPresenter>(), Is.True);
			Assert.That(_service.GetUi<TestTimeDelayPresenter>() != null, Is.True);
		}

		[UnityTest]
		public IEnumerator CurrentPopupReopenedDuringCloseTransition_StaysTracked()
		{
			_queue.Enqueue("delayed", 0, OpenFactory<TestTimeDelayPresenter>("delayed"));
			yield return null;
			var presenter = _service.GetUi<TestTimeDelayPresenter>();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			_service.CloseUi<TestTimeDelayPresenter>();
			_service.OpenUiAsync<TestTimeDelayPresenter>().Forget();
			yield return new WaitForSecondsRealtime(0.3f);

			Assert.That(_queue.IsShowing, Is.True);
			Assert.That(_drainedCount, Is.EqualTo(0));
			Assert.That(_queue.Enqueue("delayed", 0, OpenFactory<TestPopupPresenter>("dup")), Is.False);

			_service.CloseUi<TestTimeDelayPresenter>();
			yield return new WaitForSecondsRealtime(0.3f);

			Assert.That(_queue.IsShowing, Is.False);
			Assert.That(_drainedCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator PopupClosedBeforeFactoryReturns_WaitsForCloseTransition()
		{
			var gate = new UniTaskCompletionSource();
			_queue.Enqueue("gated", 5, async ct =>
			{
				var presenter = await _service.OpenUiAsync<TestTimeDelayPresenter>(ct);
				await gate.Task;
				return presenter;
			});
			_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenter>("next"));
			yield return null;
			var delayed = _service.GetUi<TestTimeDelayPresenter>();
			yield return delayed.OpenTransitionTask.ToCoroutine();

			_service.CloseUi<TestTimeDelayPresenter>();
			gate.TrySetResult();

			Assert.That(_shown, Is.Empty);

			yield return new WaitForSecondsRealtime(0.3f);
			Assert.That(_shown, Is.EqualTo(new[] { "next" }));
		}

		[UnityTest]
		public IEnumerator UnloadWhileClosing_ReleasesQueue()
		{
			_queue.Enqueue("tracked", 5, OpenFactory<TestLayerUiPresenter>("tracked"));
			_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenter>("next"));
			yield return null;
			_service.GetUi<TestLayerUiPresenter>().GetComponent<TrackingFeature>().SimulateDelayedTransitions = true;

			// 닫기 전환이 끝나지 않는 상태에서 언로드합니다
			_service.CloseUi<TestLayerUiPresenter>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "tracked" }));

			_service.UnloadUi<TestLayerUiPresenter>();
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "tracked", "next" }));
		}

		[UnityTest]
		public IEnumerator UnloadAfterCloseReopenClose_ReleasesQueue()
		{
			_queue.Enqueue("tracked", 5, OpenFactory<TestLayerUiPresenter>("tracked"));
			_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenter>("next"));
			yield return null;
			_service.GetUi<TestLayerUiPresenter>().GetComponent<TrackingFeature>().SimulateDelayedTransitions = true;

			// 큐는 첫 닫기를 기다리는데, 그 뒤 다시 열고 닫아 프레젠터의 전환 소스가 교체됩니다
			_service.CloseUi<TestLayerUiPresenter>();
			_service.OpenUiAsync<TestLayerUiPresenter>().Forget();
			_service.CloseUi<TestLayerUiPresenter>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "tracked" }));

			_service.UnloadUi<TestLayerUiPresenter>();
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "tracked", "next" }));
		}

		[UnityTest]
		public IEnumerator ThrowingClosedHook_HidesPopupAndAdvances()
		{
			_queue.Enqueue("first", 5, OpenFactory<TestPopupPresenter>("first"));
			_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenterB>("next"));
			yield return null;
			var first = _service.GetUi<TestPopupPresenter>();
			first.ClosedCallback = () => throw new InvalidOperationException("close boom");
			LogAssert.Expect(LogType.Error, new Regex(@"\[Geuneda\.UiService\.Presenter\] \w+ open or close step failed"));
			LogAssert.Expect(LogType.Exception, new Regex("close boom"));

			_service.CloseUi<TestPopupPresenter>();
			yield return null;

			Assert.That(first.gameObject.activeSelf, Is.False);
			Assert.That(_shown, Is.EqualTo(new[] { "first", "next" }));
		}

		[UnityTest]
		public IEnumerator UnloadWithThrowingLoaderWhileClosing_ReleasesQueue()
		{
			_queue.Enqueue("tracked", 5, OpenFactory<TestLayerUiPresenter>("tracked"));
			_queue.Enqueue("next", 0, OpenFactory<TestPopupPresenter>("next"));
			yield return null;
			_service.GetUi<TestLayerUiPresenter>().GetComponent<TrackingFeature>().SimulateDelayedTransitions = true;
			_service.CloseUi<TestLayerUiPresenter>();
			_mockLoader.ThrowOnUnload = true;

			Assert.Throws<InvalidOperationException>(() => _service.UnloadUi<TestLayerUiPresenter>());
			_mockLoader.ThrowOnUnload = false;
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "tracked", "next" }));
		}

		[Test]
		public void ManySkippedEntries_DrainOnce()
		{
			var hold = _queue.Hold("batch");
			for (var i = 0; i < 3000; i++)
			{
				_queue.Enqueue("skip-" + i, 0, _ => UniTask.FromResult<UiPresenter>(null));
			}

			hold.Dispose();

			Assert.That(_queue.Count, Is.EqualTo(0));
			Assert.That(_drainedCount, Is.EqualTo(1));
		}

		[Test]
		public void Enqueue_InvalidArguments_Throw()
		{
			Assert.Throws<ArgumentException>(() => _queue.Enqueue("", 0, OpenFactory<TestPopupPresenter>("x")));
			Assert.Throws<ArgumentNullException>(() => _queue.Enqueue("key", 0, null));
			Assert.Throws<ArgumentNullException>(() => _queue.Hold(null));
		}

		private Func<CancellationToken, UniTask<UiPresenter>> OpenFactory<T>(string label) where T : UiPresenter
		{
			return async ct =>
			{
				var presenter = await _service.OpenUiAsync<T>(ct);
				_shown.Add(label);
				return presenter;
			};
		}
	}
}
