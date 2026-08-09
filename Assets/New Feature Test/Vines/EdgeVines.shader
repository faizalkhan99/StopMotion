Shader "Custom/EdgeVines"
{
    Properties
    {
        [Header(Main Color)]

        _DarkColor
        (
            "Dark Color",
            Color
        ) = (0.008, 0.006, 0.012, 1)

        _VineColor
        (
            "Vine Color",
            Color
        ) = (0.025, 0.008, 0.012, 1)

        _GlowColor
        (
            "Inner Glow",
            Color
        ) = (1.0, 0.025, 0.005, 1)

        _Alpha
        (
            "Alpha",
            Range(0,1)
        ) = 0.95


        [Header(Tendril Shape)]

        _Density
        (
            "Tendril Density",
            Range(3,20)
        ) = 10

        _Growth
        (
            "Growth",
            Range(0.05,0.65)
        ) = 0.38

        _Width
        (
            "Root Width",
            Range(0.005,0.15)
        ) = 0.055

        _Taper
        (
            "Tip Taper",
            Range(0.1,1)
        ) = 0.88

        _Curve
        (
            "Curve",
            Range(0,0.25)
        ) = 0.08


        [Header(Organic Detail)]

        _NoiseScale
        (
            "Noise Scale",
            Range(1,20)
        ) = 5

        _NoiseStrength
        (
            "Noise Strength",
            Range(0,1)
        ) = 0.45

        _ThornAmount
        (
            "Thorn Amount",
            Range(0,1)
        ) = 0.45


        [Header(Animation)]

        _AnimationSpeed
        (
            "Animation Speed",
            Range(0,3)
        ) = 0.7

        _GrowthSpeed
        (
            "Growth Speed",
            Range(0,3)
        ) = 0.45

        _Sway
        (
            "Sway",
            Range(0,0.2)
        ) = 0.055

        _Pulse
        (
            "Pulse",
            Range(0,1)
        ) = 0.12


        [Header(Center)]

        _CenterClear
        (
            "Center Clear",
            Range(0.05,0.8)
        ) = 0.28


        [Header(Glow)]

        _GlowStrength
        (
            "Glow Strength",
            Range(0,4)
        ) = 1.2

        _GlowWidth
        (
            "Glow Width",
            Range(0.01,0.3)
        ) = 0.08


        [Header(Random)]

        _Seed
        (
            "Seed",
            Range(0,100)
        ) = 17.3
    }


    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off
        ZTest Always
        Cull Off


        Pass
        {
            Name "MobileTendrils"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // =========================================================
            // STRUCTURES
            // =========================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            // =========================================================
            // MATERIAL
            // =========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _DarkColor;
                float4 _VineColor;
                float4 _GlowColor;

                float _Alpha;

                float _Density;
                float _Growth;
                float _Width;
                float _Taper;
                float _Curve;

                float _NoiseScale;
                float _NoiseStrength;
                float _ThornAmount;

                float _AnimationSpeed;
                float _GrowthSpeed;
                float _Sway;
                float _Pulse;

                float _CenterClear;

                float _GlowStrength;
                float _GlowWidth;

                float _Seed;

            CBUFFER_END


            // =========================================================
            // VERTEX
            // =========================================================

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );

                OUT.uv = IN.uv;

                return OUT;
            }


            // =========================================================
            // HASH
            // =========================================================

            float Hash21(float2 p)
            {
                p += _Seed;

                p = frac(
                    p *
                    float2(
                        127.1,
                        311.7
                    )
                );

                p +=
                    dot(
                        p,
                        p + 34.5
                    );

                return frac(
                    p.x * p.y
                );
            }


            // =========================================================
            // CHEAP VALUE NOISE
            // =========================================================

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f =
                    f * f *
                    (3.0 - 2.0 * f);

                float a =
                    Hash21(i);

                float b =
                    Hash21(
                        i +
                        float2(1,0)
                    );

                float c =
                    Hash21(
                        i +
                        float2(0,1)
                    );

                float d =
                    Hash21(
                        i +
                        float2(1,1)
                    );

                return lerp(
                    lerp(a,b,f.x),
                    lerp(c,d,f.x),
                    f.y
                );
            }


            // =========================================================
            // SCREEN UV
            // =========================================================

            float2 GetScreenUV(Varyings IN)
            {
                float2 uv =
                    IN.positionHCS.xy /
                    _ScreenParams.xy;

                uv.y = 1.0 - uv.y;

                return uv;
            }


            // =========================================================
            // SINGLE EDGE
            //
            // Every edge is converted into:
            //
            // X = position along screen edge
            // Y = distance growing inward
            //
            // This lets us use the exact same cheap tendril
            // mathematics for all four edges.
            // =========================================================

            float EdgeTendrils(
                float2 uv,
                float side
            )
            {
                float2 p;


                // -----------------------------------------------------
                // LEFT
                // -----------------------------------------------------

                if (side < 0.5)
                {
                    p =
                        float2(
                            uv.y,
                            uv.x
                        );
                }


                // -----------------------------------------------------
                // RIGHT
                // -----------------------------------------------------

                else if (side < 1.5)
                {
                    p =
                        float2(
                            1.0 - uv.y,
                            1.0 - uv.x
                        );
                }


                // -----------------------------------------------------
                // BOTTOM
                // -----------------------------------------------------

                else if (side < 2.5)
                {
                    p =
                        float2(
                            uv.x,
                            uv.y
                        );
                }


                // -----------------------------------------------------
                // TOP
                // -----------------------------------------------------

                else
                {
                    p =
                        float2(
                            1.0 - uv.x,
                            1.0 - uv.y
                        );
                }


                float along =
                    p.x;

                float inward =
                    p.y;


                // =====================================================
                // TENDRIL CELLS
                // =====================================================

                float scaled =
                    along *
                    _Density;

                float cell =
                    floor(scaled);

                float cellUV =
                    frac(scaled);


                // Center of each tendril.

                float lateral =
                    cellUV -
                    0.5;


                // =====================================================
                // UNIQUE TENDRIL RANDOMNESS
                // =====================================================

                float r1 =
                    Hash21(
                        float2(
                            cell,
                            side * 19.17
                        )
                    );

                float r2 =
                    Hash21(
                        float2(
                            cell + 31.7,
                            side * 7.13
                        )
                    );

                float r3 =
                    Hash21(
                        float2(
                            cell + 71.4,
                            side * 3.71
                        )
                    );


                // =====================================================
                // TIME
                // =====================================================

                float time =
                    _Time.y *
                    _AnimationSpeed;


                // =====================================================
                // ORGANIC CURVE
                // =====================================================

                float curveWave =
                    sin(
                        inward * 10.0 +
                        r1 * 6.283 +
                        time * 0.8
                    );

                float curveNoise =
                    Noise(
                        float2(
                            cell * 0.73,
                            inward * _NoiseScale
                            + time * 0.12
                        )
                    );


                float curve =
                    (
                        curveWave * 0.55 +
                        (curveNoise - 0.5) * 1.5
                    )
                    *
                    _Curve;


                // Curvature becomes stronger toward tip.

                float curveInfluence =
                    smoothstep(
                        0.02,
                        _Growth,
                        inward
                    );


                lateral -=
                    curve *
                    curveInfluence;


                // =====================================================
                // SIDEWAYS SWAY
                // =====================================================

                float swayWave =
                    sin(
                        time *
                        (1.0 + r2) +
                        r3 * 6.283
                    );


                lateral -=
                    swayWave *
                    _Sway *
                    curveInfluence;


                // =====================================================
                // TENDRIL LENGTH
                // =====================================================

                float randomLength =
                    lerp(
                        0.65,
                        1.35,
                        r1
                    );


                float growthPulse =
                    sin(
                        time *
                        _GrowthSpeed +
                        r2 * 6.283
                    );


                growthPulse =
                    growthPulse *
                    0.5 +
                    0.5;


                float length =
                    _Growth *
                    randomLength *
                    lerp(
                        0.78,
                        1.08,
                        growthPulse
                    );


                // =====================================================
                // TENDRIL DISTANCE
                // =====================================================

                float distanceFromCenter =
                    abs(lateral);


                // =====================================================
                // WIDTH
                //
                // Thick root -> thin tip.
                // =====================================================

                float normalized =
                    saturate(
                        inward /
                        max(
                            length,
                            0.001
                        )
                    );


                float taper =
                    pow(
                        1.0 -
                        normalized,
                        _Taper + 0.35
                    );


                float width =
                    _Width *
                    taper;


                // Individual tendril width variation.

                width *=
                    lerp(
                        0.65,
                        1.35,
                        r2
                    );


                // =====================================================
                // MAIN TENDRIL BODY
                // =====================================================

                float body =
                    1.0 -
                    smoothstep(
                        width,
                        width * 1.8,
                        distanceFromCenter
                    );


                // =====================================================
                // GROWTH / TIP
                // =====================================================

                float growthMask =
                    1.0 -
                    smoothstep(
                        length * 0.72,
                        length,
                        inward
                    );


                // =====================================================
                // ROOT CONNECTION
                // =====================================================

                float rootMask =
                    smoothstep(
                        0.0,
                        0.025,
                        inward
                    );


                // =====================================================
                // ORGANIC SURFACE
                // =====================================================

                float surfaceNoise =
                    Noise(
                        float2(
                            along *
                            _NoiseScale *
                            1.5
                            +
                            cell * 2.7,

                            inward *
                            _NoiseScale *
                            2.0
                            -
                            time * 0.18
                        )
                    );


                float surfaceBreakup =
                    lerp(
                        1.0,
                        smoothstep(
                            0.25,
                            0.72,
                            surfaceNoise
                        ),
                        _NoiseStrength
                    );


                // =====================================================
                // SECONDARY ROOT / THORN STRUCTURE
                //
                // Cheap high-frequency ridges.
                // =====================================================

                float thornNoise =
                    Noise(
                        float2(
                            along *
                            _Density *
                            3.5
                            +
                            cell * 8.1,

                            inward *
                            24.0
                            +
                            time * 0.25
                        )
                    );


                float thorn =
                    smoothstep(
                        0.68,
                        0.92,
                        thornNoise
                    );


                // Thorns strongest around root/mid section.

                float thornRegion =
                    smoothstep(
                        0.0,
                        0.45,
                        normalized
                    )
                    *
                    (
                        1.0 -
                        smoothstep(
                            0.45,
                            1.0,
                            normalized
                        )
                    );


                thorn *=
                    thornRegion *
                    _ThornAmount;


                // =====================================================
                // BREATHING
                // =====================================================

                float breathe =
                    sin(
                        time * 2.0 +
                        r3 * 6.283
                    );


                breathe =
                    breathe *
                    0.5 +
                    0.5;


                body *=
                    lerp(
                        1.0,
                        1.0 + _Pulse,
                        breathe
                    );


                // =====================================================
                // COMBINE
                // =====================================================

                float tendril =
                    body *
                    growthMask *
                    rootMask *
                    surfaceBreakup;


                // Thorns merge into main mass.

                tendril =
                    max(
                        tendril,
                        thorn *
                        growthMask *
                        rootMask
                    );


                // =====================================================
                // CENTER PROTECTION
                // =====================================================

                float centerDistance =
                    distance(
                        uv,
                        float2(
                            0.5,
                            0.5
                        )
                    );


                float centerMask =
                    smoothstep(
                        0.0,
                        _CenterClear,
                        centerDistance
                    );


                tendril *=
                    centerMask;


                return saturate(
                    tendril
                );
            }


            // =========================================================
            // ALL FOUR EDGES
            // =========================================================

            float GenerateTendrils(float2 uv)
            {
                float left =
                    EdgeTendrils(
                        uv,
                        0.0
                    );

                float right =
                    EdgeTendrils(
                        uv,
                        1.0
                    );

                float bottom =
                    EdgeTendrils(
                        uv,
                        2.0
                    );

                float top =
                    EdgeTendrils(
                        uv,
                        3.0
                    );


                return max(
                    max(
                        left,
                        right
                    ),
                    max(
                        bottom,
                        top
                    )
                );
            }


            // =========================================================
            // FRAGMENT
            // =========================================================

            half4 Frag(Varyings IN)
                : SV_Target
            {
                float2 uv =
                    GetScreenUV(IN);


                // -----------------------------------------------------
                // MAIN TENDRILS
                // -----------------------------------------------------

                float tendrils =
                    GenerateTendrils(
                        uv
                    );


                // -----------------------------------------------------
                // INNER GLOW
                //
                // Stronger near the body center and root.
                // -----------------------------------------------------

                float innerGlow =
                    smoothstep(
                        0.05,
                        0.8,
                        tendrils
                    );


                innerGlow *=
                    _GlowStrength;


                // -----------------------------------------------------
                // PULSING GLOW
                // -----------------------------------------------------

                float glowPulse =
                    sin(
                        _Time.y *
                        _AnimationSpeed *
                        2.0
                    );


                glowPulse =
                    glowPulse *
                    0.5 +
                    0.5;


                innerGlow *=
                    lerp(
                        0.75,
                        1.25,
                        glowPulse *
                        _Pulse
                    );


                // -----------------------------------------------------
                // COLOR
                // -----------------------------------------------------

                float3 color =
                    lerp(
                        _DarkColor.rgb,
                        _VineColor.rgb,
                        tendrils
                    );


                // Warm inner corruption.

                color +=
                    _GlowColor.rgb *
                    innerGlow;


                // -----------------------------------------------------
                // ALPHA
                // -----------------------------------------------------

                float alpha =
                    tendrils;


                alpha +=
                    innerGlow *
                    0.12;


                alpha =
                    saturate(
                        alpha
                    );


                alpha *=
                    _Alpha;


                return float4(
                    color,
                    alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}