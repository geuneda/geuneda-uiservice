using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// Canvas 없이 UIDocument 만 가진 모달 팝업. UI Toolkit dim 과 차단 패널을 시험합니다.
	/// </summary>
	[RequireComponent(typeof(UIDocument))]
	public class TestToolkitPopupPresenter : TestPopupPresenter
	{
		/// <summary>
		/// Canvas 없는 프리팹을 만듭니다. 패널 연결에 필요한 PanelSettings 를 넣어 둡니다.
		/// </summary>
		public static GameObject CreatePrefab(PanelSettings panelSettings)
		{
			return CreatePrefab<TestToolkitPopupPresenter>(panelSettings);
		}

		/// <summary>
		/// 파생 타입으로 Canvas 없는 프리팹을 만듭니다.
		/// </summary>
		public static GameObject CreatePrefab<T>(PanelSettings panelSettings) where T : TestToolkitPopupPresenter
		{
			var go = new GameObject(typeof(T).Name);
			go.SetActive(false);
			go.AddComponent<UIDocument>().panelSettings = panelSettings;
			go.AddComponent<T>();
			return go;
		}
	}
}
