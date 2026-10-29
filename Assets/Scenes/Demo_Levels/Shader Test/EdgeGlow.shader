// Shader "SpriteInnerEdgeGlow"
// {
//     Properties
//     {
//         [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
//         _Color ("Tint", Color) = (1,1,1,1)

//         [Header(Edge Glow)]
//         [HDR] _EdgeColor ("Edge Color", Color) = (0.2, 0.9, 1, 1)
//         _EdgeWidth ("Edge Width (texels)", Range(0.5, 32)) = 4
//         _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2

//         [Header(Options)]
//         [Toggle] _HighQuality ("High Quality (16 taps, wide edges)", Float) = 0
//         [Toggle] _UVBorder ("Texture Border Counts As Edge", Float) = 1
//     }

//     SubShader
//     {
//         Tags
//         {
//             "Queue" = "Transparent"
//             "RenderType" = "Transparent"
//             "RenderPipeline" = "UniversalPipeline"
//             "IgnoreProjector" = "True"
//             "CanUseSpriteAtlas" = "True"
//         }

//         Cull Off
//         ZWrite Off
//         Blend SrcAlpha OneMinusSrcAlpha

//         HLSLINCLUDE
//         #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//         TEXTURE2D(_MainTex);
//         SAMPLER(sampler_MainTex);

//         CBUFFER_START(UnityPerMaterial)
//             float4 _MainTex_ST;
//             float4 _MainTex_TexelSize;
//             half4  _Color;
//             half4  _EdgeColor;
//             half   _EdgeWidth;
//             half   _EdgeIntensity;
//             half   _HighQuality;
//             half   _UVBorder;
//         CBUFFER_END

//         struct Attributes
//         {
//             float3 positionOS : POSITION;
//             float4 color      : COLOR;
//             float2 uv         : TEXCOORD0;
//         };

//         struct Varyings
//         {
//             float4 positionCS : SV_POSITION;
//             half4  color      : COLOR;
//             float2 uv         : TEXCOORD0;
//         };

//         // 8 directions on a unit circle (diagonals normalised -> round, even ring)
//         static const float2 kDirs[8] =
//         {
//             float2( 1.0,    0.0),   float2(-1.0,    0.0),
//             float2( 0.0,    1.0),   float2( 0.0,   -1.0),
//             float2( 0.7071, 0.7071), float2(-0.7071, 0.7071),
//             float2( 0.7071,-0.7071), float2(-0.7071,-0.7071)
//         };

//         Varyings vert(Attributes IN)
//         {
//             Varyings OUT;
//             OUT.positionCS = TransformObjectToHClip(IN.positionOS);
//             OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
//             OUT.color = half4(IN.color) * _Color;
//             return OUT;
//         }

//         // Alpha of a neighbour tap. Optionally treats outside of 0..1 UV as empty,
//         // so shapes that fill the whole texture (plain squares) still glow on the border.
//         half TapAlpha(float2 uv)
//         {
//             half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
//             if (_UVBorder > 0.5h)
//             {
//                 float2 inside = step(0.0, uv) * step(uv, 1.0);
//                 a *= inside.x * inside.y;
//             }
//             return a;
//         }

//         half4 frag(Varyings IN) : SV_Target
//         {
//             half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

//             // Edge width in texels -> UV offset
//             float2 o = _MainTex_TexelSize.xy * _EdgeWidth;

//             // Ring sampling: sum of neighbour alpha
//             half sum = 0.0h;
//             half count = 8.0h;

//             [unroll]
//             for (int i = 0; i < 8; i++)
//                 sum += TapAlpha(IN.uv + kDirs[i] * o);

//             // Optional inner ring (half radius) = smoother falloff on wide edges
//             if (_HighQuality > 0.5h)
//             {
//                 [unroll]
//                 for (int j = 0; j < 8; j++)
//                     sum += TapAlpha(IN.uv + kDirs[j] * o * 0.5);
//                 count = 16.0h;
//             }

//             // Fraction of the ring that is empty. ~0.5 on a straight edge, more at
//             // convex corners, 0 deep inside. *2 normalises a straight edge to ~1.
//             half edge = saturate((1.0h - sum / count) * 2.0h);
//             edge = edge * edge * (3.0h - 2.0h * edge);   // smoothstep
//             edge *= tex.a;                               // inward only

//             half4 col;
//             col.rgb = tex.rgb * IN.color.rgb + _EdgeColor.rgb * (edge * _EdgeIntensity);
//             col.a   = tex.a * IN.color.a;
//             return col;
//         }
//         ENDHLSL

//         // 2D Renderer
//         Pass
//         {
//             Name "Universal2D"
//             Tags { "LightMode" = "Universal2D" }
//             HLSLPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             ENDHLSL
//         }

//         // Forward / Unlit fallback
//         Pass
//         {
//             Name "UniversalForward"
//             Tags { "LightMode" = "UniversalForward" }
//             HLSLPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             ENDHLSL
//         }
//     }

//     FallBack "Sprites/Default"
// }












































Shader "Custom/URP2D/SpriteInnerEdgeGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Edge Glow)]
        [HDR] _EdgeColor ("Edge Color", Color) = (0.2, 0.9, 1, 1)
        _EdgeWidth ("Edge Width (texels)", Range(0.5, 32)) = 4
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2

        [Header(Options)]
        [Toggle(_HIGH_QUALITY)] _HighQuality ("High Quality (24 taps, smoother)", Float) = 0
        [Toggle] _UVBorder ("Texture Border Counts As Edge", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            half4  _Color;
            half4  _EdgeColor;
            half   _EdgeWidth;
            half   _EdgeIntensity;
            half   _UVBorder;
        CBUFFER_END

        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color      : COLOR;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4  color      : COLOR;
            float2 uv         : TEXCOORD0;
        };

        // 3 rings x 8 taps on the unit disk (radius 1/3, 2/3, 1).
        // Each ring is rotated 15 degrees from the previous one, so all 24 taps sit at
        // different angles. On any straight edge this gives 24 distinct ramp steps
        // instead of ~3, and on corners it samples diagonals from every side.
        // Taps 0-7 = inner ring, 8-15 = middle ring, 16-23 = outer ring.
        static const float2 kTaps[24] =
        {
            float2( 0.3333,  0.0000), float2( 0.2357,  0.2357), float2( 0.0000,  0.3333),
            float2(-0.2357,  0.2357), float2(-0.3333,  0.0000), float2(-0.2357, -0.2357),
            float2( 0.0000, -0.3333), float2( 0.2357, -0.2357), float2( 0.6440,  0.1725),
            float2( 0.3333,  0.5774), float2(-0.1725,  0.6440), float2(-0.5774,  0.3333),
            float2(-0.6440, -0.1725), float2(-0.3333, -0.5774), float2( 0.1725, -0.6440),
            float2( 0.5774, -0.3333), float2( 0.8660,  0.5000), float2( 0.2588,  0.9659),
            float2(-0.5000,  0.8660), float2(-0.9659,  0.2588), float2(-0.8660, -0.5000),
            float2(-0.2588, -0.9659), float2( 0.5000, -0.8660), float2( 0.9659, -0.2588)
        };

        #if defined(_HIGH_QUALITY)
            #define TAP_START 0
        #else
            #define TAP_START 8
        #endif
        #define TAP_COUNT (24 - TAP_START)

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.color = half4(IN.color) * _Color;
            return OUT;
        }

        // Alpha of one neighbour tap. Branch-free: when _UVBorder is on, anything outside
        // the 0..1 UV range counts as empty, so shapes that fill the whole texture
        // (plain squares/rectangles) still glow along the texture border.
        half TapAlpha(float2 uv)
        {
            half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            float2 inside = step(0.0, uv) * step(uv, 1.0);
            return a * lerp(1.0h, (half)(inside.x * inside.y), _UVBorder);
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

            // Edge width in texels -> UV offset, never smaller than ~1.5 screen pixels
            float2 o = max(_MainTex_TexelSize.xy * _EdgeWidth, fwidth(IN.uv) * 1.5);

            half sum = 0.0h;
            [unroll]
            for (int i = TAP_START; i < 24; i++)
                sum += TapAlpha(IN.uv + kTaps[i] * o);

            // Fraction of the disk that is empty: ~0.5 right on a straight edge, higher at
            // convex corners, 0 deep inside. *2 maps a straight edge to ~1.
            half edge = saturate((1.0h - sum / TAP_COUNT) * 2.0h);
            edge = edge * edge * (3.0h - 2.0h * edge);   // smoothstep
            edge *= tex.a;                               // inward only

            half4 col;
            col.rgb = tex.rgb * IN.color.rgb + _EdgeColor.rgb * (edge * _EdgeIntensity);
            col.a   = tex.a * IN.color.a;
            return col;
        }
        ENDHLSL

        // 2D Renderer
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _HIGH_QUALITY
            ENDHLSL
        }

        // Forward / Unlit fallback
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _HIGH_QUALITY
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}