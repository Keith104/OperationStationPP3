Shader "Hidden/PSXPostProcess"
{
    Properties
    {
        _PixelScale       ("Pixel Scale", Float) = 2
        _PosterizeSteps   ("Posterize Steps (RGB)", Vector) = (32,32,32,0)
        // Fixed, very low dither; slider removed by request.
        //_DitherStrength  ("Dither Strength", Range(0,1)) = 0.00
        [Toggle]_EnableDither ("Enable Dither", Float) = 1
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

            // Per-material data (SRP Batcher compatible) :contentReference[oaicite:3]{index=3}
            CBUFFER_START(UnityPerMaterial)
                float  _PixelScale;
                float4 _PosterizeSteps;            // xyz used
                float  _EnableDither;
            CBUFFER_END

            // Unbiased (nearest) posterize — preserves average brightness
            float3 PosterizeRound(float3 c, float3 steps)
            {
                steps = max(steps, 1.0.xxx);
                return floor(c * steps + 0.5.xxx) / steps;    // nearest neighbor (round) :contentReference[oaicite:4]{index=4}
            }

            // 4×4 Bayer thresholds (0..15) — classic ordered dithering map :contentReference[oaicite:5]{index=5}
            static const float dither4x4[16] = {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Pixel snap for PSX look
                float2 uv       = i.texcoord;
                float2 screenPx = uv * _ScreenParams.xy;
                float  scale    = max(_PixelScale, 1.0);
                float2 snappedPx= (floor(screenPx / scale) + 0.5) * scale;
                float2 snappedUV= snappedPx / _ScreenParams.xy;

                // Point-sample the camera color from the blit texture (URP Full Screen Pass) :contentReference[oaicite:6]{index=6}
                float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snappedUV);

                // Posterize in-place (nearest, not floor) to remove dark bias
                float3 steps = max(_PosterizeSteps.xyz, 1.0.xxx);
                float3 pc    = PosterizeRound(src.rgb, steps);

                // Dither OFF? Return pure posterized color.
                if (_EnableDither < 0.5)
                    return float4(pc, src.a);

                // Ordered dither in [-1,1] with very small, adaptive amplitude
                int2  pi  = int2(snappedPx) & 3;
                float b   = (dither4x4[pi.x + pi.y * 4] + 0.5) / 16.0;  // 0..1
                float sgn = b * 2.0 - 1.0;                               // -1..1

                // Keep dither safely within the quantization interval and fade near 0/1
                float3 stepSize = 1.0 / steps;                           // quantization width
                float3 edge     = smoothstep(0.0, 0.85, saturate(min(pc, 1.0 - pc) * 2.0));

                // VERY LOW strength (fixed): about 1% of the step at mid-tones
                const float kFixed = 0.01;                               
                float3 amp = kFixed * stepSize * edge;

                float3 outRGB = saturate(pc + amp * sgn);
                return float4(outRGB, src.a);
            }
            ENDHLSL
        }
    }
}
