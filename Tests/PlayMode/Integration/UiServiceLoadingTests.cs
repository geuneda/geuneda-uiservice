using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiServiceLoadingTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("test_presenter");
			_mockLoader.RegisterPrefab<TestDataUiPresenter>("data_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0),
				TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "data_presenter", 1)
			);
			_service.Init(configs);
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}
	
		[UnityTest]
		public IEnumerator LoadUiAsync_ValidConfig_LoadsPresenter()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Assert
			Assert.IsNotNull(presenter);
			Assert.IsInstanceOf<TestUiPresenter>(presenter);
			Assert.That(presenter.gameObject.activeSelf, Is.False);
		}

		[UnityTest]
		public IEnumerator LoadUiAsync_WithOpenAfter_OpensPresenter()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: true);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Assert
			Assert.IsNotNull(presenter);
			Assert.That(presenter.gameObject.activeSelf, Is.True);
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
		}
	
		[UnityTest]
		public IEnumerator LoadUiAsync_AlreadyLoaded_ReturnsExisting()
		{
			// Arrange
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var firstPresenter = task1.GetAwaiter().GetResult();

			// Act
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task2.ToCoroutine();
			var secondPresenter = task2.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual(firstPresenter, secondPresenter);
			Assert.AreEqual(1, _mockLoader.InstantiateCallCount);
		}
	
		[UnityTest]
		public IEnumerator LoadUiAsync_MissingConfig_ThrowsException()
		{
			// Arrange - 설정을 제거하고 TestUiPresenter 설정 없이 서비스를 다시 생성합니다
			_service.Dispose();
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs());

			// Act & Assert
			KeyNotFoundException caughtException = null;
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			
			while (!task.Status.IsCompleted())
			{
				yield return null;
			}
			
			try
			{
				task.GetAwaiter().GetResult();
			}
			catch (KeyNotFoundException ex)
			{
				caughtException = ex;
			}

			Assert.IsNotNull(caughtException);
		}
	
		[UnityTest]
		public IEnumerator UnloadUi_LoadedUi_UnloadsCorrectly()
		{
			// Arrange
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Act
			_service.UnloadUi(typeof(TestUiPresenter));

			// Assert
			Assert.AreEqual(1, _mockLoader.UnloadCallCount);
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}
	
		[UnityTest]
		public IEnumerator LoadUiAsync_WithCancellation_CancelsOperation()
		{
			// Arrange
			_mockLoader.SimulatedDelayMs = 1000;
			var cts = new CancellationTokenSource();

			// Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter), false, cts.Token);
			yield return null; // Let it start
			cts.Cancel();

			// Wait for the task to complete (cancelled or otherwise)
			while (!task.Status.IsCompleted())
			{
				yield return null;
			}

			// Assert
			System.OperationCanceledException caughtException = null;
			try
			{
				task.GetAwaiter().GetResult();
			}
			catch (System.OperationCanceledException ex)
			{
				caughtException = ex;
			}
			
			Assert.IsNotNull(caughtException);
		}

		#region 표시 상태 일관성 테스트

		/// <summary>
		/// 이미 표시 중인 프레젠터에 대한 LoadUiAsync가 표시 상태를 변경하지 않는지 확인합니다.
		/// LoadUiAsync는 로드 작업이며, 표시 관리 작업이 아닙니다.
		/// openAfter 매개변수는 프레젠터가 새로 로드될 때만 적용됩니다.
		/// </summary>
		[UnityTest]
		public IEnumerator LoadUiAsync_OnVisiblePresenter_WithOpenAfterFalse_DoesNotChangeVisibility()
		{
			// Arrange - 먼저 프레젠터를 엽니다
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.gameObject.activeSelf, Is.True);

			// Act - 이미 표시 중인 프레젠터에 openAfter=false로 LoadUiAsync를 호출합니다
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: false);
			yield return loadTask.ToCoroutine();

			// Assert - 프레젠터는 표시 상태를 유지해야 합니다 (LoadUiAsync는 이미 로드된 프레젠터의 표시를 관리하지 않습니다)
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.gameObject.activeSelf, Is.True);
		}

		/// <summary>
		/// openAfter=true로 이미 표시 중인 프레젠터에 대한 LoadUiAsync가 표시 상태를 유지하는지 확인합니다.
		/// </summary>
		[UnityTest]
		public IEnumerator LoadUiAsync_OnVisiblePresenter_WithOpenAfterTrue_RemainsOpen()
		{
			// Arrange - 먼저 프레젠터를 엽니다
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));

			// Act - 이미 표시 중인 프레젠터에 openAfter=true로 LoadUiAsync를 호출합니다
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: true);
			yield return loadTask.ToCoroutine();

			// Assert - 프레젠터는 표시 상태를 유지해야 합니다 (상태 변경 없음)
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.gameObject.activeSelf, Is.True);
		}

		/// <summary>
		/// CloseUi가 프레젠터를 올바르게 닫고 OpenUiAsync가 다시 열 수 있는지 확인합니다.
		/// 표시 관리를 위한 올바른 API(LoadUiAsync가 아닌 CloseUi)를 테스트합니다.
		/// </summary>
		[UnityTest]
		public IEnumerator OpenUiAsync_AfterCloseUi_OpensSuccessfully()
		{
			// Arrange - 프레젠터를 열고 CloseUi를 통해 닫습니다
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter = openTask1.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// CloseUi를 통해 닫습니다 (닫기를 위한 올바른 API)
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));

			// Act - 다시 엽니다
			var openTask2 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask2.ToCoroutine();
			var reopenedPresenter = openTask2.GetAwaiter().GetResult();
			yield return reopenedPresenter.OpenTransitionTask.ToCoroutine();

			// Assert - 프레젠터가 성공적으로 열려야 합니다
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(reopenedPresenter.gameObject.activeSelf, Is.True);
			Assert.AreEqual(presenter, reopenedPresenter); // 동일한 인스턴스
		}

		/// <summary>
		/// CloseUi가 닫기 생명주기 훅을 올바르게 호출하는지 확인합니다.
		/// 닫기를 위한 올바른 API(LoadUiAsync가 아닌 CloseUi)를 테스트합니다.
		/// </summary>
		[UnityTest]
		public IEnumerator CloseUi_OnVisiblePresenter_CallsCloseLifecycleHooks()
		{
			// Arrange - 먼저 프레젠터를 엽니다
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult() as TestUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			var closeCountBefore = presenter.CloseCount;
			var closeTransitionCountBefore = presenter.CloseTransitionCompletedCount;

			// Act - CloseUi를 호출합니다 (닫기를 위한 올바른 API)
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert - 닫기 생명주기 훅이 호출되었어야 합니다
			Assert.That(presenter.CloseCount, Is.EqualTo(closeCountBefore + 1));
			Assert.That(presenter.CloseTransitionCompletedCount, Is.EqualTo(closeTransitionCountBefore + 1));
		}

		/// <summary>
		/// 이미 로드되었지만 닫힌 프레젠터에 openAfter=true로 LoadUiAsync를 호출해도
		/// 열리지 않는지 확인합니다 (openAfter는 새로 로드된 프레젠터에만 적용됩니다).
		/// 이미 로드된 프레젠터를 열려면 OpenUiAsync를 사용하세요.
		/// </summary>
		[UnityTest]
		public IEnumerator LoadUiAsync_OnClosedPresenter_WithOpenAfterTrue_DoesNotOpen()
		{
			// Arrange - 로드하지만 열지 않습니다 (프레젠터는 닫힌 상태)
			var loadTask1 = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: false);
			yield return loadTask1.ToCoroutine();
			var presenter = loadTask1.GetAwaiter().GetResult() as TestUiPresenter;

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));
			var openCountBefore = presenter.OpenCount;

			// Act - 이미 로드된 닫힌 프레젠터에 openAfter=true로 LoadUiAsync를 호출합니다
			var loadTask2 = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: true);
			yield return loadTask2.ToCoroutine();

			// Assert - 프레젠터는 닫힌 상태를 유지해야 합니다 (openAfter는 새 로드에만 적용)
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));
			Assert.That(presenter.OpenCount, Is.EqualTo(openCountBefore));
		}

		/// <summary>
		/// OpenUiAsync가 이미 로드되었지만 닫힌 프레젠터를 올바르게 여는지 확인합니다.
		/// 이것이 이미 로드된 프레젠터를 여는 올바른 방법입니다.
		/// </summary>
		[UnityTest]
		public IEnumerator OpenUiAsync_OnClosedPresenter_CallsOpenLifecycleHooks()
		{
			// Arrange - 로드하지만 열지 않습니다 (프레젠터는 닫힌 상태)
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: false);
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult() as TestUiPresenter;

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));
			var openCountBefore = presenter.OpenCount;
			var openTransitionCountBefore = presenter.OpenTransitionCompletedCount;

			// Act - OpenUiAsync를 호출합니다 (이미 로드된 프레젠터를 여는 올바른 API)
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Assert - 열기 생명주기 훅이 호출되었어야 합니다
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.OpenCount, Is.EqualTo(openCountBefore + 1));
			Assert.That(presenter.OpenTransitionCompletedCount, Is.EqualTo(openTransitionCountBefore + 1));
		}

		#endregion
	}
}
