using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// BlockInput 토큰과 uGUI·UI Toolkit 차단막 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiInputBlockerTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private PanelSettings _panelSettings;
		private GameObject _eventSystem;

		[SetUp]
		public void Setup()
		{
			_panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
			_panelSettings.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("hud");
			_mockLoader.RegisterPrefab("toolkit_popup", TestToolkitPopupPresenter.CreatePrefab(_panelSettings));

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "hud", 0),
				TestHelpers.CreateTestConfig(typeof(TestToolkitPopupPresenter), "toolkit_popup", 10)));
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();

			if (_eventSystem != null)
			{
				Object.Destroy(_eventSystem);
			}

			Object.Destroy(_panelSettings.themeStyleSheet);
			Object.Destroy(_panelSettings);
		}

		[Test]
		public void BlockInput_CountsTokensUntilAllReleased()
		{
			var first = _service.BlockInput("loading");
			var second = _service.BlockInput("purchase");

			Assert.That(_service.IsInputBlocked, Is.True);
			Assert.That(_service.InputBlockReasons, Is.EqualTo(new[] { "loading", "purchase" }));

			first.Dispose();
			Assert.That(_service.IsInputBlocked, Is.True);
			Assert.That(_service.InputBlockReasons, Is.EqualTo(new[] { "purchase" }));

			second.Dispose();
			Assert.That(_service.IsInputBlocked, Is.False);
			Assert.That(_service.InputBlockReasons, Is.Empty);
		}

		[Test]
		public void BlockInput_DoubleDispose_WarnsAndKeepsOtherBlocks()
		{
			var first = _service.BlockInput("first");
			_service.BlockInput("second");
			first.Dispose();
			LogAssert.Expect(LogType.Warning, new Regex("이미 풀린 입력 차단 토큰"));

			first.Dispose();

			Assert.That(_service.IsInputBlocked, Is.True);
		}

		[Test]
		public void DefaultToken_Dispose_DoesNothing()
		{
			_service.BlockInput("kept");

			default(UiInputBlock).Dispose();

			Assert.That(_service.IsInputBlocked, Is.True);
		}

		[Test]
		public void BlockInput_NullReason_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => _service.BlockInput(null));
		}

		[UnityTest]
		public IEnumerator CanvasBlocker_TakesRaycastsOnlyWhileBlocked()
		{
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			var loadTask = _service.LoadUiAsync<TestUiPresenter>();
			yield return loadTask.ToCoroutine();
			var blocker = loadTask.GetAwaiter().GetResult().transform.parent.Find("UiInputBlocker");

			Assert.That(blocker, Is.Not.Null);
			Assert.That(blocker.gameObject.activeSelf, Is.False);
			Assert.That(blocker.GetComponent<Canvas>().sortingOrder, Is.EqualTo(32767));

			var block = _service.BlockInput("test");
			yield return null;

			var hits = RaycastScreenCenter();
			Assert.That(hits.Count, Is.GreaterThan(0));
			Assert.That(hits[0].gameObject, Is.SameAs(blocker.gameObject));

			block.Dispose();
			yield return null;

			Assert.That(blocker.gameObject.activeSelf, Is.False);
			Assert.That(RaycastScreenCenter().Exists(hit => hit.gameObject == blocker.gameObject), Is.False);
		}

		[UnityTest]
		public IEnumerator ToolkitBlocker_IsCreatedForUiDocumentPresenters_AndFollowsBlockState()
		{
			var loadTask = _service.LoadUiAsync<TestToolkitPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var panel = loadTask.GetAwaiter().GetResult().transform.parent.Find("UiInputBlockerPanel");

			Assert.That(panel, Is.Not.Null);
			var document = panel.GetComponent<UIDocument>();
			Assert.That(document.panelSettings.sortingOrder, Is.EqualTo(32767f));
			Assert.That(document.rootVisualElement.style.display.value, Is.EqualTo(DisplayStyle.None));

			var block = _service.BlockInput("test");
			Assert.That(document.rootVisualElement.style.display.value, Is.EqualTo(DisplayStyle.Flex));
			Assert.That(document.rootVisualElement.pickingMode, Is.EqualTo(PickingMode.Position));

			block.Dispose();
			Assert.That(document.rootVisualElement.style.display.value, Is.EqualTo(DisplayStyle.None));
		}

		[UnityTest]
		public IEnumerator ToolkitBlocker_IsNotCreatedForCanvasPresenters()
		{
			var loadTask = _service.LoadUiAsync<TestUiPresenter>();
			yield return loadTask.ToCoroutine();

			Assert.That(loadTask.GetAwaiter().GetResult().transform.parent.Find("UiInputBlockerPanel"), Is.Null);
		}

		[UnityTest]
		public IEnumerator BlockInput_ClearsSelectionAndNavigation_ThenRestoresNavigationOnly()
		{
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			var button = new GameObject("Button", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
			yield return null;
			var eventSystem = _eventSystem.GetComponent<EventSystem>();
			eventSystem.SetSelectedGameObject(button);

			var block = _service.BlockInput("purchase");

			Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
			Assert.That(eventSystem.sendNavigationEvents, Is.False);

			block.Dispose();

			// 선택은 복구하지 않습니다(복구 콜백이 다시 막으면 상태가 엇갈림)
			Assert.That(eventSystem.sendNavigationEvents, Is.True);
			Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
			Object.Destroy(button);
		}

		[UnityTest]
		public IEnumerator BlockInput_StopsToolkitSubmitOnFocusedElement()
		{
			var openTask = _service.OpenUiAsync<TestToolkitPopupPresenter>();
			yield return openTask.ToCoroutine();
			var document = openTask.GetAwaiter().GetResult().GetComponent<UIDocument>();
			yield return TestHelpers.WaitForPanelAttachment(document);
			var clicks = 0;
			var button = new Button(() => clicks++) { focusable = true };
			document.rootVisualElement.Add(button);
			button.Focus();

			var block = _service.BlockInput("purchase");
			Assert.That(document.rootVisualElement.panel.focusController.focusedElement, Is.Null);
			button.Focus();
			Submit(button);
			Assert.That(clicks, Is.EqualTo(0));

			block.Dispose();
			Submit(button);
			Assert.That(clicks, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator BlockInputFromDeselectCallback_KeepsDistinctTokens()
		{
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			var target = new GameObject("Target", typeof(TestSelectionCallback));
			yield return null;
			var nested = default(UiInputBlock);
			target.GetComponent<TestSelectionCallback>().Deselected = () => nested = _service.BlockInput("nested");
			_eventSystem.GetComponent<EventSystem>().SetSelectedGameObject(target);

			var outer = _service.BlockInput("outer");
			outer.Dispose();
			nested.Dispose();

			Assert.That(_service.IsInputBlocked, Is.False);
			Assert.That(_service.InputBlockReasons, Is.Empty);
			Object.Destroy(target);
		}

		[UnityTest]
		public IEnumerator BlockInputFromSelectCallback_ReleasesSelectionNextFrame()
		{
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			var target = new GameObject("Target", typeof(TestSelectionCallback));
			yield return null;
			var eventSystem = _eventSystem.GetComponent<EventSystem>();
			var block = default(UiInputBlock);
			target.GetComponent<TestSelectionCallback>().Selected = () => block = _service.BlockInput("select");

			// 선택 콜백 도중에는 선택을 풀 수 없으므로 선택된 채 남습니다
			eventSystem.SetSelectedGameObject(target);
			Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(target));

			yield return null;
			yield return null;

			Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
			block.Dispose();
			Object.Destroy(target);
		}

		[UnityTest]
		public IEnumerator TokenReleasedInDeselectDuringReapply_LeavesToolkitUnblocked()
		{
			var openTask = _service.OpenUiAsync<TestToolkitPopupPresenter>();
			yield return openTask.ToCoroutine();
			var document = openTask.GetAwaiter().GetResult().GetComponent<UIDocument>();
			yield return TestHelpers.WaitForPanelAttachment(document);
			var clicks = 0;
			var button = new Button(() => clicks++) { focusable = true };
			document.rootVisualElement.Add(button);
			var block = _service.BlockInput("loading");

			// 막힌 뒤 생긴 EventSystem 의 선택이 풀릴 때 마지막 토큰을 풉니다
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			var target = new GameObject("Target", typeof(TestSelectionCallback));
			target.GetComponent<TestSelectionCallback>().Deselected = () => block.Dispose();
			_eventSystem.GetComponent<EventSystem>().SetSelectedGameObject(target);
			yield return null;
			yield return null;

			Assert.That(_service.IsInputBlocked, Is.False);
			button.Focus();
			Submit(button);
			Assert.That(clicks, Is.EqualTo(1));
			Object.Destroy(target);
		}

		[UnityTest]
		public IEnumerator PanelOpenedWhileBlocked_IsBlockedToo()
		{
			var block = _service.BlockInput("loading");
			var openTask = _service.OpenUiAsync<TestToolkitPopupPresenter>();
			yield return openTask.ToCoroutine();
			var document = openTask.GetAwaiter().GetResult().GetComponent<UIDocument>();
			yield return TestHelpers.WaitForPanelAttachment(document);
			var clicks = 0;
			var button = new Button(() => clicks++) { focusable = true };
			document.rootVisualElement.Add(button);
			button.Focus();

			Submit(button);
			Assert.That(clicks, Is.EqualTo(0));

			block.Dispose();
			Submit(button);
			Assert.That(clicks, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator EventSystemCreatedWhileBlocked_IsBlockedToo()
		{
			var block = _service.BlockInput("scene-load");
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			yield return null;
			yield return null;
			var eventSystem = _eventSystem.GetComponent<EventSystem>();

			Assert.That(eventSystem.sendNavigationEvents, Is.False);

			block.Dispose();
			Assert.That(eventSystem.sendNavigationEvents, Is.True);
		}

		private static void Submit(VisualElement target)
		{
			using (var submit = NavigationSubmitEvent.GetPooled())
			{
				submit.target = target;
				target.SendEvent(submit);
			}
		}

		private static List<RaycastResult> RaycastScreenCenter()
		{
			var data = new PointerEventData(EventSystem.current)
			{
				position = new Vector2(Screen.width / 2f, Screen.height / 2f)
			};
			var results = new List<RaycastResult>();
			EventSystem.current.RaycastAll(data, results);
			return results;
		}
	}
}
