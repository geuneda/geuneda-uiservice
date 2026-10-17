using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using Geuneda.Core;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// <see cref="UiPresenter"/>를 <see cref="UiPresenterData{T}"/>로 태그하여
	/// <see cref="UiService"/>를 통해 UI를 열 때 특정 상태를 정의할 수 있게 합니다
	/// </summary>
	public interface IUiPresenterData
	{
	}

	/// <summary>
	/// <seealso cref="IUiService"/>의 UI 프레젠터 최상위 기본 클래스입니다.
	/// 올바른 UI 생명주기를 실행하려면 이 추상 클래스를 구현하세요
	/// </summary>
	public abstract class UiPresenter : MonoBehaviour
	{
		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.Presenter");

		protected IUiService _uiService;
		private List<PresenterFeatureBase> _features;
		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;
		// 열기·닫기마다 올라갑니다. 전환을 기다리는 동안 반대 동작이 끼어들었는지 확인하는 데 씁니다
		private int _lifecycleVersion;
		// 아직 끝나지 않은 열기·닫기의 완료 소스. 서비스에서 빠질 때 이전 단계까지 모두 완료하는 데 씁니다
		private readonly List<UniTaskCompletionSource> _inFlight = new List<UniTaskCompletionSource>();

		/// <summary>
		/// 이 프레젠터 인스턴스를 고유하게 식별하는 인스턴스 주소입니다.
		/// 기본/싱글턴 인스턴스의 경우 빈 문자열입니다.
		/// </summary>
		internal string InstanceAddress { get; private set; } = string.Empty;

		/// <summary>
		/// <see cref="UiPresenter"/>의 열림 상태를 요청합니다
		/// </summary>
		public bool IsOpen => gameObject.activeSelf;

		/// <summary>
		/// 열기 전환이 완료되면 완료되는 태스크입니다.
		/// UI가 완전히 준비될 때까지 대기해야 하는 외부 코드에 유용합니다.
		/// </summary>
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <summary>
		/// 닫기 전환이 완료되면 완료되는 태스크입니다.
		/// UI가 완전히 닫힐 때까지 대기해야 하는 외부 코드에 유용합니다.
		/// </summary>
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <summary>
		/// UI 프레젠터 구현체가 서비스를 직접 호출하지 않고도 UI 프레젠터를 직접 닫을 수 있게 합니다
		/// </summary>
		protected void Close(bool destroy)
		{
			_uiService.CloseUi(GetType(), InstanceAddress, destroy);
		}

		/// <summary>
		/// UI 프레젠터 구현체가 초기화될 때 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnInitialized() {}

		/// <summary>
		/// UI 프레젠터 구현체가 열릴 때 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnOpened() {}

		/// <summary>
		/// UI 프레젠터 구현체가 닫힐 때 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnClosed() {}

		/// <summary>
		/// 모든 열기 전환(애니메이션, 딜레이)이 완료된 후 호출됩니다.
		/// UI가 완전히 보이고 상호작용할 준비가 되었을 때 반응하려면 이 메서드를 재정의하세요.
		/// 전환 기능이 없는 프레젠터에서도 항상 호출됩니다.
		/// </summary>
		protected virtual void OnOpenTransitionCompleted() {}

		/// <summary>
		/// 모든 닫기 전환(애니메이션, 딜레이)이 완료된 후 호출됩니다.
		/// UI가 닫기 전환을 완료했을 때 반응하려면 이 메서드를 재정의하세요.
		/// 전환 기능이 없는 프레젠터에서도 항상 호출됩니다.
		/// </summary>
		protected virtual void OnCloseTransitionCompleted() {}

		/// <summary>
		/// 뒤로가기 대상인지 여부입니다. 기본값은 false 라 HUD 같은 화면은 뒤로가기로 닫히지 않습니다.
		/// 팝업은 true 로 재정의합니다. 뒤로가기는 표시 중인 대상 중 레이어가 가장 높고, 같은 레이어면 나중에 연 프레젠터로 갑니다.
		/// </summary>
		protected virtual bool HandlesBack => false;

		/// <summary>
		/// 이 프레젠터가 뒤로가기를 받았을 때 호출됩니다. 기본 동작은 <see cref="Close"/>(false) 입니다.
		/// 닫을 수 없는 팝업은 재정의해 아무것도 하지 않거나 안내를 띄웁니다. 어느 쪽이든 뒤로가기 처리는 여기서 끝납니다.
		/// </summary>
		protected virtual void OnBackPressed()
		{
			Close(false);
		}

		/// <summary>
		/// 모달 여부입니다. true 면 표시 중인 모달 중 맨 위 하나의 바로 뒤에 서비스의 공용 dim 이 깔리고,
		/// dim 이 그 아래 UI 의 입력을 막습니다. 루트에 <see cref="Canvas"/> 나 UIDocument 가 있어야 합니다.
		/// </summary>
		protected virtual bool IsModal => false;

		/// <summary>
		/// 이 프레젠터 뒤에 깔린 dim 을 누르면 호출됩니다. 기본 동작은 없습니다.
		/// 바깥을 눌러 닫으려면 재정의해 <see cref="Close"/> 를 부릅니다.
		/// </summary>
		protected virtual void OnDimClicked() {}

		internal void Init(IUiService uiService, string instanceAddress)
		{
			_uiService = uiService;
			InstanceAddress = instanceAddress ?? string.Empty;
			InitializeFeatures();
			OnInitialized();
		}

		internal bool InternalHandlesBack => HandlesBack;

		internal bool InternalIsModal => IsModal;

		internal void InternalBackPressed()
		{
			OnBackPressed();
		}

		internal void InternalDimClicked()
		{
			OnDimClicked();
		}

		/// <summary>
		/// 이 프레젠터의 열기·닫기 세대입니다. 열기·닫기를 준비할 때마다 올라갑니다.
		/// </summary>
		internal int LifecycleVersion => _lifecycleVersion;

		/// <summary>
		/// 서비스가 열 때마다 매기는 순번입니다. UI Toolkit 문서를 연 순서대로 그리는 데 씁니다.
		/// </summary>
		internal long OpenSequence { get; set; }

		/// <summary>
		/// 열기를 준비합니다. 세대를 올리고 이번 열기의 완료 소스를 만들어, 표시 상태 이벤트 구독자가 새 전환 태스크를 보게 합니다.
		/// </summary>
		internal LifecycleStep PrepareOpen()
		{
			var completion = new UniTaskCompletionSource();
			_openTransitionCompletion = completion;
			_inFlight.Add(completion);
			return new LifecycleStep(++_lifecycleVersion, completion);
		}

		/// <summary>
		/// 닫기를 준비합니다. <see cref="PrepareOpen"/> 과 같은 규칙입니다.
		/// </summary>
		internal LifecycleStep PrepareClose()
		{
			var completion = new UniTaskCompletionSource();
			_closeTransitionCompletion = completion;
			_inFlight.Add(completion);
			return new LifecycleStep(++_lifecycleVersion, completion);
		}

		internal void RunOpen(LifecycleStep step)
		{
			InternalOpenProcessAsync(step).Forget();
		}

		internal void RunClose(LifecycleStep step, bool destroy)
		{
			InternalCloseProcessAsync(step, destroy).Forget();
		}

		internal void InternalOpen()
		{
			RunOpen(PrepareOpen());
		}

		internal void InternalClose(bool destroy)
		{
			RunClose(PrepareClose(), destroy);
		}

		/// <summary>
		/// 서비스에서 빠질 때 부릅니다. 실행 중인 열기·닫기가 다음 확인에서 멈추도록 세대를 올리고,
		/// 지금까지 준비한(이미 새 단계로 교체된 이전 단계 포함) 완료 소스 묶음을 떼어 돌려줍니다.
		/// 이후 다시 등록해 준비한 단계는 새 세대와 새 묶음에 속하므로 영향이 없습니다.
		/// </summary>
		internal UniTaskCompletionSource[] DetachLifecycle()
		{
			_lifecycleVersion++;

			if (_inFlight.Count == 0)
			{
				return Array.Empty<UniTaskCompletionSource>();
			}

			var detached = _inFlight.ToArray();
			_inFlight.Clear();
			return detached;
		}

		/// <summary>
		/// <see cref="DetachLifecycle"/> 로 뗀 태스크를 완료해, 그것을 기다리던 쪽(팝업 큐 등)이 멈추지 않게 합니다.
		/// </summary>
		internal static void CompleteDetached(UniTaskCompletionSource[] detached)
		{
			foreach (var completion in detached)
			{
				completion.TrySetResult();
			}
		}

		/// <summary>
		/// 실행하지 않기로 한 단계의 태스크를 완료합니다.
		/// </summary>
		internal void Abandon(LifecycleStep step)
		{
			Complete(step);
		}

		private void Complete(LifecycleStep step)
		{
			_inFlight.Remove(step.Completion);
			step.Completion.TrySetResult();
		}

		// 훅 예외는 버그이므로 태스크를 원래 예외로 실패시키고 한 번 기록합니다.
		// 실패시킨 태스크를 여기서 한 번 관찰해 두어, 아무도 기다리지 않아도 UniTask 가 GC 시점에 다시 기록하지 않게 합니다.
		// 기다리는 다른 쪽(팝업 큐, 게임 코드)도 같은 예외를 받습니다
		private void Fail(LifecycleStep step, Exception exception)
		{
			_inFlight.Remove(step.Completion);

			if (step.Completion.TrySetException(exception))
			{
				try
				{
					step.Completion.Task.GetAwaiter().GetResult();
				}
				catch (Exception)
				{
					// 관찰 표시만 하고, 기록은 아래에서 한 번 합니다
				}
			}

			Logger.Error(exception, $"{GetType().Name} open or close step failed");
		}


		// 살아 있고, 이 단계 뒤로 다른 열기·닫기가 준비되지 않았는지 확인합니다
		private bool IsCurrent(LifecycleStep step)
		{
			return this && step.Version == _lifecycleVersion;
		}

		private async UniTask InternalOpenProcessAsync(LifecycleStep step)
		{
			// 이 단계에서 시작한 피처 전환과 그 예외를 관찰했는지. 정상 대기, 일찍 끝내기, 예외 종료가 모두 이것만 씁니다
			UniTask[] started = null;
			// 단계 시작 때 전환 피처들의 태스크. 이 값과 같으면 이번 단계에서 시작한 전환이 아니므로 다시 읽지 않습니다
			UniTask[] before = null;
			var notified = 0;
			// 피처 콜백을 부르는 중이었는지. 이때 던졌으면 그 피처도 태스크가 바뀐 경우에만 담습니다
			var inCallback = false;
			var observed = false;

			try
			{
				before = SnapshotTransitions(true);

				// 사용자 훅과 피처 콜백이 반대 동작을 하거나 파괴하면 그 뒤 단계는 진행하지 않고 이번 태스크만 완료합니다
				if (!IsCurrent(step) || !NotifyFeaturesOpening(step))
				{
					Complete(step);
					return;
				}

				gameObject.SetActive(true);

				if (!IsCurrent(step))
				{
					Complete(step);
					return;
				}

				OnOpened();

				if (!IsCurrent(step))
				{
					Complete(step);
					return;
				}

				// 기본 피처는 열린 알림에서 열기 전환을 시작하므로, 알린 피처의 태스크만 이 단계의 것으로 고정합니다.
				// 콜백이 던져도 그때까지 알린 피처를 알 수 있게 순서대로 셉니다. 반대 동작을 하면 이후 피처에는 알리지 않습니다
				while (_features != null && notified < _features.Count)
				{
					inCallback = true;
					_features[notified].OnPresenterOpened();
					inCallback = false;
					notified++;

					if (!IsCurrent(step))
					{
						break;
					}
				}

				started = CaptureTransitions(true, notified, before, out var statusError);

				// 새 태스크의 상태를 읽을 수 없으면(예: UniTask.Defer 팩토리 예외) 나머지 전환을 관찰로 넘기고 그 오류로 단계를 실패시킵니다
				if (statusError != null)
				{
					observed = true;
					UiTransitionTasks.ObserveTransitions(started);
					ExceptionDispatchInfo.Capture(statusError).Throw();
				}

				if (!IsCurrent(step))
				{
					observed = true;
					UiTransitionTasks.ObserveTransitions(started);
					Complete(step);
					return;
				}

				// 모든 기능의 열기 전환을 대기합니다
				observed = true;
				await UiTransitionTasks.WaitForTransitionsAsync(started);

				// 전환 중에 닫혔거나 파괴됐으면 열기 완료를 알리지 않습니다
				if (IsCurrent(step))
				{
					OnOpenTransitionCompleted();
				}

				Complete(step);
			}
			catch (Exception exception)
			{
				// 관찰이 다시 실패해도 원래 단계는 반드시 실패로 끝냅니다
				try
				{
					if (!observed)
					{
						UiTransitionTasks.ObserveTransitions(started ?? CaptureAfterFailure(true, inCallback ? notified + 1 : notified, before));
					}
				}
				finally
				{
					Fail(step, exception);
				}
			}
		}

		private async UniTask InternalCloseProcessAsync(LifecycleStep step, bool destroy)
		{
			UniTask[] started = null;
			// 단계 시작 때 전환 피처들의 태스크. 이 값과 같으면 이번 단계에서 시작한 전환이 아니므로 다시 읽지 않습니다
			UniTask[] before = null;
			var notified = 0;
			// 피처 콜백을 부르는 중이었는지. 이때 던졌으면 그 피처도 태스크가 바뀐 경우에만 담습니다
			var inCallback = false;
			var observed = false;

			try
			{
				if (!IsCurrent(step))
				{
					Complete(step);
					return;
				}

				before = SnapshotTransitions(false);

				// 기본 피처는 닫는 중 알림에서 닫기 전환을 시작하므로, 알린 피처의 태스크만 이 단계의 것으로 고정합니다.
				// 콜백이 던져도 그때까지 알린 피처를 알 수 있게 순서대로 셉니다. 반대 동작을 하면 이후 피처에는 알리지 않습니다
				while (_features != null && notified < _features.Count)
				{
					inCallback = true;
					_features[notified].OnPresenterClosing();
					inCallback = false;
					notified++;

					if (!IsCurrent(step))
					{
						break;
					}
				}

				started = CaptureTransitions(false, notified, before, out var statusError);

				// 새 태스크의 상태를 읽을 수 없으면(예: UniTask.Defer 팩토리 예외) 나머지 전환을 관찰로 넘기고 그 오류로 단계를 실패시킵니다
				if (statusError != null)
				{
					observed = true;
					UiTransitionTasks.ObserveTransitions(started);
					ExceptionDispatchInfo.Capture(statusError).Throw();
				}

				if (!IsCurrent(step))
				{
					observed = true;
					UiTransitionTasks.ObserveTransitions(started);
					Complete(step);
					return;
				}

				// 시작 훅이 이미 실패했으면 다음 사용자 훅으로 넘어가기 전에 이 단계를 실패시킵니다
				if (UiTransitionTasks.HasFaulted(started))
				{
					observed = true;
					UiTransitionTasks.ThrowFirstFault(started);
				}

				OnClosed();

				if (!IsCurrent(step) || !NotifyFeaturesClosed(step))
				{
					observed = true;
					UiTransitionTasks.ObserveTransitions(started);
					Complete(step);
					return;
				}

				// 모든 기능의 닫기 전환을 대기합니다
				observed = true;
				await UiTransitionTasks.WaitForTransitionsAsync(started);

				// 전환 중에 다시 열렸거나 파괴됐으면 숨기거나 언로드하지 않습니다
				if (!IsCurrent(step))
				{
					Complete(step);
					return;
				}

				// 여기서 항상 숨김 - 단일 책임 지점
				gameObject.SetActive(false);

				// 숨김(OnDisable)에서 다시 열렸으면 완료 훅과 언로드를 건너뜁니다
				if (IsCurrent(step))
				{
					OnCloseTransitionCompleted();
				}

				// 완료 훅에서 다시 열렸을 수 있으니 한 번 더 확인하고, 서비스에서 이미 빠진 프레젠터는 언로드하지 않습니다.
				// 완료 태스크를 기다리던 쪽(팝업 큐 등)이 같은 인스턴스를 다시 열 수 있으므로 언로드를 마친 뒤 완료합니다
				if (destroy && IsCurrent(step) && IsRegisteredInService())
				{
					// 이 단계의 결과는 닫기 과정이 직접 정하도록, 언로드가 이 단계를 성공으로 완료하지 않게 먼저 뺍니다
					_inFlight.Remove(step.Completion);

					// UI 서비스가 Addressables.UnloadAsset을 호출하여 메모리에서 에셋을 언로드하고 게임 오브젝트를 파괴합니다
					_uiService.UnloadUi(GetType(), InstanceAddress);
				}

				Complete(step);
			}
			catch (Exception exception)
			{
				// 관찰이 다시 실패해도 아래 정리와 실패 처리는 반드시 합니다
				try
				{
					if (!observed)
					{
						UiTransitionTasks.ObserveTransitions(started ?? CaptureAfterFailure(false, inCallback ? notified + 1 : notified, before));
					}
				}
				catch (Exception observeException)
				{
					Logger.Error(observeException, $"{GetType().Name} could not observe its feature transitions");
				}

				// 표시 목록에서는 이미 빠졌으므로 화면에 남지 않게 숨기고 요청된 언로드를 한 뒤 실패를 알립니다.
				// 언로드가 이 단계를 성공으로 완료하지 않도록 먼저 진행 중 묶음에서 뺍니다
				_inFlight.Remove(step.Completion);

				// 정리 작업이 다시 던져도 원래 단계는 반드시 실패로 끝냅니다. 원래 예외는 Fail 이, 정리 예외는 Forget 이 기록합니다
				try
				{
					if (IsCurrent(step))
					{
						gameObject.SetActive(false);

						if (destroy && IsCurrent(step) && IsRegisteredInService())
						{
							_uiService.UnloadUi(GetType(), InstanceAddress);
						}
					}
				}
				finally
				{
					Fail(step, exception);
				}
			}
		}

		// 단계 시작 때 전환 피처들의 태스크 값을 기억합니다(상태는 읽지 않음). 전환 피처가 없으면 null 입니다
		private UniTask[] SnapshotTransitions(bool open)
		{
			if (_features == null)
			{
				return null;
			}

			UniTask[] snapshot = null;

			for (var i = 0; i < _features.Count; i++)
			{
				if (_features[i] is ITransitionFeature)
				{
					snapshot ??= new UniTask[_features.Count];
					snapshot[i] = ReadTransition(_features[i], open);
				}
			}

			return snapshot;
		}

		// 앞의 count 개 피처 중 이번 단계에서 태스크가 바뀐(새로 시작한) 전환만, 아직 끝나지 않았거나 실패했으면 고정합니다. 없으면 null 입니다.
		// 바뀌지 않은 태스크는 이전 단계에서 넘겨받은 것(이미 관찰했거나 소비돼 상태를 읽을 수 없을 수 있음)이라 읽지 않습니다.
		// 새 태스크의 상태 읽기 오류(예: UniTask.Defer 팩토리 예외)는 별도로 돌려주고, 호출자가 그 오류로 단계를 실패시킵니다
		// 한 태스크의 상태 읽기가 던져도 나머지 피처의 수집은 계속합니다. 던진 태스크는 담지 않고(다시 읽지 않게) 첫 오류를
		// statusError 로 돌려주며, 그 뒤 오류는 하나씩 기록합니다
		private UniTask[] CaptureTransitions(bool open, int count, UniTask[] before, out Exception statusError)
		{
			statusError = null;

			if (before == null)
			{
				return null;
			}

			UniTask[] tasks = null;
			var captured = 0;

			for (var i = 0; i < count; i++)
			{
				if (!(_features[i] is ITransitionFeature))
				{
					continue;
				}

				var task = ReadTransition(_features[i], open);

				// UniTask 는 소스와 토큰으로 이뤄진 값이라 값 비교로 새로 만든 태스크인지 압니다(전환 피처 수만큼만 비교)
				if (task.Equals(before[i]))
				{
					continue;
				}

				UniTaskStatus status;

				try
				{
					status = task.Status;
				}
				catch (Exception exception)
				{
					if (statusError == null)
					{
						statusError = exception;
					}
					else
					{
						Logger.Error(exception, $"{GetType().Name} could not read a feature transition status");
					}

					continue;
				}

				if (status == UniTaskStatus.Succeeded)
				{
					continue;
				}

				// 남는 칸은 default(UniTask) 라 이미 끝난 태스크로 취급됩니다
				tasks ??= new UniTask[count];
				tasks[captured++] = task;
			}

			return tasks;
		}

		// 다른 예외로 단계가 끝날 때 수집합니다. 상태 읽기 오류는 단계의 원래 예외와 별개라 따로 기록합니다
		private UniTask[] CaptureAfterFailure(bool open, int count, UniTask[] before)
		{
			var tasks = CaptureTransitions(open, count, before, out var statusError);

			if (statusError != null)
			{
				Logger.Error(statusError, $"{GetType().Name} could not read a feature transition status");
			}

			return tasks;
		}

		private static UniTask ReadTransition(PresenterFeatureBase feature, bool open)
		{
			if (feature is ITransitionFeature transitionFeature)
			{
				return open ? transitionFeature.OpenTransitionTask : transitionFeature.CloseTransitionTask;
			}

			return default;
		}

		private bool IsRegisteredInService()
		{
			return _uiService.TryGetUi(new UiInstanceId(GetType(), InstanceAddress), out var registered) &&
				ReferenceEquals(registered, this);
		}

		private void InitializeFeatures()
		{
			_features = new List<PresenterFeatureBase>();
			GetComponents(_features);

			foreach (var feature in _features)
			{
				feature.OnPresenterInitialized(this);
			}
		}

		// 피처가 반대 동작을 하면 이후 피처에는 낡은 알림을 보내지 않고 false 를 돌려줍니다
		private bool NotifyFeaturesOpening(LifecycleStep step)
		{
			if (_features == null) return IsCurrent(step);
			
			foreach (var feature in _features)
			{
				feature.OnPresenterOpening();

				if (!IsCurrent(step))
				{
					return false;
				}
			}

			return true;
		}

		// 피처가 반대 동작을 하면 이후 피처에는 낡은 알림을 보내지 않고 false 를 돌려줍니다
		private bool NotifyFeaturesClosed(LifecycleStep step)
		{
			if (_features == null) return IsCurrent(step);
			
			foreach (var feature in _features)
			{
				feature.OnPresenterClosed();

				if (!IsCurrent(step))
				{
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>
	/// 준비한 열기·닫기 한 번의 세대와 완료 소스입니다.
	/// </summary>
	internal readonly struct LifecycleStep
	{
		public readonly int Version;
		public readonly UniTaskCompletionSource Completion;

		public LifecycleStep(int version, UniTaskCompletionSource completion)
		{
			Version = version;
			Completion = completion;
		}
	}

	/// <inheritdoc cref="UiPresenter"/>
	/// <remarks>
	/// <see cref="UiPresenter"/> 동작을 확장하여 <typeparamref name="T"/> 타입의 데이터를 보유합니다
	/// </remarks>
	public abstract class UiPresenter<T> : UiPresenter, IUiPresenterData where T : struct
	{
		private T _data;

		/// <summary>
		/// <see cref="UiService"/>를 통해 열릴 때 정의되는 UI 데이터입니다
		/// </summary>
		public T Data
		{
			get => _data;
			set
			{
				_data = value;
				OnSetData();
			}
		}

		/// <summary>
		/// 프레젠터에 정의된 데이터가 설정될 때 UI 프레젠터 구현체가 추가 동작을 수행할 수 있게 합니다
		/// </summary>
		protected virtual void OnSetData() {}
	}
}

