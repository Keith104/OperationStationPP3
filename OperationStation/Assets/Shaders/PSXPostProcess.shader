Shader "Hidden/PSXPostProcess"
{
    Properties
    {
        _PixelScale     ("Pixel Scale", Float) = 2
        _PosterizeSteps ("Posterize Steps (RGB)", Vector) = (32,32,32,0)
        [Toggle]_EnableDither ("Enable Dither", Float) = 1
    }

    SubShader
    {
        // Use the URP tag that many versions require in builds
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "PSXPostProcess"
            HLSLPROGRAM
            #pragma target   3.0
            #pragma vertex   Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _PixelScale;
                float4 _PosterizeSteps;   // xyz used
                float  _EnableDither;
            CBUFFER_END

            float3 PosterizeRound(float3 c, float3 steps)
            {
                steps = max(steps, 1.0.xxx);
                return floor(c * steps + 0.5.xxx) / steps;
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

                // URP blit source
                float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snappedUV);

                float3 steps = max(_PosterizeSteps.xyz, 1.0.xxx);
                float3 pc    = PosterizeRound(src.rgb, steps);

                if (_EnableDither < 0.5)
                    return float4(pc, src.a);

                int2  pi  = int2(snappedPx) & 3;
                float b   = (dither4x4[pi.x + pi.y * 4] + 0.5) / 16.0;
                float sgn = b * 2.0 - 1.0;

                float3 stepSize = 1.0 / steps;
                float3 edge     = smoothstep(0.0, 0.85, saturate(min(pc, 1.0 - pc) * 2.0));
                const float kFixed = 0.01;
                float3 amp = kFixed * stepSize * edge;

                float3 outRGB = saturate(pc + amp * sgn);
                return float4(outRGB, src.a);
            }
            ENDHLSL
        }
    }
}
