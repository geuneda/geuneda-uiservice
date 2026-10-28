using System.Collections;
using System.Text.RegularExpressions;
using Geuneda.UiService.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 열림/닫힘 참조 수만 확인한다. 배치 모드(-nographics)에는 그릴 화면이 없고 렌더 텍스처 카메라는 설계상 제외되므로
	/// GPU 렌더링은 강제하지 않는다. 흐림이 보기 좋은지는 문서에 적은 대로 눈으로 확인한다(docs/urp-rendering.md).
	/// </summary>
	[TestFixture]
	public class UiBackdropBlurPresenterFeatureTests
	{
		private GameObject _go;
		private UiBackdropBlurPresenterFeature _feature;

		[SetUp]
		public void Setup()
		{
			_go = new GameObject(nameof(UiBackdropBlurPresenterFeatureTests));
			_feature = _go.AddComponent<UiBackdropBlurPresenterFeature>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_go != null) Object.DestroyImmediate(_go);
		}

		[UnityTest]
		public IEnumerator OnPresenterOpening_MarksBlurOpen()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();

			Assert.IsTrue(AnyOpen);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterOpening_CalledTwice_DoesNotDoubleCount()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			_feature.OnPresenterOpening();

			// 두 번 세면 한 번 닫아도 흐림이 켜진 채 남는다
			_go.SetActive(false);

			Assert.IsFalse(AnyOpen);
			yield return null;
		}

		[UnityTest]
		public IEnumerator DisablingPresenter_ClearsBlur()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			Assert.IsTrue(AnyOpen);

			_go.SetActive(false); // OnDisable

			Assert.IsFalse(AnyOpen);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Reopen_AfterDisable_MarksBlurOpenAgain()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			_go.SetActive(false);
			_go.SetActive(true);
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();

			Assert.IsTrue(AnyOpen);
			yield return null;
		}

		[UnityTest]
		public IEnumerator TwoPresenters_BlurStaysOpenUntilBothClose()
		{
			var secondGo = new GameObject("SecondBlurPresenter");
			var second = secondGo.AddComponent<UiBackdropBlurPresenterFeature>();

			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			ExpectMissingRendererFeatureError();
			second.OnPresenterOpening();

			_go.SetActive(false);
			Assert.IsTrue(AnyOpen, "한 프레젠터가 닫혀도 다른 프레젠터가 원하는 흐림은 남아야 한다");

			secondGo.SetActive(false);
			Assert.IsFalse(AnyOpen);

			Object.DestroyImmediate(secondGo);
			yield return null;
		}

		private static bool AnyOpen => UiBackdropBlurPresenterFeature.AnyOpen;

		// 렌더러 피처가 설치되지 않았을 때만 열기마다 설정 오류를 남기므로 기대도 환경을 따른다. 배치 모드는 URP 렌더러를 만들지 않아
		// Create() 가 불리지 않고 오류가 나며, 에디터에서는 Game 뷰가 그려져 피처가 등록되고 오류가 없다
		private static void ExpectMissingRendererFeatureError()
		{
			if (UiBackdropBlurRendererFeature.IsInstalled)
			{
				return;
			}

			LogAssert.Expect(LogType.Error, new Regex(".*UiBackdropBlurRendererFeature.*Add Renderer Feature.*"));
		}
	}
}
