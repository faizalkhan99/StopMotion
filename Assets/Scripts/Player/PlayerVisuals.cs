using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Player's UI")]
    [SerializeField] private CanvasGroup keyImage;
    [SerializeField] private float keyFadeDuration = 0.35f;

    [Header("Player's Face Data")]
    [SerializeField] private FaceChangeMechanic faceData;

    [Header("Player Squash Config")]
    [SerializeField] private float squashAmount;
    [SerializeField] private float squashDuration;

    [Header("Player's Animation")]
    [SerializeField] private float blinkDelay;
    private float tempTimer;
    public bool isBlinking = false;

    public bool hasKey { get; private set; }

    // Internal References & Caching
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rootRigidbody;
    private PlayerController playerController;
    private PlayerKeyboardInput playerKeyboardInput;
    private Playerface currentPlayerFace;

    // Destroy State
    private bool isDestroyed;

    private Coroutine keyFadeRoutine;

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
    private void Start()
    {
       keyFadeRoutine = null; 
    }
    private void OnEnable()
    {
        // Subscribe directly to our centralized, zero-allocation switchboard
        GameEventBus.OnChronoStateChanged += HandleChronoStateChanged;
        GameEventBus.OnGracePeriodUpdated += HandleGraceViolation;
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
        GameEventBus.OnGameOverTriggered += HandleGameOverTriggered;
        GameEventBus.OnPlayerFaceChange += UpdateFaceOnPlayer;
        GameEventBus.OnPlayerDash += HandleDashEffects;
        GameEventBus.OnPlayerIdle += PlayAnimationWithDelay;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent dangling delegates and memory leaks
        GameEventBus.OnChronoStateChanged -= HandleChronoStateChanged;
        GameEventBus.OnGracePeriodUpdated -= HandleGraceViolation;
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
        GameEventBus.OnGameOverTriggered -= HandleGameOverTriggered;
        GameEventBus.OnPlayerFaceChange -= UpdateFaceOnPlayer;       
        GameEventBus.OnPlayerDash -= HandleDashEffects;
        GameEventBus.OnPlayerIdle -= PlayAnimationWithDelay;
    }

    private void Update()
    {
        if (isDestroyed) return;

        // Halt visual updates if the game is paused or over
        if (currentGameState == GameState.Paused) return;

        HandleDirectionalFacing();
        ApplyJuiceEffects();
        HandleUpdatedFace();
        // CheckFrozenViolation();
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
            // UpdateFaceOnPlayer(Playerface.Moving);
        }
        // else
        // {
        //     UpdateFaceOnPlayer(Playerface.Idle);
        // }
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

        if(currentGameState == GameState.LevelComplete)
        {
            StopPlayerMovement();
        }
    }

    private void HandleGameOverTriggered(GameOverReason reason)
    {
        GameEventBus.TriggerPlayerFaceChange(Playerface.Die);

        if (reason == GameOverReason.TimeExpired)
        {
            TriggerDestroy();
        }
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

        StopPlayerMovement();
    }

    private void StopPlayerMovement()
    {
        if (playerKeyboardInput != null) playerKeyboardInput.enabled = false;
        if (playerController != null) playerController.StopMovement();
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
            GameEventBus.TriggerGameOver(GameOverReason.MotionBomb);
        }
    }

    /// <summary>
    /// Shows canvas in Player's UI
    /// </summary>
    public void ShowKeyInUI()
    {
        if (keyFadeRoutine != null) StopCoroutine(keyFadeRoutine);
        keyFadeRoutine = StartCoroutine(FadeKeyImage(1f));
        
        hasKey = true;
    }

    /// <summary>
    /// Hides Key Canvas in Player's UI only once
    /// </summary>
    public void HideKeyInUI()
    {
        if (keyFadeRoutine == null)
        {
           keyFadeRoutine = StartCoroutine(FadeKeyImage(0f)); 
        }
    }
    /// <summary>
    /// Hides Key Canvas in Player's UI
    /// </summary>
    public bool CheckForKey()
    {
        HideKeyInUI();
        return hasKey;
    }

    private System.Collections.IEnumerator FadeKeyImage(float targetAlpha)
    {
        if (keyImage == null) yield break;

        float startAlpha = keyImage.alpha;
        float elapsed = 0f;

        while (elapsed < keyFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            keyImage.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / keyFadeDuration);
            yield return null;
        }

        keyImage.alpha = targetAlpha;
        keyFadeRoutine = null;
    }

#region Face Changing

    private void UpdateFaceOnPlayer(Playerface face)
    {
        if( currentPlayerFace == face) return;

        var tempface = currentPlayerFace;
        currentPlayerFace = face;

        Debug.Log($"[PlayerVisuals] : Face changed from {tempface} to {currentPlayerFace}");
    }

    private void PlayAnimationWithDelay()
    {
        Debug.Log($" Idle Trigger ");
        tempTimer += 1f;

        if (tempTimer >= blinkDelay)
        {
            tempTimer = 0f;
            // PlayIdleAnimation
            Debug.Log($" [PlayerVisuals] : playing Blink Anim ");
            BlinkAnimation();
        }
    }
    private void BlinkAnimation()
    {
        if( !isBlinking )
        {
            Debug.Log($" [PlayerVisuals] : starting co rountine ");
            StartCoroutine(BlinkRoutine());
        }
    }
    private IEnumerator BlinkRoutine()
    {
        isBlinking = true;

        // spriteRenderer.sprite = faceData.blink;
        UpdateFaceOnPlayer(Playerface.Blink);

        yield return new WaitForSeconds( 2f );

        // spriteRenderer.sprite = faceData.idle;
        UpdateFaceOnPlayer(Playerface.Idle);

        isBlinking = false;
    }
    private void HandleUpdatedFace()
    {
        switch (currentPlayerFace)
        {
            case Playerface.Moving :
                spriteRenderer.sprite = faceData.moving;
            break;

            case Playerface.Dash :
                spriteRenderer.sprite = faceData.dash;
            break;

            case Playerface.JumpUp :
                spriteRenderer.sprite = faceData.jumpUp;
            break;

            case Playerface.JumpDown :
                spriteRenderer.sprite = faceData.jumpDown;
            break;

            case Playerface.FallDown_Impact :
                spriteRenderer.sprite = faceData.fallDownImpact;
            break;

            case Playerface.Blink :
                spriteRenderer.sprite = faceData.blink;
            break;

            case Playerface.Die :
                spriteRenderer.sprite = faceData.die;
            break;

            default:
            Debug.Log($" default switch state (changing to idle face) ");
                spriteRenderer.sprite = faceData.idle;
            break;
        }
    }

#endregion

#region Dashing Effects

    private void HandleDashEffects()
    {
        SquashPlayer( squashAmount, squashDuration );
        UpdateFaceOnPlayer(Playerface.Dash);
    }
    private void SquashPlayer(float amount, float duration)
    {
        transform.DOScaleY(amount, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                transform.DOScaleY(1f, duration)
            );
    }
#endregion
}
