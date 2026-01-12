using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Geuneda.UiService.Views
{
    /// <summary>
    /// This view is responsible to handle all types of links (ex: hyperlinks) set in the <see cref="TMP_Text"/>
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
            // Get Canvas Camera if using WorldCamera view
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