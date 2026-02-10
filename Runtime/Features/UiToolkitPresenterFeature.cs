using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Geuneda.UiService
{
	/// <summary>
	/// <see cref="UiPresenter"/>에 UI Toolkit 통합을 제공하는 기능입니다.
	/// 비주얼 트리 타이밍을 처리하므로 구독자가 이에 대해 신경 쓸 필요가 없습니다.
	/// </summary>
	[RequireComponent(typeof(UIDocument))]
	public class UiToolkitPresenterFeature : PresenterFeatureBase
	{
		[SerializeField] private UIDocument _document;

		private readonly UnityEvent<VisualElement> _onVisualTreeReady = new UnityEvent<VisualElement>();
		private bool _callbacksRegistered;

		/// <summary>
		/// 부착된 <see cref="UIDocument"/>입니다.
		/// </summary>
		public UIDocument Document => _document;

		/// <summary>
		/// UIDocument의 루트 <see cref="VisualElement"/>입니다.
		/// </summary>
		public VisualElement Root => _document?.rootVisualElement;

		private void OnValidate()
		{
			_document = _document ?? GetComponent<UIDocument>();
		}

		private void OnDestroy()
		{
			_onVisualTreeReady.RemoveAllListeners();
			UnregisterPanelCallbacks();
		}

		/// <summary>
		/// 비주얼 트리가 준비되었을 때 호출될 콜백을 등록합니다.
		/// 매 열기마다 호출됩니다 (UI Toolkit은 활성화 시 요소를 재생성합니다).
		/// OnInitialized()에서 호출해도 안전합니다.
		/// </summary>
		public void AddVisualTreeAttachedListener(UnityAction<VisualElement> callback)
		{
			if (callback == null)
			{
				return;
			}

			_onVisualTreeReady.AddListener(callback);

			// 비주얼 트리가 이미 준비되었으면 즉시 호출합니다
			if (Root?.panel != null)
			{
				callback(Root);
			}
		}

		/// <summary>
		/// 이전에 등록된 콜백을 제거합니다.
		/// </summary>
		public void RemoveVisualTreeAttachedListener(UnityAction<VisualElement> callback)
		{
			if (callback == null)
			{
				return;
			}

			_onVisualTreeReady.RemoveListener(callback);
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			RegisterPanelCallbacks();
			TryInvokeListeners();
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			base.OnPresenterOpened();
			RegisterPanelCallbacks();
			TryInvokeListeners();
		}

		private void RegisterPanelCallbacks()
		{
			if (_callbacksRegistered)
			{
				return;
			}

			var root = Root;
			if (root == null)
			{
				return;
			}

			root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			_callbacksRegistered = true;
		}

		private void UnregisterPanelCallbacks()
		{
			if (!_callbacksRegistered)
			{
				return;
			}

			var root = Root;
			if (root != null)
			{
				root.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			}
			_callbacksRegistered = false;
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			TryInvokeListeners();
		}

		private void TryInvokeListeners()
		{
			var root = Root;
			if (root?.panel == null)
			{
				return;
			}

			_onVisualTreeReady.Invoke(root);
		}
	}
}

