using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Geuneda.Core;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 표시 목록 변경과 그 알림을 담당하는 <see cref="UiService"/> 의 나머지 부분입니다.
	/// 열기·닫기는 '준비'(목록 변경, 세대 증가, 전환 태스크 생성)를 즉시 하고, '알림과 실행'(UI Toolkit·uGUI 정렬, dim, 이벤트, 사용자 훅)은
	/// 한 번에 하나씩 차례로 처리합니다. 알림이나 훅 도중에 생긴 열기·닫기는 준비만 하고 앞의 것이 끝난 뒤 이어서 처리하므로,
	/// 모든 구독자가 같은 순서로 알림을 받고 훅이 다른 훅 도중에 끼어들지 않습니다.
	/// </summary>
	public partial class UiService
	{
		private static readonly Comparison<ToolkitEntry> ByLayerThenSequence = (left, right) =>
			left.Layer != right.Layer ? left.Layer.CompareTo(right.Layer) : left.Sequence.CompareTo(right.Sequence);
		private static readonly Comparison<CanvasEntry> CanvasByLayerThenSequence = (left, right) =>
			left.Layer != right.Layer ? left.Layer.CompareTo(right.Layer) : left.Sequence.CompareTo(right.Sequence);

		private readonly Queue<PendingChange> _pendingChanges = new Queue<PendingChange>();
		// 서비스에 등록된 UIDocument 프레젠터. UI Toolkit 정렬과 입력 차단에 씁니다
		private readonly List<ToolkitPresenter> _toolkitPresenters = new List<ToolkitPresenter>();
		private readonly List<ToolkitEntry> _toolkitOrder = new List<ToolkitEntry>();
		// 서비스에 등록된 루트 Canvas 프레젠터. uGUI 정렬에 씁니다
		private readonly List<CanvasPresenter> _canvasPresenters = new List<CanvasPresenter>();
		private readonly List<CanvasEntry> _canvasOrder = new List<CanvasEntry>();
		private readonly HashSet<int> _canvasGapWarnedLayers = new HashSet<int>();
		private bool _publishing;
		// 알림 중이라 아직 실행하지 않은 변경. 알림 안에서 Dispose 하면 언로드 전에 실행합니다
		private PendingChange _notifying;
		private bool _notifyingNotRun;
		private long _openSequence;
		private int _canvasOrderStep = 1;

		/// <summary>
		/// 같은 레이어에서 나중에 연 uGUI 프레젠터의 루트 <see cref="Canvas.sortingOrder"/> 에 더하는 간격입니다. 기본값은 1 입니다.
		/// 프레젠터 안의 자식 Canvas 를 루트 정렬값 기준 오프셋(+1, +2 ...)으로 그리는 프로젝트는 그 오프셋보다 크게 잡아
		/// 아래 프레젠터의 자식이 위 프레젠터를 덮지 않게 합니다(예: 10). 다음 열기·닫기부터 적용됩니다.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">1 보다 작은 값</exception>
		public int CanvasOrderStep
		{
			get => _canvasOrderStep;
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "CanvasOrderStep must be at least 1");
				}

				_canvasOrderStep = value;
			}
		}

		private void OpenVisible(UiInstanceId instanceId, UiPresenter presenter)
		{
			if (_disposed)
			{
				return;
			}

			// OnOpened 에서 다른 UI 를 열어도 연 순서가 맞도록 먼저 목록에 넣습니다
			_visibleUiList.Add(instanceId);
			presenter.OpenSequence = ++_openSequence;
			var step = presenter.PrepareOpen();
			Publish(new PendingChange(instanceId, presenter, step, true, false));
		}

		private bool TryCloseVisible(UiInstanceId instanceId, bool destroy)
		{
			if (!_visibleUiList.Remove(instanceId))
			{
				return false;
			}

			if (!TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter))
			{
				Publish(new PendingChange(instanceId, null, default, false, false));
				return true;
			}

			// 준비에서 새 닫기 전환 태스크를 만들어 두므로 알림을 받은 구독자는 이번 닫기를 기다릴 수 있습니다
			var step = presenter.PrepareClose();
			Publish(new PendingChange(instanceId, presenter, step, false, destroy));
			return true;
		}

		private void Publish(PendingChange change)
		{
			_pendingChanges.Enqueue(change);

			if (_publishing)
			{
				return;
			}

			_publishing = true;

			try
			{
				while (_pendingChanges.Count > 0)
				{
					var next = _pendingChanges.Dequeue();
					_notifying = next;
					_notifyingNotRun = true;

					if (!_disposed)
					{
						RefreshPresentation();
						Raise(next.Opened ? PresenterOpened : PresenterClosed, next.Id);
					}

					// 알림 안에서 Dispose 했으면 이미 실행됐습니다
					if (_notifyingNotRun)
					{
						_notifyingNotRun = false;
						Run(next);
					}
				}
			}
			finally
			{
				_publishing = false;
				_notifyingNotRun = false;
				_notifying = default;
			}
		}

		// 정렬과 dim 갱신의 예외(프레젠터 IsModal 재정의 등)가 남은 변경 처리를 멈추지 않도록 기록하고 이어 갑니다
		private void RefreshPresentation()
		{
			try
			{
				ApplyToolkitOrder();
				ApplyCanvasOrder();
				RefreshModalDim();
			}
			catch (Exception exception)
			{
				Logger.Error(exception, "Refreshing the UI order or modal dim threw");
			}
		}

		// 처리 중인 알림이나 훅 안에서 Dispose 하면, 알림 중인 변경과 대기 중인 닫기를 언로드 전에 실행합니다
		private void RunPendingChangesForDispose()
		{
			if (_notifyingNotRun)
			{
				_notifyingNotRun = false;
				Run(_notifying);
			}

			while (_pendingChanges.Count > 0)
			{
				Run(_pendingChanges.Dequeue());
			}
		}

		private void Run(PendingChange change)
		{
			// 표시 목록에서 바로 뺀 경우(RemoveUi)는 실행할 생명주기가 없습니다
			if (ReferenceEquals(change.Presenter, null))
			{
				return;
			}

			if (!change.Opened)
			{
				change.Presenter.RunClose(change.Step, change.Destroy);
				return;
			}

			// 알림 중에 언로드되거나 서비스에서 빠졌으면 열지 않고 이번 태스크만 완료합니다
			if (_disposed || !TryFindPresenter(change.Id.PresenterType, change.Id.InstanceAddress, out var registered) ||
				!ReferenceEquals(registered, change.Presenter))
			{
				change.Presenter.Abandon(change.Step);
				return;
			}

			change.Presenter.RunOpen(change.Step);

			// 막힌 동안 새로 붙은 패널도 곧바로 막습니다
			if (IsInputBlocked)
			{
				ReapplyInputBlock();
			}
		}

		// 서비스에서 프레젠터를 뺍니다. 표시 중이었으면 닫힘을 알립니다.
		// 빠진 등록의 진행 중인 열기·닫기 완료 소스를 알림 전에 떼어 돌려주므로, 알림에서 다시 등록해 준비한 단계는 섞이지 않습니다
		private bool RemoveRegistration(Type type, string instanceAddress, out UniTaskCompletionSource[] detached)
		{
			var instanceId = new UiInstanceId(type, instanceAddress);
			var wasVisible = _visibleUiList.Remove(instanceId);
			UiPresenter removedPresenter = null;
			detached = Array.Empty<UniTaskCompletionSource>();

			// 인스턴스를 찾아 제거합니다
			if (_uiPresenters.TryGetValue(type, out var instanceList))
			{
				for (int i = 0; i < instanceList.Count; i++)
				{
					if (instanceList[i].Address == instanceAddress)
					{
						removedPresenter = instanceList[i].Presenter;
						instanceList.RemoveAt(i);
						_uiLayers.Remove(instanceId);

						// 빈 타입 항목을 정리합니다
						if (instanceList.Count == 0)
						{
							_uiPresenters.Remove(type);
						}

						break;
					}
				}
			}

			if (!ReferenceEquals(removedPresenter, null))
			{
				// 실행 중인 열기·닫기가 빠진 객체를 계속 다루지 않도록 알림보다 먼저 무효화합니다
				detached = removedPresenter.DetachLifecycle();
				RemoveToolkitPresenter(removedPresenter);
				RemoveCanvasPresenter(removedPresenter);
			}

			// 상태를 모두 정리한 뒤 알립니다
			if (wasVisible)
			{
				Publish(new PendingChange(instanceId, null, default, false, false));
			}

			return !ReferenceEquals(removedPresenter, null);
		}

		private void PrepareNavigation(UiPresenter presenter)
		{
			// 프리팹 루트의 Canvas 만 정렬합니다. 자식 Canvas 는 손대지 않습니다
			if (presenter.TryGetComponent<Canvas>(out var canvas))
			{
				_canvasPresenters.Add(new CanvasPresenter(presenter, canvas));
			}

			if (!presenter.TryGetComponent<UIDocument>(out var document))
			{
				return;
			}

			_toolkitPresenters.Add(new ToolkitPresenter(presenter, document));

			// EventSystem 없이 입력을 받는 UI Toolkit 도 막을 수 있게 차단 패널을 준비합니다
			_inputBlocker.EnsureToolkitBlocker();
		}

		private void RemoveToolkitPresenter(UiPresenter presenter)
		{
			for (var i = 0; i < _toolkitPresenters.Count; i++)
			{
				if (ReferenceEquals(_toolkitPresenters[i].Presenter, presenter))
				{
					_toolkitPresenters.RemoveAt(i);
					return;
				}
			}
		}

		private void RemoveCanvasPresenter(UiPresenter presenter)
		{
			for (var i = 0; i < _canvasPresenters.Count; i++)
			{
				if (ReferenceEquals(_canvasPresenters[i].Presenter, presenter))
				{
					_canvasPresenters.RemoveAt(i);
					return;
				}
			}
		}

		// 복사해 둔 뒤 닫았다 다시 열거나 다른 객체로 바뀐 인스턴스는 닫지 않습니다
		private void TryCloseSnapshot(VisibleSnapshot entry)
		{
			if (!TryFindPresenter(entry.Id.PresenterType, entry.Id.InstanceAddress, out var presenter) ||
				!ReferenceEquals(presenter, entry.Presenter) || presenter.LifecycleVersion != entry.Version)
			{
				return;
			}

			TryCloseVisible(entry.Id, false);
		}

		private VisibleSnapshot[] CopyVisibleList()
		{
			var snapshot = new VisibleSnapshot[_visibleUiList.Count];
			for (var i = 0; i < snapshot.Length; i++)
			{
				var instanceId = _visibleUiList[i];
				TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter);
				snapshot[i] = new VisibleSnapshot(instanceId, presenter, presenter != null ? presenter.LifecycleVersion : 0);
			}

			return snapshot;
		}

		// UIDocument 는 정렬값이 float 라 같은 레이어 안에서도 연 순서대로 그려지게 소수 단계를 매깁니다.
		// 같은 정렬값끼리의 앞뒤는 Unity 가 정하므로(다시 켜도 바뀌지 않음, 실험으로 확인) 이렇게 해야 화면 맨 위 규칙과 맞습니다.
		// 닫기 전환 중이라 아직 그려지는 문서도 연 순번대로 함께 매겨, 새로 연 문서보다 위에 남지 않게 합니다
		private void ApplyToolkitOrder()
		{
			_toolkitOrder.Clear();

			for (var i = 0; i < _toolkitPresenters.Count; i++)
			{
				var entry = _toolkitPresenters[i];
				if (entry.Presenter == null || entry.Document == null)
				{
					continue;
				}

				var instanceId = new UiInstanceId(entry.Presenter.GetType(), entry.Presenter.InstanceAddress);
				if (!entry.Presenter.gameObject.activeSelf && !_visibleUiList.Contains(instanceId))
				{
					continue;
				}

				_toolkitOrder.Add(new ToolkitEntry(entry.Document, GetSortingLayer(instanceId), entry.Presenter.OpenSequence));
			}

			_toolkitOrder.Sort(ByLayerThenSequence);

			var layer = 0;
			var rank = 0;
			for (var i = 0; i < _toolkitOrder.Count; i++)
			{
				var entry = _toolkitOrder[i];
				if (i == 0 || entry.Layer != layer)
				{
					layer = entry.Layer;
					rank = 0;
				}

				rank++;
				entry.Document.sortingOrder = layer + rank * UiModalDim.ToolkitOrderStep;
			}
		}

		// uGUI 도 같은 sortingOrder 캔버스끼리의 앞뒤는 Unity 가 정하고(형제 순서와 무관, 공식 보장 없음) 닫기 전환 중 다시 연
		// 캔버스처럼 꺼진 적 없는 캔버스는 아래에 남으므로(실험으로 확인), 루트 Canvas 에 '레이어 + 레이어 안의 순위'를 매겨
		// 화면 맨 위 규칙과 맞춥니다. 대상과 순위는 UI Toolkit 과 같습니다
		private void ApplyCanvasOrder()
		{
			_canvasOrder.Clear();

			for (var i = 0; i < _canvasPresenters.Count; i++)
			{
				var entry = _canvasPresenters[i];
				if (entry.Presenter == null || entry.Canvas == null)
				{
					continue;
				}

				var instanceId = new UiInstanceId(entry.Presenter.GetType(), entry.Presenter.InstanceAddress);
				if (!entry.Presenter.gameObject.activeSelf && !_visibleUiList.Contains(instanceId))
				{
					continue;
				}

				_canvasOrder.Add(new CanvasEntry(entry.Canvas, GetSortingLayer(instanceId), entry.Presenter.OpenSequence));
			}

			_canvasOrder.Sort(CanvasByLayerThenSequence);

			var start = 0;
			while (start < _canvasOrder.Count)
			{
				var layer = _canvasOrder[start].Layer;
				var end = start + 1;
				while (end < _canvasOrder.Count && _canvasOrder[end].Layer == layer)
				{
					end++;
				}

				AssignCanvasOrders(start, end - start, layer);
				start = end;
			}
		}

		private void AssignCanvasOrders(int first, int count, int layer)
		{
			var limit = GetCanvasOrderLimit(layer);
			var start = GetCanvasOrderStart(layer, limit);
			var slots = GetCanvasOrderSlots(start, limit, _canvasOrderStep);

			if ((start != layer || count > slots) && _canvasGapWarnedLayers.Add(layer))
			{
				Logger.Warning(start != layer
					? $"레이어 {layer} 는 uGUI 정렬값으로 쓸 수 있는 범위({short.MinValue}~{limit}, 입력 차단막 {UiInputBlocker.SortingOrder} 아래) 밖이라 {start} 부터 매깁니다. " +
					  "범위 밖 레이어의 uGUI 는 다른 레이어와 같은 정렬값을 쓸 수 있습니다. 레이어를 범위 안으로 옮기세요."
					: $"레이어 {layer} 에 uGUI 프레젠터 {count} 개가 그려지는데 다음 레이어까지 간격 {_canvasOrderStep} 으로 정렬값이 {slots} 개뿐이라 먼저 연 {count - slots + 1} 개가 같은 정렬값({start})을 씁니다. " +
					  "같은 정렬값 캔버스끼리는 앞뒤가 정해지지 않아 dim 이 일부를 가리지 못하거나 뒤로가기 순서가 화면과 다를 수 있습니다. 레이어 사이를 더 띄워 두세요.");
			}

			for (var i = 0; i < count; i++)
			{
				_canvasOrder[first + i].Canvas.sortingOrder = GetCanvasOrder(layer, limit, count, i, _canvasOrderStep);
			}
		}

		// UiConfigs 에서 이 레이어 다음으로 높은 레이어의 바로 아래 값입니다. 없으면 입력 차단막 바로 아래이고, Canvas 범위(-32768)보다 작아지지 않습니다
		internal int GetCanvasOrderLimit(int layer)
		{
			var limit = UiInputBlocker.SortingOrder - 1;
			foreach (var config in _uiConfigs.Values)
			{
				if (config.Layer > layer && config.Layer - 1 < limit)
				{
					limit = config.Layer - 1;
				}
			}

			return Math.Max(limit, short.MinValue);
		}

		// 레이어의 첫(먼저 연) 캔버스 값입니다. Canvas 범위(-32768 부터)와 limit 안으로 넣습니다
		internal static int GetCanvasOrderStart(int layer, int limit)
		{
			return Math.Min(Math.Max(layer, short.MinValue), limit);
		}

		// 시작 값부터 limit 까지 step 간격으로 쓸 수 있는 정렬값 수입니다
		internal static int GetCanvasOrderSlots(int start, int limit, int step)
		{
			return (limit - start) / step + 1;
		}

		// 한 레이어의 count 개 캔버스 중 연 순번으로 index 번째의 값입니다. 시작 값부터 step 씩 오르고 limit 을 넘지 않으며,
		// 값이 모자라면 먼저 연 캔버스들이 시작 값을 같이 써서 나중에 연 캔버스는 계속 위에 그려집니다
		internal static int GetCanvasOrder(int layer, int limit, int count, int index, int step = 1)
		{
			var start = GetCanvasOrderStart(layer, limit);
			var shared = Math.Max(0, count - GetCanvasOrderSlots(start, limit, step));
			return start + Math.Max(0, index - shared) * step;
		}

		private void RefreshModalDim()
		{
			_modalDim.Show(FindTopmostVisible(IsModalPresenter));
		}

		private static void Raise(Action<UiInstanceId> handlers, UiInstanceId instanceId)
		{
			if (handlers == null)
			{
				return;
			}

			// 한 구독자의 예외가 다른 구독자(팝업 큐 등)에게 가는 알림을 막지 않도록 따로 부릅니다
			foreach (var handler in handlers.GetInvocationList())
			{
				try
				{
					((Action<UiInstanceId>)handler)(instanceId);
				}
				catch (Exception exception)
				{
					Logger.Error(exception, $"A visibility handler threw for {instanceId}");
				}
			}
		}

		private readonly struct PendingChange
		{
			public readonly UiInstanceId Id;
			public readonly UiPresenter Presenter;
			public readonly LifecycleStep Step;
			public readonly bool Opened;
			public readonly bool Destroy;

			public PendingChange(UiInstanceId id, UiPresenter presenter, LifecycleStep step, bool opened, bool destroy)
			{
				Id = id;
				Presenter = presenter;
				Step = step;
				Opened = opened;
				Destroy = destroy;
			}
		}

		private readonly struct VisibleSnapshot
		{
			public readonly UiInstanceId Id;
			public readonly UiPresenter Presenter;
			public readonly int Version;

			public VisibleSnapshot(UiInstanceId id, UiPresenter presenter, int version)
			{
				Id = id;
				Presenter = presenter;
				Version = version;
			}
		}

		private readonly struct ToolkitPresenter
		{
			public readonly UiPresenter Presenter;
			public readonly UIDocument Document;

			public ToolkitPresenter(UiPresenter presenter, UIDocument document)
			{
				Presenter = presenter;
				Document = document;
			}
		}

		private readonly struct ToolkitEntry
		{
			public readonly UIDocument Document;
			public readonly int Layer;
			public readonly long Sequence;

			public ToolkitEntry(UIDocument document, int layer, long sequence)
			{
				Document = document;
				Layer = layer;
				Sequence = sequence;
			}
		}

		private readonly struct CanvasPresenter
		{
			public readonly UiPresenter Presenter;
			public readonly Canvas Canvas;

			public CanvasPresenter(UiPresenter presenter, Canvas canvas)
			{
				Presenter = presenter;
				Canvas = canvas;
			}
		}

		private readonly struct CanvasEntry
		{
			public readonly Canvas Canvas;
			public readonly int Layer;
			public readonly long Sequence;

			public CanvasEntry(Canvas canvas, int layer, long sequence)
			{
				Canvas = canvas;
				Layer = layer;
				Sequence = sequence;
			}
		}
	}
}
