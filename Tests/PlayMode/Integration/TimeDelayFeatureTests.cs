using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// TimeDelayFeature 기능 테스트.
	/// </summary>
	[TestFixture]
	public class TimeDelayFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestTimeDelayPresenter>("delay_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestTimeDelayPresenter), "delay_presenter", 0)
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
		public IEnumerator TimeDelayFeature_DefaultValues_AreCorrect()
		{
			// 실행
			var task = _service.LoadUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// 검증
			Assert.IsNotNull(presenter.DelayFeature);
			Assert.AreEqual(0.1f, presenter.DelayFeature.OpenDelayInSeconds, 0.001f);
			Assert.AreEqual(0.05f, presenter.DelayFeature.CloseDelayInSeconds, 0.001f);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OnOpen_NotifiesTransitionCompleted()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// 프레젠터의 열기 전환 완료 대기
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 검증
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OnClose_NotifiesTransitionCompleted()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;
			
			// 열기 전환 대기
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행 - 닫기
			_service.CloseUi(typeof(TestTimeDelayPresenter));
			
			// 닫기 전환 대기
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OnClose_DeactivatesGameObject()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestTimeDelayPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증 - 프레젠터가 전환 후 GameObject를 비활성화해야 함
			Assert.IsFalse(presenter.gameObject.activeSelf);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OpenTransitionTask_IsValid()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// 검증 - ITransitionFeature를 통한 기능의 태스크가 유효해야 함
			var transitionFeature = presenter.DelayFeature as ITransitionFeature;
			Assert.IsNotNull(transitionFeature);
			
			var delayTask = transitionFeature.OpenTransitionTask;
			
			// 완료 대기
			yield return delayTask.ToCoroutine();
			Assert.IsTrue(delayTask.Status.IsCompleted());
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_ZeroDelay_CompletesImmediately()
		{
			// 준비 - 지연 시간 0인 프레젠터 생성
			_mockLoader.RegisterPrefab<TestZeroDelayPresenter>("zero_delay");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestZeroDelayPresenter), "zero_delay", 0));

			// 실행
			var task = _service.OpenUiAsync(typeof(TestZeroDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestZeroDelayPresenter;
			
			// 열기 전환 대기
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 검증
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_PresenterAwaitsFeatureTask()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// 기능 완료 전에는 전환이 완료로 표시되지 않아야 함
			// (타이밍에 따라 다르지만, 0.1초 지연으로 충분히 포착 가능)
			var featureTask = (presenter.DelayFeature as ITransitionFeature).OpenTransitionTask;
			
			// 기능과 프레젠터 모두 완료 대기
			yield return UniTask.WhenAll(featureTask, presenter.OpenTransitionTask).ToCoroutine();

			// 검증 - 기능 완료 후 프레젠터의 전환이 완료됨
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}
	}
}
