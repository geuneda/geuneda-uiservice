using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Geuneda.Core;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <inheritdoc />
	/// <remarks>
	/// 메인 스레드에서만 씁니다. 장면마다 큐를 만들고, 장면을 떠날 때 Dispose 합니다. 서비스보다 먼저 Dispose 해야 합니다.
	/// </remarks>
	public sealed class UiPopupQueue : IUiPopupQueue
	{
		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.PopupQueue");

		private readonly IUiService _uiService;
		// 우선순위가 높은 것부터, 같으면 넣은 순서대로 정렬해 둡니다
		private readonly List<Entry> _pending = new List<Entry>();
		private readonly List<int> _holdIds = new List<int>();
		private readonly List<string> _holdReasons = new List<string>();
		private readonly Action<int> _releaseHold;
		private readonly CancellationTokenSource _disposeCancellation = new CancellationTokenSource();
		private int _lastHoldId;
		// 꺼낸 항목의 키. 여는 중부터 닫기 전환이 끝날 때까지 남아 있습니다
		private string _currentKey;
		// 꺼낸 팝업이 표시 중인 동안의 인스턴스 ID
		private UiInstanceId? _currentId;
		private bool _disposed;
		// 동기로 끝나는 항목(건너뛰기 등)을 재귀 없이 반복 처리하기 위한 상태
		private bool _advancing;
		private bool _advanceAgain;
		// 꺼낸 항목이 끝났는지. 진행이 멈췄을 때 Drained 를 판단합니다
		private bool _finished;

		/// <param name="uiService">팝업을 열고 닫힘을 알려 줄 서비스</param>
		/// <exception cref="ArgumentNullException"><paramref name="uiService"/>가 null 인 경우 발생합니다.</exception>
		public UiPopupQueue(IUiService uiService)
		{
			_uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
			_releaseHold = ReleaseHold;
			HoldReasons = _holdReasons.AsReadOnly();
			_uiService.PresenterClosed += OnPresenterClosed;
		}

		/// <inheritdoc />
		public int Count => _pending.Count;

		/// <inheritdoc />
		public bool IsShowing => _currentKey != null;

		/// <inheritdoc />
		public bool IsHeld => _holdIds.Count > 0;

		/// <inheritdoc />
		public IReadOnlyList<string> HoldReasons { get; }

		/// <inheritdoc />
		public event Action Drained;

		/// <inheritdoc />
		public bool Enqueue(string key, int priority, Func<CancellationToken, UniTask<UiPresenter>> factory)
		{
			ThrowIfDisposed();

			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentException("Key cannot be null or empty", nameof(key));
			}

			if (factory == null)
			{
				throw new ArgumentNullException(nameof(factory));
			}

			if (key == _currentKey || IndexOf(key) >= 0)
			{
				return false;
			}

			var index = 0;

			// 같은 우선순위의 기존 항목 뒤에 넣어 넣은 순서를 지킵니다
			while (index < _pending.Count && _pending[index].Priority >= priority)
			{
				index++;
			}

			_pending.Insert(index, new Entry(key, priority, factory));
			Advance();

			return true;
		}

		/// <inheritdoc />
		public bool Remove(string key)
		{
			var index = IndexOf(key);
			if (index < 0)
			{
				return false;
			}

			_pending.RemoveAt(index);
			return true;
		}

		/// <inheritdoc />
		public void Clear()
		{
			_pending.Clear();
		}

		/// <inheritdoc />
		public UiPopupQueueHold Hold(string reason)
		{
			ThrowIfDisposed();

			if (reason == null)
			{
				throw new ArgumentNullException(nameof(reason));
			}

			// 0 은 default 토큰과 겹치므로 건너뜁니다
			_lastHoldId = _lastHoldId == int.MaxValue ? 1 : _lastHoldId + 1;
			_holdIds.Add(_lastHoldId);
			_holdReasons.Add(reason);

			return new UiPopupQueueHold(_releaseHold, _lastHoldId);
		}

		/// <summary>
		/// 서비스 구독을 끊고 대기 항목을 버리며, 여는 중인 팩토리의 토큰을 취소합니다. 이미 띄운 팝업은 닫지 않습니다.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_uiService.PresenterClosed -= OnPresenterClosed;
			_pending.Clear();
			_currentKey = null;
			_currentId = null;
			Drained = null;

			// 취소 콜백이 예외를 던져도 상태는 이미 정리돼 있습니다
			try
			{
				_disposeCancellation.Cancel();
			}
			finally
			{
				_disposeCancellation.Dispose();
			}
		}

		// 띄울 수 있으면 다음 항목을 꺼냅니다. 팩토리가 동기로 끝나면 같은 루프에서 이어 가므로 항목 수만큼 스택이 깊어지지 않습니다
		private void Advance()
		{
			if (_advancing)
			{
				_advanceAgain = true;
				return;
			}

			_advancing = true;

			try
			{
				do
				{
					_advanceAgain = false;

					if (_disposed || _currentKey != null || IsHeld || _pending.Count == 0 || HasOutsideBackTarget())
					{
						continue;
					}

					var entry = _pending[0];
					_pending.RemoveAt(0);
					_currentKey = entry.Key;
					ShowAsync(entry).Forget();
				}
				while (_advanceAgain);
			}
			finally
			{
				_advancing = false;
			}

			if (_finished && _currentKey == null)
			{
				_finished = false;

				if (_pending.Count == 0)
				{
					Drained?.Invoke();
				}
			}
		}

		private async UniTaskVoid ShowAsync(Entry entry)
		{
			UiPresenter presenter = null;

			try
			{
				presenter = await entry.Factory(_disposeCancellation.Token);
			}
			catch (OperationCanceledException)
			{
				// 큐를 Dispose 해서 취소됐으면 그대로 끝내고, 팩토리가 스스로 취소했으면 건너뜁니다
			}
			catch (Exception exception)
			{
				// 팝업 하나가 실패해도 나머지 팝업은 띄웁니다
				Logger.Error(exception, $"The popup factory threw (key '{entry.Key}')");
			}

			if (_disposed)
			{
				return;
			}

			if (presenter == null)
			{
				Finish();
				return;
			}

			var instanceId = new UiInstanceId(presenter.GetType(), presenter.InstanceAddress);

			if (IsVisible(instanceId))
			{
				_currentId = instanceId;
				return;
			}

			// 팩토리가 돌려주기 전에 닫혔으면 그 닫기 전환이 끝날 때까지 기다립니다
			WaitUntilClosedAsync(instanceId).Forget();
		}

		private void OnPresenterClosed(UiInstanceId instanceId)
		{
			if (_currentId.HasValue && _currentId.Value == instanceId)
			{
				_currentId = null;
				WaitUntilClosedAsync(instanceId).Forget();
				return;
			}

			// 큐 밖의 팝업이 닫혀 진행 조건이 풀렸을 수 있습니다
			Advance();
		}

		// 꺼낸 팝업이 실제로 닫힐 때까지 기다립니다. 닫기 전환 중에 다시 열리면 계속 현재 항목으로 추적하고,
		// 다시 닫히면 새 닫기 전환을 기다립니다. 닫기 전환까지 끝나야 다음 팝업을 띄워 전환이 겹치지 않습니다
		private async UniTaskVoid WaitUntilClosedAsync(UiInstanceId instanceId)
		{
			while (true)
			{
				// 언로드되거나 서비스에서 빠졌으면 끝난 것으로 봅니다
				if (!_uiService.TryGetUi(instanceId, out var presenter) || presenter == null)
				{
					break;
				}

				if (IsVisible(instanceId))
				{
					_currentId = instanceId;
					return;
				}

				var closing = presenter.CloseTransitionTask;

				// 숨겨졌거나 기다릴 닫기가 없으면(서비스를 거치지 않은 상태 변경) 끝난 것으로 봅니다
				if (!presenter.IsOpen || closing.Status != UniTaskStatus.Pending)
				{
					break;
				}

				if (_disposed)
				{
					return;
				}

				// 서비스에서 빠지거나 언로드되면 서비스가 이 태스크를 완료하고, 큐를 Dispose 하면 기다림을 그만둡니다
				try
				{
					await closing.AttachExternalCancellation(_disposeCancellation.Token);
				}
				catch (Exception)
				{
					// Dispose 로 취소됐으면 끝냅니다. 닫기 훅 예외로 실패한 태스크는 프레젠터 쪽에서 이미 기록했으므로 다음 확인으로 넘어갑니다
					if (_disposed)
					{
						return;
					}
				}

				if (_disposed)
				{
					return;
				}
			}

			Finish();
		}

		private void Finish()
		{
			_currentKey = null;
			_currentId = null;
			_finished = true;
			Advance();
		}

		private void ReleaseHold(int id)
		{
			var index = _holdIds.IndexOf(id);
			if (index < 0)
			{
				Logger.Warning("이미 풀린 멈춤 토큰을 다시 풀었습니다. 같은 토큰을 두 번 Dispose 하지 않았는지 확인하세요.");
				return;
			}

			_holdIds.RemoveAt(index);
			_holdReasons.RemoveAt(index);

			if (_holdIds.Count == 0)
			{
				Advance();
			}
		}

		private bool HasOutsideBackTarget()
		{
			var visible = _uiService.VisiblePresenters;
			for (var i = 0; i < visible.Count; i++)
			{
				if (_uiService.TryGetUi(visible[i], out var presenter) && presenter != null && presenter.InternalHandlesBack)
				{
					return true;
				}
			}

			return false;
		}

		private bool IsVisible(UiInstanceId instanceId)
		{
			var visible = _uiService.VisiblePresenters;
			for (var i = 0; i < visible.Count; i++)
			{
				if (visible[i] == instanceId)
				{
					return true;
				}
			}

			return false;
		}

		private int IndexOf(string key)
		{
			for (var i = 0; i < _pending.Count; i++)
			{
				if (_pending[i].Key == key)
				{
					return i;
				}
			}

			return -1;
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(UiPopupQueue));
			}
		}

		private readonly struct Entry
		{
			public readonly string Key;
			public readonly int Priority;
			public readonly Func<CancellationToken, UniTask<UiPresenter>> Factory;

			public Entry(string key, int priority, Func<CancellationToken, UniTask<UiPresenter>> factory)
			{
				Key = key;
				Priority = priority;
				Factory = factory;
			}
		}
	}
}
