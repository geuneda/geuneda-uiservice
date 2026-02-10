using UnityEngine;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 레이어 테스트를 위한 테스트 프레젠터 (다른 레이어에 배치)
	/// </summary>
	public class TestLayerUiPresenter : UiPresenter
	{
		public int AssignedLayer { get; private set; }

		protected override void OnInitialized()
		{
			// Canvas 정렬 순서가 레이어를 반영
			var canvas = GetComponent<Canvas>();
			if (canvas != null)
			{
				AssignedLayer = canvas.sortingOrder;
			}
		}
	}
}

