using Geuneda.UiService.Rendering;
using UnityEngine;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// Canvas, UiCameraStackFeature, 자식 오버레이 카메라를 묶은 테스트 프레젠터.
	/// Camera.main 은 테스트 씬에 없으므로, UiService 로 로드하기 전에 <see cref="ConfigureBaseCamera"/> 로 기본 카메라를 고정한다.
	/// </summary>
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(UiCameraStackFeature))]
	public class TestCameraStackPresenter : UiPresenter
	{
		private const string OverlayCameraChildName = "OverlayCamera";

		public Canvas Canvas { get; private set; }
		public Camera OverlayCamera { get; private set; }
		public UiCameraStackFeature StackFeature { get; private set; }

		private void Awake()
		{
			// MockAssetLoader.RegisterPrefab<T>() 가 템플릿을 만들 때 한 번, Instantiate 할 때마다 다시 Awake 가 불린다.
			// Instantiate 는 자식 GameObject 를 복사하므로(C# 프로퍼티는 복사되지 않음) 이름으로 찾아 두 번 만들지 않는다
			Canvas = GetComponent<Canvas>();
			if (Canvas == null)
			{
				Canvas = gameObject.AddComponent<Canvas>();
			}

			var existingCameraChild = transform.Find(OverlayCameraChildName);
			if (existingCameraChild != null)
			{
				OverlayCamera = existingCameraChild.GetComponent<Camera>();
			}
			else
			{
				var cameraGo = new GameObject(OverlayCameraChildName);
				cameraGo.transform.SetParent(transform, false);
				OverlayCamera = cameraGo.AddComponent<Camera>();
			}

			StackFeature = GetComponent<UiCameraStackFeature>();
			if (StackFeature == null)
			{
				StackFeature = gameObject.AddComponent<UiCameraStackFeature>();
			}

			StackFeature.ConfigureForTest(OverlayCamera, Canvas);
		}

		/// <summary>
		/// UiService 로 로드하거나 열기 전에 호출해야 한다
		/// </summary>
		public void ConfigureBaseCamera(Camera baseCamera, bool useCanvasSortingOrder = true, int stackPriority = 0)
		{
			StackFeature.ConfigureForTest(OverlayCamera, Canvas, () => baseCamera, useCanvasSortingOrder, stackPriority);
		}
	}
}
