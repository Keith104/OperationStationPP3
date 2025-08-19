Shader "PSX/AffineLitLike"
{
    Properties
    {
        // keep Lit names so the inspector & values carry over
        [HideInInspector]_WorkflowMode("WorkflowMode", Float) = 1.0
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        [Gamma]_Metallic("Metallic", Range(0,1)) = 0.0
        _MetallicGlossMap("Metallic Map", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0 // GUI only

        _BumpScale("Normal Scale", Float) = 1.0
        [Normal]_BumpMap("Normal Map", 2D) = "bump" {}

        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Emission Color", Color) = (0,0,0,0)
        [HDR]_EmissionMap("Emission Map", 2D) = "black" {}

        // detail props so Lit GUI doesn't NRE
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Detail Albedo Scale", Range(0,2)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Detail Normal Scale", Range(0,2)) = 1.0
        [Normal]_DetailNormalMap("Detail Normal Map", 2D) = "bump" {}

        // hidden Lit state the inspector writes
        [HideInInspector]_Surface("__surface", Float) = 0.0
        [HideInInspector]_Blend("__blend",   Float) = 0.0
        [HideInInspector]_AlphaClip("__clip", Float) = 0.0
        [HideInInspector]_SrcBlend("__src",   Float) = 1.0
        [HideInInspector]_DstBlend("__dst",   Float) = 0.0
        [HideInInspector]_ZWrite("__zw",      Float) = 1.0
        [HideInInspector]_Cull("__cull",      Float) = 2.0
        _ReceiveShadows("Receive Shadows", Float) = 1.0
        [HideInInspector]_QueueOffset("Queue offset", Float) = 0.0

        // PSX extras
        _AffineAmount("Affine UV (0..1)", Range(0,1)) = 1.0
        _VertexSnap ("Vertex Snap Pixels", Range(0,4)) = 1.0
    }

    SubShader
    {
        Tags{ "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" "IgnoreProjector"="True" }
        LOD 300

        Pass
        {
            Name "UniversalForward"
            Tags{ "LightMode"="UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull   [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            // Lit feature keywords (match the GUI)
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICSPECGLOSSMAP
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _OCCLUSIONMAP
            #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature _SPECULAR_SETUP
            #pragma shader_feature _RECEIVE_SHADOWS_OFF

            // lighting/shadow variants
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // URP 17 includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

            // only our PSX params here (avoid redefining Lit props)
            CBUFFER_START(UnityPerMaterialPSX)
                float _AffineAmount;
                float _VertexSnap;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv0        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_Position;
                noperspective float2 uv : TEXCOORD0; // PS1-style affine (non-perspective interpolation) :contentReference[oaicite:0]{index=0}
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 tangentWS  : TEXCOORD3;
                float3 bitangentWS: TEXCOORD4;
                float4 fogAndVLight : TEXCOORD5; // x=fog, yzw=vertex lights
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 SnapNDC(float2 ndc, float snapPixels)
            {
                if (snapPixels <= 0.5) return ndc;
                float2 pixel = 1.0 / _ScreenParams.xy * snapPixels;
                return floor(ndc / pixel) * pixel;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(v.normalOS, v.tangentOS);

                float4 clip = pos.positionCS;
                float  w    = max(clip.w, 1e-6);

                // optional PS1 silhouette jitter
                float2 ndc = clip.xy / w;
                ndc = SnapNDC(ndc, _VertexSnap);
                clip.xy = ndc * w;

                o.positionCS = clip;
                o.positionWS = pos.positionWS;
                o.normalWS   = nrm.normalWS;
                o.tangentWS  = nrm.tangentWS;
                o.bitangentWS= nrm.bitangentWS;

                o.uv = v.uv0;

                half3 vLight = VertexLighting(pos.positionWS, o.normalWS);
                o.fogAndVLight = half4(ComputeFogFactor(clip.z), vLight);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // TBN
                float3x3 tbn = float3x3(normalize(i.tangentWS),
                                        normalize(i.bitangentWS),
                                        normalize(i.normalWS));

                // use BaseMap tiling/offset for all maps; TRANSFORM_TEX + _BaseMap_ST is guaranteed in URP docs :contentReference[oaicite:1]{index=1}
                float2 uv = TRANSFORM_TEX(i.uv, _BaseMap);

                // --- Base (albedo + alpha) ---
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half4 baseColor   = _BaseColor;
                half3 albedo = albedoAlpha.rgb * baseColor.rgb;
                half  alpha  = albedoAlpha.a   * baseColor.a;

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif
                #ifdef _ALPHAPREMULTIPLY_ON
                    albedo *= alpha;
                #endif

                // --- Normal ---
                half3 normalTS = half3(0,0,1);
                #ifdef _NORMALMAP
                    normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv),
                        _BumpScale
                    );
                #endif
                float3 normalWS = normalize(TransformTangentToWorld(normalTS, tbn));

                // --- Metallic/Smoothness (URP Lit packing R=metallic, A=smoothness) :contentReference[oaicite:2]{index=2}
                half metallic   = _Metallic;
                half smoothness = _Smoothness;
                #ifdef _METALLICSPECGLOSSMAP
                    half4 mg = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
                    metallic = mg.r;
                    #if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
                        smoothness = albedoAlpha.a;
                    #else
                        smoothness = mg.a;
                    #endif
                #endif

                // --- Occlusion ---
                #ifdef _OCCLUSIONMAP
                    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
                    occ = lerp(1.0h, occ, _OcclusionStrength);
                #else
                    half occ = 1.0h;
                #endif

                // --- Emission ---
                #ifdef _EMISSION
                    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
                #else
                    half3 emission = 0;
                #endif

                // --- SurfaceData (URP) ---
                SurfaceData s;
                s.albedo = albedo; s.alpha = alpha; s.normalTS = normalTS;
                s.metallic = metallic; s.specular = 0; s.smoothness = smoothness;
                s.occlusion = occ; s.emission = emission;
                s.clearCoatMask = 0; s.clearCoatSmoothness = 0;

                // --- InputData (URP 17 adds normalizedScreenSpaceUV) :contentReference[oaicite:3]{index=3}
                InputData inp = (InputData)0;
                inp.positionWS              = i.positionWS;
                inp.normalWS                = normalWS;
                inp.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inp.shadowCoord             = TransformWorldToShadowCoord(i.positionWS);
                inp.fogCoord                = i.fogAndVLight.x;
                inp.vertexLighting          = i.fogAndVLight.yzw;
                inp.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                // --- URP PBR lighting ---
                half4 col = UniversalFragmentPBR(inp, s);
                col.rgb = MixFog(col.rgb, inp.fogCoord);
                return col;
            }
            ENDHLSL
        }

        // reuse Lit’s passes so shadows/depth/meta just work
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
        // If your renderer uses it:
        // UsePass "Universal Render Pipeline/Lit/DepthNormalsOnly"
    }

    // keep Lit inspector
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
