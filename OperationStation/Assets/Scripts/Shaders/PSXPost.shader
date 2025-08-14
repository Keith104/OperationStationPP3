Shader "Custom/PSXPost"
{
    Properties
    {
        _PixelGrid("Pixel Grid (x,y)", Vector) = (480,270,0,0)
        _Steps("Posterize Steps", Float) = 26
        _DitherStrength("Dither Strength", Range(0,1)) = 0.55
        _EnablePixelate("Enable Pixelate (0/1)", Float) = 1
        _EnableDither("Enable Dither (0/1)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "PSXPost"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   Vert
            #pragma fragment Frag

            // --- Bound by URP Full Screen Pass ---
            Texture2D    _BlitTexture;
            SamplerState sampler_BlitTexture;
            // (1/w, 1/h, w, h)
            float4       _BlitTexture_TexelSize;

            // Per-material params
            cbuffer UnityPerMaterial
            {
                float2 _PixelGrid;       // e.g. 480,270
                float  _Steps;           // 24..36 typical
                float  _DitherStrength;  // 0..1
                float  _EnablePixelate;  // 0/1
                float  _EnableDither;    // 0/1
            };

            // Per-camera data (URP sets this).
            // 1 = Game, 2 = SceneView, 4 = Preview, etc.
            cbuffer UnityPerCamera
            {
                float _CameraType;
            };

            // ---------- Fullscreen triangle ----------
            float4 FS_TrianglePosition(uint id)
            {
                float2 pos = (id == 0) ? float2(-1.0, -1.0) :
                             (id == 1) ? float2(-1.0,  3.0) :
                                         float2( 3.0, -1.0);
                return float4(pos, 0.0, 1.0);
            }
            float2 FS_TriangleTexCoord(uint id)
            {
                return (id == 0) ? float2(0.0, 0.0) :
                       (id == 1) ? float2(0.0, 2.0) :
                                   float2(2.0, 0.0);
            }

            struct Attributes { uint   vertexID   : SV_VertexID; };
            struct Varyings   { float4 positionHCS: SV_Position; float2 uv: TEXCOORD0; };

            Varyings Vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = FS_TrianglePosition(IN.vertexID);
                OUT.uv          = FS_TriangleTexCoord(IN.vertexID);
                return OUT;
            }

            // ---------- Helpers ----------
            float2 PixelateUV(float2 uv, float2 grid, float enable)
            {
                if (enable < 0.5 || grid.x <= 0.0 || grid.y <= 0.0) return uv;
                return floor(uv * grid) / grid;
            }

            // 4x4 Bayer dithering prior to quantization
            float3 BayerDither(float2 uv, float3 c, float steps, float strength, float enable)
            {
                if (enable < 0.5 || strength <= 0.0 || steps <= 1.0) return c;

                float2 pixel = uv * _BlitTexture_TexelSize.zw;
                int2 p = int2(fmod(floor(pixel), 4.0));

                const float M[16] = {
                    0,8,2,10,
                    12,4,14,6,
                    3,11,1,9,
                    15,7,13,5
                };
                float t = (M[p.y*4 + p.x] + 0.5) / 16.0;

                float inv = 1.0 / (steps - 1.0);
                float3 jitter = (t - 0.5) * inv * strength;
                return saturate(c + jitter);
            }

            float3 Posterize(float3 c, float steps)
            {
                return floor(c * steps) / steps;
            }

            float4 Frag (Varyings IN) : SV_Target
            {
                // ----- Bypass Scene/Preview/etc. so Scene View stays clean -----
                // Treat any non-Game camera (>1.5) as editor and skip effect.
                if (_CameraType > 1.5)
                {
                    // Optionally handle possible top-origin UVs in some editor paths:
                    float2 uvBypass = IN.uv;
                #ifdef UNITY_UV_STARTS_AT_TOP
                    uvBypass.y = 1.0 - uvBypass.y;
                #endif
                    return _BlitTexture.Sample(sampler_BlitTexture, uvBypass);
                }

                // ----- Effect for Game cameras -----
                float2 uv  = PixelateUV(IN.uv, _PixelGrid, _EnablePixelate);

                // If you ever see a flipped Game View on a specific platform, enable this:
                // #ifdef UNITY_UV_STARTS_AT_TOP
                //     uv.y = 1.0 - uv.y;
                // #endif

                float3 col = _BlitTexture.Sample(sampler_BlitTexture, uv).rgb;
                col = BayerDither(IN.uv, col, _Steps, _DitherStrength, _EnableDither);
                col = Posterize(col, max(_Steps, 2.0));
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
