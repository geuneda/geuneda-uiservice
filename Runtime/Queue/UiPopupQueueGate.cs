using System;
using System.Collections.Generic;
using Geuneda.Core;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 한 서비스의 팝업 큐들이 팩토리를 한 번에 하나만 돌리게 합니다. 팩토리가 로드 중이면 팝업이 아직 표시 목록에 없어
	/// 큐 밖 뒤로가기 대상 대기로는 막을 수 없으므로, 먼저 시작한 큐의 팩토리가 끝날 때까지 다른 큐를 기다리게 합니다.
	/// 끝나면(실패, 취소, null 포함) 기다리기 시작한 순서대로 다시 진행시킵니다.
	/// </summary>
	internal sealed class UiPopupQueueGate
	{
		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.PopupQueue");

		// 큐의 진행 함수(큐마다 하나 만들어 둔 델리게이트)로 큐를 구분합니다
		private readonly List<Action> _waiting = new List<Action>();
		private Action _loading;

		/// <summary>
		/// 다른 큐의 팩토리가 로드 중이 아니면 <paramref name="advance"/> 의 큐가 차지하고 true 를 돌려줍니다.
		/// 로드 중이면 기다림 목록 끝에 넣고(이미 있으면 자리를 지킴) false 를 돌려줍니다.
		/// </summary>
		public bool TryEnter(Action advance)
		{
			if (_loading == null)
			{
				_loading = advance;
				_waiting.Remove(advance);
				return true;
			}

			if (!_waiting.Contains(advance))
			{
				_waiting.Add(advance);
			}

			return false;
		}

		/// <summary>
		/// 팩토리가 끝났거나 큐를 Dispose 했을 때 부릅니다. 그 큐가 로드 중이었으면 기다리던 큐를 차례로 진행시키고,
		/// 그중 하나가 팩토리를 시작하면 나머지는 계속 기다립니다.
		/// </summary>
		public void Exit(Action advance)
		{
			_waiting.Remove(advance);

			if (!ReferenceEquals(_loading, advance))
			{
				return;
			}

			_loading = null;

			while (_loading == null && _waiting.Count > 0)
			{
				var next = _waiting[0];
				_waiting.RemoveAt(0);

				// 다른 큐의 진행(Drained 구독자 등)이 던져도 남은 큐와, 로드를 마치고 이어서 정리할 큐가 멈추지 않게 합니다
				try
				{
					next();
				}
				catch (Exception exception)
				{
					Logger.Error(exception, "A waiting popup queue threw while resuming");
				}
			}
		}
	}
}
