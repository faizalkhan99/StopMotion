Shader "Custom/FollowVignette"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0,0,0,0.8)
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Range(0,1)) = 0.2
        _Softness ("Softness", Range(0.001,0.5)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Center;
                float _Radius;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Screen-space UV (0-1)
                float2 screenUV = IN.positionHCS.xy / _ScreenParams.xy;

                // Viewport coordinates have origin at bottom-left
                screenUV.y = 1.0 - screenUV.y;

                // Distance from current pixel to spotlight center
                float d = distance(screenUV, _Center.xy);

                // Soft edge
                float alpha = smoothstep(_Radius, _Radius + _Softness, d);

                // Black overlay with transparent hole
                return half4(_Color.rgb, _Color.a * alpha);
            }

            ENDHLSL
        }
    }
}