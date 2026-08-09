Shader "Custom/Vines(V2)"
{
    Properties
    {
        [Header(Vine Appearance)]

        _VineColor
        (
            "Vine Color",
            Color
        ) = (0.015, 0.002, 0.002, 1)

        _GlowColor
        (
            "Glow Color",
            Color
        ) = (0.8, 0.015, 0.003, 1)

        _Alpha
        (
            "Alpha",
            Range(0,1)
        ) = 1


        [Header(Vine Shape)]

        _Density
        (
            "Vine Density",
            Range(3,20)
        ) = 10

        _Growth
        (
            "Growth",
            Range(0.05,0.6)
        ) = 0.32

        _Width
        (
            "Width",
            Range(0.002,0.08)
        ) = 0.018

        _Taper
        (
            "Taper",
            Range(0.05,1)
        ) = 0.8


        [Header(Organic Motion)]

        _Sway
        (
            "Sway",
            Range(0,0.2)
        ) = 0.06

        _AnimationSpeed
        (
            "Animation Speed",
            Range(0,3)
        ) = 0.8

        _GrowthSpeed
        (
            "Growth Speed",
            Range(0,3)
        ) = 0.5

        _Pulse
        (
            "Pulse",
            Range(0,1)
        ) = 0.12


        [Header(Noise)]

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
            Range(0,3)
        ) = 0.6

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
        ) = 12.37
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
            Name "MobileEdgeVines"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // =====================================================
            // STRUCTURES
            // =====================================================

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


            // =====================================================
            // PROPERTIES
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _VineColor;
                float4 _GlowColor;

                float _Alpha;

                float _Density;
                float _Growth;
                float _Width;
                float _Taper;

                float _Sway;
                float _AnimationSpeed;
                float _GrowthSpeed;
                float _Pulse;

                float _NoiseScale;
                float _NoiseStrength;

                float _CenterClear;

                float _GlowStrength;
                float _GlowWidth;

                float _Seed;

            CBUFFER_END


            // =====================================================
            // VERTEX
            // =====================================================

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


            // =====================================================
            // HASH
            // =====================================================

            float Hash21(float2 p)
            {
                p += _Seed;

                p = frac(
                    p * float2(
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


            // =====================================================
            // CHEAP VALUE NOISE
            //
            // No loops.
            // =====================================================

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a =
                    Hash21(i);

                float b =
                    Hash21(
                        i + float2(1,0)
                    );

                float c =
                    Hash21(
                        i + float2(0,1)
                    );

                float d =
                    Hash21(
                        i + float2(1,1)
                    );

                return lerp(
                    lerp(a,b,f.x),
                    lerp(c,d,f.x),
                    f.y
                );
            }


            // =====================================================
            // SCREEN UV
            // =====================================================

            float2 ScreenUV(Varyings IN)
            {
                float2 uv =
                    IN.positionHCS.xy /
                    _ScreenParams.xy;

                uv.y = 1.0 - uv.y;

                return uv;
            }


            // =====================================================
            // SINGLE EDGE
            //
            // side:
            //
            // 0 = LEFT
            // 1 = RIGHT
            // 2 = BOTTOM
            // 3 = TOP
            // =====================================================

            float EdgeVines(
                float2 uv,
                float side
            )
            {
                float2 edgeUV;

                // -------------------------------------------------
                // Convert all four edges into the same coordinate
                // system:
                //
                // X = position along edge
                // Y = distance growing inward
                // -------------------------------------------------

                if (side < 0.5)
                {
                    // LEFT

                    edgeUV =
                        float2(
                            uv.y,
                            uv.x
                        );
                }
                else if (side < 1.5)
                {
                    // RIGHT

                    edgeUV =
                        float2(
                            1.0 - uv.y,
                            1.0 - uv.x
                        );
                }
                else if (side < 2.5)
                {
                    // BOTTOM

                    edgeUV =
                        float2(
                            uv.x,
                            uv.y
                        );
                }
                else
                {
                    // TOP

                    edgeUV =
                        float2(
                            1.0 - uv.x,
                            1.0 - uv.y
                        );
                }


                // -------------------------------------------------
                // Coordinates
                //
                // x = along edge
                // y = inward distance
                // -------------------------------------------------

                float along =
                    edgeUV.x;

                float inward =
                    edgeUV.y;


                // -------------------------------------------------
                // CELL / VINE INDEX
                // -------------------------------------------------

                float cell =
                    floor(
                        along *
                        _Density
                    );

                float cellUV =
                    frac(
                        along *
                        _Density
                    );


                // Distance from center of each vine cell.

                float lateral =
                    cellUV -
                    0.5;


                // -------------------------------------------------
                // UNIQUE RANDOMNESS PER VINE
                // -------------------------------------------------

                float seed =
                    Hash21(
                        float2(
                            cell,
                            side * 17.3
                        )
                    );


                float seed2 =
                    Hash21(
                        float2(
                            cell + 41.7,
                            side * 9.1
                        )
                    );


                // -------------------------------------------------
                // TIME
                // -------------------------------------------------

                float time =
                    _Time.y *
                    _AnimationSpeed;


                // -------------------------------------------------
                // ORGANIC NOISE
                // -------------------------------------------------

                float noise1 =
                    Noise(
                        float2(
                            along *
                            _NoiseScale
                            + seed * 13.0,

                            inward * 4.0
                            + time * 0.15
                        )
                    );


                float noise2 =
                    Noise(
                        float2(
                            along *
                            (_NoiseScale * 2.0)
                            - seed * 7.0,

                            inward * 8.0
                            - time * 0.10
                        )
                    );


                // -------------------------------------------------
                // SWAY
                // -------------------------------------------------

                float wave =
                    sin(
                        inward * 18.0
                        + time * 1.7
                        + seed * 6.28
                    );


                float organicSway =
                    (
                        noise1 - 0.5
                    ) *
                    2.0;


                float sway =
                    (
                        wave * 0.5
                        +
                        organicSway * 0.5
                    )
                    *
                    _Sway;


                // Movement increases toward tip.

                float tipInfluence =
                    smoothstep(
                        0.0,
                        _Growth,
                        inward
                    );


                lateral -=
                    sway *
                    tipInfluence;


                // -------------------------------------------------
                // VINE WIDTH
                // -------------------------------------------------

                float randomWidth =
                    lerp(
                        0.7,
                        1.3,
                        seed2
                    );


                float width =
                    _Width *
                    randomWidth;


                // -------------------------------------------------
                // TAPER
                // -------------------------------------------------

                float normalized =
                    inward /
                    max(
                        _Growth,
                        0.001
                    );


                float taper =
                    lerp(
                        1.0,
                        1.0 - _Taper,
                        saturate(
                            normalized
                        )
                    );


                width *=
                    max(
                        taper,
                        0.05
                    );


                // -------------------------------------------------
                // VINE BODY
                // -------------------------------------------------

                float distanceFromCenter =
                    abs(
                        lateral
                    );


                float vine =
                    1.0 -
                    smoothstep(
                        width,
                        width * 2.0,
                        distanceFromCenter
                    );


                // -------------------------------------------------
                // RANDOM GROWTH LENGTH
                // -------------------------------------------------

                float randomLength =
                    lerp(
                        0.65,
                        1.25,
                        seed
                    );


                float animatedGrowth =
                    sin(
                        time *
                        _GrowthSpeed
                        +
                        seed *
                        6.283
                    );


                animatedGrowth =
                    animatedGrowth *
                    0.5 +
                    0.5;


                float length =
                    _Growth *
                    randomLength *
                    lerp(
                        0.75,
                        1.05,
                        animatedGrowth
                    );


                // -------------------------------------------------
                // GROWTH MASK
                // -------------------------------------------------

                float growthMask =
                    1.0 -
                    smoothstep(
                        length * 0.75,
                        length,
                        inward
                    );


                // -------------------------------------------------
                // BASE CONNECTION
                //
                // Keep vines attached to the screen edge.
                // -------------------------------------------------

                float baseMask =
                    smoothstep(
                        0.0,
                        0.025,
                        inward
                    );


                // -------------------------------------------------
                // ORGANIC BREAKUP
                // -------------------------------------------------

                float breakup =
                    lerp(
                        1.0,
                        smoothstep(
                            0.25,
                            0.75,
                            noise2
                        ),
                        _NoiseStrength
                    );


                // -------------------------------------------------
                // PULSE
                // -------------------------------------------------

                float pulse =
                    sin(
                        time * 2.0
                        +
                        seed * 5.0
                    );


                pulse =
                    pulse *
                    0.5 +
                    0.5;


                float pulseAmount =
                    lerp(
                        1.0,
                        1.0 + _Pulse,
                        pulse
                    );


                // -------------------------------------------------
                // FINAL VINE
                // -------------------------------------------------

                vine *=
                    growthMask;

                vine *=
                    baseMask;

                vine *=
                    breakup;

                vine *=
                    pulseAmount;


                // -------------------------------------------------
                // CENTER PROTECTION
                //
                // Fade vines as they approach screen center.
                // -------------------------------------------------

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


                vine *=
                    centerMask;


                return saturate(
                    vine
                );
            }


            // =====================================================
            // ALL FOUR EDGES
            // =====================================================

            float GenerateVines(float2 uv)
            {
                float left =
                    EdgeVines(
                        uv,
                        0.0
                    );

                float right =
                    EdgeVines(
                        uv,
                        1.0
                    );

                float bottom =
                    EdgeVines(
                        uv,
                        2.0
                    );

                float top =
                    EdgeVines(
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


            // =====================================================
            // FRAGMENT
            // =====================================================

            half4 Frag(Varyings IN)
                : SV_Target
            {
                float2 uv =
                    ScreenUV(IN);


                // -------------------------------------------------
                // VINES
                // -------------------------------------------------

                float vines =
                    GenerateVines(
                        uv
                    );


                // -------------------------------------------------
                // GLOW
                // -------------------------------------------------

                float glow =
                    smoothstep(
                        0.0,
                        _GlowWidth,
                        vines
                    );


                glow *=
                    _GlowStrength;


                // -------------------------------------------------
                // COLOR
                // -------------------------------------------------

                float3 color =
                    _VineColor.rgb *
                    vines;


                color +=
                    _GlowColor.rgb *
                    glow;


                // -------------------------------------------------
                // ALPHA
                // -------------------------------------------------

                float alpha =
                    saturate(
                        vines +
                        glow * 0.15
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