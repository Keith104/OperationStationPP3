Shader "UI/WipeURP"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {} // required by UGUI
        _Color("Overlay Color", Color) = (0,0,0,1)
        _Progress("Progress 0..1", Range(0,1)) = 0
        _Edge("Edge Softness (0..0.1)", Range(0,0.1)) = 0.02
        _Clockwise("Clockwise (1=yes)", Float) = 1
        _Center("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _StartAngleDeg("Start Angle (deg)", Range(-180,180)) = 90
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float  _Progress, _Edge, _Clockwise, _StartAngleDeg;
            float4 _Center;

            #ifndef TWO_PI
            #   define TWO_PI 6.28318530718
            #endif

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv  = v.uv;
                return o;
            }

            float wrap0_2pi(float a)
            {
                a = fmod(a, TWO_PI);
                if (a < 0.0) a += TWO_PI;
                return a;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 c = _Center.xy;
                float2 p = i.uv - c;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                p.x *= aspect;

                float theta = atan2(p.y, p.x);
                theta = wrap0_2pi(theta - radians(_StartAngleDeg));
                if (_Clockwise > 0.5) theta = TWO_PI - theta;

                float a01   = theta / TWO_PI;
                float front = saturate(_Progress);
                float e     = max(_Edge, 1e-5);

                float alpha = smoothstep(0.0, e, front - a01);
                alpha = max(alpha, step(1.0 - (e * 0.5), front));

                return float4(_Color.rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
