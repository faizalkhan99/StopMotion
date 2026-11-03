
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PlayerTendrilController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;

    [Header("Growth Cycle")]
    [Min(0.1f)]
    [SerializeField] private float growthInterval = 5f;

    [Min(0.1f)]
    [SerializeField] private float growthDuration = 3f;

    [Header("Flash")]
    [Min(0.01f)]
    [SerializeField] private float flashDuration = 0.12f;

    [Min(0f)]
    [SerializeField] private float flashGap = 0.08f;

    [Min(1)]
    [SerializeField] private int flashCount = 3;

    [Header("Shader")]
    [SerializeField] private Material sourceMaterial;

    private Material runtimeMaterial;
    private Image image;

    private float cycleTimer;
    private bool cycleStarted;

    private static readonly int PlayerPosID =
        Shader.PropertyToID("_PlayerPos");

    private static readonly int GrowthProgressID =
        Shader.PropertyToID("_ControllerGrowth");

    private static readonly int FlashAmountID =
        Shader.PropertyToID("_ControllerFlash");

    private static readonly int StartCycleID =
        Shader.PropertyToID("_ControllerStartCycle");

    private void Awake()
    {
        image = GetComponent<Image>();

        if (sourceMaterial == null)
            sourceMaterial = image.material;

        runtimeMaterial = new Material(sourceMaterial);
        image.material = runtimeMaterial;

        if (targetCamera == null)
            targetCamera = Camera.main;

        runtimeMaterial.SetFloat(GrowthProgressID, 0f);
        runtimeMaterial.SetFloat(FlashAmountID, 0f);
    }

    private void Update()
    {
        cycleTimer += Time.deltaTime;

        if (cycleTimer >= growthInterval)
        {
            cycleTimer -= growthInterval;

            BeginGrowthCycle();
        }

        if (cycleStarted)
        {
            UpdateGrowthCycle();
        }
    }

    private void BeginGrowthCycle()
    {
        CapturePlayerPosition();

        cycleStarted = true;

        runtimeMaterial.SetFloat(
            StartCycleID,
            Time.time
        );

        runtimeMaterial.SetFloat(
            GrowthProgressID,
            0f
        );
    }

    private void CapturePlayerPosition()
    {
        if (player == null || targetCamera == null)
            return;

        // Use screen coordinates because the shader
        // calculates its position using _ScreenParams.

        Vector3 screenPosition =
            targetCamera.WorldToScreenPoint(player.position);

        Vector2 playerUV = new Vector2(
            screenPosition.x / Screen.width,
            screenPosition.y / Screen.height
        );

        runtimeMaterial.SetVector(
            PlayerPosID,
            new Vector4(
                playerUV.x,
                playerUV.y,
                0f,
                0f
            )
        );
    }

    private void UpdateGrowthCycle()
    {
        float flashSequenceDuration =
            flashCount * flashDuration +
            (flashCount - 1) * flashGap;

        float growthStart =
            flashSequenceDuration;

        float growthElapsed =
            cycleTimer - growthStart;

        // Flash sequence: three rapid flashes.
        float flashAmount = 0f;

        for (int i = 0; i < flashCount; i++)
        {
            float flashStart =
                i * (flashDuration + flashGap);

            float flashEnd =
                flashStart + flashDuration;

            if (cycleTimer >= flashStart &&
                cycleTimer <= flashEnd)
            {
                float phase =
                    (cycleTimer - flashStart) / flashDuration;

                flashAmount =
                    Mathf.Sin(phase * Mathf.PI);
            }
        }

        runtimeMaterial.SetFloat(
            FlashAmountID,
            flashAmount
        );

        // Smooth growth after all three flashes.
        float growthProgress =
            Mathf.Clamp01(growthElapsed / growthDuration);

        growthProgress =
            Mathf.SmoothStep(0f, 1f, growthProgress);

        runtimeMaterial.SetFloat(
            GrowthProgressID,
            growthProgress
        );
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
