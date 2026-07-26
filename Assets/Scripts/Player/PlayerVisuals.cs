using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisuals : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("How fast the character tilts or smooth-flips when changing direction.")]
    [SerializeField] private float turnSmoothness = 15f;
    [Tooltip("The color the player flashes when violating a timer rule.")]
    [SerializeField] private Color warningColor = Color.red;
    [Tooltip("Intensity of the violent shake during the grace period.")]
    [SerializeField] private float shakeIntensity = 0.15f;
    [Tooltip("Speed at which the sprite recovers its original scale and color.")]
    [SerializeField] private float recoverySpeed = 8f;

    [Header("Optional VFX References")]
    [SerializeField] private ParticleSystem confidentTrailVFX;
    [SerializeField] private ParticleSystem destroyVFX;

    // Internal References & Caching
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rootRigidbody;
    private PlayerController playerController;
    private PlayerKeyboardInput playerKeyboardInput;

    // Destroy State
    private bool isDestroyed;

    // Zero-GC State Tracking
    private Vector3 initialLocalPosition;
    private Vector3 initialLocalScale;
    private Color originalColor;
    private float currentViolationSeverity;
    private ChronoState currentChronoState;
    private GameState currentGameState = GameState.Booting;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialLocalPosition = transform.localPosition;
        initialLocalScale = transform.localScale;
        originalColor = spriteRenderer.color;

        // We only need the parent Rigidbody to read horizontal velocity for flipping.
        // No ChronoController reference is required anymore!
        rootRigidbody = GetComponentInParent<Rigidbody2D>();
        playerController = GetComponentInParent<PlayerController>();
        playerKeyboardInput = GetComponentInParent<PlayerKeyboardInput>();

        if (rootRigidbody == null)
        {
            Debug.LogError("<b>[PlayerVisuals]</b> Could not find a Rigidbody2D on parent GameObject!");
        }
    }

    private void OnEnable()
    {
        // Subscribe directly to our centralized, zero-allocation switchboard
        GameEventBus.OnChronoStateChanged += HandleChronoStateChanged;
        GameEventBus.OnGracePeriodUpdated += HandleGraceViolation;
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent dangling delegates and memory leaks
        GameEventBus.OnChronoStateChanged -= HandleChronoStateChanged;
        GameEventBus.OnGracePeriodUpdated -= HandleGraceViolation;
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Update()
    {
        if (isDestroyed) return;

        // Halt visual updates if the game is paused or over
        if (currentGameState == GameState.Paused) return;

        HandleDirectionalFacing();
        ApplyJuiceEffects();
        CheckFrozenViolation();
    }

    /// <summary>
    /// Flips the visual sprite based on horizontal velocity without touching parent physics.
    /// </summary>
    private void HandleDirectionalFacing()
    {
        if (rootRigidbody == null) return;

        float velocityX = rootRigidbody.linearVelocity.x;

        // Tolerance check prevents rapid jittering when idle or micro-drifting
        if (Mathf.Abs(velocityX) > 0.05f)
        {
            spriteRenderer.flipX = velocityX < 0f; //This flips the sprite based on direction.
        }
    }

    /// <summary>
    /// Event receiver: Controls particle trails and visual state transitions.
    /// </summary>
    private void HandleChronoStateChanged(ChronoState newState)
    {
        currentChronoState = newState;

        if (confidentTrailVFX != null)
        {
            switch (newState)
            {
                case ChronoState.Ticking:
                    confidentTrailVFX.Play();
                    break;
                case ChronoState.Frozen:
                    confidentTrailVFX.Pause(); // Freezes particles in mid-air for visual stasis
                    break;
                default:
                    confidentTrailVFX.Stop();
                    break;
            }
        }
    }

    /// <summary>
    /// Event receiver: Updates visual severity during rule violations.
    /// </summary>
    private void HandleGraceViolation(float severity)
    {
        currentViolationSeverity = severity;
    }

    /// <summary>
    /// Event receiver: Tracks macro game flow to freeze animations when paused.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        currentGameState = newState;
    }

    /// <summary>
    /// Applies procedural juice (shaking, flashing, squash/stretch) natively in Update.
    /// </summary>
    private void ApplyJuiceEffects()
    {
        if (currentViolationSeverity > 0f)
        {
            // 1. Violent Shake: Offset local position using random coordinates inside a circle
            Vector2 randomOffset = Random.insideUnitCircle * (shakeIntensity * currentViolationSeverity);
            transform.localPosition = initialLocalPosition + (Vector3)randomOffset;

            // 2. Panic Stretch: Slightly flatten the cube as it gets closer to exploding
            float stretchFactor = Mathf.Lerp(1f, 1.25f, currentViolationSeverity);
            float squashFactor = Mathf.Lerp(1f, 0.8f, currentViolationSeverity);
            transform.localScale = new Vector3(initialLocalScale.x * stretchFactor, initialLocalScale.y * squashFactor, 1f);

            // 3. Color Shift: Flash warning color
            spriteRenderer.color = Color.Lerp(originalColor, warningColor, currentViolationSeverity);
        }
        else
        {
            // Smoothly recover position, scale, and color when the player corrects their mistake
            if (transform.localPosition != initialLocalPosition)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, initialLocalPosition, Time.deltaTime * recoverySpeed);
            }

            if (transform.localScale != initialLocalScale)
            {
                transform.localScale = Vector3.MoveTowards(transform.localScale, initialLocalScale, Time.deltaTime * recoverySpeed);
            }

            if (spriteRenderer.color != originalColor)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, originalColor, Time.deltaTime * recoverySpeed);
            }
        }
    }

    /// <summary>
    /// Triggers the destroy VFX, hides the player visuals, and disables input/control.
    /// </summary>
    public void TriggerDestroy()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        GameEventBus.TriggerCameraShake();
        if (destroyVFX != null) destroyVFX.Play();
        spriteRenderer.enabled = false;

        if (playerKeyboardInput != null) playerKeyboardInput.enabled = false;
        if (playerController != null) playerController.StopMovement();

        // TODO: After destroy VFX finishes, call GameEventBus.TriggerGameOver(GameOverReason.MotionBomb)
    }

    /// <summary>
    /// Checks if the player moves while in the Frozen state and triggers destruction.
    /// </summary>
    private void CheckFrozenViolation()
    {
        if (currentChronoState != ChronoState.Frozen) return;
        if (rootRigidbody == null) return;

        float velocityX = rootRigidbody.linearVelocity.x;
        float velocityY = rootRigidbody.linearVelocity.y;
        if (Mathf.Abs(velocityX) > 0.05f || Mathf.Abs(velocityY) > 0.05f)
        {
            TriggerDestroy();
        }
    }
}