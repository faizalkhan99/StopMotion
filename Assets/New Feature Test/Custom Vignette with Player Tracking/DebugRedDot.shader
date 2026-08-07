Shader "Custom/FollowVignette"
{
    Properties
    {
        _Center ("Center", Vector) = (0.5,0.5,0,0)
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
                float4 _Center;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Convert pixel position to normalized screen coordinates
                float2 uv = IN.positionHCS.xy / _ScreenParams.xy;

                // Flip Y because viewport origin is bottom-left
                uv.y = 1.0 - uv.y;

                float d = distance(uv, _Center.xy);

                if (d < 0.01)
                    return half4(1,0,0,1);

                return half4(0,0,0,0.8);
            }

            ENDHLSL
        }
    }
}