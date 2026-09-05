using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
    [SerializeField] private GameObject smokeVFX;

    [Header("Player's UI")]
    [SerializeField] private CanvasGroup keyImage;
    [SerializeField] private float keyFadeDuration = 0.35f;

    [Header("Player's Face Data")]
    [SerializeField] private FaceChangeMechanic faceData;

    [Header("Player Squash Config")]
    [SerializeField] private float dashSquashAmount;
    [SerializeField] private float dashSquashDuration;
    [SerializeField] private float jumpUpSquashAmount;
    [SerializeField] private float jumpUpSquashDuration;
    [SerializeField] private float landSquashAmount;
    [SerializeField] private float landSquashDuration;

    [Header("Player's Animation")]
    [SerializeField] private float blinkDelay;
    [SerializeField] private float blinkDuration = 0.15f;
    [SerializeField] private float idleDelay = 2f;

    [Header("Optional VFX References")]
    [SerializeField]  private Transform playerSquahTransform;
    private float blinkDelayTimer;
    private float blinkDurationTimer;
    private float idleTimer;
    private bool isBlinking;
    private bool isLandingSquashActive;
    private Playerface gameplayFace;
    private Playerface lastGameplayFace;

    public bool hasKey { get; private set; }

    // Internal References & Caching
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rootRigidbody;
    private PlayerController playerController;
    private PlayerKeyboardInput playerKeyboardInput;
    private Playerface currentPlayerFace;
    private ParticleSystem jumpUpTrailVFX;
    private GameObject particleObject;

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

        CreataAChild();
        if (particleObject.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
        {
            jumpUpTrailVFX = ps;

            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.None;
        }     
        else
        {
            Debug.LogWarning(" NO Particle System Found! ");
        }   

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
        GameEventBus.OnPlayerJumpSquash += HandlePlayerScale;
        GameEventBus.OnPlayerGroundImpact += SpawnVfx;
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
        GameEventBus.OnPlayerJumpSquash -= HandlePlayerScale;
        GameEventBus.OnPlayerGroundImpact -= SpawnVfx;
        isLandingSquashActive = false;
    }

    private void Update()
    {
        if (isDestroyed) return;

        // Halt visual updates if the game is paused or over
        if (currentGameState == GameState.Paused) return;

        HandleDirectionalFacing();
        UpdateBlink();
        // ApplyJuiceEffects();
        HandleUpdatedFace();
        // CheckFrozenViolation();
    }

    private void LateUpdate()
    {
        if (isDestroyed) return;
        if (currentGameState == GameState.Paused) return;

        UpdateIdleState();
        UpdateLocomotionFace();
    }

    /// <summary>
    /// Grounded locomotion face driver — replaces Router per-frame spam.
    /// Runs in LateUpdate after Controller.Update() refreshed isGrounded/horizontalInput.
    /// Gating preserves JumpUp/JumpDown/FallDown_Impact/Dash/Blink and defers to land squash.
    /// </summary>
    private void UpdateLocomotionFace()
    {
        if (playerController == null) return;
        // Airborne: keep JumpUp / JumpDown, don't show Moving/Idle
        if (!playerController.IsGroundedRaw) return;
        if (playerController.isDashing) return;
        if (isBlinking) return;
        if (isLandingSquashActive) return;

        // Preserve airborne jump faces for the single frame where jump is buffered
        // but physics hasn't left ground yet (LateUpdate runs after UpdateGroundDetection).
        if (currentPlayerFace == Playerface.JumpUp || currentPlayerFace == Playerface.JumpDown)
            return;
        if (currentPlayerFace == Playerface.Dash || currentPlayerFace == Playerface.Die)
            return;

        // Use Controller.IsIdle (raw grounded + no horizontal input) — single threshold, matches blink logic.
        // This is input-agnostic (Keyboard + Router left-drag both feed SetHorizontalInput).
        bool wantsMoving = !playerController.IsIdle;
        Playerface desired = wantsMoving ? Playerface.Moving : Playerface.Idle;

        if (desired == currentPlayerFace) return;

        // Transition-only: rely on UpdateFaceOnPlayer dedupe for bus spam, but early-out saves invoke.
        GameEventBus.TriggerPlayerFaceChange(desired);
    }

    /// <summary>
    /// Visual-only idle detection — polls PlayerController.IsIdle (raw grounded + no horizontal input).
    /// Runs in LateUpdate so Controller.Update() has already refreshed isGrounded/horizontalInput.
    /// After idleDelay, triggers repeated blinks every blinkDelay while idle persists.
    /// </summary>
    private void UpdateIdleState()
    {
        bool isIdle = playerController != null && playerController.IsIdle;

        if (!isIdle)
        {
            idleTimer = 0f;
            blinkDelayTimer = 0f;
            return;
        }

        idleTimer += Time.deltaTime;
        if (idleTimer < idleDelay) return;

        // Already blinking — let UpdateBlink() finish duration before retriggering
        if (isBlinking) return;

        blinkDelayTimer += Time.deltaTime;
        if (blinkDelayTimer >= blinkDelay)
        {
            StartBlink();
            blinkDelayTimer = 0f;
        }
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
        GameEventBus.TriggerPlaySFXCommand(SoundID.Explosion);
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

        HandleJumpVFX(face);

        // Debug.Log($"[PlayerVisuals] : Face changed from {tempface} to {currentPlayerFace}");
    }

#region Eye Blinking
    private void StartBlink()
    {
        if (isBlinking) return;

        // gameplayFace = lastGameplayFace;
        isBlinking = true;
        currentPlayerFace = Playerface.Blink;
        blinkDurationTimer = blinkDuration;
        blinkDelayTimer = 0f;
        
    }

    private void UpdateBlink()
    {
        if (!isBlinking) return;

        blinkDurationTimer -= Time.deltaTime;

        if (blinkDurationTimer <= 0f)
        {
            isBlinking = false;
            currentPlayerFace = Playerface.Idle;
        }
    }
#endregion
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

            case Playerface.Idle :
                spriteRenderer.sprite = faceData.idle;
                // HandlePlayerStopped();
            break;

            default:
                spriteRenderer.sprite = faceData.idle;
            break;
        }
    }

#endregion

#region VFX

    private void HandleJumpVFX(Playerface face)
    {

        switch (face)
        {
            case Playerface.JumpUp:

                if (jumpUpTrailVFX != null && !jumpUpTrailVFX.isPlaying)
                {
                    jumpUpTrailVFX.Play();
                    // Debug.Log($"[VFX] :  Dust Playing ");
                }
                
            break;

            case Playerface.FallDown_Impact:

                if (jumpUpTrailVFX != null && jumpUpTrailVFX.isPlaying)
                {
                    jumpUpTrailVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    // Debug.Log($"[VFX] :  Stopped Playing ");
                }

            break;
        }
    }

    private void SpawnVfx(Vector2 position)
    {
        GameObject vfx = Instantiate(smokeVFX);
        vfx.transform.position = position;
        vfx.GetComponent<ParticleSystem>().Play();
    }
#endregion

#region Dashing Effects

    private void HandleDashEffects()
    {
        SquashPlayer( dashSquashAmount, dashSquashDuration );
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

    private void HandlePlayerScale(bool onAir)
    {
        if (onAir)
            HandleJumpSquash(onAir, scaleAmount: jumpUpSquashAmount, scaleDuration: jumpUpSquashDuration, squashableObj: transform);
        else
            HandleJumpSquash(onAir, scaleAmount: landSquashAmount, scaleDuration: landSquashDuration, squashableObj: playerSquahTransform);    
    }

    // Handles the jump squash animation triggered via GameEventBus
    private void HandleJumpSquash(bool isDescending, float scaleAmount, float scaleDuration, Transform squashableObj)
    {
        // Track land squash so UpdateLocomotionFace() defers to this tween's stillMoving decision
        if (!isDescending)
            isLandingSquashActive = true;

        // Cancel any prior squash tweens on this transform
        squashableObj.DOKill();

        // First half: squash
        squashableObj.DOScaleY(scaleAmount, scaleDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Second half: restore scale
                squashableObj.DOScaleY(1f, scaleDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if( isDescending ) 
                        {
                            // Notify that the squash animation finished so the SFX can play || not used in the project
                            GameEventBus.TriggerPlayerJumpSquashComplete();
                        }
                        else
                        {
                            isLandingSquashActive = false;
                            bool stillMoving = rootRigidbody != null && Mathf.Abs(rootRigidbody.linearVelocity.x) > 0.05f;
                            
                            if( stillMoving )  
                            GameEventBus.TriggerPlayerFaceChange(Playerface.Moving);
                            else
                            GameEventBus.TriggerPlayerFaceChange(Playerface.Idle);
                        }
                    });
            });
    }

    private void CreataAChild()
    {
        particleObject = Instantiate(
            smokeVFX,
            transform
        );

        particleObject.transform.localPosition = Vector3.zero;
        particleObject.transform.localRotation = Quaternion.identity;
    }
}
