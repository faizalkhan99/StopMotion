using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonElasticJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Settings")]
    [Tooltip("A subtle scale bump on hover (e.g., 1.05 = 5% bigger).")]
    [SerializeField] private float hoverScaleMultiplier = 1.05f;
    [SerializeField] private float scaleTransitionSpeed = 18f;

    [Header("Click Wobble (Elastic Spring) Settings")]
    [Tooltip("How much the button compresses/shrinks on initial impact.")]
    [SerializeField] private float clickScaleMultiplier = 0.94f;
    [Tooltip("Maximum tilt angle in degrees when clicked.")]

    [Header("Jelly Wobble Settings")]
    [SerializeField] private float wobbleIntensity = 7f;
    [Tooltip("How fast the button oscillates back and forth.")]
    [SerializeField] private float wobbleSpeed = 35f;
    [Tooltip("How quickly the spring loses energy and settles to rest.")]
    [SerializeField] private float wobbleDecay = 8f;


    [SerializeField] private float maxScaleDeformation = 0.25f; // How "squishy" it is (0.25 = 25% stretch/squash)
    [SerializeField] private float maxRotationAngle = 6f;       // Angular tilt during the wobble

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Coroutine scaleCoroutine;
    private Coroutine wobbleCoroutine;
    private bool isHovered = false;
    private bool isPressed = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    private void OnDisable()
    {
        ResetToOriginalState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateScaleRoutine();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        UpdateScaleRoutine();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        UpdateScaleRoutine();

        // Trigger the elastic wobble burst on impact!
        if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
        wobbleCoroutine = StartCoroutine(DoClickWobble());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateScaleRoutine();
    }

    private void UpdateScaleRoutine()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale());
    }

    private IEnumerator AnimateScale()
    {
        while (true)
        {
            Vector3 targetScale = originalScale;
            if (isPressed)
            {
                targetScale = originalScale * clickScaleMultiplier;
            }
            else if (isHovered)
            {
                targetScale = originalScale * hoverScaleMultiplier;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleTransitionSpeed);

            // Stop loop when close enough to rest to save CPU cycles
            if (!isHovered && !isPressed && Vector3.Distance(transform.localScale, originalScale) < 0.001f)
            {
                transform.localScale = originalScale;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator DoClickWobble()
    {
        GameEvents.TriggerPlaySFXCommand(SoundID.ButtonClick);
        float timeElapsed = 0f;
        // We track the damping envelope from 1 (max intensity) down to near-zero
        float damping = 1f;

        while (damping > 0.01f)
        {
            timeElapsed += Time.unscaledDeltaTime;

            // 1. FRAME-RATE INDEPENDENT EXPONENTIAL DECAY
            // This creates a snappy initial spring that smoothly settles into rest
            damping = Mathf.Exp(-wobbleDecay * timeElapsed);

            // 2. THE JELLY SQUASH & STRETCH (Cosine Wave)
            // Cosine starts at peak displacement (1 or -1), perfect for springing back after a click!
            float scaleWave = Mathf.Cos(timeElapsed * wobbleSpeed) * maxScaleDeformation * damping;

            // THE SECRET: INVERTED AXES
            // When X expands (+scaleWave), Y contracts (-scaleWave) to preserve volume.
            float currentScaleX = originalScale.x * (1f + scaleWave);
            float currentScaleY = originalScale.y * (1f - scaleWave);

            transform.localScale = new Vector3(currentScaleX, currentScaleY, originalScale.z);

            // 3. PHASE-SHIFTED ROTATION (Sine Wave)
            // Sine is 90 degrees out of phase with Cosine. As the stretch hits its neutral point,
            // the rotation hits its maximum tilt, creating a fluid, rolling jelly motion.
            float zAngle = Mathf.Sin(timeElapsed * wobbleSpeed) * maxRotationAngle * damping;
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, zAngle);

            yield return null;
        }

        // 4. CLEAN SNAP TO REST
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        wobbleCoroutine = null;
    }

    private void ResetToOriginalState()
    {
        StopAllCoroutines();
        scaleCoroutine = null;
        wobbleCoroutine = null;
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
    }
}