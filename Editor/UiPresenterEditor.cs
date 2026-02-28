using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Geuneda.UiService;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.UiService
{
	/// <summary>
	/// 인스펙터에서 빠른 액션을 표시하기 위한 UiPresenter 커스텀 에디터입니다
	/// </summary>
	[CustomEditor(typeof(UiPresenter), true)]
	public class UiPresenterEditor : Editor
	{
		private Label _statusLabel;
		private VisualElement _statusIndicator;
		private VisualElement _controlsContainer;

		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			
			// 기본 인스펙터를 그립니다
			InspectorElement.FillDefaultInspector(root, serializedObject, this);
			
			// 간격 추가
			root.Add(CreateSpacer(10));
			
			// 컨트롤 섹션 헤더
			var header = new Label("UI 프레젠터 컨트롤");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.fontSize = 12;
			root.Add(header);
			
			root.Add(CreateSpacer(5));
			
			// 상태 표시
			var statusRow = CreateStatusDisplay();
			root.Add(statusRow);
			
			root.Add(CreateSpacer(5));
			
			// 컨트롤 컨테이너 (플레이 모드에 따라 채워집니다)
			_controlsContainer = new VisualElement();
			root.Add(_controlsContainer);
			
			// 플레이 모드에 따라 컨트롤을 업데이트합니다
			UpdateControls();
			
			// 플레이 모드에서 주기적 업데이트를 예약합니다
			root.schedule.Execute(() =>
			{
				if (target != null)
				{
					UpdateStatusDisplay();
				}
			}).Every(100);
			
			return root;
		}

		private VisualElement CreateStatusDisplay()
		{
			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;
			container.style.alignItems = Align.Center;
			
			var statusLabel = new Label("상태:");
			statusLabel.style.width = 100;
			container.Add(statusLabel);
			
			// 상태 표시 원
			_statusIndicator = new VisualElement();
			_statusIndicator.style.width = 12;
			_statusIndicator.style.height = 12;
			_statusIndicator.style.borderTopLeftRadius = 6;
			_statusIndicator.style.borderTopRightRadius = 6;
			_statusIndicator.style.borderBottomLeftRadius = 6;
			_statusIndicator.style.borderBottomRightRadius = 6;
			_statusIndicator.style.marginRight = 5;
			container.Add(_statusIndicator);
			
			// 상태 텍스트
			_statusLabel = new Label();
			_statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			container.Add(_statusLabel);
			
			UpdateStatusDisplay();
			
			return container;
		}

		private void UpdateStatusDisplay()
		{
			var presenter = (UiPresenter)target;
			if (presenter == null)
				return;
			
			var isOpen = presenter.IsOpen;
			_statusLabel.text = isOpen ? "열림" : "닫힘";
			_statusIndicator.style.backgroundColor = isOpen ? new Color(0, 1, 0) : new Color(1, 0, 0);
		}

		private void UpdateControls()
		{
			_controlsContainer.Clear();
			
			var presenter = (UiPresenter)target;
			if (presenter == null)
				return;
			
			if (Application.isPlaying)
			{
				CreatePlayModeControls();
			}
			else
			{
				var helpBox = new HelpBox("UI 컨트롤은 플레이 모드에서만 사용할 수 있습니다", HelpBoxMessageType.Info);
				_controlsContainer.Add(helpBox);
			}
		}

		private void CreatePlayModeControls()
		{
			var presenter = (UiPresenter)target;
			
			_controlsContainer.Add(CreateSpacer(5));
			
			// 첫 번째 버튼 행
			var row1 = new VisualElement();
			row1.style.flexDirection = FlexDirection.Row;
			
			var openButton = new Button(() => presenter.InternalOpen()) { text = "UI 열기" };
			openButton.style.flexGrow = 1;
			openButton.style.height = 30;
			row1.Add(openButton);
			
			var closeButton = new Button(() => presenter.InternalClose(false)) { text = "UI 닫기" };
			closeButton.style.flexGrow = 1;
			closeButton.style.height = 30;
			closeButton.style.marginLeft = 5;
			row1.Add(closeButton);
			
			_controlsContainer.Add(row1);
			_controlsContainer.Add(CreateSpacer(5));
			
			// 파괴 버튼
			var closeDestroyButton = new Button(() =>
			{
				if (EditorUtility.DisplayDialog("UI 제거",
					"이 UI를 제거하시겠습니까?", "확인", "취소"))
				{
					presenter.InternalClose(true);
				}
			}) { text = "닫기 및 제거" };
			closeDestroyButton.style.height = 25;
			
			_controlsContainer.Add(closeDestroyButton);
		}

		private VisualElement CreateSpacer(int height)
		{
			var spacer = new VisualElement();
			spacer.style.height = height;
			return spacer;
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (_controlsContainer != null)
			{
				UpdateControls();
			}
		}
	}
}

