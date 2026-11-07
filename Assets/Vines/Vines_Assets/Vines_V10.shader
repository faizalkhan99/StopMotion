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

        [Header(Triggered Flash Burst)]
        _FlashTriggerTime   ("Burst Start Time (Time.y snapshot)",  Float) = -1000
        _FlashCount         ("Flashes Per Burst",                   Range(1, 8)) = 3
        _FlashPulseDuration ("Single Flash Duration (s)",           Range(0.02, 0.5)) = 0.08

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
                float  _FlashTriggerTime;
                float  _FlashCount;
                float  _FlashPulseDuration;
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

            // ------------------------------------------------------------
            // Global lookup textures, generated at startup by VineShaderDataBaker.cs
            // (set via Shader.SetGlobalTexture, so no material wiring needed).
            //   _VineNoiseTex  : 256x256 R8, Repeat, Bilinear. Random lattice values.
            //   _VineSectorTex : 96x2 RGBA32, Clamp, Point.
            //       row 0 (v=0.25): R=rLen  G=rSpeed  B=rWidth
            //       row 1 (v=0.75): R=rPhase/2PI  G=rCurve/2PI
            // ------------------------------------------------------------
            TEXTURE2D(_VineNoiseTex);  SAMPLER(sampler_VineNoiseTex);
            TEXTURE2D(_VineSectorTex); SAMPLER(sampler_VineSectorTex);

            #define VINE_NOISE_SIZE   256.0
            #define VINE_MAX_SECTORS  96.0

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
            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Texture-backed value noise: one bilinear fetch replaces 4 hashes + lerps.
            // The smoothstep is applied to the fractional part BEFORE the fetch, so the
            // hardware's linear filter reproduces the same smooth interpolation as before.
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = p - i;
                float2 u = f * f * (3.0 - 2.0 * f);
                float2 uv = (i + u + 0.5) * (1.0 / VINE_NOISE_SIZE);
                return SAMPLE_TEXTURE2D_LOD(_VineNoiseTex, sampler_VineNoiseTex, uv, 0).r;
            }

            // 3 octaves (was 4), normalised by total amplitude so the mean is unchanged.
            float FBM(float2 p)
            {
                float sum  = 0.0;
                float amp  = 0.5;
                float freq = 1.0;

                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    sum  += amp * ValueNoise(p * freq);
                    freq *= 2.03;
                    amp  *= 0.5;
                }
                return sum * (1.0 / 0.875);
            }

            // Cheaper 2-octave variant for low-frequency, slow-changing
            // uses (ring jaggedness, growth pulse).
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

            // 2x2 search (was 3x3): picks the two nearest cells per axis based on
            // which half of the cell the point is in. Only drives tip-glow breakup.
            float Voronoi(float2 p)
            {
                float2 i = floor(p);
                float2 f = p - i;
                float2 baseOff = step(0.5, f) - 1.0;   // -1 if f<0.5, 0 otherwise
                float minDist = 1.0;

                [unroll]
                for (int y = 0; y <= 1; y++)
                {
                    [unroll]
                    for (int x = 0; x <= 1; x++)
                    {
                        float2 neighbor = baseOff + float2(x, y);
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
                float elapsed   = _Time.y - _StartTime;
                float flashDur  = max(_FlashDuration, 0.0001);
                float growDur   = max(_Duration, 0.0001);
                float delay     = max(_GrowthDelay, 0.0);
                float cycleLen  = flashDur + growDur + delay;

                float cycleTime = elapsed - delay;   // < 0 while in the initial wait
                float cycleIdx  = (_LoopCycles > 0.5) ? max(floor(cycleTime / cycleLen), 0.0) : 0.0;
                float tau       = cycleTime - cycleIdx * cycleLen;

                // Cycle flash only on cycles AFTER the first: no flash at the beginning.
                float prevHold   = (cycleIdx > 0.5) ? 1.0 : 0.0;
                float flashLead  = prevHold * flashDur;   // first cycle has no flash window

                // flash envelope: smooth 0 -> 1 -> 0 (sin^2), exactly 0 outside the flash window
                float flashPhase = saturate(tau / flashDur);
                float flashEnv   = sin(PI * flashPhase);
                flashEnv *= flashEnv * prevHold;

                float ramp        = saturate((tau - flashLead) / growDur);
                float retract     = prevHold * (1.0 - smoothstep(0.5, 1.0, flashPhase));
                float reveal      = _FlashReveal * flashEnv;
                float cycleGrowth = (tau < flashLead) ? max(retract, reveal) : ramp;

                // Triggered burst: _FlashCount rapid sin^2 pulses back to back,
                // starting at _FlashTriggerTime. Independent of _IsRunning, so it
                // works in trigger-based mode.
                float burstT   = (_Time.y - _FlashTriggerTime) / max(_FlashPulseDuration, 0.005);
                float burstIdx = floor(burstT);
                float burstOn  = step(0.0, burstT) * step(burstIdx, _FlashCount - 0.5);
                float burstEnv = sin(PI * frac(burstT));
                burstEnv *= burstEnv * burstOn;

                float growthAmt = lerp(_PausedGrowth, cycleGrowth, saturate(_IsRunning));
                growthAmt = max(growthAmt, _FlashReveal * burstEnv);   // flash shows even at 0 growth
                float flashAmt  = max(flashEnv * saturate(_IsRunning), burstEnv);

                // Nothing to draw yet — uniform branch, no warp divergence.
                if (growthAmt <= 0.0005)
                {
                    return half4(0, 0, 0, 0);
                }

                // ---- cheap coarse band gate --------------------------------
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
                float2 convergePoint = _PlayerPos.xy - 0.5;
                convergePoint.x *= aspect;
                convergePoint *= saturate(_PlayerInfluence);
                float maxPointLen = _Radius * (1.0 - 0.175 * _EdgeJaggedness) * 0.9;
                convergePoint *= min(1.0, maxPointLen / max(length(convergePoint), 0.0001));

                // ---- ray from the player through this pixel to the ring ------
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

                // ================= FIX 1: tighter gates =================

                // Nothing exists outside the ring (ringGate is 0 below -0.015)
                if (ringPos < -0.015) return half4(0, 0, 0, 0);

                // Longest tendril that can possibly exist this frame:
                // overgrow max = (0.35+0.85) * (0.35+0.65) = 1.2
                float maxPossibleLen = min(soilReachBase + growthAmt * _TendrilOvergrowth * 1.2,
                                           maxTendrilLen);
                bool tendrilPossible = rayRingPos < maxPossibleLen * 1.05;
                bool soilPossible    = ringPos < soilReachBase * 1.05 + 0.0001;

                if (!tendrilPossible && !soilPossible) return half4(0, 0, 0, 0);

                float ringGate = smoothstep(-0.015, 0.015, ringPos);
                float soilCoverage = ringGate * (1.0 - smoothstep(soilReachBase * 0.8,
                                                                  soilReachBase * 1.05 + 0.0001, ringPos));

                float sectorAngularWidth = (TWO_PI / tendrilCount);

                // converts ring-angle offset into lateral distance at this
                // pixel (equals dist when the player is at the screen center)
                float lateralScale = max(dp, 0.001) * _Radius * cosI / max(rayToRing, 0.001);

                float tendrilMask = 0.0;
                float tipGlow = 0.0;

                if (tendrilPossible)
                {
                    // widest a tendril can be, expressed in sector-fraction units
                    float maxWidthFrac = (_Thickness * 1.25 + 0.0012)
                                       / max(sectorAngularWidth * lateralScale, 0.0001);
                    // sway (0.10) + curl (0.05) + noise (0.5 * strength) + width + margin
                    float reachFrac = 0.15 + 0.5 * _NoiseStrength + maxWidthFrac + 0.01;

                    [loop]
                    for (int k = -1; k <= 1; k++)
                    {
                        float relFrac0 = sectorFrac - (k + 0.5);
                        if (abs(relFrac0) > reachFrac) continue;   // neighbor can't reach this pixel

                        // wrapped so the last and first sectors are neighbors
                        float sid = fmod(sectorId + k + tendrilCount, tendrilCount);

                        // FIX 2: per-sector randoms from the baked lookup (was 5x Hash11)
                        float  sx   = (sid + 0.5) * (1.0 / VINE_MAX_SECTORS);
                        float3 rA   = SAMPLE_TEXTURE2D_LOD(_VineSectorTex, sampler_VineSectorTex, float2(sx, 0.25), 0).rgb;
                        float2 rB   = SAMPLE_TEXTURE2D_LOD(_VineSectorTex, sampler_VineSectorTex, float2(sx, 0.75), 0).rg;
                        float rLen   = rA.r;
                        float rSpeed = rA.g;
                        float rWidth = rA.b;
                        float rPhase = rB.r * TWO_PI;
                        float rCurve = rB.g * TWO_PI;

                        float growPulse = 0.5 + 0.5 * FBM2(float2(sid * 0.37, t * (0.15 + 0.25 * rSpeed)));
                        float overgrow  = growthAmt * _TendrilOvergrowth * (0.35 + 0.85 * rLen) * (0.35 + 0.65 * growPulse);
                        float tendrilLen = max(min(soilReachBase + overgrow, maxTendrilLen), 0.0001);

                        float tNorm    = rayRingPos / tendrilLen;
                        float presence = ringGate * (1.0 - smoothstep(0.85, 1.05, tNorm));
                        if (presence < 0.001) continue;            // skip the expensive FBM entirely

                        // ---- sway + curl along the growth direction --------------
                        float sway = sin(t * (0.6 + 0.6 * rSpeed) + rPhase) * 0.10;
                        float curl = sin(dp * 8.0 + t * (0.4 + 0.4 * rSpeed) + rCurve) * 0.05 * saturate(tNorm);

                        float n = FBM(float2(dp * _NoiseScale, sid * 0.53 + t * 0.2)) - 0.5;

                        float centerlineFrac = relFrac0 + sway + curl + n * _NoiseStrength;
                        float arcOffset = centerlineFrac * sectorAngularWidth * lateralScale;

                        float width = _Thickness * (0.35 + 0.9 * rWidth) * pow(1.0 - saturate(tNorm), 1.6) + 0.0012;
                        float tendrilShape = 1.0 - smoothstep(width * 0.08, width, abs(arcOffset));
                        float sectorMask = saturate(tendrilShape * presence);

                        // ---- glowing tip: Voronoi only where it's visible --------
                        float sectorTipGlow = 0.0;
                        if (tNorm > 0.55 && tendrilShape > 0.01)
                        {
                            float voronoiBreak = Voronoi(float2(sid * 1.7, dp * _NoiseScale * 0.5 + t * 0.1));
                            float tipCore = pow(tendrilShape, 3.0) * smoothstep(0.55, 1.0, tNorm) * presence;
                            sectorTipGlow = tipCore * (0.6 + 0.4 * (1.0 - voronoiBreak));
                        }

                        tendrilMask = max(tendrilMask, sectorMask);
                        tipGlow     = max(tipGlow, sectorTipGlow);
                    }
                }

                // ---- flash: soil, tendrils and glow all blend toward the same
                // flash color with one shared envelope ----------------------
                half3 flashCol   = _FlashColor.rgb * _FlashIntensity;
                half3 soilColor  = lerp(_SoilColorA.rgb, flashCol, flashAmt);
                half3 glowColor  = lerp(_GlowColor.rgb * _GlowIntensity, flashCol, flashAmt);
                float soilAlpha  = soilCoverage * _SoilColorA.a * _SoilAlpha;

                // keep the convergence point (the player) clear
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