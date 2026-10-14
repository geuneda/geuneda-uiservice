using System;
using UnityEngine;
using UnityEngine.EventSystems;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// uGUI 공용 dim 의 클릭을 서비스에 전달합니다.
	/// </summary>
	internal class UiDimClickView : MonoBehaviour, IPointerClickHandler
	{
		public Action Clicked { get; set; }

		public void OnPointerClick(PointerEventData eventData)
		{
			Clicked?.Invoke();
		}
	}
}
