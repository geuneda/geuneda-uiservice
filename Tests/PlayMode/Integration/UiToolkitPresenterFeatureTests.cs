using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// UiToolkitPresenterFeature 기능에 대한 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiToolkitPresenterFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiToolkitPresenter>("uitoolkit_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiToolkitPresenter), "uitoolkit_presenter", 0)
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
		public IEnumerator UiToolkitFeature_Document_IsAssigned()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			// Assert
			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.ToolkitFeature);
			Assert.IsNotNull(presenter.ToolkitFeature.Document);
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_Root_ReturnsRootVisualElement()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			// Assert - Root는 document의 rootVisualElement입니다
			// 참고: 패널이 할당되지 않은 경우 null일 수 있지만, 예외가 발생하면 안 됩니다
			Assert.IsNotNull(presenter);
			Assert.DoesNotThrow(() => { var _ = presenter.ToolkitFeature.Root; });
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_LifecycleHooks_AreCalled()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			// Assert - Feature 생명주기가 호출되었어야 합니다
			Assert.IsNotNull(presenter);
			Assert.IsTrue(presenter.WasOpened);
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_WithMultipleFeatures_AllFeaturesWork()
		{
			// Arrange - 여러 기능을 가진 프레젠터를 등록합니다
			_mockLoader.RegisterPrefab<TestMultiFeatureToolkitPresenter>("multi_feature");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestMultiFeatureToolkitPresenter), "multi_feature", 0));

			// Act
			var task = _service.OpenUiAsync(typeof(TestMultiFeatureToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestMultiFeatureToolkitPresenter;

			// Assert
			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.ToolkitFeature);
			Assert.IsNotNull(presenter.DelayFeature);
		}

	[UnityTest]
	public IEnumerator UiToolkitFeature_ListenerInvokedOnEachOpen()
	{
		// Arrange
		var task = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
		yield return task.ToCoroutine();
		var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

		// UI Toolkit 패널 연결을 기다립니다
		yield return TestHelpers.WaitForPanelAttachment(presenter);

		var initialCallbackCount = presenter.SetupCallbackCount;

		// Act - 닫고 다시 엽니다
		_service.CloseUi<TestUiToolkitPresenter>();
		yield return null; // 한 프레임 대기

		var reopenTask = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
		yield return reopenTask.ToCoroutine();

		// 다시 열린 후 패널 재연결을 기다립니다
		yield return TestHelpers.WaitForPanelAttachment(presenter);

		// Assert - 콜백이 다시 호출되었어야 합니다
		Assert.Greater(presenter.SetupCallbackCount, initialCallbackCount,
			"콜백은 열릴 때마다 호출되어야 합니다");
	}

	[UnityTest]
	public IEnumerator UiToolkitFeature_RemoveListener_StopsInvocation()
	{
		// Arrange
		var task = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
		yield return task.ToCoroutine();
		var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

		// UI Toolkit 패널 연결을 기다립니다
		yield return TestHelpers.WaitForPanelAttachment(presenter);

		// 리스너를 제거합니다
		presenter.RemoveSetupListener();
		var countAfterRemove = presenter.SetupCallbackCount;

		// Act - 닫고 다시 엽니다
		_service.CloseUi<TestUiToolkitPresenter>();
		yield return null;

		var reopenTask = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
		yield return reopenTask.ToCoroutine();

		// 다시 열린 후 패널 재연결을 기다립니다
		yield return TestHelpers.WaitForPanelAttachment(presenter);

		// Assert - 콜백 카운트가 변경되지 않았어야 합니다
		Assert.AreEqual(countAfterRemove, presenter.SetupCallbackCount,
			"제거 후에는 콜백이 호출되지 않아야 합니다");
	}
	}
}
