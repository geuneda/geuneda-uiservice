using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	[TestFixture]
	public class MultiInstanceTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("test_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
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
		public IEnumerator LoadUi_WithInstanceAddress_CreatesMultipleInstances()
		{
			// 실행
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// 검증
			Assert.AreEqual(2, _mockLoader.InstantiateCallCount);
			Assert.AreEqual(2, _service.GetLoadedPresenters().Count);
		}

		[UnityTest]
		public IEnumerator GetUi_WithInstanceAddress_ReturnsCorrectInstance()
		{
			// 준비
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult();

			// 실행
			var retrieved = _service.GetUi<TestUiPresenter>("instance_2");

			// 검증
			Assert.AreEqual(presenter2, retrieved);
		}

		[UnityTest]
		public IEnumerator IsVisible_WithInstanceAddress_ChecksCorrectInstance()
		{
			// 준비
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// 검증
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_1"), Is.True);
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_2"), Is.False);
		}
	
		[UnityTest]
		public IEnumerator CloseUi_WithInstanceAddress_ClosesCorrectInstance()
		{
			// 준비
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestUiPresenter), "instance_1");
			yield return presenter1.CloseTransitionTask.ToCoroutine();

			// 검증
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_1"), Is.False);
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_2"), Is.True);
		}
	
		[UnityTest]
		public IEnumerator UnloadUi_WithInstanceAddress_UnloadsCorrectInstance()
		{
			// 준비
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// 실행
			_service.UnloadUi(typeof(TestUiPresenter), "instance_1");

			// 검증
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("instance_2", loaded[0].Address);
		}

		[UnityTest]
		public IEnumerator CloseWithDestroyTrue_FromPresenter_UnloadsCorrectInstance()
		{
			// 준비 - 두 인스턴스 로드
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult();

			// 실행 - 프레젠터 내부에서 destroy와 함께 instance_1 닫기
			// 프레젠터가 Close(destroy: true)를 호출하는 것을 시뮬레이션
			_service.CloseUi(presenter1, destroy: true);
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return null; // 언로드 대기

			// 검증 - instance_1만 언로드되어야 함
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count, "하나의 인스턴스만 남아 있어야 함");
			Assert.AreEqual("instance_2", loaded[0].Address, "instance_2가 아직 로드되어 있어야 함");
		}

		[UnityTest]
		public IEnumerator PresenterInstanceAddress_IsSetCorrectly()
		{
			// 준비 & Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter), "my_address");
			yield return task.ToCoroutine();
			var presenter = (TestUiPresenter) task.GetAwaiter().GetResult();

			// 검증
			Assert.AreEqual("my_address", presenter.InstanceAddress);
		}

		[UnityTest]
		public IEnumerator PresenterInstanceAddress_DefaultIsConfigAddress()
		{
			// 준비 & Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = (TestUiPresenter) task.GetAwaiter().GetResult();

			// 검증 - 기본 인스턴스 주소는 설정의 주소여야 함
			Assert.AreEqual("test_presenter", presenter.InstanceAddress);
		}
	}
}
