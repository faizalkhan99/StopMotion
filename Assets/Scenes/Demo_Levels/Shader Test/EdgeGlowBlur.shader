// Utility shader used by EdgeGlowMask.cs. One-direction 9-tap Gaussian (5 fetches,
// linear-sampling trick). Sigma is ~1.69 texels * _BlurScale.
Shader "Hidden/EdgeGlowBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BlurDir;      // (1,0) horizontal or (0,1) vertical
            float  _BlurScale;

            half4 frag(v2f_img i) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * _BlurDir.xy * _BlurScale;

                half s = tex2D(_MainTex, i.uv).r * 0.2270270270h;
                s += (tex2D(_MainTex, i.uv + d * 1.3846153846).r +
                      tex2D(_MainTex, i.uv - d * 1.3846153846).r) * 0.3162162162h;
                s += (tex2D(_MainTex, i.uv + d * 3.2307692308).r +
                      tex2D(_MainTex, i.uv - d * 3.2307692308).r) * 0.0702702703h;

                return half4(s, s, s, s);
            }
            ENDCG
        }
    }
}
