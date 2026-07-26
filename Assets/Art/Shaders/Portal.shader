Shader "Custom/URP2DMagicalPortal"
{
    Properties
    {
        [MainTexture]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)

        //[Header(Portal Shape & Boundaries)];
        //[Tooltip("Radius of the circular portal (0.5 is the exact edge of the sprite boundary).")]
        _PortalRadius ("Portal Outer Radius", Range(0.1, 0.5)) = 0.45
        _EdgeSoftness ("Outer Edge Softness", Range(0.001, 0.2)) = 0.05
        _CoreRadius ("Center Void / Core Radius", Range(0.0, 0.3)) = 0.08

        //[Header(Swirl & Vortex Magic)]
        _SwirlSpeed ("Vortex Rotation Speed", Range( -10.0, 10.0)) = 2.5
        _TwistStrength ("Spiral Twist Strength", Range( -20.0, 20.0)) = 8.0
        _ArmCount ("Number of Magic Spiral Arms", Range(1.0, 12.0)) = 4.0
        _ArmSharpness ("Spiral Arm Sharpness", Range(0.5, 8.0)) = 2.0

        //[Header(VIBGYOR Color Controls)]
        _ColorFrequency ("Rainbow Repeat Frequency", Range(0.5, 5.0)) = 1.0
        _ColorCycleSpeed ("Color Cycling Speed", Range( -5.0, 5.0)) = 1.0
        _ColorSaturation ("Magic Color Saturation", Range(0.0, 1.0)) = 0.85

        //[Header(Glow & Emission)]
        [HDR]_CoreGlowColor ("Center Core HDR Color", Color) = (3, 3, 3, 1)
        _GlowIntensity ("Overall Glow Bloom Intensity", Range(0.0, 15.0)) = 3.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteUniversal2D"
            // Ensures full compatibility with URP 2D Renderer and 2D Lights
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Strict CBuffer definition for Unity 6.5 SRP Batcher dynamic batching
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _CoreGlowColor;
            float _PortalRadius;
            float _EdgeSoftness;
            float _CoreRadius;
            float _SwirlSpeed;
            float _TwistStrength;
            float _ArmCount;
            float _ArmSharpness;
            float _ColorFrequency;
            float _ColorCycleSpeed;
            float _ColorSaturation;
            float _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            // High-performance, branchless HSV to RGB color converter
            // Guarantees smooth, continuous VIBGYOR transitions without conditional jumps
            half3 HSVtoRGB(float h, float s, float v)
            {
                float3 res = frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0;
                res = saturate(abs(res) - 1.0);
                return v * lerp(1.0, res, s);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Center UVs from [0, 1] to [-1, +1] space
                float2 centeredUV = (input.uv - 0.5) * 2.0;

                // 2. Convert to Polar Coordinates (Radius and Angle)
                float radius = length(centeredUV);
                float angle = atan2(centeredUV.y, centeredUV.x); // Returns -PI to +PI

                // 3. Vortex Swirl Math
                // As pixels get closer to the center, we twist their angle logarithmically over time
                float swirledAngle = angle + (radius * _TwistStrength) - (_Time.y * _SwirlSpeed);

                // 4. Generate Magic Spiral Arms
                // Using sine waves wrapped around the swirled angle to create distinct glowing energy filaments
                float spiralArms = sin(swirledAngle * _ArmCount) * 0.5 + 0.5;
                spiralArms = pow(spiralArms, _ArmSharpness); // Sharpen the energy beams

                // 5. Procedural VIBGYOR (Rainbow) Color Generation
                // Map hue along the swirled angle and radius, shifting dynamically with time
                float hue = frac((swirledAngle / 6.28318530718) + (radius * _ColorFrequency) - (_Time.y * _ColorCycleSpeed));

                // Convert hue to RGB, pulsing brightness slightly along the spiral arms
                half3 vibgyorColor = HSVtoRGB(hue, _ColorSaturation, 1.0);
                half3 magicEnergy = vibgyorColor * (spiralArms + 0.2) * _GlowIntensity;

                // 6. Portal Boundary & Core Masking
                // Smoothly clip the outer circle so it never hits the hard square corners of the mesh
                float outerMask = 1.0 - smoothstep(_PortalRadius - _EdgeSoftness, _PortalRadius, radius * 0.5);

                // Create a bright or dark core at the very center of the vortex
                float coreMask = smoothstep(0.0, _CoreRadius, radius * 0.5);
                float coreGlow = 1.0 - smoothstep(0.0, _CoreRadius * 2.0, radius * 0.5);

                // 7. Final Color & Transparency Assembly
                // Blend the VIBGYOR arms into the mystical center core color
                half3 finalRGB = lerp(_CoreGlowColor.rgb * _GlowIntensity, magicEnergy, coreMask);
                finalRGB += (_CoreGlowColor.rgb * coreGlow); // Add extra bloom punch to the center

                // Multiply by base sprite texture and vertex tint for full SpriteRenderer integration
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                finalRGB *= baseTex.rgb;

                // Alpha is strictly constrained by the outer circle boundary and core transparency
                half finalAlpha = saturate(outerMask * baseTex.a * lerp(0.3, 1.0, spiralArms));

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}