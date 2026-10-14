using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// HandleBack, 뒤로가기 핸들러, BackUnhandled 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiBackNavigationTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private int _unhandledCount;

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
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterB), "popup_b", 20),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterC), "popup_c", 10),
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "hud", 30)));

			_unhandledCount = 0;
			_service.BackUnhandled += () => _unhandledCount++;
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest]
		public IEnumerator HandleBack_ClosesHighestLayerTarget()
		{
			yield return _service.OpenUiAsync<TestPopupPresenterB>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(_service.HandleBack(), Is.True);

			Assert.That(_service.IsVisible<TestPopupPresenterB>(), Is.False);
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);
		}

		[UnityTest]
		public IEnumerator HandleBack_SameLayer_ClosesLaterOpenedFirst()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPopupPresenterC>().ToCoroutine();

			_service.HandleBack();
			Assert.That(_service.IsVisible<TestPopupPresenterC>(), Is.False);
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);

			_service.HandleBack();
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
		}

		[UnityTest]
		public IEnumerator HandleBack_SkipsNonTargets_AndRaisesUnhandledWhenNoneLeft()
		{
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();

			Assert.That(_service.HandleBack(), Is.False);

			Assert.That(_unhandledCount, Is.EqualTo(1));
			Assert.That(_service.IsVisible<TestUiPresenter>(), Is.True);
		}

		[UnityTest]
		public IEnumerator HandleBack_DisabledTarget_FallsThroughToNextTarget()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			var openTask = _service.OpenUiAsync<TestPopupPresenterB>();
			yield return openTask.ToCoroutine();
			openTask.GetAwaiter().GetResult().BackEnabled = false;

			_service.HandleBack();

			Assert.That(_service.IsVisible<TestPopupPresenterB>(), Is.True);
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
		}

		[UnityTest]
		public IEnumerator HandleBack_ConsumingTarget_StaysOpen()
		{
			var openTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return openTask.ToCoroutine();
			var popup = openTask.GetAwaiter().GetResult();
			popup.ConsumeBackWithoutClosing = true;

			Assert.That(_service.HandleBack(), Is.True);

			Assert.That(popup.BackPressedCount, Is.EqualTo(1));
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);
			Assert.That(_unhandledCount, Is.EqualTo(0));
		}

		[UnityTest]
		public IEnumerator Handlers_RunByPriorityThenNewestFirst_BeforePresenters()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			var calls = new List<string>();
			_service.AddBackHandler(() => { calls.Add("low-old"); return false; });
			_service.AddBackHandler(() => { calls.Add("high"); return false; }, priority: 5);
			_service.AddBackHandler(() => { calls.Add("low-new"); return false; });

			_service.HandleBack();

			Assert.That(calls, Is.EqualTo(new[] { "high", "low-new", "low-old" }));
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
		}

		[UnityTest]
		public IEnumerator Handler_ReturningTrue_StopsBeforePresenters()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			var lowerCalled = false;
			_service.AddBackHandler(() => { lowerCalled = true; return false; });
			_service.AddBackHandler(() => true, priority: 1);

			Assert.That(_service.HandleBack(), Is.True);

			Assert.That(lowerCalled, Is.False);
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);
		}

		[Test]
		public void Handler_Dispose_RemovesHandler_AndCanRepeat()
		{
			var called = 0;
			var registration = _service.AddBackHandler(() => { called++; return true; });

			registration.Dispose();
			registration.Dispose();
			_service.HandleBack();

			Assert.That(called, Is.EqualTo(0));
			Assert.That(_unhandledCount, Is.EqualTo(1));
		}

		[Test]
		public void Handler_RemovedByEarlierHandler_IsNotCalled()
		{
			var removedCalled = false;
			var otherCalled = false;
			var removed = _service.AddBackHandler(() => { removedCalled = true; return false; }, priority: -1);
			_service.AddBackHandler(() => { otherCalled = true; return false; });
			_service.AddBackHandler(() => { removed.Dispose(); return false; }, priority: 1);

			_service.HandleBack();

			Assert.That(removedCalled, Is.False);
			Assert.That(otherCalled, Is.True);
			Assert.That(_unhandledCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator HandleBack_WhileInputBlocked_DoesNothing()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			var handlerCalled = false;
			_service.AddBackHandler(() => { handlerCalled = true; return false; });
			var block = _service.BlockInput("test");

			Assert.That(_service.HandleBack(), Is.False);

			Assert.That(handlerCalled, Is.False);
			Assert.That(_unhandledCount, Is.EqualTo(0));
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);

			block.Dispose();
			Assert.That(_service.HandleBack(), Is.True);
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.False);
		}

		[Test]
		public void HandleBack_AfterDispose_ReturnsFalse()
		{
			_service.Dispose();

			Assert.That(_service.HandleBack(), Is.False);
		}

		[Test]
		public void AddBackHandler_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => _service.AddBackHandler(null));
		}
	}
}
