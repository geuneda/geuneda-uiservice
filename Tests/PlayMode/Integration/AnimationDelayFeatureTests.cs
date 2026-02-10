using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// AnimationDelayFeature 기능 테스트.
	/// </summary>
	[TestFixture]
	public class AnimationDelayFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestAnimationDelayPresenter>("animation_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestAnimationDelayPresenter), "animation_presenter", 0)
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
		public IEnumerator AnimationDelayFeature_NoClips_HasZeroDelay()
		{
			// 실행
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// 검증 - 클립이 없으면 지연 시간 0
			Assert.AreEqual(0f, presenter.AnimationFeature.OpenDelayInSeconds);
			Assert.AreEqual(0f, presenter.AnimationFeature.CloseDelayInSeconds);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_OnOpen_NotifiesTransitionCompleted()
		{
			// 실행
			var task = _service.OpenUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// 프레젠터의 열기 전환 완료 대기
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 검증
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_OnClose_NotifiesTransitionCompleted()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestAnimationDelayPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_OnClose_DeactivatesGameObject()
		{
			// 준비
			var task = _service.OpenUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// 실행
			_service.CloseUi(typeof(TestAnimationDelayPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// 검증
			Assert.IsFalse(presenter.gameObject.activeSelf);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_AnimationComponent_IsAssigned()
		{
			// 실행
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// 검증
			Assert.IsNotNull(presenter.AnimationFeature.AnimationComponent);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_WithClip_UsesClipLength()
		{
			// 준비 - 테스트 애니메이션 클립 생성
			_mockLoader.RegisterPrefab<TestAnimationDelayWithClipPresenter>("animation_clip_presenter");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestAnimationDelayWithClipPresenter), "animation_clip_presenter", 0));

			// 실행
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayWithClipPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayWithClipPresenter;

			// 검증 - 클립 길이를 사용해야 함
			Assert.AreEqual(0.1f, presenter.AnimationFeature.OpenDelayInSeconds, 0.001f);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_ImplementsITransitionFeature()
		{
			// 실행
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// 검증
			Assert.IsTrue(presenter.AnimationFeature is ITransitionFeature);
			
			var transitionFeature = presenter.AnimationFeature as ITransitionFeature;
			Assert.IsNotNull(transitionFeature.OpenTransitionTask);
			Assert.IsNotNull(transitionFeature.CloseTransitionTask);
		}
	}
}
