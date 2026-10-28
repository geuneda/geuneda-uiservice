using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Geuneda.UiService.Views
{
    /// <summary>
    /// 이 뷰는 <see cref="TMP_Text"/>에 설정된 모든 유형의 링크(예: 하이퍼링크)를 처리하는 역할을 합니다
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class InteractableTextView : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// 클릭을 어느 링크로 판정할지
        /// </summary>
        public enum InteractableTextType
        {
            IntersectingLink,
            NearestLink
        }

        public UnityEvent<TMP_LinkInfo> OnLinkedInfoClicked;
        public InteractableTextType InteractableType;

        [SerializeField] private TMP_Text _text;

        /// <summary>
        /// 링크를 판정하는 대상 텍스트
        /// </summary>
        public TMP_Text Text => _text;

        private void OnValidate()
        {
            _text = _text == null ? GetComponent<TMP_Text>() : _text;
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            var camera = ResolveEventCamera();
            var linkedText = -1;

            if (InteractableType == InteractableTextType.IntersectingLink)
            {
                linkedText = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, camera);
            }
            else
            {
                linkedText = TMP_TextUtilities.FindNearestLink(_text, eventData.position, camera);
            }

            if (linkedText > -1)
            {
                OnLinkedInfoClicked.Invoke(_text.textInfo.linkInfo[linkedText]);
            }
        }

        // TMP 는 화면 좌표를 카메라로 되돌려 텍스트 평면과 교차시키므로, 캔버스를 그리는 카메라를 넘겨야
        // Screen Space - Camera 와 World Space 캔버스에서 클릭 위치가 맞는다. Overlay 캔버스(와 카메라 없는
        // Screen Space - Camera 캔버스)는 Unity 가 Overlay 로 그리므로 null 을 넘긴다
        private Camera ResolveEventCamera()
        {
            var canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                return null;
            }

            var isOverlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
            var isCameraless = canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null;

            return isOverlay || isCameraless ? null : canvas.worldCamera;
        }
    }
}
