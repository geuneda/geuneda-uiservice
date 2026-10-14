using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 표시 상태 이벤트, 뒤로가기, 입력 차단, 모달 dim 을 담당하는 <see cref="UiService"/> 의 나머지 부분입니다.
	/// </summary>
	public partial class UiService
	{
		private static readonly Func<UiPresenter, bool> IsBackTarget = presenter => presenter.InternalHandlesBack;
		private static readonly Func<UiPresenter, bool> IsModalPresenter = presenter => presenter.InternalIsModal;

		// 우선순위가 높은 것부터, 같으면 나중에 추가한 것부터 정렬해 둡니다
		private readonly List<BackHandlerRegistration> _backHandlers = new List<BackHandlerRegistration>();
		private readonly UiInputBlocker _inputBlocker;
		private readonly UiModalDim _modalDim;
		private readonly UiFocusBlocker _focusBlocker = new UiFocusBlocker();
		private UiBackInputReader _backInputReader;
		private UiInputBlockWatcher _inputBlockWatcher;
		private bool _readsBackInput;

		/// <inheritdoc />
		public event Action<UiInstanceId> PresenterOpened;

		/// <inheritdoc />
		public event Action<UiInstanceId> PresenterClosed;

		/// <inheritdoc />
		public event Action BackUnhandled;

		/// <inheritdoc />
		public bool IsInputBlocked => _inputBlocker.IsBlocked;

		/// <inheritdoc />
		public IReadOnlyList<string> InputBlockReasons => _inputBlocker.Reasons;

		/// <summary>
		/// 모달 뒤에 까는 공용 dim 의 색입니다. 기본값은 검정 60% 입니다.
		/// </summary>
		public Color DimColor
		{
			get => _modalDim.Color;
			set => _modalDim.Color = value;
		}

		/// <summary>
		/// true 면 서비스가 매 프레임 Escape 키(Android 뒤로가기 포함)를 읽어 <see cref="HandleBack"/> 를 부릅니다. 기본값은 false 입니다.
		/// Input System 패키지가 있고 활성화돼 있으면 그것을, 아니면 레거시 Input Manager 를 씁니다. Init 전후 언제든 바꿀 수 있습니다.
		/// 게임이 직접 입력을 받아 <see cref="HandleBack"/> 를 부를 때는 끄고 둡니다.
		/// </summary>
		public bool ReadsBackInput
		{
			get => _readsBackInput;
			set
			{
				_readsBackInput = value;
				ApplyBackInputReader();
			}
		}

		/// <inheritdoc />
		public bool TryGetUi(UiInstanceId instanceId, out UiPresenter presenter)
		{
			return TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out presenter);
		}

		/// <inheritdoc />
		public UiInputBlock BlockInput(string reason)
		{
			return _inputBlocker.Block(reason);
		}

		/// <inheritdoc />
		public IDisposable AddBackHandler(Func<bool> handler, int priority = 0)
		{
			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler));
			}

			var registration = new BackHandlerRegistration(this, handler, priority);
			var index = 0;

			// 같은 우선순위의 기존 핸들러보다 앞에 넣어 나중에 추가한 핸들러가 먼저 불리게 합니다
			while (index < _backHandlers.Count && _backHandlers[index].Priority > priority)
			{
				index++;
			}

			_backHandlers.Insert(index, registration);

			return registration;
		}

		/// <inheritdoc />
		public bool HandleBack()
		{
			if (_disposed || IsInputBlocked)
			{
				return false;
			}

			// 핸들러가 핸들러를 추가하거나 제거할 수 있으므로 복사본을 돕니다
			var handlers = _backHandlers.ToArray();
			foreach (var registration in handlers)
			{
				if (registration.IsRegistered && registration.Handler())
				{
					return true;
				}
			}

			var target = FindTopmostVisible(IsBackTarget);
			if (target != null)
			{
				target.InternalBackPressed();
				return true;
			}

			BackUnhandled?.Invoke();
			return false;
		}

		/// <summary>
		/// 표시 중인 프레젠터 중 조건에 맞고 화면 맨 위에 있는 것을 찾습니다.
		/// 레이어가 높을수록 위이고, 같은 레이어면 나중에 연 프레젠터가 위입니다.
		/// </summary>
		private UiPresenter FindTopmostVisible(Func<UiPresenter, bool> match)
		{
			UiPresenter topmost = null;
			var topmostLayer = 0;

			for (var i = 0; i < _visibleUiList.Count; i++)
			{
				var instanceId = _visibleUiList[i];
				if (!TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter) ||
					presenter == null || !match(presenter))
				{
					continue;
				}

				var layer = GetSortingLayer(instanceId);

				// 표시 목록은 연 순서라 같은 레이어면 뒤쪽이 이깁니다
				if (topmost == null || layer >= topmostLayer)
				{
					topmost = presenter;
					topmostLayer = layer;
				}
			}

			return topmost;
		}

		// 정렬에 실제로 쓴 레이어(AddUi 에 넘긴 값)를 돌려줍니다
		private int GetSortingLayer(UiInstanceId instanceId)
		{
			if (_uiLayers.TryGetValue(instanceId, out var layer))
			{
				return layer;
			}

			return _uiConfigs.TryGetValue(instanceId.PresenterType, out var config) ? config.Layer : 0;
		}

		private void AttachNavigation(Transform uiParent)
		{
			_inputBlocker.Attach(uiParent);
			_modalDim.Attach(uiParent);
			_inputBlockWatcher = uiParent.gameObject.AddComponent<UiInputBlockWatcher>();
			_inputBlockWatcher.Tick = ReapplyInputBlock;
			_inputBlockWatcher.enabled = IsInputBlocked;
			ApplyBackInputReader();
		}

		private void DestroyNavigation()
		{
			PresenterOpened = null;
			PresenterClosed = null;
			BackUnhandled = null;

			foreach (var registration in _backHandlers)
			{
				registration.IsRegistered = false;
			}

			_backHandlers.Clear();
			_inputBlocker.Destroy();
			_focusBlocker.Resume();
			_modalDim.Destroy();
			_backInputReader = null;
			_inputBlockWatcher = null;
		}

		// 차단막은 포인터만 가리므로 선택·포커스로 들어오는 키보드·게임패드 입력은 따로 막습니다.
		// 막힌 동안에는 매 프레임 새로 생긴 EventSystem 과 새로 붙은 패널에도 적용합니다
		private void OnInputBlockChanged(bool blocked)
		{
			if (_inputBlockWatcher != null)
			{
				_inputBlockWatcher.enabled = blocked;
			}

			if (blocked)
			{
				ReapplyInputBlock();
			}
			else
			{
				_focusBlocker.Resume();
			}
		}

		private void ReapplyInputBlock()
		{
			if (!IsStillBlocked())
			{
				return;
			}

			_focusBlocker.SuspendSelection(UnityEngine.EventSystems.EventSystem.current);

			for (var i = 0; i < _toolkitPresenters.Count; i++)
			{
				// 선택 해제(OnDeselect)와 포커스 해제(Blur) 콜백이 마지막 토큰을 풀 수 있으므로 매번 다시 확인합니다
				if (!IsStillBlocked())
				{
					return;
				}

				var document = _toolkitPresenters[i].Document;
				if (document != null && document.isActiveAndEnabled)
				{
					_focusBlocker.SuspendPanel(document.rootVisualElement?.panel);
				}
			}

			IsStillBlocked();
		}

		// 콜백 도중 풀렸으면, 풀린 뒤 다시 건 것까지 되돌립니다
		private bool IsStillBlocked()
		{
			if (!_disposed && IsInputBlocked)
			{
				return true;
			}

			_focusBlocker.Resume();
			return false;
		}

		private void OnDimClicked()
		{
			var host = _modalDim.Host;
			if (host != null)
			{
				host.InternalDimClicked();
			}
		}

		private void ApplyBackInputReader()
		{
			if (_uiParent == null)
			{
				return;
			}

			if (_readsBackInput && _backInputReader == null)
			{
				_backInputReader = _uiParent.gameObject.AddComponent<UiBackInputReader>();
				_backInputReader.Service = this;
			}

			if (_backInputReader != null)
			{
				_backInputReader.enabled = _readsBackInput;
			}
		}

		private sealed class BackHandlerRegistration : IDisposable
		{
			private readonly UiService _owner;

			public BackHandlerRegistration(UiService owner, Func<bool> handler, int priority)
			{
				_owner = owner;
				Handler = handler;
				Priority = priority;
				IsRegistered = true;
			}

			public Func<bool> Handler { get; }
			public int Priority { get; }
			public bool IsRegistered { get; set; }

			public void Dispose()
			{
				if (!IsRegistered)
				{
					return;
				}

				IsRegistered = false;
				_owner._backHandlers.Remove(this);
			}
		}
	}
}
