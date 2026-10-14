using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 선택되거나 선택이 풀릴 때(입력 필드의 onSelect, onEndEdit 처럼) 사용자 코드를 부르는 테스트 컴포넌트.
	/// </summary>
	public class TestSelectionCallback : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		public Action Selected { get; set; }
		public Action Deselected { get; set; }

		public void OnSelect(BaseEventData eventData)
		{
			Selected?.Invoke();
		}

		public void OnDeselect(BaseEventData eventData)
		{
			Deselected?.Invoke();
		}
	}
}
