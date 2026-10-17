using System;
using Geuneda.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 표시 중인 모달 중 맨 위 하나의 바로 뒤에 까는 공용 dim 입니다. dim 은 그 아래 UI 의 입력도 막습니다.
	/// uGUI 모달은 dim 을 프레젠터 캔버스의 첫 자식으로 넣고,
	/// UI Toolkit 모달은 같은 PanelSettings 에 모달 문서보다 반 단계 낮은 정렬값의 dim 문서를 둡니다.
	/// </summary>
	internal sealed class UiModalDim
	{
		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.ModalDim");

		/// <summary>
		/// 같은 레이어 안에서 UI Toolkit 문서에 매기는 정렬값 단계입니다. 2 의 거듭제곱 분수라 레이어 1000 근처에서도 정확합니다.
		/// </summary>
		internal const float ToolkitOrderStep = 1f / 1024f;

		private readonly Action _clicked;
		private readonly EventCallback<ClickEvent> _onToolkitClick;
		private Transform _parent;
		private Color _color = new Color(0f, 0f, 0f, 0.6f);
		private UiPresenter _host;
		private GameObject _canvasDim;
		private Image _canvasImage;
		private GameObject _toolkitObject;
		private UIDocument _toolkitDocument;

		/// <param name="clicked">dim 을 눌렀을 때 부를 함수</param>
		public UiModalDim(Action clicked)
		{
			_clicked = clicked;
			_onToolkitClick = _ => _clicked();
		}

		/// <summary>
		/// dim 이 깔린 프레젠터입니다. 없으면 null 입니다.
		/// </summary>
		public UiPresenter Host => _host;

		public Color Color
		{
			get => _color;
			set
			{
				_color = value;

				if (_canvasImage != null)
				{
					_canvasImage.color = value;
				}

				if (_toolkitDocument != null && _toolkitObject.activeSelf)
				{
					_toolkitDocument.rootVisualElement.style.backgroundColor = value;
				}
			}
		}

		/// <summary>
		/// 쉬는 동안 dim 을 둘 부모를 정합니다. 서비스 Init 에서 한 번 부릅니다.
		/// </summary>
		public void Attach(Transform parent)
		{
			_parent = parent;
		}

		/// <summary>
		/// dim 을 <paramref name="host"/> 바로 뒤로 옮깁니다. null 이면 dim 을 숨깁니다.
		/// </summary>
		public void Show(UiPresenter host)
		{
			if (_parent == null)
			{
				return;
			}

			// 같은 프레젠터에 이미 깔려 있으면 그대로 둡니다. 프레젠터와 함께 파괴됐으면 다시 만듭니다.
			// 파괴된 프레젠터는 Unity 의 == 에서 null 과 같으므로 참조로 비교합니다
			if (ReferenceEquals(host, _host) && IsPlaced() == (host != null))
			{
				// 같은 레이어의 다른 문서가 열리고 닫히면 모달 문서의 정렬값이 바뀌므로 따라갑니다
				if (_toolkitObject != null && _toolkitObject.activeSelf && host.TryGetComponent<UIDocument>(out var current))
				{
					_toolkitDocument.sortingOrder = current.sortingOrder - ToolkitOrderStep / 2f;
				}

				return;
			}

			Hide();
			_host = host;

			if (host == null)
			{
				return;
			}

			if (host.TryGetComponent<Canvas>(out _))
			{
				PlaceOnCanvas(host);
			}
			else if (host.TryGetComponent<UIDocument>(out var document) && document.panelSettings != null)
			{
				PlaceOnToolkit(document);
			}
			else
			{
				Logger.Warning($"모달 프레젠터 {host.GetType().Name} 의 루트에 Canvas 나 PanelSettings 가 지정된 UIDocument 가 없어 dim 을 깔지 않습니다.");
			}
		}

		/// <summary>
		/// 만든 dim 오브젝트를 파괴합니다.
		/// </summary>
		public void Destroy()
		{
			if (_canvasDim != null)
			{
				Object.Destroy(_canvasDim);
			}

			if (_toolkitObject != null)
			{
				Object.Destroy(_toolkitObject);
			}

			_canvasDim = null;
			_canvasImage = null;
			_toolkitObject = null;
			_toolkitDocument = null;
			_host = null;
			_parent = null;
		}

		private bool IsPlaced()
		{
			if (_canvasDim != null && _canvasDim.activeSelf)
			{
				return true;
			}

			return _toolkitObject != null && _toolkitObject.activeSelf;
		}

		private void Hide()
		{
			if (_canvasDim != null)
			{
				_canvasDim.SetActive(false);
				_canvasDim.transform.SetParent(_parent, false);
			}

			if (_toolkitObject != null)
			{
				_toolkitObject.SetActive(false);
			}
		}

		private void PlaceOnCanvas(UiPresenter host)
		{
			if (_canvasDim == null)
			{
				_canvasDim = new GameObject("UiDim", typeof(RectTransform));
				_canvasImage = _canvasDim.AddComponent<Image>();
				_canvasImage.color = _color;
				_canvasDim.AddComponent<UiDimClickView>().Clicked = _clicked;
			}

			var rect = (RectTransform)_canvasDim.transform;
			rect.SetParent(host.transform, false);
			rect.SetAsFirstSibling();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			_canvasDim.layer = host.gameObject.layer;
			_canvasDim.SetActive(true);
		}

		private void PlaceOnToolkit(UIDocument hostDocument)
		{
			if (_toolkitObject == null)
			{
				_toolkitObject = new GameObject("UiDimDocument");
				_toolkitObject.SetActive(false);
				_toolkitObject.transform.SetParent(_parent, false);
				_toolkitDocument = _toolkitObject.AddComponent<UIDocument>();
			}

			// 같은 패널 안에서 모달 문서 바로 아래, 같은 레이어의 먼저 연 문서보다는 위에 그려지도록 반 단계 낮춥니다
			_toolkitDocument.panelSettings = hostDocument.panelSettings;
			_toolkitDocument.sortingOrder = hostDocument.sortingOrder - ToolkitOrderStep / 2f;
			_toolkitObject.SetActive(true);

			// UIDocument 는 켜질 때마다 루트를 새로 만들므로 스타일과 콜백을 매번 다시 겁니다
			var root = _toolkitDocument.rootVisualElement;
			UiStyleUtils.FillParent(root);
			root.style.backgroundColor = _color;
			root.pickingMode = PickingMode.Position;
			root.RegisterCallback(_onToolkitClick);
		}
	}
}
