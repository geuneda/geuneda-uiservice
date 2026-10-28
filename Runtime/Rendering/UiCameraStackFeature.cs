using System;
using System.Collections.Generic;
using Geuneda.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Geuneda.UiService.Rendering
{
	/// <summary>
	/// 프레젠터의 <see cref="Canvas"/> 를 Screen Space - Camera 로 바꾸고 오버레이 카메라를 기본 카메라의 URP 카메라 스택에 넣어,
	/// UI 가 항상 맨 위에 합성되는 대신 3D 콘텐츠와 올바른 순서로 겹치게 합니다.
	/// </summary>
	/// <remarks>
	/// 이 피처를 붙이면 캔버스의 렌더 모드가 바뀝니다. Screen Space - Overlay 와 Screen Space - Camera 는 같은 정렬 순서를 공유하지 않습니다.
	/// Overlay 는 URP 렌더링이 모두 끝난 뒤 합성되므로, 스택에 넣은 프레젠터는 <c>UiConfig.Layer</c> 가 아무리 높아도
	/// 레이어 0 의 Overlay 프레젠터 아래에 그려집니다. 두 그룹을 화면에서 분리하거나 고정된 상대 순서를 받아들이세요.
	/// URP 의 카메라 스택은 씬의 기본 카메라 인스펙터 목록이라 프리팹으로 만든 프레젠터를 미리 넣을 수 없으므로, 열 때 넣고 비활성화될 때 뺍니다.
	/// </remarks>
	public class UiCameraStackFeature : PresenterFeatureBase
	{
		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.Rendering");

		// URP 의 cameraStack 은 우선순위가 없는 List<Camera> 라 이 피처가 넣은 카메라의 우선순위를 따로 기억한다.
		// 직렬화한 카메라가 프레젠터의 자식이 아닐 수 있어 계층이 아니라 Camera 로 키를 잡는다(아니면 바깥에서 넣은 카메라가 우선순위 0 으로 정렬됨)
		private static readonly Dictionary<Camera, int> StackPriorities = new Dictionary<Camera, int>();

		[SerializeField] private Camera _overlayCamera;
		[SerializeField] private Canvas _canvas;
		[SerializeField] private float _planeDistance = 100f;
		[SerializeField] private bool _useCanvasSortingOrder = true;
		[SerializeField] private int _stackPriority;

		private Func<Camera> _baseCameraResolver = ResolveMainCamera;
		private bool _isStacked;
		private bool _ownsDetachedCamera;

		/// <summary>
		/// 이 프레젠터의 오버레이 카메라가 기본 카메라의 스택에 들어가 있는 동안 true
		/// </summary>
		public bool IsStacked => _isStacked;

		/// <summary>
		/// 스택 안에서 이 프레젠터의 오버레이 카메라를 정렬하는 우선순위. 낮을수록 먼저(아래에) 그립니다.
		/// 기본은 캔버스의 <see cref="Canvas.sortingOrder"/> 이고, 인스펙터에서 끄면 직렬화한 값을 씁니다
		/// </summary>
		public int StackPriority => _useCanvasSortingOrder && _canvas != null ? _canvas.sortingOrder : _stackPriority;

		/// <summary>
		/// 스택에 넣을 기본 카메라를 찾는 함수. 프레젠터를 열기 전에 정합니다. null 을 넣으면 <see cref="Camera.main"/> 으로 돌아갑니다
		/// </summary>
		public Func<Camera> BaseCameraResolver
		{
			get => _baseCameraResolver;
			set => _baseCameraResolver = value ?? ResolveMainCamera;
		}

		/// <summary>
		/// 오름차순으로 정렬된 <paramref name="existingPriorities"/> 에서 <paramref name="priority"/> 를 넣을 위치를 돌려줍니다.
		/// 같은 값은 기존 항목 뒤에 둡니다
		/// </summary>
		public static int InsertIndex(IReadOnlyList<int> existingPriorities, int priority)
		{
			var index = 0;

			for (; index < existingPriorities.Count; index++)
			{
				if (existingPriorities[index] > priority)
				{
					break;
				}
			}

			return index;
		}

		private void OnDisable()
		{
			RemoveFromStack();
		}

		private void OnDestroy()
		{
			// 캔버스에서 떼어 낸 카메라는 프레젠터 계층 밖에 있어 프레젠터와 함께 파괴되지 않는다
			if (_ownsDetachedCamera && _overlayCamera != null)
			{
				Destroy(_overlayCamera.gameObject);
			}
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);

			if (_overlayCamera == null || _canvas == null)
			{
				Logger.Error($"{nameof(UiCameraStackFeature)} on '{name}' requires both an overlay Camera and a Canvas to be assigned.", this);
				return;
			}

			DetachCameraFromCanvas();

			_canvas.renderMode = RenderMode.ScreenSpaceCamera;
			_canvas.worldCamera = _overlayCamera;
			_canvas.planeDistance = _planeDistance;

			var overlayData = _overlayCamera.GetUniversalAdditionalCameraData();
			overlayData.renderType = CameraRenderType.Overlay;
			overlayData.renderPostProcessing = false;

			// 스택에 들어가기 전까지는 끈다. URP 는 Overlay 카메라를 기본 카메라 스택의 일부로만 그리므로
			// 스택 밖에서 켜 두면 아무것도 그리지 않으면서 비용만 든다. clearDepth 는 setter 가 없고 기본값이 맞다
			_overlayCamera.enabled = false;
		}

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();
			InsertIntoStack();
		}

		/// <summary>
		/// 프리팹 없이 스택 로직을 테스트할 수 있도록 직렬화 필드를 코드로 정합니다
		/// </summary>
		internal void ConfigureForTest(Camera overlayCamera, Canvas canvas, Func<Camera> baseCameraResolver = null,
			bool useCanvasSortingOrder = true, int stackPriority = 0)
		{
			_overlayCamera = overlayCamera;
			_canvas = canvas;
			_useCanvasSortingOrder = useCanvasSortingOrder;
			_stackPriority = stackPriority;
			_baseCameraResolver = baseCameraResolver ?? ResolveMainCamera;
		}

		// Camera.main 은 태그 조회를 캐시하고 열고 닫을 때 두 번만 읽으므로 따로 캐시하지 않는다
		private static Camera ResolveMainCamera() => Camera.main;

		// 도메인 리로드를 끄면 정적 상태가 플레이 세션 사이에 남는다
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			StackPriorities.Clear();
		}

		// Screen Space - Camera 캔버스는 worldCamera 에 맞춰 자기 트랜스폼을 움직인다. 카메라가 캔버스 아래에 있으면
		// 그 위치와 크기를 물려받아 캔버스 평면 위에 놓이고 아무것도 보지 못해 UI 가 빈 화면으로 나온다.
		// 프리팹 자식으로 카메라를 두는 것이 자연스러운 작성법이라 거부하지 않고 고쳐 준다
		private void DetachCameraFromCanvas()
		{
			if (!_overlayCamera.transform.IsChildOf(_canvas.transform))
			{
				return;
			}

			_overlayCamera.transform.SetParent(_canvas.transform.parent, false);
			_overlayCamera.transform.localPosition = Vector3.zero;
			_overlayCamera.transform.localRotation = Quaternion.identity;
			_overlayCamera.transform.localScale = Vector3.one;
			_ownsDetachedCamera = true;
		}

		private void InsertIntoStack()
		{
			if (_isStacked || _overlayCamera == null || _canvas == null)
			{
				return;
			}

			var baseCamera = _baseCameraResolver?.Invoke();
			if (baseCamera == null || baseCamera == _overlayCamera)
			{
				return;
			}

			var stack = baseCamera.GetUniversalAdditionalCameraData().cameraStack;
			if (stack.Contains(_overlayCamera))
			{
				_isStacked = true;
				return;
			}

			// 이 피처가 아닌 곳에서 넣은 카메라는 우선순위 0 으로 본다
			var priorities = new List<int>(stack.Count);
			foreach (var camera in stack)
			{
				priorities.Add(camera != null && StackPriorities.TryGetValue(camera, out var known) ? known : 0);
			}

			var priority = StackPriority;
			stack.Insert(InsertIndex(priorities, priority), _overlayCamera);
			StackPriorities[_overlayCamera] = priority;

			// 꼭 필요하다. UniversalRenderPipeline.RenderCameraStack 은 isActiveAndEnabled 가 아닌 스택 카메라를 건너뛰므로
			// 꺼진 채로 스택에만 넣으면 아무것도 그려지지 않는다
			_overlayCamera.enabled = true;
			_isStacked = true;
		}

		private void RemoveFromStack()
		{
			if (!_isStacked || _overlayCamera == null)
			{
				_isStacked = false;
				return;
			}

			var baseCamera = _baseCameraResolver?.Invoke();
			if (baseCamera != null)
			{
				baseCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(_overlayCamera);
			}

			_overlayCamera.enabled = false;
			StackPriorities.Remove(_overlayCamera);
			_isStacked = false;
		}
	}
}
