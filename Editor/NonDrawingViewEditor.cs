using Geuneda.UiService.Views;
using UnityEditor;
using UnityEditor.UI;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.UiService
{
	/// <summary>
	/// <see cref="NonDrawingView"/> 커스텀 인스펙터
	/// </summary>
	[CanEditMultipleObjects, CustomEditor(typeof(NonDrawingView), false)]
	public class NonDrawingViewEditor : GraphicEditor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();

			// 스크립트 필드를 추가합니다
			var scriptField = new PropertyField(serializedObject.FindProperty("m_Script"));
			scriptField.SetEnabled(false);
			root.Add(scriptField);

			// 기본 클래스의 레이캐스트 컨트롤을 IMGUI 컨테이너를 사용하여 추가합니다
			var raycastContainer = new IMGUIContainer(() =>
			{
				serializedObject.Update();
				RaycastControlsGUI();
				serializedObject.ApplyModifiedProperties();
			});
			root.Add(raycastContainer);

			return root;
		}
	}
}