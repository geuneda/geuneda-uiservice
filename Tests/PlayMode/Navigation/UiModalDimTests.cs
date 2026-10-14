using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 맨 위 모달 뒤에 까는 공용 dim 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiModalDimTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private PanelSettings _panelSettings;

		[SetUp]
		public void Setup()
		{
			_panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
			_panelSettings.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestPopupPresenter>("popup");
			_mockLoader.RegisterPrefab<TestPopupPresenterB>("popup_b");
			_mockLoader.RegisterPrefab<TestPopupPresenterC>("popup_c");
			_mockLoader.RegisterPrefab<TestUiPresenter>("hud");
			_mockLoader.RegisterPrefab("toolkit_popup", TestToolkitPopupPresenter.CreatePrefab(_panelSettings));
			_mockLoader.RegisterPrefab("toolkit_delay", TestToolkitPopupPresenter.CreatePrefab<TestToolkitDelayPopupPresenter>(_panelSettings));

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenter), "popup", 10),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterB), "popup_b", 20),
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenterC), "popup_c", 10),
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "hud", 30),
				TestHelpers.CreateTestConfig(typeof(TestToolkitPopupPresenter), "toolkit_popup", 10),
				TestHelpers.CreateTestConfig(typeof(TestToolkitDelayPopupPresenter), "toolkit_delay", 10)));
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
			Object.Destroy(_panelSettings.themeStyleSheet);
			Object.Destroy(_panelSettings);
		}

		[UnityTest]
		public IEnumerator CanvasModal_GetsDimAsFirstChild()
		{
			var popupTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return popupTask.ToCoroutine();
			var popup = popupTask.GetAwaiter().GetResult();

			var dim = popup.transform.GetChild(0);
			Assert.That(dim.name, Is.EqualTo("UiDim"));
			Assert.That(dim.gameObject.activeSelf, Is.True);
			Assert.That(dim.GetComponent<Image>().color, Is.EqualTo(_service.DimColor));
		}

		[UnityTest]
		public IEnumerator Dim_FollowsTopmostModal_AndHidesWhenNoneLeft()
		{
			var lowerTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return lowerTask.ToCoroutine();
			var lower = lowerTask.GetAwaiter().GetResult();
			var upperTask = _service.OpenUiAsync<TestPopupPresenterB>();
			yield return upperTask.ToCoroutine();
			var upper = upperTask.GetAwaiter().GetResult();

			var dim = upper.transform.GetChild(0);
			Assert.That(dim.name, Is.EqualTo("UiDim"));
			Assert.That(lower.transform.Find("UiDim"), Is.Null);

			_service.CloseUi<TestPopupPresenterB>();
			Assert.That(dim.parent, Is.SameAs(lower.transform));
			Assert.That(dim.GetSiblingIndex(), Is.EqualTo(0));

			_service.CloseUi<TestPopupPresenter>();
			Assert.That(dim.gameObject.activeSelf, Is.False);
			Assert.That(dim.parent, Is.SameAs(lower.transform.parent));
		}

		[UnityTest]
		public IEnumerator NonModalAbove_DoesNotTakeDim()
		{
			var popupTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return popupTask.ToCoroutine();
			var popup = popupTask.GetAwaiter().GetResult();
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();

			Assert.That(popup.transform.GetChild(0).name, Is.EqualTo("UiDim"));
		}

		[UnityTest]
		public IEnumerator NonModalPopup_GetsNoDim()
		{
			var loadTask = _service.LoadUiAsync<TestPopupPresenter>();
			yield return loadTask.ToCoroutine();
			var popup = loadTask.GetAwaiter().GetResult();
			popup.Modal = false;

			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();

			Assert.That(popup.transform.Find("UiDim"), Is.Null);
		}

		[UnityTest]
		public IEnumerator DimClick_NotifiesHost()
		{
			var popupTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return popupTask.ToCoroutine();
			var popup = popupTask.GetAwaiter().GetResult();

			popup.transform.GetChild(0).GetComponent<UiDimClickView>().OnPointerClick(null);

			Assert.That(popup.DimClickedCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator DimColor_UpdatesPlacedDim()
		{
			var popupTask = _service.OpenUiAsync<TestPopupPresenter>();
			yield return popupTask.ToCoroutine();
			var popup = popupTask.GetAwaiter().GetResult();

			_service.DimColor = Color.red;

			Assert.That(popup.transform.GetChild(0).GetComponent<Image>().color, Is.EqualTo(Color.red));
		}

		[UnityTest]
		public IEnumerator ToolkitModal_GetsDimDocumentJustBelow()
		{
			var popupTask = _service.OpenUiAsync<TestToolkitPopupPresenter>();
			yield return popupTask.ToCoroutine();
			var popup = popupTask.GetAwaiter().GetResult();

			var dim = popup.transform.parent.Find("UiDimDocument");
			Assert.That(dim, Is.Not.Null);
			Assert.That(dim.gameObject.activeSelf, Is.True);
			var document = dim.GetComponent<UIDocument>();
			Assert.That(document.panelSettings, Is.SameAs(_panelSettings));
			// 레이어 10 의 첫 문서는 10 + 1/1024, dim 은 그보다 반 단계 아래
			Assert.That(popup.GetComponent<UIDocument>().sortingOrder, Is.EqualTo(10f + 1f / 1024f));
			Assert.That(document.sortingOrder, Is.EqualTo(10f + 1f / 2048f));

			using (var click = ClickEvent.GetPooled())
			{
				click.target = document.rootVisualElement;
				document.rootVisualElement.SendEvent(click);
			}

			Assert.That(popup.DimClickedCount, Is.EqualTo(1));

			_service.CloseUi<TestToolkitPopupPresenter>();
			Assert.That(dim.gameObject.activeSelf, Is.False);
		}

		[UnityTest]
		public IEnumerator ToolkitModalsOnSameLayer_DimSitsBetweenThemInOpenOrder()
		{
			var firstTask = _service.OpenUiAsync(typeof(TestToolkitPopupPresenter), "a");
			yield return firstTask.ToCoroutine();
			var first = firstTask.GetAwaiter().GetResult().GetComponent<UIDocument>();
			var secondTask = _service.OpenUiAsync(typeof(TestToolkitPopupPresenter), "b");
			yield return secondTask.ToCoroutine();
			var second = secondTask.GetAwaiter().GetResult().GetComponent<UIDocument>();
			var dim = first.transform.parent.Find("UiDimDocument").GetComponent<UIDocument>();

			Assert.That(first.sortingOrder, Is.LessThan(dim.sortingOrder));
			Assert.That(dim.sortingOrder, Is.LessThan(second.sortingOrder));
			Assert.That(second.sortingOrder, Is.LessThan(11f));

			_service.CloseUi(typeof(TestToolkitPopupPresenter), "b");

			Assert.That(dim.sortingOrder, Is.LessThan(first.sortingOrder));
			Assert.That(dim.gameObject.activeSelf, Is.True);
		}

		[UnityTest]
		public IEnumerator ClosingToolkitDocuments_StayBelowNewlyOpenedModal()
		{
			var documents = new UIDocument[4];
			var addresses = new[] { "a", "b", "c", "d" };
			for (var i = 0; i < 3; i++)
			{
				var task = _service.OpenUiAsync(typeof(TestToolkitDelayPopupPresenter), addresses[i]);
				yield return task.ToCoroutine();
				documents[i] = task.GetAwaiter().GetResult().GetComponent<UIDocument>();
			}

			// c 와 b 는 닫기 전환 중이라 아직 그려집니다
			_service.CloseUi(typeof(TestToolkitDelayPopupPresenter), "c");
			_service.CloseUi(typeof(TestToolkitDelayPopupPresenter), "b");
			var lastTask = _service.OpenUiAsync(typeof(TestToolkitDelayPopupPresenter), "d");
			yield return lastTask.ToCoroutine();
			documents[3] = lastTask.GetAwaiter().GetResult().GetComponent<UIDocument>();
			var dim = documents[0].transform.parent.Find("UiDimDocument").GetComponent<UIDocument>();

			Assert.That(documents[2].gameObject.activeSelf, Is.True);
			Assert.That(documents[0].sortingOrder, Is.LessThan(documents[1].sortingOrder));
			Assert.That(documents[1].sortingOrder, Is.LessThan(documents[2].sortingOrder));
			Assert.That(documents[2].sortingOrder, Is.LessThan(dim.sortingOrder));
			Assert.That(dim.sortingOrder, Is.LessThan(documents[3].sortingOrder));
		}

		[UnityTest]
		public IEnumerator CanvasModalSharingLayer_WarnsOncePerType()
		{
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			LogAssert.Expect(LogType.Warning, new Regex("같은 레이어"));

			yield return _service.OpenUiAsync<TestPopupPresenterC>().ToCoroutine();
			_service.CloseUi<TestPopupPresenterC>();
			yield return _service.OpenUiAsync<TestPopupPresenterC>().ToCoroutine();

			LogAssert.NoUnexpectedReceived();
		}

		[Test]
		public void ModalWithoutCanvasOrDocument_LogsWarning()
		{
			var go = new GameObject("NoRootPopup");
			var popup = go.AddComponent<TestPopupPresenter>();
			LogAssert.Expect(LogType.Warning, new Regex("dim 을 깔지 않습니다"));

			_service.AddUi(popup, 10, openAfter: true);

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			_service.UnloadUi(popup);
		}
	}
}
