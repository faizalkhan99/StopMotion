Shader "CityBuster/V4"
{
    // ----------------------------------------------------------------------
    // Border-telegraph overlay: thorny vines grow INWARD from the four
    // screen edges. The "soil" (mottled red/black cellular noise) sits
    // flush against the border; tendrils root inside the soil and poke
    // OUT beyond it, tapering to glowing points.
    //
    // Soil reach and tendril overgrowth are separate controls: soil is
    // the base material, tendrils are allowed to extend further than
    // the soil itself. Growth driven externally via _GrowthAmount (0 =
    // hidden, 1 = full), intended to be wired to
    // GameEventBus.OnChronoStateChanged / OnGracePeriodUpdated.
    // ----------------------------------------------------------------------

    Properties
    {
        [Header(Growth Control)]
        _GrowthAmount       ("Growth Amount (0 hidden, 1 full)", Range(0.0, 1.0)) = 1.0
        _SoilReach          ("Soil Reach (UV units)",            Range(0.01, 0.5)) = 0.12
        _TendrilOvergrowth  ("Tendril Overgrowth Length",        Range(0.0, 0.6))  = 0.25
        _EdgeJaggedness     ("Edge Jaggedness",                  Range(0.0, 1.0))  = 0.6

        [Header(Tendril Shape)]
        _TendrilCount    ("Tendrils Per Edge",   Range(2, 64))    = 18
        _Thickness       ("Thickness",           Range(0.001, 0.2)) = 0.02
        _AnimationSpeed  ("Animation Speed",     Range(0.0, 5.0)) = 1.0
        _NoiseScale      ("Noise Scale",         Range(0.1, 20.0)) = 6.0
        _NoiseStrength   ("Noise Strength",      Range(0.0, 1.0)) = 0.35

        [Header(Thorn Detail)]
        _ThornAmount     ("Thorn Amount",     Range(0.0, 1.0))  = 0.7
        _ThornSharpness  ("Thorn Sharpness",  Range(1.0, 12.0)) = 6.0
        _ThornScale      ("Thorn Scale",      Range(1.0, 60.0)) = 26.0
        _ThornAlpha      ("Thorn Alpha",      Range(0.0, 1.0))  = 1.0

        [Header(Glow Tip)]
        _GlowColor       ("Tip Glow Color", Color) = (1.0, 0.28, 0.06, 1)
        _GlowIntensity   ("Glow Intensity", Range(0.0, 8.0)) = 2.5

        [Header(Soil Root Mass)]
        _SoilColorA      ("Soil Color A Base",  Color) = (0.05, 0.01, 0.01, 0.85)
        _SoilColorB      ("Soil Color B Veins", Color) = (0.55, 0.05, 0.03, 0.95)
        _SoilCellScale   ("Soil Cell Scale",    Range(1.0, 60.0)) = 18.0
        _SoilVeinWidth   ("Soil Vein Width",    Range(0.0, 0.5))  = 0.12
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
                float  _GrowthAmount;
                float  _SoilReach;
                float  _TendrilOvergrowth;
                float  _EdgeJaggedness;
                float  _TendrilCount;
                float  _Thickness;
                float  _AnimationSpeed;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _ThornAmount;
                float  _ThornSharpness;
                float  _ThornScale;
                float  _ThornAlpha;
                half4  _GlowColor;
                half   _GlowIntensity;
                half4  _SoilColorA;
                half4  _SoilColorB;
                float  _SoilCellScale;
                float  _SoilVeinWidth;
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

            // Ridged noise: sharp thin peaks for thorn spikes.
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
                float2 uv = IN.positionHCS.xy / _ScreenParams.xy;
                uv.y = 1.0 - uv.y;

                float t = _Time.y * _AnimationSpeed;

                // ---- distance to nearest screen edge + which edge/lane --
                float dl = uv.x;
                float dr = 1.0 - uv.x;
                float db = uv.y;
                float dt = 1.0 - uv.y;

                float d = min(min(dl, dr), min(db, dt));

                float edgeId;
                float lane;
                if (d == dl)      { edgeId = 0.0; lane = uv.y; }
                else if (d == dr) { edgeId = 1.0; lane = uv.y; }
                else if (d == db) { edgeId = 2.0; lane = uv.x; }
                else              { edgeId = 3.0; lane = uv.x; }

                float growthAmt = saturate(_GrowthAmount);

                // ---- per-tendril lane identity ---------------------------
                float tendrilCount = max(_TendrilCount, 1.0);
                float sectorF    = lane * tendrilCount;
                float sectorId   = floor(sectorF) + edgeId * 10000.0;
                float sectorFrac = frac(sectorF);

                float rLen    = Hash11(sectorId * 13.17 + 1.7);
                float rSpeed  = Hash11(sectorId * 7.31  + 4.2);
                float rPhase  = Hash11(sectorId * 3.71  + 9.3) * TWO_PI;
                float rWidth  = Hash11(sectorId * 5.13  + 2.9);
                float rCurve  = Hash11(sectorId * 9.77  + 6.1) * TWO_PI;

                // ---- soil reach: base root-mass extent from the edge -----
                float soilJag = FBM(float2(lane * 6.0 + edgeId * 3.0, t * 0.05)) - 0.5;
                float soilReachBase = growthAmt * _SoilReach;
                float soilReach = max(soilReachBase * (1.0 + soilJag * 0.35 * _EdgeJaggedness), 0.0001);
                float soilCoverage = 1.0 - smoothstep(soilReach * 0.8, soilReach * 1.1, d);

                // ---- tendril length: soil reach + overgrowth beyond it ---
                // Per-lane variation applies to the overgrowth portion, so
                // every lane guarantees at least soilReachBase (rooted
                // inside the soil) and typically extends well past it.
                float growPulse = 0.5 + 0.5 * FBM(float2(sectorId * 0.37, t * (0.15 + 0.25 * rSpeed)));
                float overgrow  = growthAmt * _TendrilOvergrowth * (0.35 + 0.85 * rLen) * (0.35 + 0.65 * growPulse);
                float tendrilLen = max(soilReachBase + overgrow, 0.0001);

                // root (d = 0, at the screen edge) -> tip (tNorm = 1)
                float tNorm = d / tendrilLen;
                float presence = 1.0 - smoothstep(0.85, 1.05, tNorm);

                // ---- sway + curl along the growth direction --------------
                float sway = sin(t * (0.6 + 0.6 * rSpeed) + rPhase) * 0.10;
                float curl = sin(d * 8.0 + t * (0.4 + 0.4 * rSpeed) + rCurve) * 0.05 * saturate(tNorm);

                float warpUV1 = sectorId * 0.53 + t * 0.2;
                float n = FBM(float2(d * _NoiseScale, warpUV1)) - 0.5;

                float laneWidth = 1.0 / tendrilCount;
                float centerlineFrac = (sectorFrac - 0.5) + sway + curl + n * _NoiseStrength;
                float laneOffset = centerlineFrac * laneWidth;

                float width = _Thickness * (0.35 + 0.9 * rWidth) * pow(1.0 - saturate(tNorm), 1.6) + 0.0012;
                float distFromCenterline = abs(laneOffset);

                // edge0 < edge1 (spec-legal smoothstep, was inverted originally)
                float tendrilShape = 1.0 - smoothstep(width * 0.08, width, distFromCenterline);
                float tendrilMask = saturate(tendrilShape * presence);

                // ---- dense thorn cluster, thick near root, thin at tip --
                float2 thornUV = float2(
                    lane * tendrilCount * 5.0 + sectorId * 11.3,
                    d * _ThornScale + t * 0.5 * (0.5 + rSpeed));
                float ridged = pow(saturate(RidgedNoise(thornUV)), _ThornSharpness);

                float rootBias = smoothstep(1.0, 0.0, tNorm); // 1 at root, 0 at tip
                float thornMask = ridged * presence * lerp(0.25, 1.0, rootBias) * _ThornAmount;

                float2 clumpUV = float2(lane * tendrilCount * 1.6 + sectorId * 4.1, d * 5.0 + t * 0.1);
                float clump = smoothstep(0.35, 0.8, ValueNoise(clumpUV));
                thornMask *= lerp(0.4, 1.0, clump);

                // single point of control for thorn opacity, applied here
                // so it scales both the color blend and the alpha below
                thornMask *= _ThornAlpha;

                // ---- glowing tip ------------------------------------------
                float voronoiBreak = Voronoi(float2(sectorId * 1.7, d * _NoiseScale * 0.5 + t * 0.1));
                float tipCore = pow(saturate(tendrilShape), 3.0) * smoothstep(0.55, 1.0, tNorm) * presence;
                float tipGlow = tipCore * (0.6 + 0.4 * (1.0 - voronoiBreak));

                // ---- soil / root mass color: mottled cellular pattern ----
                float2 soilUV = uv * _SoilCellScale + float2(t * 0.01, 0.0);
                float cell = Voronoi(soilUV);
                float veins = 1.0 - smoothstep(_SoilVeinWidth, _SoilVeinWidth + 0.05, cell);

                half3 soilColor = lerp(_SoilColorA.rgb, _SoilColorB.rgb, veins);
                float soilAlpha = soilCoverage * lerp(_SoilColorA.a, _SoilColorB.a, veins) * _SoilAlpha;

                // ---- composite ---------------------------------------------
                half3 col = soilColor * soilAlpha;

                // tendril body fills in with soil-base tone where it
                // reaches past the soil coverage area (out toward the tip)
                col += _SoilColorA.rgb * tendrilMask * (1.0 - soilAlpha);

                half3 thornTint = lerp(_SoilColorB.rgb * 1.3, _GlowColor.rgb, 0.15);
                col += thornTint * thornMask * 0.5;
                col += _GlowColor.rgb * _GlowIntensity * tipGlow;

                half outAlpha = saturate(
                    soilAlpha +
                    tendrilMask * 0.6 * (1.0 - soilAlpha) +
                    tipGlow * 0.7 +
                    thornMask * 0.25);

                return half4(col, outAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
