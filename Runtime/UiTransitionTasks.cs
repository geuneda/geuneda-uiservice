using System;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 프레젠터가 한 열기·닫기 단계에서 시작한 피처 전환 태스크를 기다리고 관찰하는 도구입니다.
	/// 실패한 UniTask 를 관찰하지 않으면 GC 때 다시 기록되므로, 어느 경로로 끝나든 한 번만 관찰·기록되게 합니다.
	/// </summary>
	internal static class UiTransitionTasks
	{
		// 기다리지 않고 끝날 때, 시작한 전환의 실패가 버려지지 않게 관찰을 넘깁니다(실패하면 Forget 이 한 번 기록)
		public static void ObserveTransitions(UniTask[] tasks)
		{
			if (tasks == null)
			{
				return;
			}

			foreach (var task in tasks)
			{
				if (task.Status != UniTaskStatus.Succeeded)
				{
					task.Forget();
				}
			}
		}

		public static bool HasFaulted(UniTask[] tasks)
		{
			if (tasks == null)
			{
				return false;
			}

			foreach (var task in tasks)
			{
				if (task.Status == UniTaskStatus.Faulted)
				{
					return true;
				}
			}

			return false;
		}

		// 실패한 태스크를 모두 관찰해 첫 예외를 던지고(Fail 이 기록) 나머지 예외는 하나씩 기록합니다. 진행 중인 것은 관찰을 넘깁니다
		public static void ThrowFirstFault(UniTask[] tasks)
		{
			Exception first = null;

			foreach (var task in tasks)
			{
				if (task.Status == UniTaskStatus.Faulted)
				{
					try
					{
						task.GetAwaiter().GetResult();
					}
					catch (Exception exception)
					{
						if (first == null)
						{
							first = exception;
						}
						else
						{
							Debug.LogException(exception);
						}
					}
				}
				else if (task.Status == UniTaskStatus.Pending)
				{
					task.Forget();
				}
			}

			ExceptionDispatchInfo.Capture(first).Throw();
		}

		// 모든 전환을 기다립니다. 첫 예외를 던지고(Fail 이 기록) 나머지 예외는 하나씩 기록합니다
		public static async UniTask WaitForTransitionsAsync(UniTask[] tasks)
		{
			if (tasks == null)
			{
				return;
			}

			Exception first = null;

			foreach (var task in tasks)
			{
				try
				{
					await task;
				}
				catch (Exception exception)
				{
					if (first == null)
					{
						first = exception;
					}
					else
					{
						Debug.LogException(exception);
					}
				}
			}

			if (first != null)
			{
				ExceptionDispatchInfo.Capture(first).Throw();
			}
		}
	}
}
