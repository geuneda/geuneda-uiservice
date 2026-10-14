using System;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 입력이 막힌 동안에만 켜져, 매 프레임 새로 생긴 EventSystem 과 새로 붙은 UI Toolkit 패널에도 차단을 적용하게 합니다.
	/// </summary>
	internal class UiInputBlockWatcher : MonoBehaviour
	{
		public Action Tick { get; set; }

		private void Update()
		{
			Tick?.Invoke();
		}
	}
}
