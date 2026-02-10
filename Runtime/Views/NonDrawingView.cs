using UnityEngine;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService.Views
{
	/// <summary>
	/// 드로잉을 건너뛰는 Unity UI `Graphic` 클래스의 구체적인 서브클래스입니다.
	/// 실제로 아무것도 그리지 않고 레이캐스트 대상을 제공하는 데 유용합니다.
	/// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
	public class NonDrawingView : Graphic
	{
		public override void SetMaterialDirty() { }
		public override void SetVerticesDirty() { }
		
		/// <summary>
		/// `Rebuild()`->`UpdateGeometry()`->`DoMeshGeneration()`->`OnPopulateMesh()` 호출 체인이
		/// 발생하지 않으므로 아마 필요하지 않을 수 있습니다.
		/// 여기에는 단지 안전장치로 존재합니다.
		/// </summary>
		protected override void OnPopulateMesh(VertexHelper vh) 
		{
			vh.Clear();
		}
	}
}