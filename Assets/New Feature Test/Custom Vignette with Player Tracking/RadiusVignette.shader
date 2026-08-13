Shader "Custom/RadiusVignette"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0,0,0,0.8)
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Softness ("Softness", Range(0.001,0.5)) = 0.1

        _StartRadius ("Start Radius", Range(0,1)) = 0.6
        _EndRadius ("End Radius", Range(0,1)) = 0.05

        _Duration ("Countdown Duration (s)", Float) = 10
        _StartTime ("Countdown Start Time (_Time.y snapshot)", Float) = 0
        _PausedRadius ("Radius When Paused/Stopped", Range(0,1)) = 0.6
        _IsRunning ("Is Running (0/1)", Range(0,1)) = 0
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
                float _Softness;
                float _StartRadius;
                float _EndRadius;
                float _Duration;
                float _StartTime;
                float _PausedRadius;
                float _IsRunning;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionHCS.xy / _ScreenParams.xy;
                screenUV.y = 1.0 - screenUV.y;

                float d = distance(screenUV, _Center.xy);

                // Work out the current radius.
                float safeDuration = max(_Duration, 0.0001);
                float t = saturate((_Time.y - _StartTime) / safeDuration);
                float runningRadius = lerp(_StartRadius, _EndRadius, t);

                // When stopped/paused, hold whatever radius was frozen in.
                float radius = lerp(_PausedRadius, runningRadius, _IsRunning);

                float alpha = smoothstep(radius, radius + _Softness, d);

                return half4(_Color.rgb, _Color.a * alpha);
            }

            ENDHLSL
        }
    }
}