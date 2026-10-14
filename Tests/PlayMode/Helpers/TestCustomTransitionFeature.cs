using System;
using Cysharp.Threading.Tasks;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 사용자가 만든 닫기 전환 피처를 흉내 냅니다. 일반 async UniTask(풀 반환) 또는 완료 소스를 쓰고,
	/// 새 태스크를 만들기 전에 던지게 할 수 있습니다.
	/// </summary>
	public class TestCustomTransitionFeature : PresenterFeatureBase, ITransitionFeature
	{
		private UniTask _closeTask = UniTask.CompletedTask;

		public bool UsePooledTask { get; set; }
		public bool UseDeferredThrow { get; set; }
		public bool UseAlwaysThrowingStatus { get; set; }
		public bool ThrowBeforeAssign { get; set; }
		public Exception FailWith { get; set; }

		public UniTask OpenTransitionTask => UniTask.CompletedTask;

		public UniTask CloseTransitionTask => _closeTask;

		public override void OnPresenterClosing()
		{
			if (ThrowBeforeAssign)
			{
				throw new InvalidOperationException("custom closing boom");
			}

			if (UseAlwaysThrowingStatus)
			{
				_closeTask = new UniTask(new ThrowingStatusSource(), 0);
				return;
			}

			if (UseDeferredThrow)
			{
				// 상태를 읽을 때 팩토리가 실행돼 예외가 납니다
				_closeTask = UniTask.Defer(() => throw new InvalidOperationException("deferred close boom"));
				return;
			}

			if (UsePooledTask)
			{
				_closeTask = DelayAsync();
				return;
			}

			var source = new UniTaskCompletionSource();
			if (FailWith != null)
			{
				source.TrySetException(FailWith);
			}
			else
			{
				source.TrySetResult();
			}

			_closeTask = source.Task;
		}

		private static async UniTask DelayAsync()
		{
			await UniTask.Delay(20);
		}

		// 상태를 읽을 때마다 던지는 소스(망가진 커스텀 전환)
		private sealed class ThrowingStatusSource : IUniTaskSource
		{
			public UniTaskStatus GetStatus(short token) => throw new InvalidOperationException("status boom");

			public UniTaskStatus UnsafeGetStatus() => throw new InvalidOperationException("status boom");

			public void GetResult(short token) => throw new InvalidOperationException("status boom");

			public void OnCompleted(Action<object> continuation, object state, short token) => throw new InvalidOperationException("status boom");
		}
	}
}
