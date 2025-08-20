Shader "PSX/AffineUnlit"
{
    Properties
    {
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {}
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)

        [Toggle(_SURFACE_TYPE_TRANSPARENT)] _Transparent("Surface Type: Transparent", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Clip Threshold", Range(0,1)) = 0.5

        [Toggle(_DEPTH_PREPASS_ON)] _DepthPrepass("Self-Occluding (Depth Prepass)", Float) = 0

        [HideInInspector]_SrcBlend("SrcBlend", Float) = 1
        [HideInInspector]_DstBlend("DstBlend", Float) = 0
        [HideInInspector]_ZWrite ("ZWrite",  Float) = 1

        _TargetRes("Vertex Snap Target Res (x,y)", Vector) = (320,240,0,0)
        [Toggle(_VERTEX_SNAP_ON)] _VertexSnap("Enable Vertex Snap", Float) = 1

        _AffineStrength("Affine Strength", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Back

        // ---------- Depth prepass ----------
        Pass
        {
            Name "DepthOnlyPrepass"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment fragDepth
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _DEPTH_PREPASS_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _Cutoff;
                float2 _TargetRes;
                float  _VertexSnap;
                float  _AffineStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uvPersp    : TEXCOORD0;
                noperspective float2 uvAffine : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);

                if (_VertexSnap > 0.5)
                {
                    float2 grid = _TargetRes * 0.5;
                    float invW = 1.0 / posCS.w;
                    float2 ndc = posCS.xy * invW;
                    ndc = floor(ndc * grid) / grid;
                    posCS.xy = ndc * posCS.w;
                }

                OUT.positionCS = posCS;
                float2 uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uvPersp  = uv;
                OUT.uvAffine = uv;
                return OUT;
            }

            half4 fragDepth (Varyings IN) : SV_Target
            {
                #if !defined(_DEPTH_PREPASS_ON)
                    clip(-1);
                #endif
                #if defined(_ALPHATEST_ON)
                    half a = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uvPersp) * _BaseColor).a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------- Forward ----------
        Pass
        {
            Name "UnlitForward"
            Tags { "LightMode"="UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _SURFACE_TYPE_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _Cutoff;
                float2 _TargetRes;
                float  _VertexSnap;
                float  _AffineStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uvPersp    : TEXCOORD0;
                noperspective float2 uvAffine : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);

                if (_VertexSnap > 0.5)
                {
                    float2 grid = _TargetRes * 0.5;
                    float invW = 1.0 / posCS.w;
                    float2 ndc = posCS.xy * invW;
                    ndc = floor(ndc * grid) / grid;
                    posCS.xy = ndc * posCS.w;
                }

                OUT.positionCS = posCS;
                float2 uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uvPersp  = uv;
                OUT.uvAffine = uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 colP = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uvPersp);
                half4 colA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uvAffine);
                half  t    = saturate(_AffineStrength);
                half4 col  = lerp(colP, colA, t) * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(col.a - _Cutoff);
                #endif
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
