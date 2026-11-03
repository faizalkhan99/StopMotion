using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds the shared, blurred silhouette mask used by SpriteEdgeGlowUnion.shader.
/// Put ONE of these in the scene. A hidden helper camera renders all shapes on
/// "glowLayers" into a small render texture (union of their alpha), which is then
/// blurred and exposed to the shader as a global texture.
///
/// Requirements / assumptions:
///  - Orthographic, unrotated main camera (standard 2D platformer setup).
///  - Every object on glowLayers uses SpriteEdgeGlowUnion (other shaders would
///    pollute the mask). Main camera must also render glowLayers.
///  - Shapes that touch must really touch (no gaps) and their sprites should fill
///    the texture up to the border (Clamp wrap) so the union has no seam.
/// </summary>
[DisallowMultipleComponent]
public class EdgeGlowMask : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [Tooltip("Layer(s) containing the glow shapes. Use a dedicated layer.")]
    [SerializeField] private LayerMask glowLayers;
    [Tooltip("Mask resolution divisor. 2 = half resolution (recommended for mobile).")]
    [SerializeField, Range(1, 4)] private int downscale = 2;
    [Tooltip("Largest Edge Width (world units) any material may use. Keep it as small as you need: smaller = sharper.")]
    [SerializeField] private float maxEdgeWidth = 0.3f;
    [Tooltip("Mask camera view size relative to the main camera (avoids border effects).")]
    [SerializeField, Range(1.05f, 2f)] private float viewPadding = 1.25f;
    [Tooltip("Optional: index of a lightweight URP renderer (no lights/post) for the mask camera. -1 = default.")]
    [SerializeField] private int rendererIndex = -1;
    [SerializeField] private Shader blurShader;

    private static readonly int MaskModeId = Shader.PropertyToID("_EdgeGlowMaskMode");
    private static readonly int RectId     = Shader.PropertyToID("_EdgeGlowMaskRect");
    private static readonly int MaxWidthId = Shader.PropertyToID("_EdgeGlowMaxWidth");
    private static readonly int BlurTexId  = Shader.PropertyToID("_EdgeGlowBlurTex");
    private static readonly int DirId      = Shader.PropertyToID("_BlurDir");
    private static readonly int ScaleId    = Shader.PropertyToID("_BlurScale");

    private const float GaussianSigmaTexels = 1.69f;   // sigma of the 9-tap kernel at scale 1

    private Camera _maskCam;
    private Material _blurMat;
    private RenderTexture _rtMask, _rtTmp, _rtBlur;

    private void Reset()
    {
        mainCamera = Camera.main;
        blurShader = Shader.Find("Hidden/EdgeGlowBlur");
    }

    private void OnEnable()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (blurShader == null) blurShader = Shader.Find("Hidden/EdgeGlowBlur");
        if (mainCamera == null || blurShader == null)
        {
            Debug.LogError("EdgeGlowMask: main camera or blur shader missing.", this);
            enabled = false;
            return;
        }

        _blurMat = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };

        var go = new GameObject("EdgeGlowMaskCamera") { hideFlags = HideFlags.HideAndDontSave };
        _maskCam = go.AddComponent<Camera>();
        _maskCam.orthographic = true;
        _maskCam.clearFlags = CameraClearFlags.SolidColor;
        _maskCam.backgroundColor = Color.clear;
        _maskCam.cullingMask = glowLayers;
        _maskCam.allowHDR = false;
        _maskCam.allowMSAA = false;
        _maskCam.useOcclusionCulling = false;
        _maskCam.depth = mainCamera.depth - 1;          // render before the main camera

        var data = _maskCam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.antialiasing = AntialiasingMode.None;
        if (rendererIndex >= 0) data.SetRenderer(rendererIndex);

        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
        Shader.SetGlobalFloat(MaskModeId, 0f);

        if (_maskCam != null) Destroy(_maskCam.gameObject);
        if (_blurMat != null) Destroy(_blurMat);
        ReleaseTargets();
    }

    private void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == _maskCam)
        {
            SyncMaskCamera();
            Shader.SetGlobalFloat(MaskModeId, 1f);
        }
        else
        {
            Shader.SetGlobalFloat(MaskModeId, 0f);
        }
    }

    private void OnEndCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != _maskCam || _rtMask == null) return;

        _blurMat.SetVector(DirId, new Vector4(1f, 0f, 0f, 0f));
        Graphics.Blit(_rtMask, _rtTmp, _blurMat);
        _blurMat.SetVector(DirId, new Vector4(0f, 1f, 0f, 0f));
        Graphics.Blit(_rtTmp, _rtBlur, _blurMat);
    }

    private void SyncMaskCamera()
    {
        Camera m = mainCamera;
        int rtH = Mathf.Max(8, Mathf.CeilToInt(m.pixelHeight * viewPadding / downscale));
        int rtW = Mathf.Max(8, Mathf.CeilToInt(m.pixelWidth * viewPadding / downscale));
        EnsureTargets(rtW, rtH);

        Transform t = _maskCam.transform;
        t.SetPositionAndRotation(m.transform.position, m.transform.rotation);
        _maskCam.nearClipPlane = m.nearClipPlane;
        _maskCam.farClipPlane = m.farClipPlane;
        _maskCam.cullingMask = glowLayers;
        _maskCam.orthographicSize = m.orthographicSize * viewPadding;
        _maskCam.aspect = (float)rtW / rtH;

        float halfH = _maskCam.orthographicSize;
        float halfW = halfH * _maskCam.aspect;
        Vector3 p = m.transform.position;
        Shader.SetGlobalVector(RectId, new Vector4(p.x - halfW, p.y - halfH, 1f / (2f * halfW), 1f / (2f * halfH)));

        // Blur sigma in world units follows maxEdgeWidth; clamp to what 9 taps can do cleanly.
        float worldPerTexel = (2f * halfH) / rtH;
        float sigmaWorld = maxEdgeWidth * 0.5f;
        float scale = Mathf.Clamp(sigmaWorld / (GaussianSigmaTexels * worldPerTexel), 0.3f, 2f);
        _blurMat.SetFloat(ScaleId, scale);

        // Tell the shader the width the blur actually produces (after clamping)
        Shader.SetGlobalFloat(MaxWidthId, 2f * GaussianSigmaTexels * scale * worldPerTexel);
    }

    private void EnsureTargets(int w, int h)
    {
        if (_rtMask != null && _rtMask.width == w && _rtMask.height == h) return;
        ReleaseTargets();

        RenderTextureFormat single = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
            ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

        _rtMask = Create(w, h, RenderTextureFormat.ARGB32);   // camera target
        _rtTmp  = Create(w, h, single);
        _rtBlur = Create(w, h, single);

        _maskCam.targetTexture = _rtMask;
        Shader.SetGlobalTexture(BlurTexId, _rtBlur);
    }

    private static RenderTexture Create(int w, int h, RenderTextureFormat fmt)
    {
        var rt = new RenderTexture(w, h, 0, fmt)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            hideFlags = HideFlags.HideAndDontSave
        };
        rt.Create();
        return rt;
    }

    private void ReleaseTargets()
    {
        if (_maskCam != null) _maskCam.targetTexture = null;
        foreach (var rt in new[] { _rtMask, _rtTmp, _rtBlur })
        {
            if (rt == null) continue;
            rt.Release();
            Destroy(rt);
        }
        _rtMask = _rtTmp = _rtBlur = null;
    }
}
