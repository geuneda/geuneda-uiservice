using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 팝업을 우선순위 순서로 하나씩 띄우는 큐입니다. 로비 진입 때 공지, 이벤트, 보상 팝업을 차례로 보여 주는 데 씁니다.
	/// </summary>
	/// <remarks>
	/// 팝업 하나가 닫히고 닫기 전환까지 끝나야 다음 팝업을 띄웁니다. 다음 경우에는 띄우지 않고 기다립니다.
	/// <list type="bullet">
	/// <item><see cref="Hold"/> 토큰이 하나라도 남아 있을 때</item>
	/// <item>큐 밖에서 연 뒤로가기 대상 프레젠터(<see cref="UiPresenter.HandlesBack"/>)가 표시 중일 때. 다른 큐가 띄운 팝업도 여기에 들어갑니다</item>
	/// </list>
	/// 조건은 이벤트로 다시 확인하므로 매 프레임 확인하지 않습니다.
	/// </remarks>
	public interface IUiPopupQueue : IDisposable
	{
		/// <summary>
		/// 대기 중인 항목 수입니다. 여는 중이거나 표시 중인 항목은 포함하지 않습니다.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// 큐가 꺼낸 팝업을 여는 중이거나, 표시 중이거나, 닫기 전환을 기다리는 중이면 true 입니다.
		/// </summary>
		bool IsShowing { get; }

		/// <summary>
		/// <see cref="Hold"/> 토큰이 하나라도 남아 있는지 여부입니다.
		/// </summary>
		bool IsHeld { get; }

		/// <summary>
		/// 남아 있는 멈춤 토큰의 사유 목록입니다. 큐가 진행하지 않는 원인을 찾을 때 씁니다.
		/// </summary>
		IReadOnlyList<string> HoldReasons { get; }

		/// <summary>
		/// 큐가 띄운 팝업이 끝났을 때(건너뛰거나 실패한 경우 포함) 대기 항목도 남지 않았으면 발생합니다.
		/// </summary>
		event Action Drained;

		/// <summary>
		/// 팝업을 예약합니다. 우선순위가 높은 것부터, 같으면 넣은 순서대로 띄웁니다.
		/// </summary>
		/// <param name="key">중복 방지 키. 같은 키가 대기 중이거나 표시 중이면 넣지 않습니다.</param>
		/// <param name="priority">클수록 먼저 띄웁니다.</param>
		/// <param name="factory">
		/// 띄울 차례가 되면 부를 함수. 서비스로 팝업을 열고 그 프레젠터를 돌려줍니다. 그 시점의 상태로 판단해 null 을 돌려주면 건너뜁니다.
		/// 예외를 던지면 기록하고 다음 항목으로 넘어갑니다. 큐를 Dispose 하면 토큰이 취소됩니다.
		/// </param>
		/// <returns>넣었으면 true, 같은 키가 이미 있으면 false.</returns>
		/// <exception cref="ArgumentException"><paramref name="key"/>가 null 이거나 비어 있는 경우 발생합니다.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="factory"/>가 null 인 경우 발생합니다.</exception>
		/// <exception cref="ObjectDisposedException">큐를 Dispose 한 뒤 부른 경우 발생합니다.</exception>
		bool Enqueue(string key, int priority, Func<CancellationToken, UniTask<UiPresenter>> factory);

		/// <summary>
		/// 대기 중인 항목을 뺍니다. 여는 중이거나 표시 중인 항목은 빼지 않습니다.
		/// </summary>
		/// <returns>뺐으면 true.</returns>
		bool Remove(string key);

		/// <summary>
		/// 대기 중인 항목을 모두 뺍니다. 여는 중이거나 표시 중인 팝업은 그대로 둡니다.
		/// </summary>
		void Clear();

		/// <summary>
		/// 큐를 멈춥니다. 토큰이 모두 풀리면 다시 진행합니다. 이미 띄운 팝업은 닫지 않습니다.
		/// </summary>
		/// <param name="reason">진단용 사유. <see cref="HoldReasons"/> 에 나옵니다.</param>
		/// <exception cref="ArgumentNullException"><paramref name="reason"/>이 null 인 경우 발생합니다.</exception>
		/// <exception cref="ObjectDisposedException">큐를 Dispose 한 뒤 부른 경우 발생합니다.</exception>
		UiPopupQueueHold Hold(string reason);
	}
}
