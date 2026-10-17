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
	/// 한 서비스의 여러 UiPopupQueue 가 로드 중인 팩토리를 서로 기다리는지 테스트합니다.
	/// </summary>
	[TestFixture]
	public class UiPopupQueueMultiQueueTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private UiPopupQueue _first;
		private UiPopupQueue _second;
		private UiPopupQueue _third;
		private List<string> _shown;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestPopupPresenter>("popup");
			_mockLoader.RegisterPrefab<TestPopupPresenterB>("popup_b");
			_mockLoader.RegisterPrefab<TestPopupPresenterC>("popup_c");
			_mockLoader.RegisterPrefab<TestUiPresenter>("hud");

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenter), "popup", 10),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterB), "popup_b", 10),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterC), "popup_c", 10),
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "hud", 0)));

			_first = new UiPopupQueue(_service);
			_second = new UiPopupQueue(_service);
			_third = new UiPopupQueue(_service);
			_shown = new List<string>();
		}

		[TearDown]
		public void TearDown()
		{
			_first?.Dispose();
			_second?.Dispose();
			_third?.Dispose();
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest]
		public IEnumerator OtherQueue_WaitsWhileFactoryLoads_ThenForItsPopup()
		{
			var load = new UniTaskCompletionSource();
			_first.Enqueue("first", 0, LoadThenOpen<TestPopupPresenter>("first", load));
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			yield return null;

			Assert.That(_shown, Is.Empty);
			Assert.That(_second.IsShowing, Is.False);
			Assert.That(_second.Count, Is.EqualTo(1));

			// 먼저 시작한 큐의 팝업이 뜨고, 다른 큐는 그 팝업(뒤로가기 대상)이 닫힐 때까지 기다립니다
			load.TrySetResult();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "first" }));

			_service.CloseUi<TestPopupPresenter>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "first", "second" }));
		}

		[UnityTest]
		public IEnumerator FactoryReturningNull_WaitingQueueGoesBeforeOwnNextEntry()
		{
			var load = new UniTaskCompletionSource<UiPresenter>();
			_first.Enqueue("skip", 5, _ => load.Task);
			_first.Enqueue("first-next", 0, OpenFactory<TestPopupPresenter>("first-next"));
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			yield return null;
			Assert.That(_shown, Is.Empty);

			load.TrySetResult(null);
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "second" }));
			Assert.That(_first.Count, Is.EqualTo(1));

			_service.CloseUi<TestPopupPresenterB>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "second", "first-next" }));
		}

		[UnityTest]
		public IEnumerator FactoryThrowing_LetsWaitingQueueProceed()
		{
			var load = new UniTaskCompletionSource<UiPresenter>();
			_first.Enqueue("broken", 0, _ => load.Task);
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			yield return null;
			Assert.That(_shown, Is.Empty);

			LogAssert.Expect(LogType.Error, new Regex(@"\[Geuneda\.UiService\.PopupQueue\] The popup factory threw \(key 'broken'\)"));
			LogAssert.Expect(LogType.Exception, new Regex("load failed"));
			load.TrySetException(new InvalidOperationException("load failed"));
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "second" }));
		}

		[UnityTest]
		public IEnumerator FactoryCancelled_LetsWaitingQueueProceed()
		{
			var load = new UniTaskCompletionSource<UiPresenter>();
			_first.Enqueue("cancelled", 0, _ => load.Task);
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			yield return null;
			Assert.That(_shown, Is.Empty);

			load.TrySetCanceled();
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "second" }));
		}

		[UnityTest]
		public IEnumerator DisposingLoadingQueue_LetsWaitingQueueProceed()
		{
			// 토큰을 무시하고 끝나지 않는 팩토리
			var load = new UniTaskCompletionSource<UiPresenter>();
			_first.Enqueue("hanging", 0, _ => load.Task);
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			yield return null;
			Assert.That(_shown, Is.Empty);

			_first.Dispose();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "second" }));

			load.TrySetCanceled();
		}

		[UnityTest]
		public IEnumerator WaitingQueues_ProceedInTheOrderTheyStartedWaiting()
		{
			var load = new UniTaskCompletionSource<UiPresenter>();
			_first.Enqueue("skip", 0, _ => load.Task);
			_third.Enqueue("third", 0, OpenFactory<TestPopupPresenterC>("third"));
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			yield return null;
			Assert.That(_shown, Is.Empty);

			load.TrySetResult(null);
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "third" }));

			_service.CloseUi<TestPopupPresenterC>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "third", "second" }));
		}

		[UnityTest]
		public IEnumerator HeldWaitingQueue_IsSkipped_AndResumesAfterRelease()
		{
			var load = new UniTaskCompletionSource<UiPresenter>();
			_first.Enqueue("skip", 0, _ => load.Task);
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			_third.Enqueue("third", 0, OpenFactory<TestPopupPresenterC>("third"));
			yield return null;
			var hold = _second.Hold("tutorial");

			load.TrySetResult(null);
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "third" }));

			// 풀려도 다른 큐의 팝업(뒤로가기 대상)이 닫힐 때까지 기다립니다
			hold.Dispose();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "third" }));

			_service.CloseUi<TestPopupPresenterC>();
			yield return null;
			Assert.That(_shown, Is.EqualTo(new[] { "third", "second" }));
		}

		[UnityTest]
		public IEnumerator WaitingQueueDrainedThrowing_DoesNotStopLoadedQueue()
		{
			var load = new UniTaskCompletionSource<UiPresenter>();
			_first.Enqueue("skip", 5, _ => load.Task);
			_first.Enqueue("first-next", 0, OpenFactory<TestPopupPresenter>("first-next"));
			_second.Enqueue("second-skip", 0, _ => UniTask.FromResult<UiPresenter>(null));
			_second.Drained += () => throw new InvalidOperationException("drained boom");
			yield return null;

			// 기다리던 큐가 진행 중에 던져도 로드를 마친 큐는 정리하고 다음 항목을 띄웁니다
			LogAssert.Expect(LogType.Error, new Regex(@"\[Geuneda\.UiService\.PopupQueue\] A waiting popup queue threw while resuming"));
			LogAssert.Expect(LogType.Exception, new Regex("drained boom"));
			load.TrySetResult(null);
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "first-next" }));
		}

		[UnityTest]
		public IEnumerator NonBackPopupFromOtherQueue_DoesNotDelayAfterItsLoad()
		{
			var load = new UniTaskCompletionSource();
			_first.Enqueue("hud", 0, LoadThenOpen<TestUiPresenter>("hud", load));
			_second.Enqueue("second", 0, OpenFactory<TestPopupPresenterB>("second"));
			yield return null;
			Assert.That(_shown, Is.Empty);

			load.TrySetResult();
			yield return null;

			Assert.That(_shown, Is.EqualTo(new[] { "hud", "second" }));
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

		// 로드가 끝날 때까지 팝업을 열지 않는 팩토리
		private Func<CancellationToken, UniTask<UiPresenter>> LoadThenOpen<T>(string label, UniTaskCompletionSource load) where T : UiPresenter
		{
			return async ct =>
			{
				await load.Task;
				var presenter = await _service.OpenUiAsync<T>(ct);
				_shown.Add(label);
				return presenter;
			};
		}
	}
}
