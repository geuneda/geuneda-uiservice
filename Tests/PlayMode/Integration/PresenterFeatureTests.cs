using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 프레젠터 기능(TimeDelayFeature, AnimationDelayFeature) 및 전환 생명주기 훅에 대한 테스트.
	/// </summary>
	[TestFixture]
	public class PresenterFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestPresenterWithFeature>("feature_presenter");
			_mockLoader.RegisterPrefab<TestPresenterWithTransitionFeature>("transition_feature_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPresenterWithFeature), "feature_presenter", 0),
				TestHelpers.CreateTestConfig(typeof(TestPresenterWithTransitionFeature), "transition_feature_presenter", 0)
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
		public IEnumerator Feature_OnPresenterInitialized_CalledOnLoad()
		{
			// 실행
			var task = _service.LoadUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// 검증
			Assert.IsNotNull(presenter);
			Assert.IsTrue(presenter.Feature.WasInitialized);
			Assert.AreEqual(presenter, presenter.Feature.ReceivedPresenter);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterOpening_CalledBeforeOpen()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// 검증
			Assert.IsTrue(presenter.Feature.WasOpening);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterOpened_CalledAfterOpen()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// 검증
			Assert.IsTrue(presenter.Feature.WasOpened);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterClosing_CalledOnClose()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// 실행
			_service.CloseUi(typeof(TestPresenterWithFeature));

			// 검증
			Assert.IsTrue(presenter.Feature.WasClosing);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterClosed_CalledAfterClose()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// 실행
			_service.CloseUi(typeof(TestPresenterWithFeature));

			// 검증
			Assert.IsTrue(presenter.Feature.WasClosed);
		}

		[UnityTest]
		public IEnumerator OnOpenTransitionCompleted_AlwaysCalledForPresentersWithoutFeatures()
		{
			// 준비 - 비전환 기능을 가진 프레젠터 사용 (ITransitionFeature 없음)
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// 비동기 프로세스 완료를 위해 한 프레임 대기
			yield return null;

			// 검증 - OnOpenTransitionCompleted가 항상 호출되어야 함
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
			Assert.AreEqual(1, presenter.OpenTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator OnCloseTransitionCompleted_AlwaysCalledForPresentersWithoutFeatures()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// 열기 전환 완료 대기
			yield return null;

			// 실행
			_service.CloseUi(typeof(TestPresenterWithFeature));
			
			// 닫기 전환 완료 대기
			yield return null;

			// 검증 - OnCloseTransitionCompleted가 항상 호출되어야 함
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.AreEqual(1, presenter.CloseTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator TransitionFeature_PresenterAwaitsOpenTransition()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;

			// 전환이 아직 완료되지 않음
			Assert.IsFalse(presenter.WasOpenTransitionCompleted);

			// 실행 - 전환 완료
			presenter.TransitionFeature.CompleteOpenTransition();
			
			// 프레젠터 처리 대기
			yield return null;

			// 검증
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TransitionFeature_PresenterAwaitsCloseTransition()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;
			
			// 먼저 열기 전환 완료
			presenter.TransitionFeature.CompleteOpenTransition();
			yield return null;

			// 실행 - 닫기 및 전환이 대기되는지 확인
			_service.CloseUi(typeof(TestPresenterWithTransitionFeature));
			yield return null;

			// 닫기 전환이 아직 완료되지 않음
			Assert.IsFalse(presenter.WasCloseTransitionCompleted);
			Assert.IsTrue(presenter.gameObject.activeSelf); // 전환 중에는 여전히 표시

			// 전환 완료
			presenter.TransitionFeature.CompleteCloseTransition();
			yield return null;

			// 검증
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.IsFalse(presenter.gameObject.activeSelf); // 전환 후 숨김
		}

		[UnityTest]
		public IEnumerator TransitionFeature_GameObjectHiddenOnlyAfterTransitionCompletes()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;
			presenter.TransitionFeature.CompleteOpenTransition();
			yield return null;

			// 실행 - 닫기 시작
			_service.CloseUi(typeof(TestPresenterWithTransitionFeature));
			yield return null;

			// 검증 - 전환 중에는 여전히 표시
			Assert.IsTrue(presenter.gameObject.activeSelf);

			// 전환 완료
			presenter.TransitionFeature.CompleteCloseTransition();
			yield return null;

			// 검증 - 이제 숨김
			Assert.IsFalse(presenter.gameObject.activeSelf);
		}
	}

	/// <summary>
	/// 기본 기능 생명주기 테스트를 위한 모의 기능(비전환)이 있는 테스트 프레젠터
	/// </summary>
	[RequireComponent(typeof(MockPresenterFeature))]
	public class TestPresenterWithFeature : UiPresenter
	{
		public MockPresenterFeature Feature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }
		public int OpenTransitionCompletedCount { get; private set; }
		public int CloseTransitionCompletedCount { get; private set; }

		private void Awake()
		{
			Feature = GetComponent<MockPresenterFeature>();
			if (Feature == null)
			{
				Feature = gameObject.AddComponent<MockPresenterFeature>();
			}
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
			OpenTransitionCompletedCount++;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
			CloseTransitionCompletedCount++;
		}
	}

	/// <summary>
	/// ITransitionFeature 테스트를 위한 모의 전환 기능이 있는 테스트 프레젠터
	/// </summary>
	[RequireComponent(typeof(MockTransitionFeature))]
	public class TestPresenterWithTransitionFeature : UiPresenter
	{
		public MockTransitionFeature TransitionFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }

		private void Awake()
		{
			TransitionFeature = GetComponent<MockTransitionFeature>();
			if (TransitionFeature == null)
			{
				TransitionFeature = gameObject.AddComponent<MockTransitionFeature>();
			}
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
		}
	}

	/// <summary>
	/// 기본 기능 생명주기 테스트를 위한 모의 기능 (ITransitionFeature를 구현하지 않음)
	/// </summary>
	public class MockPresenterFeature : PresenterFeatureBase
	{
		public bool WasInitialized { get; private set; }
		public bool WasOpening { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosing { get; private set; }
		public bool WasClosed { get; private set; }
		public UiPresenter ReceivedPresenter { get; private set; }

		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			WasInitialized = true;
			ReceivedPresenter = presenter;
		}

		public override void OnPresenterOpening()
		{
			WasOpening = true;
		}

		public override void OnPresenterOpened()
		{
			WasOpened = true;
		}

		public override void OnPresenterClosing()
		{
			WasClosing = true;
		}

		public override void OnPresenterClosed()
		{
			WasClosed = true;
		}
	}

	/// <summary>
	/// 전환 대기 테스트를 위한 ITransitionFeature를 구현하는 모의 기능
	/// </summary>
	public class MockTransitionFeature : PresenterFeatureBase, ITransitionFeature
	{
		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		public override void OnPresenterOpened()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();
		}

		public override void OnPresenterClosing()
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();
		}

		public void CompleteOpenTransition()
		{
			_openTransitionCompletion?.TrySetResult();
		}

		public void CompleteCloseTransition()
		{
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
