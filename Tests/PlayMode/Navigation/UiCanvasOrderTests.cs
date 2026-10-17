using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// uGUI 루트 Canvas 의 sortingOrder 를 '레이어 + 레이어 안의 연 순위'로 매기는지 테스트합니다.
	/// </summary>
	[TestFixture]
	public class UiCanvasOrderTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
		private GameObject _eventSystem;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			// 모달은 화면을 덮는 내용 없이 dim 만 레이캐스트에 걸리게 합니다
			_mockLoader.RegisterPrefab("popup", CreateRaycastPrefab<TestPopupPresenter>(false));
			_mockLoader.RegisterPrefab("hud", CreateRaycastPrefab<TestUiPresenter>(true));
			_mockLoader.RegisterPrefab("plain", CreateRaycastPrefab<TestPlainPresenter>(true));
			_mockLoader.RegisterPrefab("delay", CreateRaycastPrefab<TestTimeDelayPresenter>(true));
			_mockLoader.RegisterPrefab<TestPlainPresenterB>("tight");
			_mockLoader.RegisterPrefab<TestLayerUiPresenter>("next");

			// 레이어 10 은 19 까지, 레이어 20 은 21 까지 쓸 수 있습니다
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPopupPresenter), "popup", 10),
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "hud", 10),
				TestHelpers.CreateTestConfig(typeof(TestPlainPresenter), "plain", 10),
				TestHelpers.CreateTestConfig(typeof(TestTimeDelayPresenter), "delay", 10),
				TestHelpers.CreateTestConfig(typeof(TestPlainPresenterB), "tight", 20),
				TestHelpers.CreateTestConfig(typeof(TestLayerUiPresenter), "next", 22)));
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
		}

		[UnityTest]
		public IEnumerator SameLayerCanvases_LaterOpenedIsHitFirst_InEitherOrder()
		{
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPlainPresenter>().ToCoroutine();
			var hud = _service.GetUi<TestUiPresenter>();
			var plain = _service.GetUi<TestPlainPresenter>();
			yield return null;

			Assert.That(TopmostHitPresenter(), Is.SameAs(plain));

			// 닫았다 다시 열면 같은 레이어의 맨 위로 갑니다
			_service.CloseUi<TestUiPresenter>();
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();
			yield return null;

			Assert.That(TopmostHitPresenter(), Is.SameAs(hud));
			Assert.That(plain.GetComponent<Canvas>().sortingOrder, Is.EqualTo(10));
			Assert.That(hud.GetComponent<Canvas>().sortingOrder, Is.EqualTo(11));
		}

		[UnityTest]
		public IEnumerator CanvasReopenedDuringCloseTransition_IsHitFirst()
		{
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
			var firstTask = _service.OpenUiAsync(typeof(TestTimeDelayPresenter), "a");
			yield return firstTask.ToCoroutine();
			var first = firstTask.GetAwaiter().GetResult();
			var secondTask = _service.OpenUiAsync(typeof(TestTimeDelayPresenter), "b");
			yield return secondTask.ToCoroutine();
			var second = secondTask.GetAwaiter().GetResult();
			yield return second.OpenTransitionTask.ToCoroutine();
			yield return null;
			Assert.That(TopmostHitPresenter(), Is.SameAs(second));

			// 닫기 전환 중에 다시 열면 꺼진 적이 없어 Unity 순서로는 아래에 남지만, 나중에 연 것이므로 위에 그려져야 합니다
			_service.CloseUi(typeof(TestTimeDelayPresenter), "a");
			_service.OpenUiAsync(typeof(TestTimeDelayPresenter), "a").Forget();
			yield return null;

			Assert.That(first.gameObject.activeSelf, Is.True);
			Assert.That(TopmostHitPresenter(), Is.SameAs(first));
		}

		[UnityTest]
		public IEnumerator SameLayerModal_DimCoversEarlierCanvas_WithoutWarning()
		{
			_eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();
			yield return _service.OpenUiAsync<TestPopupPresenter>().ToCoroutine();
			var popup = _service.GetUi<TestPopupPresenter>();
			yield return null;

			// 나중에 연 모달의 캔버스가 위라 그 첫 자식인 dim 이 먼저 연 캔버스를 덮습니다
			var hits = RaycastScreenCenter();
			Assert.That(hits.Count, Is.GreaterThan(0));
			Assert.That(hits[0].gameObject.name, Is.EqualTo("UiDim"));
			Assert.That(hits[0].gameObject.transform.parent, Is.SameAs(popup.transform));
			// 레이어를 같이 써도 간격이 있으면 경고하지 않습니다
			LogAssert.NoUnexpectedReceived();
		}

		[UnityTest]
		public IEnumerator ClosingCanvas_StaysBelowNewlyOpened()
		{
			var presenters = new UiPresenter[3];
			var addresses = new[] { "a", "b", "c" };
			for (var i = 0; i < 2; i++)
			{
				var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter), addresses[i]);
				yield return task.ToCoroutine();
				presenters[i] = task.GetAwaiter().GetResult();
			}

			yield return presenters[1].OpenTransitionTask.ToCoroutine();

			// b 는 닫기 전환 중이라 아직 그려집니다
			_service.CloseUi(typeof(TestTimeDelayPresenter), "b");
			var lastTask = _service.OpenUiAsync(typeof(TestTimeDelayPresenter), "c");
			yield return lastTask.ToCoroutine();
			presenters[2] = lastTask.GetAwaiter().GetResult();

			Assert.That(presenters[1].gameObject.activeSelf, Is.True);
			Assert.That(presenters[0].GetComponent<Canvas>().sortingOrder, Is.EqualTo(10));
			Assert.That(presenters[1].GetComponent<Canvas>().sortingOrder, Is.EqualTo(11));
			Assert.That(presenters[2].GetComponent<Canvas>().sortingOrder, Is.EqualTo(12));
		}

		[UnityTest]
		public IEnumerator NotEnoughGap_EarlierCanvasesShareLayerValue_AndWarnsOnce()
		{
			var canvases = new List<Canvas>();
			LogAssert.Expect(LogType.Warning, new Regex(@"\[Geuneda\.UiService\] 레이어 20 에 uGUI 프레젠터 3 개"));
			foreach (var address in new[] { "a", "b", "c", "d" })
			{
				var task = _service.OpenUiAsync(typeof(TestPlainPresenterB), address);
				yield return task.ToCoroutine();
				canvases.Add(task.GetAwaiter().GetResult().GetComponent<Canvas>());
			}

			yield return _service.OpenUiAsync<TestLayerUiPresenter>().ToCoroutine();

			// 다음 레이어(22)를 넘지 않고, 나중에 연 캔버스가 계속 맨 위입니다
			Assert.That(canvases.ConvertAll(canvas => canvas.sortingOrder), Is.EqualTo(new[] { 20, 20, 20, 21 }));
			Assert.That(_service.GetUi<TestLayerUiPresenter>().GetComponent<Canvas>().sortingOrder, Is.EqualTo(22));
			LogAssert.NoUnexpectedReceived();
		}

		[UnityTest]
		public IEnumerator TopLayerCanvases_StayBelowInputBlocker()
		{
			var canvases = new List<Canvas>();
			LogAssert.Expect(LogType.Warning, new Regex("레이어 32765 에 uGUI 프레젠터 3 개"));
			foreach (var address in new[] { "a", "b", "c" })
			{
				// 설정 없이 넣은 레이어도 다음 설정 레이어가 없으면 차단막(32767) 바로 아래까지만 씁니다
				var presenter = TestHelpers.CreateTestPresenterPrefab<TestPlainPresenter>().GetComponent<TestPlainPresenter>();
				_service.AddUi(presenter, 32765, address, openAfter: true);
				canvases.Add(presenter.GetComponent<Canvas>());
			}

			yield return null;

			Assert.That(canvases.ConvertAll(canvas => canvas.sortingOrder), Is.EqualTo(new[] { 32765, 32765, 32766 }));
		}

		[Test]
		public void LayerAtInputBlocker_IsDrawnBelowIt_AndWarns()
		{
			LogAssert.Expect(LogType.Warning, new Regex("레이어 32767 는 uGUI 정렬값으로 쓸 수 있는 범위"));
			var presenter = TestHelpers.CreateTestPresenterPrefab<TestPlainPresenter>().GetComponent<TestPlainPresenter>();

			_service.AddUi(presenter, 32767, "edge", openAfter: true);

			Assert.That(presenter.GetComponent<Canvas>().sortingOrder, Is.EqualTo(32766));
		}

		[UnityTest]
		public IEnumerator ChildCanvas_KeepsItsOwnSortingOrder()
		{
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();
			var plainTask = _service.OpenUiAsync<TestPlainPresenter>();
			yield return plainTask.ToCoroutine();
			var plain = plainTask.GetAwaiter().GetResult();
			// overrideSorting 은 켜진 계층의 자식 캔버스에서만 적용되므로 연 뒤에 붙입니다
			var child = new GameObject("ChildCanvas", typeof(RectTransform)).AddComponent<Canvas>();
			child.transform.SetParent(plain.transform, false);
			child.overrideSorting = true;
			child.sortingOrder = 5;
			Assert.That(plain.GetComponent<Canvas>().sortingOrder, Is.EqualTo(11));

			// 다시 매겨도 루트만 바뀝니다
			_service.CloseUi<TestUiPresenter>();
			yield return _service.OpenUiAsync<TestUiPresenter>().ToCoroutine();

			Assert.That(plain.GetComponent<Canvas>().sortingOrder, Is.EqualTo(10));
			Assert.That(child.sortingOrder, Is.EqualTo(5));
		}

		// GraphicRaycaster 가 있는 오버레이 캔버스 프레젠터. fillScreen 이면 화면을 덮는 Image 를 자식으로 둡니다
		private static GameObject CreateRaycastPrefab<T>(bool fillScreen) where T : UiPresenter
		{
			// AddComponent 로 만든 Canvas 는 WorldSpace 라 오버레이로 바꿉니다
			var prefab = TestHelpers.CreateTestPresenterPrefab<T>();
			prefab.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			prefab.AddComponent<GraphicRaycaster>();

			if (!fillScreen)
			{
				return prefab;
			}

			var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
			var rect = (RectTransform)content.transform;
			rect.SetParent(prefab.transform, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			return prefab;
		}

		private static UiPresenter TopmostHitPresenter()
		{
			var hits = RaycastScreenCenter();
			Assert.That(hits.Count, Is.GreaterThan(0));
			return hits[0].gameObject.GetComponentInParent<UiPresenter>();
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
