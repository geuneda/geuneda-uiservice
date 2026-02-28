using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Geuneda.UiService;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.UiService
{
	/// <summary>
	/// UiService가 추적하는 UI 프레젠터를 관리하기 위한 통합 에디터 창입니다.
	/// 로드된 프레젠터와 표시 중인 프레젠터의 단일 정보 소스를 제공합니다.
	/// </summary>
	public class UiPresenterManagerWindow : EditorWindow
	{
		private bool _autoRefresh = true;
		private double _lastRefreshTime;
		private const double RefreshInterval = 0.5; // 초
		
		private const string StatsExplanation =
			"통계 요약\n\n" +
			"• 전체: 현재 메모리에 로드된 프레젠터 인스턴스 수.\n" +
			"• 열림: 현재 표시 중인 프레젠터 (UiService.VisiblePresenters로 추적).\n" +
			"• 닫힘: 메모리에 로드되었지만 숨겨진 프레젠터 (다시 로드 없이 재표시 가능).";

		private const string PresenterExplanation =
			"프레젠터 목록\n\n" +
			"• 프레젠터 타입: UiPresenter 컴포넌트의 클래스 이름, 클릭하면 씬에서 해당 GameObject를 선택합니다.\n" +
			"• 상태: 초록색 = 표시 중, 빨간색 = 로드되었지만 숨겨짐.\n" +
			"• 액션: 열기/닫기로 표시 여부를 전환, 언로드로 메모리에서 제거.\n" +
			"• 인스턴스: 멀티 인스턴스 주소 (싱글톤 프레젠터는 '(기본값)').";
		
		private ScrollView _scrollView;
		private Label _statsLabel;

		[MenuItem("Tools/UI Service/Presenter Manager")]
		public static void ShowWindow()
		{
			var window = GetWindow<UiPresenterManagerWindow>("UI 프레젠터 매니저");
			window.minSize = new Vector2(500, 300);
			window.Show();
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
			if (rootVisualElement != null)
			{
				UpdateContent();
			}
		}

		private void CreateGUI()
		{
			var root = rootVisualElement;
			root.Clear();
			
			// Header
			var header = CreateHeader();
			root.Add(header);
			
			// Stats explanation
			var statsHelpBox = new HelpBox(StatsExplanation, HelpBoxMessageType.Info);
			statsHelpBox.style.marginBottom = 5;
			root.Add(statsHelpBox);
			
			// Stats bar
			_statsLabel = new Label();
			_statsLabel.style.paddingLeft = 10;
			_statsLabel.style.paddingTop = 5;
			_statsLabel.style.paddingBottom = 5;
			_statsLabel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
			_statsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			_statsLabel.enableRichText = true;
			_statsLabel.tooltip = "전체: 메모리에 로드됨 | 열림: 표시 중 | 닫힘: 숨겨졌지만 로드됨";
			root.Add(_statsLabel);
			
			// Presenter list explanation
			var presenterHelpBox = new HelpBox(PresenterExplanation, HelpBoxMessageType.Info);
			presenterHelpBox.style.marginBottom = 5;
			root.Add(presenterHelpBox);
			
			// Column Headers
			var columnHeaders = CreateColumnHeaders();
			root.Add(columnHeaders);
			
			// Scroll view
			_scrollView = new ScrollView();
			_scrollView.style.flexGrow = 1;
			root.Add(_scrollView);
			
			// Footer
			var footer = CreateFooter();
			root.Add(footer);
			
			// Update content
			UpdateContent();
			
			// Schedule periodic updates
			root.schedule.Execute(() =>
			{
				if (_autoRefresh && Application.isPlaying && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
				{
					_lastRefreshTime = EditorApplication.timeSinceStartup;
					UpdateContent();
				}
			}).Every(100);
		}

		private VisualElement CreateHeader()
		{
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			header.style.paddingTop = 5;
			header.style.paddingBottom = 5;
			header.style.paddingLeft = 5;
			header.style.paddingRight = 5;
			
			var titleLabel = new Label("UI 프레젠터 매니저");
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			titleLabel.style.flexGrow = 1;
			header.Add(titleLabel);
			
			var autoRefreshToggle = new Toggle("자동 새로고침") { value = _autoRefresh };
			autoRefreshToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
			autoRefreshToggle.style.width = 110;
			header.Add(autoRefreshToggle);
			
			var refreshButton = new Button(() => UpdateContent()) { text = "새로고침" };
			refreshButton.style.width = 60;
			refreshButton.style.marginLeft = 5;
			header.Add(refreshButton);
			
			return header;
		}

		// 열 정의: (최소 너비, 확장 비율) - 우선순위: Type (최고) > Actions > Status/Instance (최저)
		private static readonly (float minWidth, int flexGrow)[] ColumnDefs = 
		{
			(120, 6), // Type
			(60, 1),  // Status
			(140, 2), // Actions
			(120, 1)  // Instance
		};

		private static void ApplyColumnStyle(VisualElement element, int columnIndex)
		{
			var (minWidth, flexGrow) = ColumnDefs[columnIndex];
			element.style.minWidth = minWidth;
			element.style.flexGrow = flexGrow;
			element.style.flexShrink = 0;
			element.style.flexBasis = minWidth;
		}

		private VisualElement CreateColumnHeaders()
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			row.style.paddingTop = 2;
			row.style.paddingBottom = 2;
			row.style.borderBottomWidth = 1;
			row.style.borderBottomColor = Color.black;
			
			row.Add(CreateHeaderLabel("프레젠터 타입", 0));
			row.Add(CreateHeaderLabel("상태", 1));
			row.Add(CreateHeaderLabel("액션", 2));
			row.Add(CreateHeaderLabel("인스턴스", 3));
			
			return row;
		}

		private Label CreateHeaderLabel(string text, int columnIndex)
		{
			var label = new Label(text);
			ApplyColumnStyle(label, columnIndex);
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.paddingLeft = 10;
			label.tooltip = columnIndex switch
			{
				0 => "UiPresenter 컴포넌트 클래스 이름",
				1 => "초록색 = 표시 중, 빨간색 = 숨겨짐",
				2 => "열기/닫기로 표시 여부 전환, 언로드로 메모리에서 제거",
				3 => "멀티 인스턴스 프레젠터의 인스턴스 주소",
				_ => ""
			};
			return label;
		}

		private void UpdateContent()
		{
			if (_scrollView == null)
				return;
			
			_scrollView.Clear();
			
			if (!Application.isPlaying)
			{
				var helpBox = new HelpBox("UI 프레젠터 매니저는 플레이 모드에서만 사용할 수 있습니다.", HelpBoxMessageType.Info);
				_scrollView.Add(helpBox);
				_statsLabel.text = "플레이 모드가 아닙니다";
				return;
			}

			var service = Geuneda.UiService.UiService.CurrentService;
			if (service == null)
			{
				var warningBox = new HelpBox("UiService 인스턴스를 찾을 수 없습니다.", HelpBoxMessageType.Warning);
				_scrollView.Add(warningBox);
				_statsLabel.text = "서비스를 찾을 수 없습니다";
				return;
			}

			var loadedPresenters = service.GetLoadedPresenters();
			var visiblePresenters = service.VisiblePresenters;
			
			var loadedCount = loadedPresenters.Count;
			var openCount = visiblePresenters.Count;
			var closedCount = loadedCount - openCount;
			
			_statsLabel.text = $"<b>전체 프레젠터:</b> {loadedCount} - <color=#88ff88><b>열림:</b> {openCount}</color> | <color=#ff8888><b>닫힘:</b> {closedCount}</color>";

			if (loadedPresenters.Count == 0)
			{
				var infoBox = new HelpBox("메모리에 로드된 프레젠터가 없습니다.", HelpBoxMessageType.Info);
				_scrollView.Add(infoBox);
				return;
			}

			int index = 0;
			foreach (var instance in loadedPresenters.OrderBy(i => i.Type.Name))
			{
				var isOpen = visiblePresenters.Any(id => id.PresenterType == instance.Type && id.InstanceAddress == instance.Address);
				_scrollView.Add(CreatePresenterRow(service, instance, isOpen, index++));
			}
		}

		private VisualElement CreatePresenterRow(Geuneda.UiService.UiService service, UiInstance instance, bool isOpen, int index)
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.paddingTop = 4;
			row.style.paddingBottom = 4;
			row.style.borderBottomWidth = 1;
			row.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);
			row.style.alignItems = Align.Center;

			if (index % 2 == 1)
			{
				row.style.backgroundColor = new Color(1, 1, 1, 0.03f);
			}

			// 타입 버튼 (열 0) - 클릭하면 하이어라키에서 선택됩니다
			var typeButton = new Button(() => 
			{
				Selection.activeGameObject = instance.Presenter.gameObject;
				EditorGUIUtility.PingObject(instance.Presenter.gameObject);
				if (SceneView.lastActiveSceneView != null)
				{
					SceneView.lastActiveSceneView.FrameSelected();
				}
			}) { text = instance.Type.Name };
			ApplyColumnStyle(typeButton, 0);
			typeButton.style.marginLeft = 5;
			typeButton.style.marginRight = 5;
			row.Add(typeButton);

			// 상태 (열 1)
			var statusContainer = new VisualElement();
			ApplyColumnStyle(statusContainer, 1);
			statusContainer.style.flexDirection = FlexDirection.Row;
			statusContainer.style.alignItems = Align.Center;
			statusContainer.style.paddingLeft = 10;

			var statusDot = new VisualElement();
			statusDot.style.width = 8;
			statusDot.style.height = 8;
			statusDot.style.borderTopLeftRadius = 4;
			statusDot.style.borderTopRightRadius = 4;
			statusDot.style.borderBottomLeftRadius = 4;
			statusDot.style.borderBottomRightRadius = 4;
			statusDot.style.backgroundColor = isOpen ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
			statusDot.style.marginRight = 6;
			statusContainer.Add(statusDot);

			var statusLabel = new Label(isOpen ? "열림" : "닫힘");
			statusLabel.style.color = isOpen ? Color.green : Color.red;
			statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			statusContainer.Add(statusLabel);
			row.Add(statusContainer);

			// 액션 (열 2)
			var actionsContainer = new VisualElement();
			ApplyColumnStyle(actionsContainer, 2);
			actionsContainer.style.flexDirection = FlexDirection.Row;
			actionsContainer.style.paddingLeft = 5;

			if (isOpen)
			{
				var closeButton = new Button(() => service.CloseUi(instance.Type, instance.Address, false)) { text = "닫기" };
				closeButton.style.flexGrow = 1;
				closeButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
				closeButton.style.color = Color.white;
				actionsContainer.Add(closeButton);
			}
			else
			{
				var openButton = new Button(() => service.OpenUiAsync(instance.Type, instance.Address).Forget()) { text = "열기" };
				openButton.style.flexGrow = 1;
				openButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
				openButton.style.color = Color.white;
				actionsContainer.Add(openButton);
				
				var unloadButton = new Button(() => {
					if (EditorUtility.DisplayDialog("UI 언로드", $"{instance.Type.Name} [{instance.Address}]을(를) 언로드하시겠습니까?", "확인", "취소"))
					{
						service.UnloadUi(instance.Type, instance.Address);
						UpdateContent();
					}
				}) { text = "언로드" };
				unloadButton.style.width = 60;
				unloadButton.style.marginLeft = 5;
				actionsContainer.Add(unloadButton);
			}
			row.Add(actionsContainer);

			// 인스턴스 (열 3)
			var instanceLabel = new Label(string.IsNullOrEmpty(instance.Address) ? "(기본값)" : instance.Address);
			ApplyColumnStyle(instanceLabel, 3);
			instanceLabel.style.paddingLeft = 10;
			instanceLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
			row.Add(instanceLabel);

			return row;
		}

		private VisualElement CreateFooter()
		{
			var footer = new VisualElement();
			footer.style.flexDirection = FlexDirection.Row;
			footer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			footer.style.paddingTop = 5;
			footer.style.paddingBottom = 5;
			footer.style.paddingLeft = 5;
			footer.style.paddingRight = 5;
			footer.style.marginTop = 5;
			
			var closeAllButton = new Button(() => {
				if (EditorUtility.DisplayDialog("모두 닫기", "표시 중인 모든 UI 프레젠터를 닫으시겠습니까?", "확인", "취소"))
				{
					Geuneda.UiService.UiService.CurrentService?.CloseAllUi();
					UpdateContent();
				}
			}) { text = "모두 닫기" };
			closeAllButton.style.width = 100;
			footer.Add(closeAllButton);
			
			var unloadAllButton = new Button(() => {
				var service = Geuneda.UiService.UiService.CurrentService;
				if (service == null) return;

				var loaded = service.GetLoadedPresenters();
				if (loaded.Count == 0) return;

				if (EditorUtility.DisplayDialog("모두 언로드", $"모든 프레젠터 {loaded.Count}개를 언로드하시겠습니까?", "확인", "취소"))
				{
					foreach (var instance in loaded.ToList())
					{
						service.UnloadUi(instance.Type, instance.Address);
					}
					UpdateContent();
				}
			}) { text = "모두 언로드" };
			unloadAllButton.style.width = 100;
			unloadAllButton.style.marginLeft = 5;
			footer.Add(unloadAllButton);
			
			return footer;
		}
	}
}

