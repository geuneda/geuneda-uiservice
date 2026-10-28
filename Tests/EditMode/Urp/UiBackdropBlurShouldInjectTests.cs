using Geuneda.UiService.Rendering;
using NUnit.Framework;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// 흐림 패스를 넣을지 정하는 조건 조합. bool 만 받으므로 URP 카메라 없이 모든 제외 조건을 확인한다.
	/// </summary>
	[TestFixture]
	public class UiBackdropBlurShouldInjectTests
	{
		[Test]
		public void GameCamera_NotOverlay_NoTargetTexture_PresenterOpen_Injects()
		{
			Assert.IsTrue(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		public void NoPresenterOpen_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: false));
		}

		[Test]
		public void SceneOrPreviewCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: false, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		public void StackedOverlayCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: true, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		public void RenderTextureTargetCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: true, anyPresenterOpen: true));
		}

		[Test]
		public void EveryRejectionCombined_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: false, isOverlayCamera: true, hasTargetTexture: true, anyPresenterOpen: false));
		}
	}
}
