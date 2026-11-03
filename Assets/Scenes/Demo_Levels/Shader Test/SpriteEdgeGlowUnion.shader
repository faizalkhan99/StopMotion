// Sprite Edge Glow - Union Aware (URP 2D / Mobile)
//
// Works for ANY shape. Edges that touch another glow shape do not glow because the
// glow is computed from a shared, blurred silhouette mask of ALL glow shapes
// (built by EdgeGlowMask.cs). Where two shapes touch, the union has no edge.
//
// Per pixel: ONE sprite tap + ONE mask tap + a few multiplies.
//
// How the width control works:
//   The mask is blurred with a Gaussian of sigma = EdgeGlowMask.maxEdgeWidth / 2.
//   On a straight edge the blurred value B is exactly 0.5 at the edge and rises
//   smoothly towards 1 inside the shape. We glow while B < T, where T is the blurred
//   value reached at distance _EdgeWidth. So glow = 1 on the edge, 0 at _EdgeWidth.

Shader "Custom/URP2D/SpriteEdgeGlowUnion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Edge Glow)]
        [HDR] _EdgeColor ("Edge Color", Color) = (0.2, 0.9, 1, 1)
        _EdgeWidth ("Edge Width (world units)", Range(0.005, 1)) = 0.1
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2
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

        TEXTURE2D(_MainTex);          SAMPLER(sampler_MainTex);
        TEXTURE2D(_EdgeGlowBlurTex);  SAMPLER(sampler_EdgeGlowBlurTex);

        // Globals set by EdgeGlowMask.cs
        float  _EdgeGlowMaskMode;     // 1 while the mask camera renders, 0 otherwise
        float4 _EdgeGlowMaskRect;     // xy = world min, zw = 1 / world size
        float  _EdgeGlowMaxWidth;     // world units, = 2 * blur sigma

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4  _Color;
            half4  _EdgeColor;
            float  _EdgeWidth;
            half   _EdgeIntensity;
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
            float2 maskUV     : TEXCOORD1;
            float2 glow       : TEXCOORD2;   // x = T threshold, y = 1 / (T - 0.5)
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            float3 wp = TransformObjectToWorld(IN.positionOS);
            OUT.positionCS = TransformWorldToHClip(wp);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.color = half4(IN.color) * _Color;

            // World position -> mask UV (linear, so exact when interpolated)
            OUT.maskUV = (wp.xy - _EdgeGlowMaskRect.xy) * _EdgeGlowMaskRect.zw;

            // Width -> blurred-mask threshold. Gaussian CDF approximated by a logistic:
            // T = 1 / (1 + exp(-1.702 * x)),  x = width / sigma,  sigma = maxWidth / 2.
            float x = min(2.0 * _EdgeWidth / max(_EdgeGlowMaxWidth, 1e-4), 2.2);
            float T = rcp(1.0 + exp2(-2.455 * x));
            OUT.glow = float2(T, rcp(max(T - 0.5, 1e-3)));
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

            // Mask camera pass: output plain coverage (white, alpha = shape alpha).
            if (_EdgeGlowMaskMode > 0.5)
                return half4(1.0h, 1.0h, 1.0h, tex.a * IN.color.a);

            // Blurred union silhouette: ~0.5 on an edge, -> 1 deep inside, ~1 across a seam
            half B = SAMPLE_TEXTURE2D(_EdgeGlowBlurTex, sampler_EdgeGlowBlurTex, IN.maskUV).r;

            half glow = saturate((IN.glow.x - B) * IN.glow.y);
            glow = glow * glow * (3.0h - 2.0h * glow);   // smoothstep
            glow *= tex.a;                               // inward only, follows the shape

            half4 col;
            col.rgb = tex.rgb * IN.color.rgb + _EdgeColor.rgb * (glow * _EdgeIntensity);
            col.a   = tex.a * IN.color.a;
            return col;
        }
        ENDHLSL

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
