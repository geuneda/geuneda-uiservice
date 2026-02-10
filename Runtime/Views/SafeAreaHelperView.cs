using System;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService.Views
{
	/// <summary>
	/// 기기 안전 영역(노치가 있는 화면)을 기반으로 앵커된 뷰를 변환하는 뷰 헬퍼입니다
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class SafeAreaHelperView : MonoBehaviour
	{
		[Tooltip("true이면 피벗이 안전 영역 밖에 있을 때만 앵커를 이동합니다. 안전 영역 내부에 있으면 " +
				 "동일한 앵커 위치를 유지합니다")]
		[SerializeField] private bool _checkAreaBounds = false;
		[SerializeField] private RectTransform _rectTransform;
		[SerializeField] private bool _ignoreHeight = false;
		[SerializeField] private bool _ignoreWidth = false;
		[SerializeField] private bool _onUpdate = false;
		[SerializeField] private Vector2 _refResolution;

		private Vector2 _initAnchoredPosition;
		private Vector2 _initSizeDelta;
		private Rect _resolution;
		private Rect _safeArea;

		internal void OnValidate()
		{
			_rectTransform = _rectTransform ? _rectTransform : GetComponent<RectTransform>();
			_refResolution = transform.root.GetComponent<CanvasScaler>().referenceResolution;
			_initAnchoredPosition = _rectTransform.anchoredPosition;
			_initSizeDelta = _rectTransform.sizeDelta;
		}

		private void Awake()
		{
			_initAnchoredPosition = _rectTransform.anchoredPosition;
			_initSizeDelta = _rectTransform.sizeDelta;
			_resolution = new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
			_safeArea = Screen.safeArea;
		}

		private void OnEnable()
		{
			UpdatePositions();
		}

		private void Update()
		{
			if (_onUpdate)
			{
				UpdatePositions();
			}
		}

		internal void UpdatePositions()
		{

#if UNITY_EDITOR
			// Unity Device Simulator와 Game View는 화면 해상도 설정이 다르고 때로는 데스크톱 해상도를 사용하기 때문입니다
			_safeArea = Screen.safeArea;
			_resolution = new Rect(0, 0, Screen.width, Screen.height);
			_resolution = _resolution == _safeArea ? _resolution : new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
#endif

			if (_safeArea == _resolution)
			{
				return;
			}

			var anchorMax = _rectTransform.anchorMax;
			var anchorMin = _rectTransform.anchorMin;
			var anchoredPosition = _initAnchoredPosition;

			// 모든 면이 늘어나 있는지 확인합니다
			if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
			{
				AllStretchedAnchoring();
				return;
			}

			// 상단 또는 하단에 앵커되어 있는지 확인합니다
			if (!_ignoreHeight && Mathf.Approximately(anchorMax.y, anchorMin.y) && !CheckHeightAreaBounds(anchoredPosition))
			{
				// 하단
				if (anchorMax.y < Mathf.Epsilon)
				{
					anchoredPosition.y += (_safeArea.yMin - _resolution.yMin) * _refResolution.y / _resolution.height;
				}
				else // 상단
				{
					anchoredPosition.y += (_safeArea.yMax - _resolution.yMax) * _refResolution.y / _resolution.height;
				}
			}

			// 좌측 또는 우측에 앵커되어 있는지 확인합니다
			if (!_ignoreWidth && Mathf.Approximately(anchorMax.x, anchorMin.x) && !CheckWidthAreaBounds(anchoredPosition))
			{
				// 좌측
				if (anchorMax.x < Mathf.Epsilon)
				{
					anchoredPosition.x += (_safeArea.xMin - _resolution.xMin) * _refResolution.x / _resolution.width;
				}
				else // 우측
				{
					anchoredPosition.x += (_safeArea.xMax - _resolution.xMax) * _refResolution.x / _resolution.width;
				}
			}

			_rectTransform.anchoredPosition = anchoredPosition;
		}

		private bool CheckWidthAreaBounds(Vector2 anchoredPosition)
		{
			return _checkAreaBounds && anchoredPosition.x > _safeArea.xMin && anchoredPosition.x < _safeArea.xMax;
		}

		private bool CheckHeightAreaBounds(Vector2 anchoredPosition)
		{
			return _checkAreaBounds && anchoredPosition.y > _safeArea.yMin && anchoredPosition.y < _safeArea.yMax;
		}

		private void AllStretchedAnchoring()
		{
			var anchoredPosition = _initAnchoredPosition;
			var sizeDelta = _initSizeDelta;

			// 상단 또는 하단에 앵커되어 있는지 확인합니다
			if (!_ignoreHeight && !CheckHeightAreaBounds(anchoredPosition))
			{
				var yDelta = (_safeArea.yMax - _resolution.yMax) * _refResolution.y / _resolution.height;

				anchoredPosition.y += yDelta / 2f;
				sizeDelta.y -= yDelta / 2f;
			}

			// 좌측 또는 우측에 앵커되어 있는지 확인합니다
			if (!_ignoreWidth && !CheckWidthAreaBounds(anchoredPosition))
			{
				var xDelta = (_safeArea.xMin - _resolution.xMin) * _refResolution.x / _resolution.width;

				anchoredPosition.x += xDelta / 2f;
				sizeDelta.x -= xDelta / 2f;
			}

			_rectTransform.anchoredPosition = anchoredPosition;
			_rectTransform.sizeDelta = sizeDelta;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// <see cref="SafeAreaHelperView"/> 클래스를 위한 커스텀 인스펙터입니다
	/// </summary>
	[UnityEditor.CustomEditor(typeof(SafeAreaHelperView))]
	public class SafeAreaHelperViewEditor : UnityEditor.Editor
	{
		/// <inheritdoc />
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (GUILayout.Button("Update Anchored Data"))
			{
				var view = (SafeAreaHelperView)target;

				view.OnValidate();
			}

			if (GUILayout.Button("Update Anchored View"))
			{
				var view = (SafeAreaHelperView)target;

				view.UpdatePositions();
			}
		}
	}
#endif
}