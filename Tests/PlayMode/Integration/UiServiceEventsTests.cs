using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// PresenterOpened/PresenterClosed 이벤트와 TryGetUi 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiServiceEventsTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private List<UiInstanceId> _opened;
		private List<UiInstanceId> _closed;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestPopupPresenter>("popup");

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenter), "popup", 10)));

			_opened = new List<UiInstanceId>();
			_closed = new List<UiInstanceId>();
			_service.PresenterOpened += _opened.Add;
			_service.PresenterClosed += _closed.Add;
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest]
		public IEnumerator OpenAndClose_RaiseEventsWithInstanceId()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			_service.CloseUi<TestPopupPresenter>();

			var expected = new UiInstanceId(typeof(TestPopupPresenter), "popup");
			Assert.That(_opened, Is.EqualTo(new[] { expected }));
			Assert.That(_closed, Is.EqualTo(new[] { expected }));
		}

		[UnityTest]
		public IEnumerator UnloadVisibleUi_RaisesClosedOnce()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			_service.UnloadUi<TestPopupPresenter>();

			Assert.That(_closed.Count, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator RemoveHiddenUi_DoesNotRaiseClosed()
		{
			yield return _service.LoadUiAsync<TestPopupPresenter>().ToCoroutine();

			_service.RemoveUi<TestPopupPresenter>();

			Assert.That(_closed, Is.Empty);
		}

		[UnityTest]
		public IEnumerator ThrowingHandler_DoesNotBlockOtherHandlers()
		{
			var received = 0;
			_service.PresenterOpened += _ => throw new InvalidOperationException("boom");
			_service.PresenterOpened += _ => received++;
			LogAssert.Expect(LogType.Error, new Regex(@"\[Geuneda\.UiService\] A visibility handler threw for \S+"));
			LogAssert.Expect(LogType.Exception, new Regex("boom"));

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(received, Is.EqualTo(1));
			Assert.That(_service.IsVisible<TestPopupPresenter>(), Is.True);
		}

		[UnityTest]
		public IEnumerator Dispose_DoesNotRaiseClosed()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			_service.Dispose();

			Assert.That(_closed, Is.Empty);
		}

		[UnityTest]
		public IEnumerator TryGetUi_FindsLoadedPresenterOnly()
		{
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var loaded = loadTask.GetAwaiter().GetResult();

			Assert.That(_service.TryGetUi(new UiInstanceId(typeof(TestPopupPresenter), "popup"), out var found), Is.True);
			Assert.That(found, Is.SameAs(loaded));
			Assert.That(_service.TryGetUi(new UiInstanceId(typeof(TestPopupPresenter), "other"), out var missing), Is.False);
			Assert.That(missing, Is.Null);
		}
	}
}
