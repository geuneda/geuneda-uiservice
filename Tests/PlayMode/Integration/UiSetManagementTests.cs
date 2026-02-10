using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiSetManagementTests
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
			
			// 중요: UI 세트는 Load/Open/Close/Unload 작업 간의 적절한 주소 매칭을 보장하기 위해
			// 설정 주소를 사용합니다
			var set = TestHelpers.CreateTestUiSet(1,
				new UiInstanceId(typeof(TestUiPresenter), "test_presenter"),
				new UiInstanceId(typeof(TestDataUiPresenter), "data_presenter")
			);
			
			var configs = TestHelpers.CreateTestConfigsWithSets(
				new[]
				{
					TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0),
					TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "data_presenter", 1)
				},
				new[] { set }
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
		public IEnumerator LoadUiSetAsync_ValidSet_LoadsAllPresenters()
		{
			// Act
			var tasks = _service.LoadUiSetAsync(1);
			foreach (var task in tasks)
			{
				yield return task.ToCoroutine();
			}

			// Assert
			Assert.AreEqual(2, _service.GetLoadedPresenters().Count);
		}
	
		[UnityTest]
		public IEnumerator LoadUiSetAsync_PartiallyLoaded_LoadsOnlyMissing()
		{
			// Arrange - 프레젠터 하나를 미리 로드합니다
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Act
			var tasks = _service.LoadUiSetAsync(1);
			foreach (var t in tasks)
			{
				yield return t.ToCoroutine();
			}

			// Assert - 추가 로드는 하나만 수행됩니다 (총 2회 인스턴스화)
			Assert.AreEqual(2, _mockLoader.InstantiateCallCount);
		}
	
		[UnityTest]
		public IEnumerator CloseAllUiSet_OpenSet_ClosesAllInSet()
		{
			// Arrange
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}
			
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter1 = openTask1.GetAwaiter().GetResult();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();
			var presenter2 = openTask2.GetAwaiter().GetResult();

			// Act
			_service.CloseAllUiSet(1);
			
			// 닫기 전환이 완료될 때까지 대기합니다
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}
	
		[UnityTest]
		public IEnumerator UnloadUiSet_LoadedSet_UnloadsAll()
		{
			// Arrange
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}

			// Act
			_service.UnloadUiSet(1);

			// Assert
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}
	
		[UnityTest]
		public IEnumerator RemoveUiSet_LoadedSet_RemovesAndReturnsPresenters()
		{
			// Arrange
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}

			// Act
			var removed = _service.RemoveUiSet(1);

			// Assert
			Assert.AreEqual(2, removed.Count);
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}

		#region OpenUiSetAsync 테스트

		[UnityTest]
		public IEnumerator OpenUiSetAsync_ValidSet_OpensAllPresenters()
		{
			// Act
			var task = _service.OpenUiSetAsync(1);
			yield return task.ToCoroutine();
			var presenters = task.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual(2, presenters.Length);
			Assert.AreEqual(2, _service.VisiblePresenters.Count);
			Assert.IsTrue(presenters.All(p => p.gameObject.activeSelf));
		}

		[UnityTest]
		public IEnumerator OpenUiSetAsync_NotLoaded_LoadsAndOpensAll()
		{
			// Act - 미리 로드하지 않고 열기
			var task = _service.OpenUiSetAsync(1);
			yield return task.ToCoroutine();
			var presenters = task.GetAwaiter().GetResult();

			// Assert - 둘 다 로드되고 열려야 합니다
			Assert.AreEqual(2, presenters.Length);
			Assert.AreEqual(2, _mockLoader.InstantiateCallCount);
			Assert.AreEqual(2, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator OpenUiSetAsync_ReturnsCorrectPresenterTypes()
		{
			// Act
			var task = _service.OpenUiSetAsync(1);
			yield return task.ToCoroutine();
			var presenters = task.GetAwaiter().GetResult();

			// Assert
			Assert.IsTrue(presenters.Any(p => p is TestUiPresenter));
			Assert.IsTrue(presenters.Any(p => p is TestDataUiPresenter));
		}

		[UnityTest]
		public IEnumerator OpenUiSetAsync_ThenCloseAllUiSet_ClosesAll()
		{
			// Arrange - 세트 메서드를 통해 엽니다
			var openTask = _service.OpenUiSetAsync(1);
			yield return openTask.ToCoroutine();
			var presenters = openTask.GetAwaiter().GetResult();

			// Act - 세트 메서드를 통해 닫습니다
			_service.CloseAllUiSet(1);

			// 닫기 전환을 기다립니다
			foreach (var presenter in presenters)
			{
				yield return presenter.CloseTransitionTask.ToCoroutine();
			}

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		#endregion

		#region 교차 작업 호환성 테스트 (세트 + 개별 메서드)

		[UnityTest]
		public IEnumerator OpenUiAsync_WithoutPreload_ThenCloseAllUiSet_ClosesCorrectly()
		{
			// 이 테스트는 핵심 버그 수정을 검증합니다:
			// LoadUiSetAsync를 통해 미리 로드하지 않고 OpenUiAsync(Type)으로 열 때,
			// ResolveInstanceAddress가 string.Empty 대신 config.Address를 사용하므로
			// CloseAllUiSet이 프레젠터를 올바르게 닫아야 합니다

			// Act - 세트를 통해 먼저 로드하지 않고 직접 엽니다
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter1 = openTask1.GetAwaiter().GetResult();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();
			var presenter2 = openTask2.GetAwaiter().GetResult();

			Assert.AreEqual(2, _service.VisiblePresenters.Count);

			// Act - 세트 메서드를 통해 닫습니다
			_service.CloseAllUiSet(1);

			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert - 개별 메서드로 열었더라도 둘 다 닫아야 합니다
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator OpenUiSetAsync_ThenCloseUi_ClosesIndividualPresenter()
		{
			// 세트를 통해 열린 프레젠터를 개별적으로 닫을 수 있는지 확인합니다

			// Arrange - 세트를 통해 엽니다
			var openTask = _service.OpenUiSetAsync(1);
			yield return openTask.ToCoroutine();
			var presenters = openTask.GetAwaiter().GetResult();
			var testPresenter = presenters.First(p => p is TestUiPresenter);

			// Act - 프레젠터 하나를 개별적으로 닫습니다
			_service.CloseUi(typeof(TestUiPresenter));
			yield return testPresenter.CloseTransitionTask.ToCoroutine();

			// Assert - 하나만 표시 상태로 남아있어야 합니다
			Assert.AreEqual(1, _service.VisiblePresenters.Count);
			Assert.IsFalse(_service.IsVisible<TestUiPresenter>());
			Assert.IsTrue(_service.IsVisible<TestDataUiPresenter>());
		}

		[UnityTest]
		public IEnumerator LoadUiSetAsync_ThenOpenUiAsync_ThenCloseAllUiSet_ClosesAll()
		{
			// 시나리오 검증: 세트로 로드, 개별로 열기, 세트로 닫기

			// Arrange - 세트를 통해 로드합니다
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}

			// 개별 메서드를 통해 엽니다
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter1 = openTask1.GetAwaiter().GetResult();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();
			var presenter2 = openTask2.GetAwaiter().GetResult();

			// Act - 세트를 통해 닫습니다
			_service.CloseAllUiSet(1);
			
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator OpenUiAsync_WithoutPreload_ThenUnloadUiSet_UnloadsAll()
		{
			// 개별 메서드로 열린 프레젠터에서 UnloadUiSet이 작동하는지 확인합니다

			// Arrange - 세트를 통해 로드하지 않고 직접 엽니다
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();

			// Act - 세트를 통해 언로드합니다 (닫기와 언로드가 수행되어야 합니다)
			_service.UnloadUiSet(1);

			// Assert
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		#endregion

		#region ResolveInstanceAddress 일관성 테스트

		[UnityTest]
		public IEnumerator OpenUiAsync_UsesConfigAddressWhenNotLoaded()
		{
			// 인스턴스가 아직 로드되지 않았을 때
			// ResolveInstanceAddress가 config.Address를 반환하는지 테스트합니다

			// Act - 먼저 로드하지 않고 엽니다
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();

			// Assert - 프레젠터가 설정 주소로 로드되었는지 확인합니다
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("test_presenter", loaded[0].Address);
		}

		[UnityTest]
		public IEnumerator LoadUiAsync_WithoutAddress_UsesConfigAddress()
		{
			// 명시적 주소 없이 LoadUiAsync(Type)이 config.Address를 사용하는지 확인합니다
			
			// Act
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return loadTask.ToCoroutine();

			// Assert
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("test_presenter", loaded[0].Address);
		}

		#endregion

		#region 열린 프레젠터가 있는 세트 언로드

		[UnityTest]
		public IEnumerator UnloadUiSet_WithOpenPresenters_UnloadsAll()
		{
			// 프레젠터가 아직 열려있을 때도 UnloadUiSet이 작동하는지 확인합니다

			// Arrange - 세트를 통해 엽니다
			var openTask = _service.OpenUiSetAsync(1);
			yield return openTask.ToCoroutine();
			
			Assert.AreEqual(2, _service.VisiblePresenters.Count);

			// Act - 아직 열려있는 상태에서 언로드합니다
			_service.UnloadUiSet(1);

			// Assert - 언로드되어야 합니다 (암묵적으로 닫기도 수행)
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}

		#endregion

		#region OpenUiSetAsync 엣지 케이스

		[Test]
		public void OpenUiSetAsync_InvalidSetId_ThrowsKeyNotFoundException()
		{
			// Assert
			Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(async () => 
				await _service.OpenUiSetAsync(999));
		}

		[UnityTest]
		public IEnumerator OpenUiSetAsync_CalledTwice_DoesNotDuplicatePresenters()
		{
			// 첫 번째 열기
			var task1 = _service.OpenUiSetAsync(1);
			yield return task1.ToCoroutine();
			
			// 두 번째 열기 (중복되면 안 됩니다)
			var task2 = _service.OpenUiSetAsync(1);
			yield return task2.ToCoroutine();
			
			// Assert - 여전히 프레젠터 2개만 있어야 합니다
			Assert.AreEqual(2, _service.VisiblePresenters.Count);
			Assert.AreEqual(2, _service.GetLoadedPresenters().Count);
		}

		#endregion
	}
}
