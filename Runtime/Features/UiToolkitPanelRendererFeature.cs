#if UNITY_6000_5_OR_NEWER
namespace Geuneda.UiService
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UIElements;

    /// <summary>
    /// <see cref="UiPresenter"/>에 UI Toolkit 통합을 제공하는 기능입니다(Unity 6.5+ <see cref="PanelRenderer"/> 기반).
    /// UIDocument 기반 <see cref="UiToolkitPresenterFeature"/>의 대체판으로, 동일한
    /// <see cref="AddVisualTreeAttachedListener"/> API 를 노출하므로 구독자는 렌더러 종류를 신경 쓸 필요가 없습니다.
    /// PanelRenderer 는 <c>rootVisualElement</c> 프로퍼티가 없어 <c>RegisterUIReloadCallback</c> 콜백으로만 루트를
    /// 받으므로, 그 타이밍을 이 기능이 처리합니다. Unity 6.5(6000.5) 미만에서는 컴파일되지 않습니다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class UiToolkitPanelRendererFeature : PresenterFeatureBase
    {
        [SerializeField] private PanelRenderer _panel;

        private readonly UnityEvent<VisualElement> _onVisualTreeReady = new UnityEvent<VisualElement>();
        private VisualElement _root;
        private bool _callbackRegistered;

        /// <summary>
        /// 부착된 <see cref="PanelRenderer"/>입니다.
        /// </summary>
        public PanelRenderer Panel => _panel;

        /// <summary>
        /// 최근 리로드 콜백에서 받은 루트 <see cref="VisualElement"/>입니다(아직 없으면 null).
        /// </summary>
        public VisualElement Root => _root;

        private void OnValidate()
        {
            _panel = _panel != null ? _panel : GetComponent<PanelRenderer>();
        }

        private void OnDestroy()
        {
            _onVisualTreeReady.RemoveAllListeners();
            UnregisterReload();
        }

        /// <summary>
        /// 비주얼 트리가 준비되었을 때 호출될 콜백을 등록합니다.
        /// PanelRenderer 리로드마다 호출되며, 이미 준비되어 있으면 즉시 호출합니다.
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
            if (_root != null)
            {
                callback(_root);
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
            RegisterReload();
        }

        /// <inheritdoc />
        public override void OnPresenterOpened()
        {
            base.OnPresenterOpened();
            RegisterReload();
        }

        private void RegisterReload()
        {
            if (_callbackRegistered || _panel == null)
            {
                return;
            }

            _panel.RegisterUIReloadCallback(OnUIReload);
            _callbackRegistered = true;
        }

        private void UnregisterReload()
        {
            if (!_callbackRegistered)
            {
                return;
            }

            if (_panel != null)
            {
                _panel.UnregisterUIReloadCallback(OnUIReload);
            }

            _callbackRegistered = false;
        }

        // PanelRenderer 가 UI 를 (재)로드하면 루트를 갱신하고 리스너에 통지합니다
        private void OnUIReload(PanelRenderer panel, VisualElement root)
        {
            _root = root;
            _onVisualTreeReady.Invoke(root);
        }
    }
}
#endif
