using Geuneda.UiService.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// <see cref="UiBackdropBlurRendererFeature"/> 의 런타임 반복 횟수 덮어쓰기와 재질 생명주기.
	/// <see cref="UiBackdropBlurRendererFeature.IterationsOverride"/> 가 에셋 인스턴스와 무관한 정적 프로퍼티라는 점이 설계 제약이다.
	/// </summary>
	[TestFixture]
	public class UiBackdropBlurRendererFeatureTests
	{
		private UiBackdropBlurRendererFeature _feature;

		[SetUp]
		public void Setup()
		{
			_feature = ScriptableObject.CreateInstance<UiBackdropBlurRendererFeature>();
		}

		[TearDown]
		public void TearDown()
		{
			// 정적 덮어쓰기는 프로세스의 모든 인스턴스와 테스트가 공유하므로 다른 픽스처로 새지 않게 되돌린다
			UiBackdropBlurRendererFeature.IterationsOverride = 0;

			if (_feature != null)
			{
				Object.DestroyImmediate(_feature);
			}
		}

		[Test]
		public void IterationsOverride_WhenSet_DoesNotDirtyTheSerializedAsset()
		{
			// 에셋의 직렬화 필드에 쓰면 에디터에서 디스크에 저장돼 프로젝트 전체의 모양이 바뀐다
			var jsonBefore = EditorJsonUtility.ToJson(_feature);

			UiBackdropBlurRendererFeature.IterationsOverride = 5;
			var jsonAfter = EditorJsonUtility.ToJson(_feature);

			Assert.AreEqual(jsonBefore, jsonAfter);
		}

		[Test]
		public void IterationsOverride_AboveMaxIterations_ClampsToMax()
		{
			UiBackdropBlurRendererFeature.IterationsOverride = UiBackdropBlurRendererFeature.MaxIterations + 12;

			Assert.AreEqual(UiBackdropBlurRendererFeature.MaxIterations, UiBackdropBlurRendererFeature.IterationsOverride);
			Assert.AreEqual(UiBackdropBlurRendererFeature.MaxIterations, UiBackdropBlurRendererFeature.EffectiveIterations);

			// 0 이하는 덮어쓰기를 지운다
			UiBackdropBlurRendererFeature.IterationsOverride = 0;
			Assert.AreEqual(0, UiBackdropBlurRendererFeature.IterationsOverride);
			Assert.AreEqual(UiBackdropBlurRendererFeature.AuthoredIterations, UiBackdropBlurRendererFeature.EffectiveIterations);
		}

		[Test]
		public void Create_LoadsPackagedShader_AndBuildsMaterial()
		{
			_feature.Create();

			Assert.IsNotNull(_feature.BlurMaterialInternal, "에디터에서는 패키지의 셰이더를 찾아 재질을 만들어야 한다");
			Assert.IsTrue(UiBackdropBlurRendererFeature.IsInstalled);
		}

		[Test]
		public void Dispose_AfterCreate_DestroysBlurMaterial()
		{
			_feature.Create();
			var materialBeforeDispose = _feature.BlurMaterialInternal;
			Assert.IsNotNull(materialBeforeDispose, "Create() 가 지울 재질을 만들어야 한다");

			_feature.Dispose();

			// Dispose 전에 잡아 둔 참조로 CoreUtils.Destroy 가 실제로 파괴했는지(fake null) 확인한다
			Assert.IsTrue(materialBeforeDispose == null);
			Assert.IsNull(_feature.BlurMaterialInternal);
		}
	}
}
