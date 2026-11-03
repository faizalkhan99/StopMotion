// working version:
// Shader "CityBuster/V10"
// {
//     Properties
//     {
//         [Header(Growth Control)]
//         _StartTime          ("Countdown Start Time (Time.y snapshot)", Float) = 0
//         _Duration           ("Level Duration (s)",                     Float) = 60
//         _PausedGrowth       ("Growth When Paused (0 hidden, 1 full)",  Range(0.0, 1.0)) = 0.0
//         _IsRunning          ("Is Ticking (0/1)",                       Range(0.0, 1.0)) = 0.0
//         _Radius             ("Ring Radius",                      Range(0.2, 1.2)) = 0.72
//         _SoilReach          ("Soil Reach (inward from ring)",    Range(0.01, 0.5)) = 0.12
//         _TendrilOvergrowth  ("Tendril Overgrowth Length",        Range(0.0, 1.0))  = 0.25
//         _EdgeJaggedness     ("Ring Jaggedness",                  Range(0.0, 1.0))  = 0.6

//         [Header(Tendril Shape)]
//         _TendrilCount    ("Tendril Count",       Range(4, 96))    = 32
//         _Thickness       ("Thickness",           Range(0.001, 0.5)) = 0.02
//         _AnimationSpeed  ("Animation Speed",     Range(0.0, 5.0)) = 1.0
//         _NoiseScale      ("Noise Scale",         Range(0.1, 20.0)) = 6.0
//         _NoiseStrength   ("Noise Strength",      Range(0.0, 1.0)) = 0.35

//         [Header(Glow Tip)]
//         _GlowColor       ("Tip Glow Color", Color) = (1.0, 0.28, 0.06, 1)
//         _GlowIntensity   ("Glow Intensity", Range(0.0, 8.0)) = 2.5

//         [Header(Soil Root Mass)]
//         _SoilColorA      ("Soil Color (Flat)",  Color) = (0.05, 0.01, 0.01, 0.85)
//         _SoilAlpha       ("Soil Alpha",         Range(0.0, 1.0))  = 1.0
//     }

//     SubShader
//     {
//         Tags
//         {
//             "Queue"           = "Transparent"
//             "RenderType"      = "Transparent"
//             "RenderPipeline"  = "UniversalPipeline"
//             "IgnoreProjector" = "True"
//         }

//         Pass
//         {
//             Name "EdgeVines"

//             Blend SrcAlpha OneMinusSrcAlpha
//             Cull Off
//             ZWrite Off
//             ZTest Always

//             HLSLPROGRAM
//             #pragma vertex   Vert
//             #pragma fragment Frag
//             #pragma target 3.0

//             // URP's own Core.hlsl wrapper — confirmed to resolve reliably
//             // in this project. Do NOT use the com.unity.render-pipelines.core
//             // path directly, it does not resolve consistently.
//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//             CBUFFER_START(UnityPerMaterial)
//                 float  _StartTime;
//                 float  _Duration;
//                 float  _PausedGrowth;
//                 float  _IsRunning;
//                 float  _Radius;
//                 float  _SoilReach;
//                 float  _TendrilOvergrowth;
//                 float  _EdgeJaggedness;
//                 float  _TendrilCount;
//                 float  _Thickness;
//                 float  _AnimationSpeed;
//                 float  _NoiseScale;
//                 float  _NoiseStrength;
//                 half4  _GlowColor;
//                 half   _GlowIntensity;
//                 half4  _SoilColorA;
//                 float  _SoilAlpha;
//             CBUFFER_END

//             struct Attributes
//             {
//                 float4 positionOS : POSITION;
//                 float2 uv         : TEXCOORD0;
//             };

//             struct Varyings
//             {
//                 float4 positionHCS : SV_POSITION;
//                 float2 uv          : TEXCOORD0;
//             };

//             Varyings Vert(Attributes IN)
//             {
//                 Varyings OUT;
//                 OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
//                 OUT.uv = IN.uv;
//                 return OUT;
//             }

//             // ------------------------------------------------------------
//             // Noise library
//             // ------------------------------------------------------------
//             float Hash11(float p)
//             {
//                 p = frac(p * 0.1031);
//                 p *= p + 33.33;
//                 p *= p + p;
//                 return frac(p);
//             }

//             float Hash21(float2 p)
//             {
//                 float3 p3 = frac(float3(p.xyx) * 0.1031);
//                 p3 += dot(p3, p3.yzx + 33.33);
//                 return frac((p3.x + p3.y) * p3.z);
//             }

//             float ValueNoise(float2 p)
//             {
//                 float2 i = floor(p);
//                 float2 f = frac(p);

//                 float a = Hash21(i);
//                 float b = Hash21(i + float2(1.0, 0.0));
//                 float c = Hash21(i + float2(0.0, 1.0));
//                 float d = Hash21(i + float2(1.0, 1.0));

//                 float2 u = f * f * (3.0 - 2.0 * f);
//                 return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
//             }

//             float FBM(float2 p)
//             {
//                 float sum  = 0.0;
//                 float amp  = 0.5;
//                 float freq = 1.0;

//                 [unroll]
//                 for (int i = 0; i < 4; i++)
//                 {
//                     sum  += amp * ValueNoise(p * freq);
//                     freq *= 2.03;
//                     amp  *= 0.5;
//                 }
//                 return sum;
//             }

//             // Cheaper 2-octave variant for low-frequency, slow-changing
//             // uses (ring jaggedness, growth pulse) where the extra detail
//             // from octaves 3-4 is not perceptible but still costs 8 more
//             // Hash21 calls per pixel.
//             float FBM2(float2 p)
//             {
//                 float sum  = 0.0;
//                 float amp  = 0.5;
//                 float freq = 1.0;

//                 [unroll]
//                 for (int i = 0; i < 2; i++)
//                 {
//                     sum  += amp * ValueNoise(p * freq);
//                     freq *= 2.03;
//                     amp  *= 0.5;
//                 }
//                 return sum;
//             }

//             float Voronoi(float2 p)
//             {
//                 float2 i = floor(p);
//                 float2 f = frac(p);
//                 float minDist = 1.0;

//                 [unroll]
//                 for (int y = -1; y <= 1; y++)
//                 {
//                     [unroll]
//                     for (int x = -1; x <= 1; x++)
//                     {
//                         float2 neighbor = float2(x, y);
//                         float2 cellSeed = i + neighbor;
//                         float2 pointPos = neighbor + float2(Hash21(cellSeed), Hash21(cellSeed + 19.19)) - f;
//                         minDist = min(minDist, dot(pointPos, pointPos));
//                     }
//                 }
//                 return sqrt(minDist);
//             }

//             // ------------------------------------------------------------
//             // Fragment
//             // ------------------------------------------------------------
//             half4 Frag(Varyings IN) : SV_Target
//             {
//                 float2 uv = IN.positionHCS.xy / _ScreenParams.xy;
//                 uv.y = 1.0 - uv.y;

//                 float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);

//                 // Aspect-corrected so the ring is a true circle regardless
//                 // of screen aspect ratio, not a stretched oval.
//                 float2 pos = uv - 0.5;
//                 pos.x *= aspect;

//                 float dist   = length(pos);
//                 float angle  = atan2(pos.y, pos.x);
//                 float angleN = (angle + PI) / (2.0 * PI);

//                 // Wrapped so values fed into half-precision noise calls
//                 // stay small and precise even after long play sessions.
//                 // 1000s period, sin()/noise are periodic so the wrap is
//                 // visually seamless.
//                 float t = fmod(_Time.y * _AnimationSpeed, 1000.0);

//                 // ---- growth amount computed from time, not a per-frame
//                 // C# write. _StartTime/_Duration/_IsRunning are only sent
//                 // once at level start + on Chrono state transitions.
//                 float safeDuration = max(_Duration, 0.0001);
//                 float runningGrowth = saturate((_Time.y - _StartTime) / safeDuration);
//                 float growthAmt = lerp(_PausedGrowth, runningGrowth, saturate(_IsRunning));

//                 // Nothing to draw yet — skip the rest of the noise stack
//                 // entirely. These are uniforms (same for every pixel), so
//                 // this branch causes no warp divergence.
//                 if (growthAmt <= 0.0005)
//                 {
//                     return half4(0, 0, 0, 0);
//                 }

//                 // ---- cheap coarse band gate --------------------------------
//                 // Before paying for 3x FBM (12 ValueNoise calls) + Voronoi,
//                 // work out the widest the ring band could possibly be using
//                 // only uniforms + dist (no per-pixel noise yet), and bail
//                 // for pixels nowhere near it. This is the majority of the
//                 // screen (the empty center) once the effect is active.
//                 // Spatially coherent across neighboring pixels -> real
//                 // savings on tile-based mobile GPUs despite the branch.
//                 float maxJagOffset    = _Radius * 0.35 * _EdgeJaggedness;
//                 float maxSoilReach    = growthAmt * _SoilReach;
//                 float maxOvergrow     = growthAmt * _TendrilOvergrowth;
//                 float outerBoundGuess = _Radius + maxJagOffset + 0.02;
//                 float innerBoundGuess = _Radius - maxJagOffset - maxSoilReach - maxOvergrow - 0.02;
//                 if (dist > outerBoundGuess || dist < innerBoundGuess)
//                 {
//                     return half4(0, 0, 0, 0);
//                 }

//                 // ---- jagged ring boundary: warp the effective root
//                 // radius per angle so it reads as a torn organic edge,
//                 // not a perfect circle -------------------------------------
//                 // Sampled on a circle (cos/sin of angle) so the noise is
//                 // continuous across the +/-PI wrap: no seam.
//                 float2 circ = float2(cos(angle), sin(angle));
//                 float rimNoise = FBM2(circ * 1.5 + float2(t * 0.06, 0.0)) - 0.5;
//                 float effectiveRadius = _Radius + rimNoise * _Radius * 0.35 * _EdgeJaggedness;

//                 // ---- single continuous angular sweep: no seams --------
//                 float tendrilCount = max(_TendrilCount, 1.0);
//                 float sectorF    = angleN * tendrilCount;
//                 float sectorId   = floor(sectorF);
//                 float sectorFrac = frac(sectorF);

//                 // ---- ring position relative to the jagged root ring -----
//                 // ringPos > 0 = inside the ring (growth zone), < 0 = outside
//                 float ringPos = effectiveRadius - dist;

//                 float soilReachBase = growthAmt * _SoilReach;
//                 float ringGate = smoothstep(-0.015, 0.015, ringPos);
//                 float soilCoverage = ringGate * (1.0 - smoothstep(soilReachBase * 0.8, soilReachBase * 1.05 + 0.0001, ringPos));

//                 float sectorAngularWidth = (TWO_PI / tendrilCount);

//                 // ---- evaluate this sector AND both neighbors, take the max.
//                 // A tendril that wanders past its sector border is drawn by
//                 // its own data instead of being cut off by the next sector's
//                 // values (this was the source of the hard radial edges).
//                 float tendrilMask = 0.0;
//                 float tipGlow = 0.0;

//                 [unroll]
//                 for (int k = -1; k <= 1; k++)
//                 {
//                     // wrapped so the last and first sectors are neighbors
//                     float sid = fmod(sectorId + k + tendrilCount, tendrilCount);

//                     float rLen    = Hash11(sid * 13.17 + 1.7);
//                     float rSpeed  = Hash11(sid * 7.31  + 4.2);
//                     float rPhase  = Hash11(sid * 3.71  + 9.3) * TWO_PI;
//                     float rWidth  = Hash11(sid * 5.13  + 2.9);
//                     float rCurve  = Hash11(sid * 9.77  + 6.1) * TWO_PI;

//                     float growPulse = 0.5 + 0.5 * FBM2(float2(sid * 0.37, t * (0.15 + 0.25 * rSpeed)));
//                     float overgrow  = growthAmt * _TendrilOvergrowth * (0.35 + 0.85 * rLen) * (0.35 + 0.65 * growPulse);
//                     float tendrilLen = max(soilReachBase + overgrow, 0.0001);

//                     float tNorm = ringPos / tendrilLen;
//                     float presence = ringGate * (1.0 - smoothstep(0.85, 1.05, tNorm));

//                     // ---- sway + curl along the growth direction --------------
//                     float sway = sin(t * (0.6 + 0.6 * rSpeed) + rPhase) * 0.10;
//                     float curl = sin(dist * 8.0 + t * (0.4 + 0.4 * rSpeed) + rCurve) * 0.05 * saturate(tNorm);

//                     float warpUV1 = sid * 0.53 + t * 0.2;
//                     float n = FBM(float2(dist * _NoiseScale, warpUV1)) - 0.5;

//                     // position relative to THIS neighbor's center
//                     float relFrac = sectorFrac - (k + 0.5);
//                     float centerlineFrac = relFrac + sway + curl + n * _NoiseStrength;
//                     float arcOffset = centerlineFrac * sectorAngularWidth * max(dist, 0.001);

//                     float width = _Thickness * (0.35 + 0.9 * rWidth) * pow(1.0 - saturate(tNorm), 1.6) + 0.0012;
//                     float distFromCenterline = abs(arcOffset);

//                     // edge0 < edge1 (spec-legal smoothstep, was inverted originally)
//                     float tendrilShape = 1.0 - smoothstep(width * 0.08, width, distFromCenterline);
//                     float sectorMask = saturate(tendrilShape * presence);

//                     // ---- glowing tip ------------------------------------------
//                     // Voronoi (9 hash-pair samples) only ever affects the
//                     // final color where smoothstep(0.55,1.0,tNorm) is nonzero.
//                     // Skip it entirely everywhere else in the band.
//                     float voronoiBreak = 0.5;
//                     if (tNorm > 0.5)
//                     {
//                         voronoiBreak = Voronoi(float2(sid * 1.7, dist * _NoiseScale * 0.5 + t * 0.1));
//                     }
//                     float tipCore = pow(saturate(tendrilShape), 3.0) * smoothstep(0.55, 1.0, tNorm) * presence;
//                     float sectorTipGlow = tipCore * (0.6 + 0.4 * (1.0 - voronoiBreak));

//                     tendrilMask = max(tendrilMask, sectorMask);
//                     tipGlow     = max(tipGlow, sectorTipGlow);
//                 }

//                 // ---- soil / root mass color: flat, no per-pixel cell noise ----
//                 half3 soilColor = _SoilColorA.rgb;
//                 float soilAlpha = soilCoverage * _SoilColorA.a * _SoilAlpha;

//                 // keep the exact center clear regardless of parameters
//                 float centerClear = smoothstep(0.05, 0.0, dist);

//                 // ---- composite ---------------------------------------------
//                 half3 col = soilColor * soilAlpha;
//                 col += _SoilColorA.rgb * tendrilMask * (1.0 - soilAlpha);
//                 col += _GlowColor.rgb * _GlowIntensity * tipGlow;
//                 col *= (1.0 - centerClear);

//                 half outAlpha = saturate(
//                     soilAlpha +
//                     tendrilMask * 0.6 * (1.0 - soilAlpha) +
//                     tipGlow * 0.7) * (1.0 - centerClear);

//                 return half4(col, outAlpha);
//             }
//             ENDHLSL
//         }
//     }

//     FallBack Off
// }









































Shader "CityBuster/V10"
{
    Properties
    {
        [Header(Growth Control)]
        _StartTime          ("Countdown Start Time (Time.y snapshot)", Float) = 0
        _Duration           ("Growth Duration Per Cycle (s)",          Float) = 60
        _PausedGrowth       ("Growth When Paused (0 hidden, 1 full)",  Range(0.0, 1.0)) = 0.0
        _IsRunning          ("Is Ticking (0/1)",                       Range(0.0, 1.0)) = 0.0
        _Radius             ("Ring Radius",                      Range(0.2, 1.2)) = 0.72
        _SoilReach          ("Soil Reach (inward from ring)",    Range(0.01, 0.5)) = 0.12
        _TendrilOvergrowth  ("Tendril Overgrowth Length",        Range(0.0, 1.0))  = 0.25
        _EdgeJaggedness     ("Ring Jaggedness",                  Range(0.0, 1.0))  = 0.6

        [Header(Player Direction)]
        _PlayerPos          ("Player Position (Screen UV, 0,0 = bottom-left)", Vector) = (0.5, 0.5, 0, 0)
        _PlayerInfluence    ("Player Pull (0 = screen center, 1 = player)",    Range(0.0, 1.0)) = 1.0
        _PlayerClearRadius  ("Player Clear Radius",                            Range(0.0, 0.5)) = 0.05

        [Header(Growth Cycle Timing)]
        _GrowthDelay        ("Delay Before Each Growth Cycle (s)", Float) = 5.0
        [Toggle] _LoopCycles ("Repeat Growth Cycles",              Float) = 1

        [Header(White Flash)]
        _FlashColor         ("Flash Color",                         Color) = (1, 1, 1, 1)
        _FlashDuration      ("Flash Duration (s)",                  Range(0.01, 3.0)) = 0.4
        _FlashIntensity     ("Flash Intensity (brightness gain)",   Range(0.0, 8.0))  = 2.0
        _FlashReveal        ("Root Mass Revealed By Flash",         Range(0.0, 1.0))  = 0.3

        [Header(Tendril Shape)]
        _TendrilCount    ("Tendril Count",       Range(4, 96))    = 32
        _Thickness       ("Thickness",           Range(0.001, 0.5)) = 0.02
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
                float4 _PlayerPos;
                float  _PlayerInfluence;
                float  _PlayerClearRadius;
                float  _GrowthDelay;
                float  _LoopCycles;
                half4  _FlashColor;
                float  _FlashDuration;
                float  _FlashIntensity;
                float  _FlashReveal;
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

            // Cheaper 2-octave variant for low-frequency, slow-changing
            // uses (ring jaggedness, growth pulse) where the extra detail
            // from octaves 3-4 is not perceptible but still costs 8 more
            // Hash21 calls per pixel.
            float FBM2(float2 p)
            {
                float sum  = 0.0;
                float amp  = 0.5;
                float freq = 1.0;

                [unroll]
                for (int i = 0; i < 2; i++)
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
                // Origin = screen center = anchor of the ring / root mass.
                float2 pos = uv - 0.5;
                pos.x *= aspect;

                float dist = length(pos);

                // Wrapped so values fed into noise calls stay small and
                // precise even after long play sessions.
                float t = fmod(_Time.y * _AnimationSpeed, 1000.0);

                // ---- growth cycle timing (all uniforms, no per-pixel cost)
                // Timeline:
                //   [initial delay] -> ( [flash] -> [grow] -> [hold = delay] ) repeated
                // Every cycle after the first begins by flashing the fully
                // grown vines white, which then burn back as the flash fades,
                // and growth restarts from zero: continuous, no popping.
                float elapsed   = _Time.y - _StartTime;
                float flashDur  = max(_FlashDuration, 0.0001);
                float growDur   = max(_Duration, 0.0001);
                float delay     = max(_GrowthDelay, 0.0);
                float cycleLen  = flashDur + growDur + delay;

                float cycleTime = elapsed - delay;   // < 0 while in the initial wait
                float cycleIdx  = (_LoopCycles > 0.5) ? max(floor(cycleTime / cycleLen), 0.0) : 0.0;
                float tau       = cycleTime - cycleIdx * cycleLen;

                // flash envelope: smooth 0 -> 1 -> 0 (sin^2), exactly 0 outside the flash window
                float flashPhase = saturate(tau / flashDur);
                float flashEnv   = sin(PI * flashPhase);
                flashEnv *= flashEnv;

                float ramp       = saturate((tau - flashDur) / growDur);
                float prevHold   = (cycleIdx > 0.5) ? 1.0 : 0.0;
                float retract    = prevHold * (1.0 - smoothstep(0.5, 1.0, flashPhase));
                float reveal     = _FlashReveal * flashEnv;
                float cycleGrowth = (tau < flashDur) ? max(retract, reveal) : ramp;

                float growthAmt = lerp(_PausedGrowth, cycleGrowth, saturate(_IsRunning));
                float flashAmt  = flashEnv * saturate(_IsRunning);

                // Nothing to draw yet — skip the rest of the noise stack
                // entirely. These are uniforms (same for every pixel), so
                // this branch causes no warp divergence.
                if (growthAmt <= 0.0005)
                {
                    return half4(0, 0, 0, 0);
                }

                // ---- cheap coarse band gate --------------------------------
                // Tendrils stay within (soil + overgrowth) of the ring, so the
                // band bound measured from the screen center still holds when
                // they point at the player instead of the center.
                float maxJagOffset    = _Radius * 0.35 * _EdgeJaggedness;
                float maxSoilReach    = growthAmt * _SoilReach;
                float maxOvergrow     = growthAmt * _TendrilOvergrowth;
                float outerBoundGuess = _Radius + maxJagOffset + 0.02;
                float innerBoundGuess = _Radius - maxJagOffset - maxSoilReach - maxOvergrow - 0.02;
                if (dist > outerBoundGuess || dist < innerBoundGuess)
                {
                    return half4(0, 0, 0, 0);
                }

                // ---- convergence point (player), relative to screen center ---
                // Clamped inside the smallest possible ring radius so every
                // ray from the player hits the ring exactly once.
                float2 convergePoint = _PlayerPos.xy - 0.5;
                convergePoint.x *= aspect;
                convergePoint *= saturate(_PlayerInfluence);
                float maxPointLen = _Radius * (1.0 - 0.175 * _EdgeJaggedness) * 0.9;
                convergePoint *= min(1.0, maxPointLen / max(length(convergePoint), 0.0001));

                // ---- ray from the player through this pixel to the ring ------
                // Tendril sectors are indexed by where the ray lands on the
                // ring, so roots stay anchored on the ring while every tendril
                // body points at the player. One quadratic, no extra atan2.
                float2 toPix   = pos - convergePoint;
                float  dp      = length(toPix);
                float2 rayDir  = toPix / max(dp, 0.0001);
                float  rb      = dot(convergePoint, rayDir);
                float  rc      = dot(convergePoint, convergePoint) - _Radius * _Radius;
                float  rayToRing = -rb + sqrt(max(rb * rb - rc, 0.0));

                float2 hitDir  = (convergePoint + rayDir * rayToRing) / _Radius;
                float  cosI    = max(dot(rayDir, hitDir), 0.3);
                float  angleN  = (atan2(hitDir.y, hitDir.x) + PI) / TWO_PI;

                // ---- jagged ring boundary (anchored to screen center) --------
                // Sampled on the unit circle through the pixel's own direction,
                // so it is continuous across the +/-PI wrap: no seam.
                float2 circ = pos / max(dist, 0.0001);
                float rimNoise = FBM2(circ * 1.5 + float2(t * 0.06, 0.0)) - 0.5;
                float rimOffset = rimNoise * _Radius * 0.35 * _EdgeJaggedness;
                float effectiveRadius = _Radius + rimOffset;

                // ---- single continuous angular sweep: no seams --------
                float tendrilCount = max(_TendrilCount, 1.0);
                float sectorF    = angleN * tendrilCount;
                float sectorId   = floor(sectorF);
                float sectorFrac = frac(sectorF);

                // ringPos > 0 = inside the ring (radial depth, used by soil)
                float ringPos = effectiveRadius - dist;

                // distance travelled from the ring toward the player along
                // this pixel's ray (used by tendrils)
                float rayRingPos = (rayToRing - dp) + rimOffset / cosI;

                // tendrils end at the player's clear zone, never behind it
                float maxTendrilLen = max(rayToRing - _PlayerClearRadius, 0.02);

                float soilReachBase = growthAmt * _SoilReach;
                float ringGate = smoothstep(-0.015, 0.015, ringPos);
                float soilCoverage = ringGate * (1.0 - smoothstep(soilReachBase * 0.8, soilReachBase * 1.05 + 0.0001, ringPos));

                float sectorAngularWidth = (TWO_PI / tendrilCount);

                // converts ring-angle offset into lateral distance at this
                // pixel (equals dist when the player is at the screen center)
                float lateralScale = max(dp, 0.001) * _Radius * cosI / max(rayToRing, 0.001);

                // ---- evaluate this sector AND both neighbors, take the max.
                float tendrilMask = 0.0;
                float tipGlow = 0.0;

                [unroll]
                for (int k = -1; k <= 1; k++)
                {
                    // wrapped so the last and first sectors are neighbors
                    float sid = fmod(sectorId + k + tendrilCount, tendrilCount);

                    float rLen    = Hash11(sid * 13.17 + 1.7);
                    float rSpeed  = Hash11(sid * 7.31  + 4.2);
                    float rPhase  = Hash11(sid * 3.71  + 9.3) * TWO_PI;
                    float rWidth  = Hash11(sid * 5.13  + 2.9);
                    float rCurve  = Hash11(sid * 9.77  + 6.1) * TWO_PI;

                    float growPulse = 0.5 + 0.5 * FBM2(float2(sid * 0.37, t * (0.15 + 0.25 * rSpeed)));
                    float overgrow  = growthAmt * _TendrilOvergrowth * (0.35 + 0.85 * rLen) * (0.35 + 0.65 * growPulse);
                    float tendrilLen = max(min(soilReachBase + overgrow, maxTendrilLen), 0.0001);

                    float tNorm = rayRingPos / tendrilLen;
                    float presence = ringGate * (1.0 - smoothstep(0.85, 1.05, tNorm));

                    // ---- sway + curl along the growth direction --------------
                    float sway = sin(t * (0.6 + 0.6 * rSpeed) + rPhase) * 0.10;
                    float curl = sin(dp * 8.0 + t * (0.4 + 0.4 * rSpeed) + rCurve) * 0.05 * saturate(tNorm);

                    float warpUV1 = sid * 0.53 + t * 0.2;
                    float n = FBM(float2(dp * _NoiseScale, warpUV1)) - 0.5;

                    // position relative to THIS neighbor's center
                    float relFrac = sectorFrac - (k + 0.5);
                    float centerlineFrac = relFrac + sway + curl + n * _NoiseStrength;
                    float arcOffset = centerlineFrac * sectorAngularWidth * lateralScale;

                    float width = _Thickness * (0.35 + 0.9 * rWidth) * pow(1.0 - saturate(tNorm), 1.6) + 0.0012;
                    float distFromCenterline = abs(arcOffset);

                    float tendrilShape = 1.0 - smoothstep(width * 0.08, width, distFromCenterline);
                    float sectorMask = saturate(tendrilShape * presence);

                    // ---- glowing tip ------------------------------------------
                    // Voronoi only affects the final color where
                    // smoothstep(0.55,1.0,tNorm) is nonzero; skip elsewhere.
                    float voronoiBreak = 0.5;
                    if (tNorm > 0.5)
                    {
                        voronoiBreak = Voronoi(float2(sid * 1.7, dp * _NoiseScale * 0.5 + t * 0.1));
                    }
                    float tipCore = pow(saturate(tendrilShape), 3.0) * smoothstep(0.55, 1.0, tNorm) * presence;
                    float sectorTipGlow = tipCore * (0.6 + 0.4 * (1.0 - voronoiBreak));

                    tendrilMask = max(tendrilMask, sectorMask);
                    tipGlow     = max(tipGlow, sectorTipGlow);
                }

                // ---- flash: soil, tendrils and glow all blend toward the same
                // flash color with one shared envelope, so the whole effect
                // brightens and relaxes together ---------------------------
                half3 flashCol   = _FlashColor.rgb * _FlashIntensity;
                half3 soilColor  = lerp(_SoilColorA.rgb, flashCol, flashAmt);
                half3 glowColor  = lerp(_GlowColor.rgb * _GlowIntensity, flashCol, flashAmt);
                float soilAlpha  = soilCoverage * _SoilColorA.a * _SoilAlpha;

                // keep the convergence point (the player) clear regardless of
                // parameters; with _PlayerInfluence = 0 this is the screen center
                float centerClear = 1.0 - smoothstep(0.0, max(_PlayerClearRadius, 0.0001), dp);

                // ---- composite ---------------------------------------------
                half3 col = soilColor * soilAlpha;
                col += soilColor * tendrilMask * (1.0 - soilAlpha);
                col += glowColor * tipGlow;
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