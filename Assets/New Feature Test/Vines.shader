Shader "Custom/Vines"
{
    // ----------------------------------------------------------------------
    // Procedural animated "tendril" vignette with a dense thorny rim.
    // No textures, no sprites, no meshes beyond the fullscreen quad the
    // material is applied to (UI Image / Fullscreen Pass triangle).
    // Everything (noise, shapes, animation) is generated in HLSL.
    // ----------------------------------------------------------------------

    Properties
    {
        _Center          ("Center (UV space)",    Vector) = (0.5, 0.5, 0, 0)
        _Radius          ("Radius",                Range(0.01, 1.0))  = 0.30
        _Softness        ("Softness",              Range(0.001, 1.0)) = 0.12
        _DarkColor       ("Dark Color",             Color)  = (0.02, 0.05, 0.04, 1)
        _GlowColor       ("Tip Glow Color",         Color)  = (1.0, 0.28, 0.06, 1)
        _TendrilCount    ("Tendril Count",          Range(4, 96))   = 28
        _TendrilLength   ("Tendril Length",         Range(0.0, 1.0)) = 0.30
        _Thickness       ("Thickness",              Range(0.001, 0.2)) = 0.018
        _AnimationSpeed  ("Animation Speed",        Range(0.0, 5.0))  = 1.0
        _NoiseScale      ("Noise Scale",            Range(0.1, 20.0)) = 6.0
        _NoiseStrength   ("Noise Strength",         Range(0.0, 1.0))  = 0.35
        _GlowIntensity   ("Glow Intensity",         Range(0.0, 8.0))  = 2.5

        [Header(Thorn Detail)]
        _ThornAmount     ("Thorn Amount",           Range(0.0, 1.0))  = 0.7
        _ThornSharpness  ("Thorn Sharpness",        Range(1.0, 12.0)) = 6.0
        _ThornScale      ("Thorn Scale",            Range(1.0, 60.0)) = 26.0
        _EdgeJaggedness  ("Edge Jaggedness",        Range(0.0, 1.0))  = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "TendrilVignette"

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0

            // Confirmed working in-project: URP's own package exposes a
            // Core.hlsl wrapper (which internally includes the SRP Core
            // RP Library) — this resolves reliably since the Universal
            // RP package is always explicitly installed, unlike relying
            // on the core package's path directly.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ------------------------------------------------------------
            // SRP-batcher-friendly material properties
            // ------------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _Center;         // xy used
                float  _Radius;
                float  _Softness;
                half4  _DarkColor;
                half4  _GlowColor;
                float  _TendrilCount;
                float  _TendrilLength;
                float  _Thickness;
                float  _AnimationSpeed;
                float  _NoiseScale;
                float  _NoiseStrength;
                half   _GlowIntensity;
                float  _ThornAmount;
                float  _ThornSharpness;
                float  _ThornScale;
                float  _EdgeJaggedness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // ------------------------------------------------------------
            // Noise library (all procedural, no textures)
            // ------------------------------------------------------------

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float FBM(float2 p)
            {
                float sum  = 0.0;
                float amp  = 0.5;
                float freq = 1.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    sum  += amp * ValueNoise(p * freq);
                    freq *= 2.03;
                    amp  *= 0.5;
                }
                return sum;
            }

            // Ridged noise: turns smooth value noise into sharp, thin
            // peaks — this is what gives the thorn spikes their pointy
            // silhouette instead of soft blobs.
            float RidgedNoise(float2 p)
            {
                float n = ValueNoise(p);
                return 1.0 - abs(n * 2.0 - 1.0);
            }

            float Voronoi(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float minDist = 1.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 cellSeed = i + neighbor;
                        float2 pointPos = neighbor + float2(Hash21(cellSeed), Hash21(cellSeed + 19.19)) - f;
                        minDist = min(minDist, dot(pointPos, pointPos));
                    }
                }
                return sqrt(minDist);
            }

            // ------------------------------------------------------------
            // Fragment
            // ------------------------------------------------------------
            half4 Frag(Varyings IN) : SV_Target
            {
                // Convert current fragment to screen-space UV (0-1)
    float2 uv = IN.positionHCS.xy / _ScreenParams.xy;
    uv.y = 1.0 - uv.y;

    float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);

    // Use screen-space coordinates instead of mesh UVs
    float2 pos = uv - _Center.xy;
    pos.x *= aspect;

                float dist   = length(pos);
                float angle  = atan2(pos.y, pos.x);
                float angleN = (angle + PI) / (2.0 * PI);

                float t = _Time.y * _AnimationSpeed;

                // ---- jagged rim: warp the effective boundary radius per
                // angle with slow low-frequency noise so the inner edge of
                // the dark mass is torn/organic, not a clean circle -------
                float rimNoise = FBM(float2(angleN * 6.0, t * 0.06)) - 0.5;
                float effectiveRadius = _Radius + rimNoise * _Radius * 0.35 * _EdgeJaggedness;

                // ---- sector selection --------------------------------
                float tendrilCount = max(_TendrilCount, 1.0);
                float sectorF    = angleN * tendrilCount;
                float sectorId   = floor(sectorF);
                float sectorFrac = frac(sectorF);

                // ---- per-sector random identity ------------------------
                float rLen    = Hash11(sectorId * 13.17 + 1.7);
                float rSpeed  = Hash11(sectorId * 7.31  + 4.2);
                float rPhase  = Hash11(sectorId * 3.71  + 9.3) * TWO_PI;
                float rWidth  = Hash11(sectorId * 5.13  + 2.9);
                float rCurve  = Hash11(sectorId * 9.77  + 6.1) * TWO_PI;

                float growPulse = 0.5 + 0.5 * FBM(float2(sectorId * 0.37, t * (0.15 + 0.25 * rSpeed)));
                float tendrilLen = _TendrilLength * (0.35 + 0.85 * rLen) * (0.35 + 0.65 * growPulse);
                tendrilLen = max(tendrilLen, 0.0001);

                // ---- ring position relative to the jagged rim ----------
                float ringPos = effectiveRadius - dist;
                float tNorm   = ringPos / tendrilLen;

                float presence =
                    smoothstep(-0.015, 0.015, ringPos) *
                    (1.0 - smoothstep(0.75, 1.05, tNorm));

                // ---- sway + curl (secondary, sin-based motion) ----------
                float sway = sin(t * (0.6 + 0.6 * rSpeed) + rPhase) * 0.10;
                float curl = sin(dist * 8.0 + t * (0.4 + 0.4 * rSpeed) + rCurve) * 0.05 * saturate(tNorm);

                float warpUV1 = sectorId * 0.53 + t * 0.2;
                float n = FBM(float2(dist * _NoiseScale, warpUV1)) - 0.5;

                float sectorAngularWidth = (TWO_PI / tendrilCount);
                float centerlineFrac = (sectorFrac - 0.5) + sway + curl + n * _NoiseStrength;
                float arcOffset = centerlineFrac * sectorAngularWidth * max(dist, 0.001);

                // Sharpened taper: thinner overall, pointed tip rather than
                // a rounded root — closer to a thorn/spike silhouette.
                float width = _Thickness * (0.35 + 0.9 * rWidth) * pow(1.0 - saturate(tNorm), 1.6) + 0.0012;

                float distFromCenterline = abs(arcOffset);
                float tendrilShape = smoothstep(width, width * 0.08, distFromCenterline);

                float tendrilMask = saturate(tendrilShape * presence);

                // ---- dense thorn cluster: small sharp spikes scattered
                // across the tendril body, biased to be thickest near the
                // rim (bushy mass) and thinning out toward the tip --------
                float2 thornUV = float2(
                    angleN * tendrilCount * 5.0 + sectorId * 11.3,
                    dist * _ThornScale + t * 0.5 * (0.5 + rSpeed));
                float ridged = RidgedNoise(thornUV);
                ridged = pow(saturate(ridged), _ThornSharpness);

                float rootBias = smoothstep(1.0, 0.0, tNorm); // 1 at rim, 0 at tip
                float thornMask = ridged * presence * lerp(0.25, 1.0, rootBias) * _ThornAmount;

                // A second, coarser thorn pass gives a bit of clumping so
                // spikes read as small clusters rather than pure static.
                float2 clumpUV = float2(angleN * tendrilCount * 1.6 + sectorId * 4.1, dist * 5.0 + t * 0.1);
                float clump = smoothstep(0.35, 0.8, ValueNoise(clumpUV));
                thornMask *= lerp(0.4, 1.0, clump);

                // ---- sharp glowing tip: power-sharpened core plus a bit
                // of voronoi break-up so tips don't glow perfectly evenly -
                float voronoiBreak = Voronoi(float2(sectorId * 1.7, dist * _NoiseScale * 0.5 + t * 0.1));
                float tipCore  = pow(saturate(tendrilShape), 3.0) * smoothstep(0.55, 1.0, tNorm) * presence;
                float tipGlow  = tipCore * (0.6 + 0.4 * (1.0 - voronoiBreak));

                // ---- base soft radial vignette using the jagged rim -----
                float vignetteDark = smoothstep(effectiveRadius - _Softness, effectiveRadius + _Softness, dist);

                float darkAlpha = saturate(vignetteDark + tendrilMask * (1.0 - vignetteDark));
                darkAlpha = saturate(darkAlpha + thornMask * 0.6 * (1.0 - vignetteDark * 0.3));

                // Keep the exact center completely clear.
                float centerClear = smoothstep(_Radius * 0.18, 0.0, dist);
                darkAlpha *= (1.0 - centerClear);
                float tipGlowFinal   = tipGlow   * (1.0 - centerClear);
                float thornMaskFinal = thornMask * (1.0 - centerClear);

                // Thorn clusters pick up a faint warm rim-light so the mass
                // doesn't read as flat silhouette, echoing the reference.
                half3 thornTint = lerp(_DarkColor.rgb * 1.6, _GlowColor.rgb, 0.12);

                half3 col = _DarkColor.rgb * darkAlpha;
                col += thornTint * thornMaskFinal * 0.5;
                col += _GlowColor.rgb * _GlowIntensity * tipGlowFinal;

                half outAlpha = saturate(darkAlpha * _DarkColor.a + tipGlowFinal * 0.7 + thornMaskFinal * 0.25);

                return half4(col, outAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off

    // ------------------------------------------------------------------
    // Fullscreen Pass Renderer Feature usage:
    // If you instead wire this up via a URP "Full Screen Pass Renderer
    // Feature" (instead of a Canvas UI Image), the built-in Blit draws a
    // full-screen triangle whose TEXCOORD0 already spans 0..1, and the
    // vertex function above (TransformObjectToHClip) still works with the
    // feature's default blit geometry. No shader changes are required —
    // just assign this shader's material to the Renderer Feature.
    // ------------------------------------------------------------------
}
