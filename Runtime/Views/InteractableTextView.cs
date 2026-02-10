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
        public enum InteractableTextType
        {
            IntersectingLink,
            NearestLink
        }
        
        public UnityEvent<TMP_LinkInfo> OnLinkedInfoClicked;
        public InteractableTextType InteractableType;
        
        [SerializeField] private TMP_Text _text;

        public TMP_Text Text => _text;

        private void OnValidate()
        {
            _text = _text == null ? GetComponent<TMP_Text>() : _text;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // WorldCamera 뷰를 사용하는 경우 Canvas 카메라를 가져옵니다
            var linkedText = -1;

            if (InteractableType == InteractableTextType.IntersectingLink)
            {
                linkedText = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, null);
            }
            else
            {
                linkedText = TMP_TextUtilities.FindNearestLink(_text, eventData.position, null);
            }

            if (linkedText > -1)
            {
                OnLinkedInfoClicked.Invoke(_text.textInfo.linkInfo[linkedText]);
            }
        }
    }
}