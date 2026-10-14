using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 입력이 막힌 동안 포커스·선택 기반 입력(키보드, 게임패드의 Submit/Move/Cancel, 텍스트 입력)을 막습니다.
	/// 차단막은 포인터 레이캐스트만 가리므로, 이미 선택된 버튼이나 포커스된 요소로 가는 입력은 따로 막아야 합니다.
	/// </summary>
	/// <remarks>
	/// 풀 때 선택은 복구하지 않습니다. 복구하는 선택 콜백(OnSelect)이 다시 막으면 상태가 엇갈리기 때문입니다.
	/// </remarks>
	internal sealed class UiFocusBlocker
	{
		private readonly List<EventSystem> _eventSystems = new List<EventSystem>();
		private readonly List<bool> _sentNavigationEvents = new List<bool>();
		private readonly List<IPanel> _panels = new List<IPanel>();
		private readonly EventCallback<NavigationSubmitEvent> _stopSubmit = Stop;
		private readonly EventCallback<NavigationCancelEvent> _stopCancel = Stop;
		private readonly EventCallback<NavigationMoveEvent> _stopMove = Stop;
		private readonly EventCallback<KeyDownEvent> _stopKeyDown = Stop;

		/// <summary>
		/// EventSystem 의 내비게이션 이벤트(Submit/Move/Cancel)를 끄고 선택을 풉니다. 처음 막을 때 원래 값을 기록합니다.
		/// 막힌 동안 매번 불러, 그사이 생긴 선택(입력 필드 자동 선택 등)도 풉니다.
		/// </summary>
		public void SuspendSelection(EventSystem eventSystem)
		{
			if (eventSystem == null)
			{
				return;
			}

			// 선택 해제 콜백(OnDeselect)이 다시 막거나 풀 수 있으므로 상태를 먼저 기록하고 마지막에 선택을 풉니다
			if (!_eventSystems.Contains(eventSystem))
			{
				_eventSystems.Add(eventSystem);
				_sentNavigationEvents.Add(eventSystem.sendNavigationEvents);
			}

			eventSystem.sendNavigationEvents = false;

			// 선택 콜백 도중에는 선택을 바꿀 수 없으므로(Unity 가 오류를 남김) 다음 재적용(매 프레임)에서 다시 풉니다.
			// 입력 필드의 텍스트 입력(UpdateSelected)은 내비게이션 설정과 무관하게 선택된 대상으로 가므로 선택을 꼭 풀어야 합니다
			if (!eventSystem.alreadySelecting && eventSystem.currentSelectedGameObject != null)
			{
				eventSystem.SetSelectedGameObject(null);
			}
		}

		/// <summary>
		/// 패널의 포커스를 풀고, 패널로 오는 키·내비게이션 이벤트를 루트에서 멈춥니다. 이미 막은 패널은 건너뜁니다.
		/// </summary>
		public void SuspendPanel(IPanel panel)
		{
			if (panel == null || _panels.Contains(panel))
			{
				return;
			}

			var root = panel.visualTree;
			root.RegisterCallback(_stopSubmit, TrickleDown.TrickleDown);
			root.RegisterCallback(_stopCancel, TrickleDown.TrickleDown);
			root.RegisterCallback(_stopMove, TrickleDown.TrickleDown);
			root.RegisterCallback(_stopKeyDown, TrickleDown.TrickleDown);
			_panels.Add(panel);

			panel.focusController?.focusedElement?.Blur();
		}

		/// <summary>
		/// 막았던 것을 모두 되돌립니다. 사용자 콜백을 부르지 않습니다.
		/// </summary>
		public void Resume()
		{
			foreach (var panel in _panels)
			{
				var root = panel.visualTree;
				root?.UnregisterCallback(_stopSubmit, TrickleDown.TrickleDown);
				root?.UnregisterCallback(_stopCancel, TrickleDown.TrickleDown);
				root?.UnregisterCallback(_stopMove, TrickleDown.TrickleDown);
				root?.UnregisterCallback(_stopKeyDown, TrickleDown.TrickleDown);
			}

			_panels.Clear();

			for (var i = 0; i < _eventSystems.Count; i++)
			{
				if (_eventSystems[i] != null)
				{
					_eventSystems[i].sendNavigationEvents = _sentNavigationEvents[i];
				}
			}

			_eventSystems.Clear();
			_sentNavigationEvents.Clear();
		}

		private static void Stop(EventBase evt)
		{
			evt.StopImmediatePropagation();
			((VisualElement)evt.currentTarget).focusController?.IgnoreEvent(evt);
		}
	}
}
