// 다운샘플한 해상도에서 5탭 박스 블러를 UiBackdropBlurRendererFeature 의 반복 횟수만큼 되풀이한다.
// 박스 블러를 반복하면 중심극한정리로 가우시안에 가까워지므로, 분리형 2패스 커널 없이도 싸고 단순하게 흐릴 수 있다.
Shader "Hidden/Geuneda/UiService/UiBackdropBlur"
{
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name "UiBackdropBlur"

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _Spread;
			half4 _Tint;

			half4 Frag(Varyings input) : SV_Target
			{
				float2 texelSize = _BlitTexture_TexelSize.xy * _Spread;
				float2 uv = input.texcoord;

				half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel) * 4.0;
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( texelSize.x,  texelSize.y), _BlitMipLevel);
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-texelSize.x,  texelSize.y), _BlitMipLevel);
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( texelSize.x, -texelSize.y), _BlitMipLevel);
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-texelSize.x, -texelSize.y), _BlitMipLevel);
				color /= 8.0;

				return color * _Tint;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
