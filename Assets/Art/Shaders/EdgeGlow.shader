// Shader "Custom/URP2DSquareInwardGlow"
// {
//     Properties
//     {
//         [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
//         _Color ("Tint Color", Color) = (1, 1, 1, 1)

//         [Header(Inward Glow Settings)]
//         [HDR] _GlowColor ("Glow Color", Color) = (0, 1, 1, 4)
//         _BorderWidth ("Border Width (World Units)", Range(0.0, 5.0)) = 0.2
//         _GlowFalloff ("Glow Smoothness / Falloff", Range(0.1, 4.0)) = 1.5
//     }

//     SubShader
//     {
//         Tags
//         {
//             "Queue" = "Transparent"
//             "RenderType" = "Transparent"
//             "RenderPipeline" = "UniversalPipeline"
//             "CanUseSpriteAtlas" = "True"
//             "IgnoreProjector" = "True"
//             "PreviewType" = "Plane"
//         }

//         Cull Off
//         Lighting Off
//         ZWrite Off
//         Blend SrcAlpha OneMinusSrcAlpha

//         Pass
//         {
//             Name "SquareInwardGlow"
//             Tags { "LightMode" = "SRPDefaultUnlit" }

//             HLSLPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag

//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//             struct Attributes
//             {
//                 float4 positionOS   : POSITION;
//                 float4 color        : COLOR;
//                 float2 uv           : TEXCOORD0;
//             };

//             struct Varyings
//             {
//                 float4 positionCS   : SV_POSITION;
//                 float4 color        : COLOR;
//                 float2 uv           : TEXCOORD0;
//                 float2 dimensions   : TEXCOORD1; // Stores our calculated world-space width and height
//             };

//             TEXTURE2D(_MainTex);
//             SAMPLER(sampler_MainTex);

//             CBUFFER_START(UnityPerMaterial)
//                 float4 _MainTex_ST;
//                 float4 _Color;
//                 float4 _GlowColor;
//                 float _BorderWidth;
//                 float _GlowFalloff;
//             CBUFFER_END

//             Varyings vert(Attributes input)
//             {
//                 Varyings output;
//                 output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
//                 output.uv = TRANSFORM_TEX(input.uv, _MainTex);
//                 output.color = input.color * _Color;

//                 // 1. CALCULATE THE DIMENSIONS OF THE SQUARE
//                 // By transforming object-space unit vectors into world space and measuring their length,
//                 // we dynamically calculate the exact physical dimensions of the square mesh/sprite!
//                 float3 worldOrigin = TransformObjectToWorld(float3(0, 0, 0));
//                 float3 worldX      = TransformObjectToWorld(float3(1, 0, 0));
//                 float3 worldY      = TransformObjectToWorld(float3(0, 1, 0));

//                 output.dimensions = float2(length(worldX - worldOrigin), length(worldY - worldOrigin));

//                 return output;
//             }

//             half4 frag(Varyings input) : SV_Target
//             {
//                 // 1. Sample the base sprite and tint
//                 half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

//                 // 2. ANALYTICAL INWARD EDGE DETECTION (Box Distance Math)
//                 // UVs range from 0.0 to 1.0. The center of the square is at (0.5, 0.5).
//                 // abs(input.uv - 0.5) gives distance from center (0.0 at center, 0.5 at edge).
//                 // Subtracting from 0.5 gives distance TO the edge (0.0 at edge, 0.5 at center).
//                 float2 distToEdgeUV = 0.5 - abs(input.uv - 0.5);

//                 // 3. Convert UV distance into physical World Space distance using our calculated dimensions
//                 float2 distToEdgeWorld = distToEdgeUV * input.dimensions;

//                 // The closest edge determines our border distance
//                 float minDistToEdge = min(distToEdgeWorld.x, distToEdgeWorld.y);

//                 // 4. Calculate Glow Factor (1.0 at the exact border, dropping to 0.0 inside)
//                 // We use saturate to clamp the division so the glow stops completely past _BorderWidth
//                 half glowFactor = 1.0 - saturate(minDistToEdge / max(_BorderWidth, 0.0001));

//                 // Apply an exponential curve to give it a natural, luminous "neon" falloff
//                 glowFactor = pow(glowFactor, _GlowFalloff);

//                 // 5. Blend the inward glow over the base texture color
//                 // We only apply the glow where the base sprite actually has opacity
//                 half3 finalRGB = lerp(baseColor.rgb, _GlowColor.rgb, glowFactor * step(0.001, baseColor.a));

//                 // Additive alpha boost for transparent backgrounds
//                 half finalAlpha = saturate(baseColor.a + (glowFactor * _GlowColor.a * baseColor.a));

//                 return half4(finalRGB, finalAlpha);
//             }
//             ENDHLSL
//         }
//     }
// }



























// Shader "Custom/URP2DSquareLitGlow"
// {
//     Properties
//     {
//         [MainTexture]_MainTex ("Sprite Texture", 2D) = "white" {}
//         _Color ("Tint Color", Color) = (1, 1, 1, 1)

//         [Header(Edge Glow Settings)]
//         [HDR]_EdgeColor ("HDR Edge Color", Color) = (0, 1, 1, 1)
//         _GlowIntensity ("Glow Intensity", Range(0.0, 10.0)) = 2.0
//         _EdgeWidth ("Edge Width (UV Space)", Range(0.001, 0.5)) = 0.1
//         _EdgeSoftness ("Edge Softness", Range(0.0001, 0.5)) = 0.05
//     }

//     SubShader
//     {
//         Tags
//         {
//             "Queue" = "Transparent"
//             "RenderType" = "Transparent"
//             "RenderPipeline" = "UniversalPipeline"
//             "CanUseSpriteAtlas" = "True"
//             "IgnoreProjector" = "True"
//             "PreviewType" = "Plane"
//         }

//         Cull Off
//         ZWrite Off
//         Blend SrcAlpha OneMinusSrcAlpha

//         Pass
//         {
//             Name "SpriteUniversal2D"
//             // Universal2D ensures the URP 2D Renderer processes this pass for 2D Lights
//             Tags { "LightMode" = "Universal2D" }

//             HLSLPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag

//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//             struct Attributes
//             {
//                 float4 positionOS : POSITION;
//                 float4 color : COLOR;
//                 float2 uv : TEXCOORD0;
//             };

//             struct Varyings
//             {
//                 float4 positionCS : SV_POSITION;
//                 float4 color : COLOR;
//                 float2 uv : TEXCOORD0;
//             };

//             TEXTURE2D(_MainTex);
//             SAMPLER(sampler_MainTex);

//             // SRP Batcher compatibility block
//             CBUFFER_START(UnityPerMaterial)
//             float4 _MainTex_ST;
//             float4 _Color;
//             float4 _EdgeColor;
//             float _GlowIntensity;
//             float _EdgeWidth;
//             float _EdgeSoftness;
//             CBUFFER_END

//             Varyings vert(Attributes input)
//             {
//                 Varyings output;
//                 output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
//                 output.uv = TRANSFORM_TEX(input.uv, _MainTex);
//                 output.color = input.color * _Color;
//                 return output;
//             }

//             half4 frag(Varyings input) : SV_Target
//             {
//                 // 1. Sample base sprite texture and apply SpriteRenderer vertex color tint
//                 half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

//                 // 2. Normalized UV Edge Distance Algorithm
//                 // Finds the shortest distance from the current pixel to any of the 4 square boundaries.
//                 // Returns 0.0 exactly on the border, increasing up to 0.5 at the exact center of the square.
//                 float edgeDist = min(min(input.uv.x, input.uv.y), min(1.0 - input.uv.x, 1.0 - input.uv.y));

//                 // 3. Smooth Step Glow Falloff
//                 // Maps the distance to a clean 0-to-1 gradient based on width and softness controls.
//                 float innerBoundary = max(_EdgeWidth - _EdgeSoftness, 0.0001);
//                 half glowFactor = 1.0 - smoothstep(innerBoundary, _EdgeWidth, edgeDist);

//                 // 4. Emissive Color & Alpha Isolation
//                 // We multiply by _GlowIntensity here rather than relying on HDR alpha channel scaling.
//                 half3 emissiveGlow = _EdgeColor.rgb * _GlowIntensity;

//                 // Lerp the base RGB toward the emissive glow so borders remain punchy over dark textures
//                 half3 finalRGB = lerp(baseColor.rgb, emissiveGlow, glowFactor * step(0.001, baseColor.a));

//                 // ISOLATED ALPHA: Keep transparency strictly bound by the sprite's texture alpha.
//                 // This prevents HDR alpha values (>1.0) from turning transparent areas into solid blocks.
//                 half finalAlpha = baseColor.a;

//                 return half4(finalRGB, finalAlpha);
//             }
//             ENDHLSL
//         }
//     }
// }








































Shader "Custom/URP2DSquareCircumferenceGlow"
{
    Properties
    {
        [MainTexture]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)

        //[Header(Circumference Straddling Setup)]
        //[Tooltip("Defines where the physical square border sits in UV space. Leave space below 1.0 for outward glow!")]
        _SquareSize ("Square Border Scale", Range(0.1, 1.0)) = 0.75
        _GlowWidth ("Glow Thickness", Range(0.001, 0.5)) = 0.15
        _GlowFalloff ("Glow Smoothness / Falloff", Range(0.1, 4.0)) = 1.5

        //[Header(Glow Color & Intensity)]
        [HDR]_GlowColor ("HDR Glow Color", Color) = (0, 1, 1, 1)
        _GlowIntensity ("Glow Intensity (For Bloom)", Range(0.0, 20.0)) = 3.0

        //[Header(Animated Travel Around Perimeter)]
        //[Tooltip("Set speed to 0 for a solid, static circumference border.")]
        _TravelSpeed ("Travel Speed Around Edges", Range( -10.0, 10.0)) = 1.5
        _TrailLength ("Travel Trail Length", Range(0.01, 1.0)) = 0.5
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
            // Universal2D ensures URP 2D Lights and sorting layers process this sprite
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

            // SRP Batcher CBuffer block
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GlowColor;
            float _SquareSize;
            float _GlowWidth;
            float _GlowFalloff;
            float _GlowIntensity;
            float _TravelSpeed;
            float _TrailLength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Center UVs from [0, 1] to [-1, +1]
                float2 centeredUV = (input.uv - 0.5) * 2.0;

                // 2. Analytical Square Perimeter Math (Chebyshev Distance)
                // max(|x|, |y|) measures concentric square rings expanding from the center.
                float squareDist = max(abs(centeredUV.x), abs(centeredUV.y));

                // 3. Circumference Straddling
                // When squareDist equals _SquareSize, distFromLine is EXACTLY 0.0 (sitting right on the perimeter!).
                // Using abs() ensures the light attenuates symmetrically INWARD and OUTWARD from the line.
                float distFromLine = abs(squareDist - _SquareSize);

                // 4. Calculate Luminous Falloff
                float glowFactor = 1.0 - smoothstep(0.0, _GlowWidth, distFromLine);
                glowFactor = pow(max(glowFactor, 0.0), _GlowFalloff);

                // 5. Perimeter Travel Animation (Runs around the 4 edges over time)
                // Calculate angular progress (0.0 to 1.0) around the square perimeter
                float angle = (atan2(centeredUV.y, centeredUV.x) / 6.28318530718) + 0.5;
                float travelProgress = frac(angle - (_Time.y * _TravelSpeed));
                float trailFactor = smoothstep(0.0, _TrailLength, travelProgress);

                // If Travel Speed is non-zero, multiply the glow by the traveling trail mask
                glowFactor *= lerp(1.0, trailFactor, step(0.001, abs(_TravelSpeed)));

                // 6. Texture Remapping
                // Remap the base sprite graphic so it fits snugly inside the defined _SquareSize boundary
                float2 remappedUV = (centeredUV / max(_SquareSize, 0.0001)) * 0.5 + 0.5;
                half inBounds = step(0.0, remappedUV.x) * step(remappedUV.x, 1.0) * step(0.0, remappedUV.y) * step(remappedUV.y, 1.0);
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, remappedUV) * input.color * inBounds;

                // 7. Emissive & Transparency Blending
                // Decouple HDR RGB emission from alpha so bloom triggers without turning transparent space opaque
                half3 emissive = _GlowColor.rgb * _GlowIntensity * glowFactor;

                // Blend emissive light over solid pixels, and additively over empty padding space
                half3 finalRGB = max(lerp(baseColor.rgb, emissive, glowFactor), emissive);
                half finalAlpha = saturate(baseColor.a + (glowFactor * _GlowColor.a));

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}