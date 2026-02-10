using Geuneda.UiService;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.UiService
{
	/// <summary>
	/// 에디터에서 <see cref="UiConfigs"/> 에셋 파일 선택을 도와주는 유틸리티입니다.
	/// </summary>
	public static class UiConfigsMenuItems
	{
		[MenuItem("Tools/UI Service/Select UiConfigs")]
		private static void SelectUiConfigs()
		{
			// UiConfigs 에셋을 검색합니다 (AddressablesUiConfigs, ResourcesUiConfigs 등 파생 타입 포함)
			var assets = AssetDatabase.FindAssets($"t:{nameof(UiConfigs)}");
			var scriptableObject = assets.Length > 0 ? 
				AssetDatabase.LoadAssetAtPath<UiConfigs>(AssetDatabase.GUIDToAssetPath(assets[0])) :
				ScriptableObject.CreateInstance<AddressablesUiConfigs>();

			if (assets.Length == 0)
			{
				// 하위 호환성을 위해 기본적으로 AddressablesUiConfigs를 생성합니다
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{nameof(AddressablesUiConfigs)}.asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			Selection.activeObject = scriptableObject;
			FocusInspectorWindow();
		}

		[MenuItem("Tools/UI Service/Layer Visualizer")]
		public static void ShowLayerVisualizer()
		{
			// OnEnable가 올바른 값을 읽을 수 있도록 선택 전에 Pref를 설정합니다
			EditorPrefs.SetBool("UiConfigsEditor_ShowVisualizer", true);
			
			SelectUiConfigs();

			// UI를 다시 빌드하기 위해 인스펙터를 강제 새로고침합니다
			ActiveEditorTracker.sharedTracker.ForceRebuild();
		}
		
		private static void FocusInspectorWindow()
		{
			// 리플렉션을 사용하여 인스펙터 창 타입을 가져옵니다
			var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			if (inspectorType != null)
			{
				// 인스펙터 창을 포커스하거나 새로 생성합니다
				EditorWindow.GetWindow(inspectorType);
			}
		}
	}
}
