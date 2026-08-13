Shader "CityBuster/EdgeVines_NoThorn_FlatSoil"
{
    // ----------------------------------------------------------------------
    // Border-telegraph overlay: thorny vines grow INWARD from a single
    // continuous ring wrapped around the screen (angle-based, no corner
    // seams like a rect-edge approach would have). The ring sits near the
    // screen boundary so it still reads as "growing in from the edges,"
    // but converges as one smooth circle rather than four overlapping
    // strips.
    //
    // Soil (mottled red/black cellular noise) is the root material at the
    // ring; tendrils root inside it and poke further inward, tapering to
    // glowing points. Growth driven externally via _GrowthAmount (0 =
    // hidden, 1 = full), intended to be wired to
    // GameEventBus.OnChronoStateChanged / OnGracePeriodUpdated.
    // ----------------------------------------------------------------------

    Properties
    {
        [Header(Growth Control)]
        _StartTime          ("Countdown Start Time (Time.y snapshot)", Float) = 0
        _Duration           ("Level Duration (s)",                     Float) = 60
        _PausedGrowth       ("Growth When Paused (0 hidden, 1 full)",  Range(0.0, 1.0)) = 0.0
        _IsRunning          ("Is Ticking (0/1)",                       Range(0.0, 1.0)) = 0.0
        _Radius             ("Ring Radius",                      Range(0.2, 1.2)) = 0.72
        _SoilReach          ("Soil Reach (inward from ring)",    Range(0.01, 0.5)) = 0.12
        _TendrilOvergrowth  ("Tendril Overgrowth Length",        Range(0.0, 0.6))  = 0.25
        _EdgeJaggedness     ("Ring Jaggedness",                  Range(0.0, 1.0))  = 0.6

        [Header(Tendril Shape)]
        _TendrilCount    ("Tendril Count",       Range(4, 96))    = 32
        _Thickness       ("Thickness",           Range(0.001, 0.2)) = 0.02
        _AnimationSpeed  ("Animation Speed",     Range(0.0, 5.0)) = 1.0
        _NoiseScale      ("Noise Scale",         Range(0.1, 20.0)) = 6.0
        _NoiseStrength   ("Noise Strength",      Range(0.0, 1.0)) = 0.35

        [Header(Glow Tip)]
        _GlowColor       ("Tip Glow Color", Color) = (1.0, 0.28, 0.06, 1)
        _GlowIntensity   ("Glow Intensity", Range(0.0, 8.0)) = 2.5

        [Header(Soil Root Mass)]
        _SoilColorA      ("Soil Color (Flat)",  Color) = (0.05, 0.01, 0.01, 0.85)
        _SoilAlpha       ("Soil Alpha",         Range(0.0, 1.0))  = 1.0
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
            Name "EdgeVines"

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0

            // URP's own Core.hlsl wrapper — confirmed to resolve reliably
            // in this project. Do NOT use the com.unity.render-pipelines.core
            // path directly, it does not resolve consistently.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _StartTime;
                float  _Duration;
                float  _PausedGrowth;
                float  _IsRunning;
                float  _Radius;
                float  _SoilReach;
                float  _TendrilOvergrowth;
                float  _EdgeJaggedness;
                float  _TendrilCount;
                float  _Thickness;
                float  _AnimationSpeed;
                float  _NoiseScale;
                float  _NoiseStrength;
                half4  _GlowColor;
                half   _GlowIntensity;
                half4  _SoilColorA;
                float  _SoilAlpha;
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
            // Noise library
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
                float2 uv = IN.positionHCS.xy / _ScreenParams.xy;
                uv.y = 1.0 - uv.y;

                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);

                // Aspect-corrected so the ring is a true circle regardless
                // of screen aspect ratio, not a stretched oval.
                float2 pos = uv - 0.5;
                pos.x *= aspect;

                float dist   = length(pos);
                float angle  = atan2(pos.y, pos.x);
                float angleN = (angle + PI) / (2.0 * PI);

                float t = _Time.y * _AnimationSpeed;

                // ---- growth amount computed from time, not a per-frame
                // C# write. _StartTime/_Duration/_IsRunning are only sent
                // once at level start + on Chrono state transitions.
                float safeDuration = max(_Duration, 0.0001);
                float runningGrowth = saturate((_Time.y - _StartTime) / safeDuration);
                float growthAmt = lerp(_PausedGrowth, runningGrowth, saturate(_IsRunning));

                // Nothing to draw yet — skip the rest of the noise stack
                // entirely. These are uniforms (same for every pixel), so
                // this branch causes no warp divergence.
                if (growthAmt <= 0.0005)
                {
                    return half4(0, 0, 0, 0);
                }

                // ---- jagged ring boundary: warp the effective root
                // radius per angle so it reads as a torn organic edge,
                // not a perfect circle -------------------------------------
                float rimNoise = FBM(float2(angleN * 6.0, t * 0.06)) - 0.5;
                float effectiveRadius = _Radius + rimNoise * _Radius * 0.35 * _EdgeJaggedness;

                // ---- single continuous angular sweep: no seams --------
                float tendrilCount = max(_TendrilCount, 1.0);
                float sectorF    = angleN * tendrilCount;
                float sectorId   = floor(sectorF);
                float sectorFrac = frac(sectorF);

                float rLen    = Hash11(sectorId * 13.17 + 1.7);
                float rSpeed  = Hash11(sectorId * 7.31  + 4.2);
                float rPhase  = Hash11(sectorId * 3.71  + 9.3) * TWO_PI;
                float rWidth  = Hash11(sectorId * 5.13  + 2.9);
                float rCurve  = Hash11(sectorId * 9.77  + 6.1) * TWO_PI;

                // ---- ring position relative to the jagged root ring -----
                // ringPos > 0 = inside the ring (growth zone), < 0 = outside
                float ringPos = effectiveRadius - dist;

                float soilReachBase = growthAmt * _SoilReach;
                float ringGate = smoothstep(-0.015, 0.015, ringPos);
                float soilCoverage = ringGate * (1.0 - smoothstep(soilReachBase * 0.8, soilReachBase * 1.05 + 0.0001, ringPos));

                float growPulse = 0.5 + 0.5 * FBM(float2(sectorId * 0.37, t * (0.15 + 0.25 * rSpeed)));
                float overgrow  = growthAmt * _TendrilOvergrowth * (0.35 + 0.85 * rLen) * (0.35 + 0.65 * growPulse);
                float tendrilLen = max(soilReachBase + overgrow, 0.0001);

                float tNorm = ringPos / tendrilLen;
                float presence = ringGate * (1.0 - smoothstep(0.85, 1.05, tNorm));

                // ---- sway + curl along the growth direction --------------
                float sway = sin(t * (0.6 + 0.6 * rSpeed) + rPhase) * 0.10;
                float curl = sin(dist * 8.0 + t * (0.4 + 0.4 * rSpeed) + rCurve) * 0.05 * saturate(tNorm);

                float warpUV1 = sectorId * 0.53 + t * 0.2;
                float n = FBM(float2(dist * _NoiseScale, warpUV1)) - 0.5;

                float sectorAngularWidth = (TWO_PI / tendrilCount);
                float centerlineFrac = (sectorFrac - 0.5) + sway + curl + n * _NoiseStrength;
                float arcOffset = centerlineFrac * sectorAngularWidth * max(dist, 0.001);

                float width = _Thickness * (0.35 + 0.9 * rWidth) * pow(1.0 - saturate(tNorm), 1.6) + 0.0012;
                float distFromCenterline = abs(arcOffset);

                // edge0 < edge1 (spec-legal smoothstep, was inverted originally)
                float tendrilShape = 1.0 - smoothstep(width * 0.08, width, distFromCenterline);
                float tendrilMask = saturate(tendrilShape * presence);

                // ---- glowing tip ------------------------------------------
                float voronoiBreak = Voronoi(float2(sectorId * 1.7, dist * _NoiseScale * 0.5 + t * 0.1));
                float tipCore = pow(saturate(tendrilShape), 3.0) * smoothstep(0.55, 1.0, tNorm) * presence;
                float tipGlow = tipCore * (0.6 + 0.4 * (1.0 - voronoiBreak));

                // ---- soil / root mass color: flat, no per-pixel cell noise ----
                half3 soilColor = _SoilColorA.rgb;
                float soilAlpha = soilCoverage * _SoilColorA.a * _SoilAlpha;

                // keep the exact center clear regardless of parameters
                float centerClear = smoothstep(0.05, 0.0, dist);

                // ---- composite ---------------------------------------------
                half3 col = soilColor * soilAlpha;
                col += _SoilColorA.rgb * tendrilMask * (1.0 - soilAlpha);
                col += _GlowColor.rgb * _GlowIntensity * tipGlow;
                col *= (1.0 - centerClear);

                half outAlpha = saturate(
                    soilAlpha +
                    tendrilMask * 0.6 * (1.0 - soilAlpha) +
                    tipGlow * 0.7) * (1.0 - centerClear);

                return half4(col, outAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
