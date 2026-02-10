using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 단일 프레젠터의 동시/다중 기능에 대한 테스트.
	/// 여러 ITransitionFeature 구현이 올바르게 대기되는지 검증합니다.
	/// </summary>
	[TestFixture]
	public class ConcurrentFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestDualFeaturePresenter>("dual_feature");
			_mockLoader.RegisterPrefab<TestTripleFeaturePresenter>("triple_feature");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestDualFeaturePresenter), "dual_feature", 0),
				TestHelpers.CreateTestConfig(typeof(TestTripleFeaturePresenter), "triple_feature", 1)
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
		public IEnumerator DualFeatures_BothReceiveLifecycleCallbacks()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;

			// 검증 - 두 기능 모두 생명주기 콜백을 수신
			Assert.IsTrue(presenter.FeatureA.WasOpened);
			Assert.IsTrue(presenter.FeatureB.WasOpened);
		}

		[UnityTest]
		public IEnumerator DualFeatures_OnOpenTransitionCompleted_CalledOnce()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;

			// 프레젠터 전환 완료 대기
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 검증 - 프레젠터가 정확히 하나의 알림을 수신
			Assert.AreEqual(1, presenter.OpenTransitionCount);
		}

		[UnityTest]
		public IEnumerator DualFeatures_WithDelays_PresenterAwaitsAll()
		{
			// 준비 - 프레젠터 참조를 얻기 위해 먼저 지연 없이 열기
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();
			
			// 먼저 지연 없이 닫기
			_service.CloseUi(typeof(TestDualFeaturePresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();
			
			// 두 번째 열기를 위해 지연 전환 활성화
			presenter.FeatureA.SimulateDelayedTransitions = true;
			presenter.FeatureB.SimulateDelayedTransitions = true;
			
			// 두 번째 열기 - 이번에는 지연 전환과 함께
			task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();

			// 전환이 아직 완료되지 않아야 함 (기능이 대기 중)
			yield return null;
			
			// 기능 A만 완료
			presenter.FeatureA.SimulateOpenTransitionComplete();
			yield return null;
			
			// B를 아직 기다리는 중이므로 전환 횟수는 첫 번째 열기의 1 그대로여야 함
			Assert.AreEqual(1, presenter.OpenTransitionCount);
			
			// 기능 B 완료
			presenter.FeatureB.SimulateOpenTransitionComplete();
			yield return presenter.OpenTransitionTask.ToCoroutine();
			
			// 이제 전환이 완료되어야 함
			Assert.AreEqual(2, presenter.OpenTransitionCount); // 첫 번째 열기 1 + 두 번째 열기 1
		}

		[UnityTest]
		public IEnumerator TripleFeatures_AllReceiveCallbacksInOrder()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTripleFeaturePresenter;

			// 검증 - 세 기능 모두 콜백을 수신
			Assert.IsTrue(presenter.FeatureA.WasOpened);
			Assert.IsTrue(presenter.FeatureB.WasOpened);
			Assert.IsTrue(presenter.FeatureC.WasOpened);
			
			// 순서 확인 (A가 B보다 먼저, B가 C보다 먼저)
			Assert.IsTrue(presenter.FeatureA.OpenOrder < presenter.FeatureB.OpenOrder);
			Assert.IsTrue(presenter.FeatureB.OpenOrder < presenter.FeatureC.OpenOrder);
		}

		[UnityTest]
		public IEnumerator TripleFeatures_OnOpenTransitionCompleted_CalledOnce()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTripleFeaturePresenter;

			// 전환 대기
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 검증 - 알림이 하나만 (세 개가 아닌)
			Assert.AreEqual(1, presenter.OpenTransitionCount);
		}

		[UnityTest]
		public IEnumerator MixedFeatures_CloseLifecycle_WorksCorrectly()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestDualFeaturePresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증 - 두 기능 모두 닫기 콜백을 수신
			Assert.IsTrue(presenter.FeatureA.WasClosing);
			Assert.IsTrue(presenter.FeatureB.WasClosing);
		}

		[UnityTest]
		public IEnumerator RapidOpenClose_FeaturesHandleCorrectly()
		{
			// 실행 - 빠른 열기/닫기 반복
			for (int i = 0; i < 3; i++)
			{
				var openTask = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
				yield return openTask.ToCoroutine();
				var presenter = openTask.GetAwaiter().GetResult() as TestDualFeaturePresenter;
				yield return presenter.OpenTransitionTask.ToCoroutine();
				
				_service.CloseUi(typeof(TestDualFeaturePresenter));
				yield return presenter.CloseTransitionTask.ToCoroutine();
			}

			// 검증 - 예외 없이 모든 반복이 완료되어야 함
			Assert.Pass("Rapid open/close cycles completed without errors");
		}

		[UnityTest]
		public IEnumerator FeatureOrder_Deterministic()
		{
			// 여러 번 열기에서 기능 순서가 일관되는지 테스트
			int[] firstRunOrder = new int[3];
			int[] secondRunOrder = new int[3];

			// 첫 번째 실행
			var task1 = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult() as TestTripleFeaturePresenter;
			yield return presenter1.OpenTransitionTask.ToCoroutine();
			
			firstRunOrder[0] = presenter1.FeatureA.OpenOrder;
			firstRunOrder[1] = presenter1.FeatureB.OpenOrder;
			firstRunOrder[2] = presenter1.FeatureC.OpenOrder;
			
			_service.CloseUi(typeof(TestTripleFeaturePresenter));
			yield return presenter1.CloseTransitionTask.ToCoroutine();

			// 언로드 후 다시 로드
			_service.UnloadUi(typeof(TestTripleFeaturePresenter));
			
			// 두 번째 실행
			var task2 = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult() as TestTripleFeaturePresenter;
			yield return presenter2.OpenTransitionTask.ToCoroutine();
			
			secondRunOrder[0] = presenter2.FeatureA.OpenOrder;
			secondRunOrder[1] = presenter2.FeatureB.OpenOrder;
			secondRunOrder[2] = presenter2.FeatureC.OpenOrder;

			// 검증 - 순서가 일관되어야 함 (절대값이 아닌 상대적 순서)
			Assert.IsTrue(secondRunOrder[0] < secondRunOrder[1]);
			Assert.IsTrue(secondRunOrder[1] < secondRunOrder[2]);
		}

		[UnityTest]
		public IEnumerator MultipleFeatures_CloseTransition_PresenterAwaitsAll()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();
			
			// 닫기를 위한 지연 전환 활성화
			presenter.FeatureA.SimulateDelayedTransitions = true;
			presenter.FeatureB.SimulateDelayedTransitions = true;

			// 실행 - 닫기
			_service.CloseUi(typeof(TestDualFeaturePresenter));
			yield return null;

			// GameObject가 아직 활성 상태여야 함 (전환 대기 중)
			Assert.IsTrue(presenter.gameObject.activeSelf);
			
			// 두 전환 모두 완료
			presenter.FeatureA.SimulateCloseTransitionComplete();
			presenter.FeatureB.SimulateCloseTransitionComplete();
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 이제 GameObject가 숨겨져야 함
			Assert.IsFalse(presenter.gameObject.activeSelf);
		}
	}
}
