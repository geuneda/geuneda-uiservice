using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 다중 기능(UiToolkit + TimeDelay)을 가진 테스트 프레젠터
	/// </summary>
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	[RequireComponent(typeof(TimeDelayFeature))]
	[RequireComponent(typeof(UIDocument))]
	public class TestMultiFeatureToolkitPresenter : UiPresenter
	{
		public UiToolkitPresenterFeature ToolkitFeature { get; private set; }
		public TimeDelayFeature DelayFeature { get; private set; }

	private void Awake()
	{
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

			DelayFeature = GetComponent<TimeDelayFeature>();
			if (DelayFeature == null)
			{
				DelayFeature = gameObject.AddComponent<TimeDelayFeature>();
			}

			// 문서 참조 설정
			var docField = typeof(UiToolkitPresenterFeature).GetField("_document",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			docField?.SetValue(ToolkitFeature, document);

			// 짧은 지연 시간 설정
			var openField = typeof(TimeDelayFeature).GetField("_openDelayInSeconds",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			openField?.SetValue(DelayFeature, 0.01f);
		}
	}
}

