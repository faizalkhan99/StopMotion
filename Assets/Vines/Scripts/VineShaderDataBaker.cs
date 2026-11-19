using UnityEngine;

/// <summary>
/// Builds the lookup textures used by CityBuster/V10 and binds them globally.
/// Runs automatically at startup; no scene setup required.
/// </summary>
public static class VineShaderDataBaker
{
    private const int NoiseSize  = 256;
    private const int MaxSectors = 96;   // must match _TendrilCount max and VINE_MAX_SECTORS in the shader

    private static Texture2D noiseTex;
    private static Texture2D sectorTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bake()
    {
        // Safe if Domain Reload is disabled in Enter Play Mode settings.
        if (noiseTex  != null) Object.Destroy(noiseTex);
        if (sectorTex != null) Object.Destroy(sectorTex);

        noiseTex  = BuildNoiseTexture();
        sectorTex = BuildSectorTexture();

        Shader.SetGlobalTexture("_VineNoiseTex",  noiseTex);
        Shader.SetGlobalTexture("_VineSectorTex", sectorTex);
    }

    // 256x256 tiling lattice noise. Each texel is an independent random value,
    // so Repeat wrapping tiles seamlessly.
    private static Texture2D BuildNoiseTexture()
    {
        bool r8 = SystemInfo.SupportsTextureFormat(TextureFormat.R8);
        TextureFormat fmt = r8 ? TextureFormat.R8 : TextureFormat.RGBA32;

        var tex = new Texture2D(NoiseSize, NoiseSize, fmt, false, true)
        {
            name       = "VineNoise",
            wrapMode   = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags  = HideFlags.HideAndDontSave
        };

        var rng = new System.Random(1337);
        if (r8)
        {
            var data = new byte[NoiseSize * NoiseSize];
            rng.NextBytes(data);
            tex.SetPixelData(data, 0);
        }
        else
        {
            var data = new Color32[NoiseSize * NoiseSize];
            for (int i = 0; i < data.Length; i++)
            {
                byte b = (byte)rng.Next(0, 256);
                data[i] = new Color32(b, b, b, 255);
            }
            tex.SetPixelData(data, 0);
        }

        tex.Apply(false, true); // no mips, release CPU copy
        return tex;
    }

    // 96x2 point-sampled table of per-tendril random values.
    // Same hash as the old in-shader Hash11, so tendril personalities are unchanged.
    private static Texture2D BuildSectorTexture()
    {
        var tex = new Texture2D(MaxSectors, 2, TextureFormat.RGBA32, false, true)
        {
            name       = "VineSectors",
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            hideFlags  = HideFlags.HideAndDontSave
        };

        var px = new Color[MaxSectors * 2];
        for (int sid = 0; sid < MaxSectors; sid++)
        {
            float rLen   = Hash11(sid * 13.17f + 1.7f);
            float rSpeed = Hash11(sid * 7.31f  + 4.2f);
            float rPhase = Hash11(sid * 3.71f  + 9.3f);   // shader multiplies by 2*PI
            float rWidth = Hash11(sid * 5.13f  + 2.9f);
            float rCurve = Hash11(sid * 9.77f  + 6.1f);   // shader multiplies by 2*PI

            px[sid]              = new Color(rLen,   rSpeed, rWidth, 1f); // row 0
            px[MaxSectors + sid] = new Color(rPhase, rCurve, 0f,     1f); // row 1
        }

        tex.SetPixels(px);
        tex.Apply(false, true);
        return tex;
    }

    private static float Frac(float x) => x - Mathf.Floor(x);

    private static float Hash11(float p)
    {
        p = Frac(p * 0.1031f);
        p *= p + 33.33f;
        p *= p + p;
        return Frac(p);
    }
}