using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// UiToolkitPresenterFeature가 있는 테스트 프레젠터.
	/// 재열기 시 UI Toolkit 요소 재생성을 처리하는 올바른 패턴을 시연합니다.
	/// </summary>
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	[RequireComponent(typeof(UIDocument))]
	public class TestUiToolkitPresenter : UiPresenter
	{
		public UiToolkitPresenterFeature ToolkitFeature { get; private set; }
		public bool WasOpened { get; private set; }
		public int SetupCallbackCount { get; private set; }

		private Button _testButton;

	private void Awake()
	{
		// UIDocument가 존재하는지 확인
		var document = GetComponent<UIDocument>();
		if (document == null)
		{
			document = gameObject.AddComponent<UIDocument>();
		}

		// 테스트 환경을 위한 PanelSettings 생성 (패널 연결에 필요)
		if (document.panelSettings == null)
		{
			document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
		}

		ToolkitFeature = GetComponent<UiToolkitPresenterFeature>();
		if (ToolkitFeature == null)
		{
			ToolkitFeature = gameObject.AddComponent<UiToolkitPresenterFeature>();
		}

		// 리플렉션을 통해 문서 참조 설정
		var docField = typeof(UiToolkitPresenterFeature).GetField("_document",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		docField?.SetValue(ToolkitFeature, document);
	}

		protected override void OnInitialized()
		{
			// 열기마다 호출될 콜백 등록
			ToolkitFeature.AddVisualTreeAttachedListener(SetupUI);
		}

		private void SetupUI(VisualElement root)
		{
			SetupCallbackCount++;

			// 올바른 패턴: 이전 것에서 해제, 새로 쿼리, 새 것에 등록
			_testButton?.UnregisterCallback<ClickEvent>(OnButtonClicked);
			_testButton = root?.Q<Button>("TestButton");
			_testButton?.RegisterCallback<ClickEvent>(OnButtonClicked);
		}

		private void OnButtonClicked(ClickEvent evt)
		{
			// 테스트 클릭 핸들러
		}

		/// <summary>
		/// RemoveVisualTreeAttachedListener 테스트를 위해 설정 리스너를 제거합니다.
		/// </summary>
		public void RemoveSetupListener()
		{
			ToolkitFeature.RemoveVisualTreeAttachedListener(SetupUI);
		}

		protected override void OnOpened()
		{
			WasOpened = true;
		}

		private void OnDestroy()
		{
			_testButton?.UnregisterCallback<ClickEvent>(OnButtonClicked);
		}
	}
}

