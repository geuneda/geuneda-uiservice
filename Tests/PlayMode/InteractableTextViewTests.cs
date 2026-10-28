using System.Collections;
using System.Collections.Generic;
using Geuneda.UiService.Views;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	[TestFixture]
	public class InteractableTextViewTests
	{
		private GameObject _canvasGo;
		private GameObject _textGo;
		private TextMeshProUGUI _tmp;
		private InteractableTextView _view;
		private int _onLinkedInfoClickedCount;
		private TMP_LinkInfo _lastLinkInfo;
		private readonly List<GameObject> _extraGameObjects = new List<GameObject>();

		[SetUp]
		public void Setup()
		{
			_canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
			_canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

			_textGo = new GameObject("Text", typeof(RectTransform));
			_textGo.transform.SetParent(_canvasGo.transform, false);

			_tmp = _textGo.AddComponent<TextMeshProUGUI>();
			_view = _textGo.AddComponent<InteractableTextView>();

			// _text 는 에디터 콜백 OnValidate 에서 채워지므로 런타임에는 같은 경로를 SendMessage 로 부른다
			_view.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);

			_onLinkedInfoClickedCount = 0;
			_lastLinkInfo = default;
			_view.OnLinkedInfoClicked = new UnityEngine.Events.UnityEvent<TMP_LinkInfo>();
			_view.OnLinkedInfoClicked.AddListener(info =>
			{
				_onLinkedInfoClickedCount++;
				_lastLinkInfo = info;
			});
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var go in _extraGameObjects)
			{
				if (go != null)
				{
					Object.DestroyImmediate(go);
				}
			}
			_extraGameObjects.Clear();

			if (_textGo != null)
			{
				Object.DestroyImmediate(_textGo);
			}
			if (_canvasGo != null)
			{
				Object.DestroyImmediate(_canvasGo);
			}
		}

		[UnityTest]
		public IEnumerator OnPointerClick_OnLinkedTextHit_FiresOnLinkedInfoClickedWithLinkInfo()
		{
			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();
			yield return null;

			AssumeLinkRegistered();

			var eventData = new PointerEventData(EventSystem.current) { position = GetFirstLinkScreenPoint(null) };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(1, _onLinkedInfoClickedCount);
			Assert.AreEqual("my_link_id", _lastLinkInfo.GetLinkID());
		}

		[UnityTest]
		public IEnumerator OnPointerClick_NoLinkInText_DoesNotFireOnLinkedInfoClicked()
		{
			_tmp.text = "no links here at all";
			_tmp.ForceMeshUpdate();
			yield return null;

			var screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(0, _onLinkedInfoClickedCount);
		}

		[UnityTest]
		public IEnumerator OnPointerClick_ScreenSpaceCameraCanvas_UsesWorldCameraForLinkHitTest()
		{
			var canvas = _canvasGo.GetComponent<Canvas>();
			var camera = CreateCamera();

			// 카메라의 위치와 pixelRect 를 기본값에서 벗어나게 해, 카메라 없이 화면 중심으로 판정하는 경로와 실제 투영이 달라지게 한다
			camera.transform.position = new Vector3(2f, 1f, -8f);
			camera.orthographic = false;
			camera.fieldOfView = 45f;
			camera.pixelRect = new Rect(50f, 50f, Screen.width - 100f, Screen.height - 100f);

			canvas.renderMode = RenderMode.ScreenSpaceCamera;
			canvas.worldCamera = camera;
			canvas.planeDistance = 10f;

			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();

			// Screen Space - Camera 캔버스는 매 프레임 카메라 뷰포트에 맞춰 옮겨지므로 자리를 잡을 시간을 준다
			yield return null;
			yield return null;

			AssumeLinkRegistered();

			// TMP 는 camera.ScreenPointToRay 로 클릭의 월드 점을 되찾으므로 같은 카메라로 만든 화면 점만 의미가 있다.
			// 카메라 없이 판정하면 Overlay 규약(화면 점이 캔버스 원점 기준)으로 돌아가 링크를 놓친다
			var eventData = new PointerEventData(EventSystem.current) { position = GetFirstLinkScreenPoint(camera) };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(1, _onLinkedInfoClickedCount);
			Assert.AreEqual("my_link_id", _lastLinkInfo.GetLinkID());
		}

		[UnityTest]
		public IEnumerator OnPointerClick_WorldSpaceCanvas_UsesWorldCameraForLinkHitTest()
		{
			var canvas = _canvasGo.GetComponent<Canvas>();
			var camera = CreateCamera();

			camera.transform.position = Vector3.zero;
			camera.transform.rotation = Quaternion.identity;
			camera.orthographic = false;
			camera.fieldOfView = 60f;

			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = camera;

			// World Space 캔버스는 Unity 가 옮기지 않으므로 화면 중심 규약과 멀리 떨어진 자리에 둔다
			_canvasGo.transform.position = new Vector3(15f, 8f, 40f);
			_canvasGo.transform.rotation = Quaternion.Euler(10f, -20f, 0f);

			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();
			yield return null;

			AssumeLinkRegistered();

			var eventData = new PointerEventData(EventSystem.current) { position = GetFirstLinkScreenPoint(camera) };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(1, _onLinkedInfoClickedCount);
			Assert.AreEqual("my_link_id", _lastLinkInfo.GetLinkID());
		}

		[UnityTest]
		public IEnumerator OnPointerClick_TextWithNoParentCanvas_DoesNotThrow()
		{
			// Canvas 없이 만든 프리팹이나 부모를 옮기는 중인 텍스트
			_textGo.transform.SetParent(null, false);

			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();
			yield return null;

			var screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			Assert.DoesNotThrow(() => _view.OnPointerClick(eventData));
		}

		private Camera CreateCamera()
		{
			var cameraGo = new GameObject("EventCamera", typeof(Camera));
			_extraGameObjects.Add(cameraGo);
			return cameraGo.GetComponent<Camera>();
		}

		private void AssumeLinkRegistered()
		{
			Assume.That(_tmp.textInfo.linkInfo.Length, Is.GreaterThan(0),
				"TMP 가 링크 정보를 만들지 못했다. 테스트 런타임에 기본 폰트 에셋이 없을 수 있다");
			Assume.That(_tmp.textInfo.linkInfo[0].GetLinkID(), Is.EqualTo("my_link_id"));
		}

		// 첫 링크의 첫 글자 중심을 캔버스를 그리는 카메라 기준 화면 좌표로 바꾼다
		private Vector2 GetFirstLinkScreenPoint(Camera camera)
		{
			var rectTransform = _textGo.GetComponent<RectTransform>();
			var firstLinkChar = _tmp.textInfo.linkInfo[0].linkTextfirstCharacterIndex;
			var charInfo = _tmp.textInfo.characterInfo[firstLinkChar];
			var localCenter = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;
			var worldPos = rectTransform.TransformPoint(localCenter);

			return RectTransformUtility.WorldToScreenPoint(camera, worldPos);
		}
	}
}
