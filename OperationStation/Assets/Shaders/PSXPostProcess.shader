Shader "Hidden/PSXPostProcess"
{
    Properties
    {
        _PixelScale       ("Pixel Scale", Float) = 2
        _PosterizeSteps   ("Posterize Steps (RGB)", Vector) = (32,32,32,0)
        _DitherStrength   ("Dither Strength", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "PSXPostProcess"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float  _PixelScale;
            float4 _PosterizeSteps; // xyz used
            float  _DitherStrength;

            float3 Posterize(float3 c, float3 steps)
            {
                steps = max(steps, 1.0.xxx);
                return floor(c * steps) / steps;
            }

            static const float dither4x4[16] = {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv       = i.texcoord;
                float2 screenPx = uv * _ScreenParams.xy;
                float  scale    = max(_PixelScale, 1.0);
                float2 snappedPx= (floor(screenPx / scale) + 0.5) * scale;
                float2 snappedUV= snappedPx / _ScreenParams.xy;

                // POINT sampling keeps edges crisp/pixelated
                float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snappedUV);

                float3 pc = Posterize(src.rgb, _PosterizeSteps.xyz);
                int2  pi  = int2(snappedPx) & 3;
                float th  = (dither4x4[pi.x + pi.y * 4] + 0.5) / 16.0;
                float3 d  = pc + _DitherStrength * (th - 0.5);

                return float4(saturate(d), src.a);
            }
            ENDHLSL
        }
    }
}
