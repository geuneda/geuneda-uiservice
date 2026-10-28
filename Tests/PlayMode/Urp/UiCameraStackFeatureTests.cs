using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Geuneda.UiService.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 피처만 떼어 낸 테스트. 프레젠터 GameObject 를 직접 만들고 UiPresenter 가 부르는 것과 같은 공개 진입점
	/// (OnPresenterInitialized, OnPresenterOpening)을 직접 호출해, 초기화 전에 기본 카메라를 주입한다.
	/// </summary>
	[TestFixture]
	public class UiCameraStackFeatureTests
	{
		private GameObject _presenterGo;
		private Canvas _canvas;
		private GameObject _overlayCameraGo;
		private Camera _overlayCamera;
		private GameObject _baseCameraGo;
		private Camera _baseCamera;
		private UiCameraStackFeature _feature;

		[SetUp]
		public void Setup()
		{
			_presenterGo = new GameObject(nameof(UiCameraStackFeatureTests));
			_canvas = _presenterGo.AddComponent<Canvas>();

			_overlayCameraGo = new GameObject("OverlayCamera");
			_overlayCamera = _overlayCameraGo.AddComponent<Camera>();

			_baseCameraGo = new GameObject("BaseCamera");
			_baseCamera = _baseCameraGo.AddComponent<Camera>();

			_feature = _presenterGo.AddComponent<UiCameraStackFeature>();
			_feature.ConfigureForTest(_overlayCamera, _canvas, () => _baseCamera);
		}

		[TearDown]
		public void TearDown()
		{
			if (_presenterGo != null) Object.DestroyImmediate(_presenterGo);
			if (_overlayCameraGo != null) Object.DestroyImmediate(_overlayCameraGo);
			if (_baseCameraGo != null) Object.DestroyImmediate(_baseCameraGo);
		}

		[UnityTest]
		public IEnumerator OnPresenterInitialized_SetsCanvasToScreenSpaceCameraWithOverlayCamera()
		{
			_feature.OnPresenterInitialized(null);

			Assert.AreEqual(RenderMode.ScreenSpaceCamera, _canvas.renderMode);
			Assert.AreSame(_overlayCamera, _canvas.worldCamera);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterInitialized_SetsOverlayCameraAsUrpOverlay_AndDisablesIt()
		{
			_feature.OnPresenterInitialized(null);

			var data = _overlayCamera.GetUniversalAdditionalCameraData();
			Assert.AreEqual(CameraRenderType.Overlay, data.renderType);
			Assert.IsFalse(data.renderPostProcessing);
			Assert.IsFalse(_overlayCamera.enabled, "스택에 들어가기 전에는 꺼져 있어야 한다. URP 는 스택 밖의 Overlay 카메라를 그리지 않는다");
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterOpening_InsertsOverlayCameraIntoBaseStack()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			Assert.IsTrue(baseData.cameraStack.Contains(_overlayCamera));
			Assert.IsTrue(_feature.IsStacked);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterOpening_CalledTwice_DoesNotDoubleInsert()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();
			_feature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			var count = 0;
			foreach (var cam in baseData.cameraStack)
			{
				if (cam == _overlayCamera) count++;
			}

			Assert.AreEqual(1, count, "다시 열어도 오버레이 카메라를 두 번 넣지 않는다");
			yield return null;
		}

		[UnityTest]
		public IEnumerator DisablingPresenter_RemovesOverlayCameraFromStack()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			_presenterGo.SetActive(false); // 피처를 포함한 모든 컴포넌트의 OnDisable

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			Assert.IsFalse(baseData.cameraStack.Contains(_overlayCamera));
			Assert.IsFalse(_feature.IsStacked);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Reopen_AfterDisable_ReInsertsIntoStack()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();
			_presenterGo.SetActive(false);
			_presenterGo.SetActive(true);
			_feature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			Assert.IsTrue(baseData.cameraStack.Contains(_overlayCamera));
			yield return null;
		}

		[UnityTest]
		public IEnumerator StackedCamera_IsEnabled_SoUrpActuallyRendersIt()
		{
			_feature.OnPresenterInitialized(null);

			Assert.IsFalse(_overlayCamera.enabled, "스택에 들어가기 전에는 꺼져 있어야 한다");

			_feature.OnPresenterOpening();

			// UniversalRenderPipeline.RenderCameraStack 은 isActiveAndEnabled 가 아닌 스택 카메라를 건너뛴다
			Assert.IsTrue(_overlayCamera.enabled, "스택에 넣은 오버레이 카메라는 켜져 있어야 URP 가 그린다");
			Assert.IsTrue(_overlayCamera.isActiveAndEnabled, "URP 는 isActiveAndEnabled 를 보므로 GameObject 도 켜져 있어야 한다");
			yield return null;
		}

		[UnityTest]
		public IEnumerator UnstackedCamera_IsDisabledAgain_OnClose()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();
			Assert.IsTrue(_overlayCamera.enabled);

			_presenterGo.SetActive(false);

			Assert.IsFalse(_overlayCamera.enabled, "스택에서 빠진 오버레이 카메라는 다시 꺼져야 한다");
			yield return null;
		}

		[UnityTest]
		public IEnumerator Open_WhenOverlayCameraIsChildOfItsOwnCanvas_DetachesCameraFromCanvas()
		{
			// 오버레이 카메라를 자기 캔버스 아래에 두는, 이 피처가 고쳐 주는 작성 실수
			_overlayCameraGo.transform.SetParent(_canvas.transform, false);
			_overlayCameraGo.transform.localPosition = new Vector3(1f, 2f, 3f);
			Assume.That(_overlayCamera.transform.IsChildOf(_canvas.transform), "전제: 오버레이 카메라가 자기 캔버스의 자식이어야 한다");

			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.IsFalse(_overlayCamera.transform.IsChildOf(_canvas.transform),
				"카메라를 캔버스 밖으로 꺼내야 한다. 아니면 Screen Space - Camera 캔버스가 카메라를 캔버스 평면으로 끌어가 아무것도 그리지 않는다");
			Assert.AreEqual(Vector3.zero, _overlayCamera.transform.localPosition, "꺼낸 뒤 자식일 때의 오프셋을 지워야 한다");
			yield return null;
		}

		[UnityTest]
		public IEnumerator MultipleStackedCameras_OrderByCanvasSortingOrder()
		{
			_canvas.sortingOrder = 20;
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			// 같은 기본 카메라에 올리는, 정렬값이 더 낮은 두 번째 프레젠터
			var secondGo = new GameObject("SecondPresenter");
			var secondCanvas = secondGo.AddComponent<Canvas>();
			secondCanvas.sortingOrder = 5;
			var secondCameraGo = new GameObject("SecondOverlayCamera");
			var secondCamera = secondCameraGo.AddComponent<Camera>();
			var secondFeature = secondGo.AddComponent<UiCameraStackFeature>();
			secondFeature.ConfigureForTest(secondCamera, secondCanvas, () => _baseCamera);

			secondFeature.OnPresenterInitialized(null);
			secondFeature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			var firstIndex = baseData.cameraStack.IndexOf(_overlayCamera);
			var secondIndex = baseData.cameraStack.IndexOf(secondCamera);

			Assert.Less(secondIndex, firstIndex, "정렬값이 낮은(5) 카메라가 높은(20) 카메라보다 먼저 와야 한다");

			Object.DestroyImmediate(secondGo);
			Object.DestroyImmediate(secondCameraGo);
			yield return null;
		}

		[UnityTest]
		public IEnumerator DestroyedBaseCamera_DoesNotThrow()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Object.DestroyImmediate(_baseCameraGo);
			_baseCameraGo = null;

			Assert.DoesNotThrow(() => _presenterGo.SetActive(false));
			yield return null;
		}

		[UnityTest]
		public IEnumerator NullBaseCamera_OnPresenterOpening_DoesNotThrow_AndDoesNotStack()
		{
			_feature.ConfigureForTest(_overlayCamera, _canvas, () => null);
			_feature.OnPresenterInitialized(null);

			Assert.DoesNotThrow(() => _feature.OnPresenterOpening());
			Assert.IsFalse(_feature.IsStacked);
			yield return null;
		}
	}

	/// <summary>
	/// 실제 UiService + MockAssetLoader 경로로 피처의 기본 기본 카메라 해석(Camera.main)을 확인한다.
	/// Main Camera 가 없다는 전제는 픽스처가 직접 만든다(SetUp 주석 참고).
	/// </summary>
	[TestFixture]
	public class UiCameraStackFeatureIntegrationTests
	{
		private const string Address = "camerastack_presenter";

		private readonly List<Camera> _disabledMainCameras = new List<Camera>();

		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestCameraStackPresenter>(Address);

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestCameraStackPresenter), Address, layer: 0)));

			// Camera.main 은 패키지가 아니라 열린 씬의 상태다. 배치 모드의 빈 씬에는 없고 Main Camera 가 있는 씬에서는 있으므로,
			// IsStacked 가 러너가 아니라 피처에 대한 단언이 되도록 여기서 없는 상태를 만든다
			foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
			{
				if (camera.enabled && camera.CompareTag("MainCamera"))
				{
					camera.enabled = false;
					_disabledMainCameras.Add(camera);
				}
			}

			Assume.That(Camera.main == null, Is.True, "전제: 피처의 기본 해석기가 기본 카메라를 찾지 못해야 한다");
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();

			foreach (var camera in _disabledMainCameras)
			{
				if (camera != null) camera.enabled = true;
			}
			_disabledMainCameras.Clear();
		}

		[UnityTest]
		public IEnumerator LoadThroughUiService_DoesNotThrow_WithNoMainCameraInScene()
		{
			var task = _service.OpenUiAsync(typeof(TestCameraStackPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestCameraStackPresenter;

			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.StackFeature);
			Assert.IsFalse(presenter.StackFeature.IsStacked, "기본 Camera.main 해석기가 null 을 돌려주므로 스택에 넣지 않고 끝나야 한다");
		}
	}
}
