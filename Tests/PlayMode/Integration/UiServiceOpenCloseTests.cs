using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiServiceOpenCloseTests
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
		public IEnumerator OpenUiAsync_LoadedUi_OpensSuccessfully()
		{
			// 준비
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return loadTask.ToCoroutine();

			// 실행
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();

			// 검증
			Assert.That(presenter.gameObject.activeSelf, Is.True);
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator OpenUiAsync_NotLoaded_LoadsAndOpens()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// 검증
			Assert.IsNotNull(presenter);
			Assert.That(presenter.gameObject.activeSelf, Is.True);
			Assert.AreEqual(1, _mockLoader.InstantiateCallCount);
		}

		[UnityTest]
		public IEnumerator OpenUiAsync_WithData_SetsData()
		{
			// 준비
			var data = new TestPresenterData { Id = 42, Name = "Test" };

			// 실행
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter), data);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// 검증
			Assert.IsNotNull(presenter);
			Assert.That(presenter.WasDataSet, Is.True);
			Assert.AreEqual(42, presenter.ReceivedData.Id);
			Assert.AreEqual("Test", presenter.ReceivedData.Name);
		}

		[UnityTest]
		public IEnumerator CloseUi_OpenUi_ClosesSuccessfully()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증
			Assert.That(presenter.gameObject.activeSelf, Is.False);
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator CloseUi_WithDestroy_UnloadsUi()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestUiPresenter), destroy: true);
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증
			Assert.AreEqual(1, _mockLoader.UnloadCallCount);
		}

		[UnityTest]
		public IEnumerator CloseAllUi_MultipleOpen_ClosesAll()
		{
			// 준비
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult();

			// 실행
			_service.CloseAllUi();
			
			// 두 프레젠터의 닫기 완료 대기
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// 검증
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator CloseAllUi_WithLayer_ClosesOnlyLayer()
		{
			// 준비
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task2.ToCoroutine();

			// 실행 - 레이어 0만 닫기
			_service.CloseAllUi(0);
			yield return presenter1.CloseTransitionTask.ToCoroutine();

			// 검증 - TestUiPresenter(레이어 0) 닫힘, TestDataUiPresenter(레이어 1) 여전히 표시
			Assert.AreEqual(1, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator VisiblePresenters_TracksOpenClose()
		{
			// 실행 1 - 열기
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// 검증1
			Assert.AreEqual(1, _service.VisiblePresenters.Count);

			// 실행 2 - 닫기
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증2
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator OpenUiAsync_Twice_ReturnsSamePresenter()
		{
			// 실행
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var p1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task2.ToCoroutine();
			var p2 = task2.GetAwaiter().GetResult();

			// 검증
			Assert.AreEqual(p1, p2);
			Assert.AreEqual(1, _mockLoader.InstantiateCallCount);
		}

		[UnityTest]
		public IEnumerator CloseUi_NotVisible_DoesNothing()
		{
			// 실행
			_service.CloseUi(typeof(TestUiPresenter));

			// 검증
			Assert.Pass();
			yield return null;
		}

		[UnityTest]
		public IEnumerator CloseAllUi_Empty_DoesNothing()
		{
			// 실행
			_service.CloseAllUi();

			// 검증
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OpenTransitionCompleted_AlwaysCalled()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 검증 - OnOpenTransitionCompleted는 전환 기능 없이도 호출되어야 함
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
			Assert.AreEqual(1, presenter.OpenTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator CloseTransitionCompleted_AlwaysCalled()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증 - OnCloseTransitionCompleted는 전환 기능 없이도 호출되어야 함
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.AreEqual(1, presenter.CloseTransitionCompletedCount);
		}

		#region 데이터 설정자 테스트

		[UnityTest]
		public IEnumerator SetData_DirectAssignment_CallsOnSetData()
		{
			// 준비 - 초기 데이터 없이 열기
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// 실행 - 프로퍼티 설정자를 통해 직접 데이터 설정
			presenter.Data = new TestPresenterData { Id = 99, Name = "Direct" };

			// 검증
			Assert.That(presenter.WasDataSet, Is.True);
			Assert.AreEqual(99, presenter.ReceivedData.Id);
			Assert.AreEqual("Direct", presenter.ReceivedData.Name);
		}

		[UnityTest]
		public IEnumerator SetData_DirectAssignment_DataPropertyReturnsValue()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;
			var data = new TestPresenterData { Id = 123, Name = "Readable" };

			// 실행
			presenter.Data = data;

			// 검증 - Data 프로퍼티 getter가 할당된 값을 반환
			Assert.AreEqual(123, presenter.Data.Id);
			Assert.AreEqual("Readable", presenter.Data.Name);
		}

		[UnityTest]
		public IEnumerator SetData_MultipleUpdates_CallsOnSetDataEachTime()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// 실행 - 데이터를 여러 번 설정
			presenter.Data = new TestPresenterData { Id = 1, Name = "First" };
			presenter.Data = new TestPresenterData { Id = 2, Name = "Second" };
			presenter.Data = new TestPresenterData { Id = 3, Name = "Third" };

			// 검증 - OnSetData가 3번 호출되어야 함
			Assert.AreEqual(3, presenter.OnSetDataCallCount);
			Assert.AreEqual(3, presenter.Data.Id);
			Assert.AreEqual("Third", presenter.Data.Name);
		}

		[UnityTest]
		public IEnumerator SetData_OnAlreadyOpenPresenter_UpdatesDynamically()
		{
			// 준비 - 초기 데이터와 함께 열기
			var initialData = new TestPresenterData { Id = 1, Name = "Initial" };
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter), initialData);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행 - 프레젠터가 이미 열린 상태에서 데이터 업데이트
			var updatedData = new TestPresenterData { Id = 2, Name = "Updated" };
			presenter.Data = updatedData;

			// 검증 - OnSetData가 두 번 호출되어야 함 (열기 시 한 번, 업데이트 시 한 번)
			Assert.AreEqual(2, presenter.OnSetDataCallCount);
			Assert.AreEqual(2, presenter.Data.Id);
			Assert.AreEqual("Updated", presenter.Data.Name);
		}

		[UnityTest]
		public IEnumerator SetData_ConsecutiveUpdates_PreservesLatestValue()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// 실행 - 빠른 연속 업데이트
			for (var i = 0; i < 10; i++)
			{
				presenter.Data = new TestPresenterData { Id = i, Name = $"Update{i}" };
			}

			// 검증 - 최신 값만 유지되어야 하며, OnSetData는 10번 호출되어야 함
			Assert.AreEqual(9, presenter.Data.Id);
			Assert.AreEqual("Update9", presenter.Data.Name);
			Assert.AreEqual(10, presenter.OnSetDataCallCount);
		}

		#endregion
	}
}
