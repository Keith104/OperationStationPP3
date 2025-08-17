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
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "IgnoreProjector"="True" }

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

            // ---- Bound by URP Full Screen Pass ----
            // Source frame
            Texture2D    _BlitTexture;
            SamplerState sampler_BlitTexture;
            // (1/w, 1/h, w, h)  NOTE: y can be NEGATIVE when a flip is required.
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

            // Per-camera (URP sets this): 1=Game, 2=SceneView, 4=Preview...
            cbuffer UnityPerCamera
            {
                float _CameraType;
            };

            // -------- Fullscreen triangle (no mesh) --------
            float4 FS_Pos(uint id)
            {
                float2 p = (id == 0) ? float2(-1,-1) :
                           (id == 1) ? float2(-1, 3) :
                                       float2( 3,-1);
                return float4(p, 0, 1);
            }
            float2 FS_UV(uint id)
            {
                // 0..1 across the visible area via 0..2 UVs on a big triangle
                return (id == 0) ? float2(0,0) :
                       (id == 1) ? float2(0,2) :
                                   float2(2,0);
            }

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 pos : SV_Position; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.pos = FS_Pos(IN.vertexID);
                OUT.uv  = FS_UV(IN.vertexID);
                return OUT;
            }

            // -------- Helpers --------
            // Conditional UV flip (Unity-recommended): flip only when top-origin AND texelSize.y < 0
            float2 FixUV(float2 uv)
            {
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    if (_BlitTexture_TexelSize.y < 0.0)
                        uv.y = 1.0 - uv.y;
                #endif
                return uv;
            }

            float2 PixelateUV(float2 uv, float2 grid, float enable)
            {
                if (enable < 0.5 || grid.x <= 0.0 || grid.y <= 0.0) return uv;
                return floor(uv * grid) / grid;
            }

            // 4x4 Bayer dithering before quantization
            float3 BayerDither(float2 uvForPattern, float3 c, float steps, float strength, float enable)
            {
                if (enable < 0.5 || strength <= 0.0 || steps <= 1.0) return c;

                float2 pixel = uvForPattern * _BlitTexture_TexelSize.zw; // (width,height)
                int2 p = int2(fmod(floor(pixel), 4.0));

                const float M[16] = {
                    0,8,2,10,
                    12,4,14,6,
                    3,11,1,9,
                    15,7,13,5
                };
                float t = (M[p.y*4 + p.x] + 0.5) / 16.0;

                float inv = 1.0 / (steps - 1.0);
                return saturate(c + (t - 0.5) * inv * strength);
            }

            float3 Posterize(float3 c, float steps)
            {
                return floor(c * steps) / steps;
            }

            float4 Frag (Varyings IN) : SV_Target
            {
                // ---- Keep Scene/Preview clean ----
                if (_CameraType > 1.5) // non-Game cameras
                {
                    float2 uvBypass = FixUV(IN.uv);
                    return _BlitTexture.Sample(sampler_BlitTexture, uvBypass);
                }

                // ---- Game cameras: apply effect ----
                // Get correct-orientation UV first
                float2 uvScreen = FixUV(IN.uv);

                // Pixelation works on screen-space UVs
                float2 uvSample = PixelateUV(uvScreen, _PixelGrid, _EnablePixelate);

                float3 col = _BlitTexture.Sample(sampler_BlitTexture, uvSample).rgb;

                // Use unpixelated screen UV for a stable Bayer pattern
                col = BayerDither(uvScreen, col, _Steps, _DitherStrength, _EnableDither);

                col = Posterize(col, max(_Steps, 2.0));
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
