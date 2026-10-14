using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 서비스가 런타임에 만드는 UI Toolkit 요소의 공통 스타일입니다.
	/// </summary>
	internal static class UiStyleUtils
	{
		/// <summary>
		/// 요소를 절대 위치로 바꿔 부모를 꽉 채웁니다. UIDocument 루트는 기본 높이가 0 이라 이렇게 해야 화면을 덮습니다.
		/// </summary>
		public static void FillParent(VisualElement element)
		{
			var style = element.style;
			style.position = Position.Absolute;
			style.left = 0;
			style.top = 0;
			style.right = 0;
			style.bottom = 0;
		}
	}
}
