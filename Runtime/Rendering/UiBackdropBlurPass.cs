using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Geuneda.UiService.Rendering
{
	/// <summary>
	/// 카메라 색상을 다운샘플해 흐린 뒤 원래 해상도의 대상 위에 다시 그려, 이 패스 뒤에 합성되는 UI 가 흐려진 배경 위에 놓이게 합니다
	/// </summary>
	internal class UiBackdropBlurPass : ScriptableRenderPass
	{
		// URP 가 캐시해 둔 셰이더 프로퍼티 ID 는 URP 어셈블리 내부라 같은 이름(Core RP 의 Blit.hlsl 규약)으로 다시 만든다
		private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
		private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
		private static readonly int SpreadId = Shader.PropertyToID("_Spread");
		private static readonly int TintId = Shader.PropertyToID("_Tint");

		private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

		private readonly Material _material;
		private readonly UiBackdropBlurRendererFeature _settings;

		public UiBackdropBlurPass(Material material, UiBackdropBlurRendererFeature settings)
		{
			_material = material;
			_settings = settings;
			profilingSampler = new ProfilingSampler(nameof(UiBackdropBlurPass));

			// 이 패스는 활성 색상 대상을 텍스처로 읽는데 백버퍼는 읽을 수 없다
			requiresIntermediateTexture = true;
		}

		/// <inheritdoc />
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (_material == null || _settings == null)
			{
				return;
			}

			var resourceData = frameData.Get<UniversalResourceData>();
			var cameraData = frameData.Get<UniversalCameraData>();
			var isOverlay = cameraData.renderType == CameraRenderType.Overlay;
			var hasTargetTexture = cameraData.camera != null && cameraData.camera.targetTexture != null;

			if (!ShouldInject(cameraData.cameraType == CameraType.Game, isOverlay, hasTargetTexture, UiBackdropBlurPresenterFeature.AnyOpen))
			{
				return;
			}

			// 백버퍼는 텍스처로 읽을 수 없어 흐릴 원본이 없다
			if (resourceData.isActiveTargetBackBuffer)
			{
				return;
			}

			var source = resourceData.activeColorTexture;
			if (!source.IsValid())
			{
				return;
			}

			var descriptor = renderGraph.GetTextureDesc(source);
			var downsampleFactor = 1 << Mathf.Clamp(_settings.Downsample, 0, 3);
			descriptor.width = Mathf.Max(1, descriptor.width / downsampleFactor);
			descriptor.height = Mathf.Max(1, descriptor.height / downsampleFactor);
			descriptor.name = "_UiBackdropBlurWork";
			descriptor.clearBuffer = false;
			// 복사한 설명자에는 카메라 대상의 깊이와 memoryless 설정이 따라오는데 작업 텍스처는 색상만 쓴다.
			// 그대로 두면 흐림이 도는 프레임마다 URP 가 "Ignoring depth surface load/store action as it is memoryless" 를 남긴다
			descriptor.depthBufferBits = DepthBits.None;
			descriptor.memoryless = RenderTextureMemoryless.None;

			var downsampled = renderGraph.CreateTexture(descriptor);
			renderGraph.AddBlitPass(source, downsampled, Vector2.one, Vector2.zero, passName: "UiBackdropBlur_Downsample");

			var iterations = Mathf.Max(1, _settings.Iterations);
			var current = downsampled;

			for (var i = 0; i < iterations; i++)
			{
				var next = renderGraph.CreateTexture(descriptor);
				// 반복마다 색을 곱하면 틴트가 누적되므로 마지막 반복에만 적용한다
				var isLastIteration = i == iterations - 1;
				AddBlurIterationPass(renderGraph, current, next, Mathf.Max(0f, _settings.Spread),
					isLastIteration ? _settings.Tint : Color.white);
				current = next;
			}

			renderGraph.AddBlitPass(current, source, Vector2.one, Vector2.zero, passName: "UiBackdropBlur_Composite");
		}

		/// <summary>
		/// 주어진 성질의 카메라에 흐림을 넣을지 정합니다
		/// </summary>
		/// <remarks>
		/// Scene 뷰와 미리보기 카메라는 에디터 화면을 흐리게 하고, URP 오버레이 카메라는 뒤의 월드가 아니라 스택에 올린 UI 를 흐리게 하며,
		/// 렌더 텍스처로 그리는 카메라는 캡처 중인 UI 를 흐리게 하므로 모두 제외합니다
		/// </remarks>
		internal static bool ShouldInject(bool isGameCamera, bool isOverlayCamera, bool hasTargetTexture, bool anyPresenterOpen)
		{
			return isGameCamera && !isOverlayCamera && !hasTargetTexture && anyPresenterOpen;
		}

		private void AddBlurIterationPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination,
			float spread, Color tint)
		{
			using var builder = renderGraph.AddRasterRenderPass<PassData>("UiBackdropBlur_Iteration", out var passData, profilingSampler);

			passData.Material = _material;
			passData.Source = source;
			passData.Spread = spread;
			passData.Tint = tint;

			builder.UseTexture(source, AccessFlags.Read);
			builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

			builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
			{
				PropertyBlock.Clear();
				PropertyBlock.SetTexture(BlitTextureId, data.Source);
				PropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
				PropertyBlock.SetFloat(SpreadId, data.Spread);
				PropertyBlock.SetColor(TintId, data.Tint);

				context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3, 1, PropertyBlock);
			});
		}

		private class PassData
		{
			internal Material Material;
			internal TextureHandle Source;
			internal float Spread;
			internal Color Tint;
		}
	}
}
