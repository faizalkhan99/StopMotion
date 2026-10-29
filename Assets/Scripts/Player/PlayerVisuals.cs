using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisuals : MonoBehaviour
{
    [Header("Visual Profile (ScriptableObject)")]
    [SerializeField] private PlayerVisualProfileSO visualProfile;

    [Header("VFX References")]
    [SerializeField] private ParticleSystem destroyVFX;
    [SerializeField] private GameObject smokeVFX;

    [Header("Player's UI")]
    [SerializeField] private CanvasGroup keyImage;

    [Header("Player's Face Data")]
    [SerializeField] private FaceChangeMechanic faceData;
    [SerializeField] private Playerface currentPlayerFace;

    [Header("Directional Facing")]
    [Tooltip("Rotated 180 degrees on Y to face left. Child of the player root; keeps physics colliders and ground checks unflipped.")]
    [SerializeField] private Transform facingRoot;

    [Header("Player Transform")]
    [SerializeField] private Transform playerSquahTransform;

    private float blinkDelayTimer;
    private float blinkDurationTimer;
    private float idleTimer;
    private bool isBlinking;
    private bool isLandingSquashActive;
    private Playerface gameplayFace;
    private Playerface lastGameplayFace;

    // Key UI is view-only. The gate (LevelGoalTrigger) owns collected/total counts.
    public int keysCollected { get; private set; }

    // Internal References & Caching
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rootRigidbody;
    private PlayerController playerController;
    private PlayerInputRouter playerInputRouter;

    private Coroutine keyFadeRoutine;

    // Pooled jump/land puffs — plain-class pool from Modules/SmokePool.cs;
    // its LateUpdate Tick() releases finished puffs back into the pool.
    private SmokePool smokePool;

    // Destroy State
    private bool isDestroyed;

    // Zero-GC State Tracking
    private GameState currentGameState = GameState.Booting;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        keysCollected = 0;
        spriteRenderer.flipX = false;

        if (visualProfile == null)
        {
            Debug.LogError("<b>[PlayerVisuals]</b> Visual profile not assigned — squash/blink/puff settings fall back to code defaults!");
        }

        // We only need the parent Rigidbody to read horizontal velocity for flipping.
        // No ChronoController reference is required anymore!
        rootRigidbody = GetComponentInParent<Rigidbody2D>();
        playerController = GetComponentInParent<PlayerController>();
        playerInputRouter = GetComponentInParent<PlayerInputRouter>();

        if (rootRigidbody == null)
        {
            Debug.LogError("<b>[PlayerVisuals]</b> Could not find a Rigidbody2D on parent GameObject!");
        }

        if (facingRoot == null)
        {
            Debug.LogError("<b>[PlayerVisuals]</b> FacingRoot not assigned — directional facing disabled!");
        }

        smokePool = new SmokePool(smokeVFX, visualProfile != null ? visualProfile.SmokePoolSize : 6);
        smokePool.TryCreate();
    }

    private void OnEnable()
    {
        // Subscribe directly to our centralized, zero-allocation switchboard
        // GameEventBus.OnGracePeriodUpdated += HandleGraceViolation;
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
        GameEventBus.OnGameOverTriggered += HandleGameOverTriggered;
        GameEventBus.OnPlayerFaceChange += UpdateFaceOnPlayer;
        GameEventBus.OnPlayerDash += HandleDashEffects;
        GameEventBus.OnPlayerJumpSquash += HandlePlayerScale;
        GameEventBus.OnPlayerGroundImpact += HandleGroundImpact;
        GameEventBus.OnPlayerRespawned += HandlePlayerRespawned;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent dangling delegates and memory leaks
        // GameEventBus.OnGracePeriodUpdated -= HandleGraceViolation;
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
        GameEventBus.OnGameOverTriggered -= HandleGameOverTriggered;
        GameEventBus.OnPlayerFaceChange -= UpdateFaceOnPlayer;
        GameEventBus.OnPlayerDash -= HandleDashEffects;
        GameEventBus.OnPlayerJumpSquash -= HandlePlayerScale;
        GameEventBus.OnPlayerGroundImpact -= HandleGroundImpact;
        GameEventBus.OnPlayerRespawned -= HandlePlayerRespawned;
        isLandingSquashActive = false;
        if (smokePool != null) smokePool.ReleaseAll();
    }

    private void Update()
    {
        if (isDestroyed) return;

        // Halt visual updates if the game is paused or over
        if (currentGameState == GameState.Paused) return;

        HandleDirectionalFacing();
        UpdateBlink();
        HandleUpdatedFace();
    }

    private void LateUpdate()
    {
        smokePool?.Tick();

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
    /// Mirrors facing by rotating the FacingRoot child 180 degrees on Y based on
    /// horizontal velocity. Replaces SpriteRenderer.flipX so Cinemachine tracking
    /// targets parented under FacingRoot swing sides with facing. Physics root untouched.
    /// </summary>
    private void HandleDirectionalFacing()
    {
        if (rootRigidbody == null || facingRoot == null) return;

        float velocityX = rootRigidbody.linearVelocity.x;

        // Tolerance check prevents rapid jittering when idle or micro-drifting
        if (Mathf.Abs(velocityX) > 0.05f)
        {
            Quaternion targetRotation = velocityX < 0f
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;

            if (facingRoot.localRotation != targetRotation)
            {
                facingRoot.localRotation = targetRotation;
            }
        }
    }

    /// <summary>
    /// Event receiver: Tracks macro game flow to freeze animations when paused.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        currentGameState = newState;

        if (currentGameState == GameState.LevelComplete)
        {
            StopPlayerMovement();
        }
    }

    private void HandleGameOverTriggered(GameOverReason reason)
    {
        GameEventBus.TriggerPlayerFaceChange(Playerface.Die);
        StopPlayerMovement(); //added
        if (reason == GameOverReason.TimeExpired || reason == GameOverReason.Hazard)
        {
            TriggerDestroy();
        }
    }

    /// <summary>
    /// Respawn reset: clears the destroyed lock so the player animates again.
    /// Only fires on non-final deaths (lives remain). Final Hazard depletion
    /// goes through HandleGameOverTriggered above and stays dead.
    /// </summary>
    private void HandlePlayerRespawned()
    {
        isDestroyed = false;
        isBlinking = false;
        isLandingSquashActive = false;
        currentPlayerFace = Playerface.Idle;
        HandleUpdatedFace();
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (playerInputRouter != null) playerInputRouter.enabled = true;
        if (facingRoot != null) facingRoot.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Triggers the destroy VFX, hides the player visuals, and disables input/control.
    /// </summary>
    public void TriggerDestroy()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // Stop any blink in progress so it can't overwrite the die face
        isBlinking = false;

        // Force the die face and apply the sprite right now,
        // because Update() stops running once isDestroyed is true
        GameEventBus.TriggerPlayerFaceChange(Playerface.Die);
        currentPlayerFace = Playerface.Die;
        HandleUpdatedFace();

        GameEventBus.TriggerCameraShake();
        destroyVFX?.Play();
        GameEventBus.TriggerPlaySFXCommand(SoundID.Explosion);

        StopPlayerMovement();
    }

    private void StopPlayerMovement()
    {
        if (playerInputRouter != null) playerInputRouter.enabled = false;
        if (playerController != null) playerController.StopMovement();
    }

#region Key Collected
    /// <summary>
    /// Shows canvas in Player's UI
    /// </summary>
    public void ShowKeyInUI()
    {
        if (keyFadeRoutine != null) StopCoroutine(keyFadeRoutine);
        keyFadeRoutine = StartCoroutine(FadeKeyImage(1f));
    }

    public void AddKey()
    {
        keysCollected++;
        ShowKeyInUI();
    }

    public void ResetKeys()
    {
        keysCollected = 0;
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

    private bool HasVisualProfile => visualProfile != null;

    private System.Collections.IEnumerator FadeKeyImage(float targetAlpha)
    {
        if (keyImage == null) yield break;

        float startAlpha = keyImage.alpha;
        float elapsed = 0f;
        float fadeDuration = HasVisualProfile ? visualProfile.KeyFadeDuration : 0.35f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            keyImage.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        keyImage.alpha = targetAlpha;
        keyFadeRoutine = null;
    }
#endregion
    #region Face Changing

    private void UpdateFaceOnPlayer(Playerface face)
    {
        // Once dead, the face is locked to Die
        if (currentPlayerFace == Playerface.Die && face != Playerface.Die) return;
        if (currentPlayerFace == face) return;

        currentPlayerFace = face;

        HandleJumpVFX(face);

        if (face == Playerface.Idle)
            UpdateFaceAfterAction();
    }
    private void UpdateFaceAfterAction()
    {
        bool stillMoving = rootRigidbody != null && Mathf.Abs(rootRigidbody.linearVelocity.x) > 0.05f;

        if (stillMoving)
            GameEventBus.TriggerPlayerFaceChange(Playerface.Moving);
        else
            GameEventBus.TriggerPlayerFaceChange(Playerface.Idle);
    }

    #region Eye Blinking
    /// <summary>
    /// Visual-only idle detection — polls PlayerController.IsIdle (raw grounded + no horizontal input).
    /// Runs in LateUpdate so Controller.Update() has already refreshed isGrounded/horizontalInput.
    /// After idleDelay, triggers repeated blinks every blinkDelay while idle persists.
    /// </summary>
    private void UpdateIdleState()
    {
        bool isIdle = playerController != null && playerController.IsIdle;
        float idleDelay = HasVisualProfile ? visualProfile.IdleDelay : 2f;
        float blinkDelay = HasVisualProfile ? visualProfile.BlinkDelay : 2f;

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
    private void StartBlink()
    {
        if (isBlinking || currentPlayerFace == Playerface.Die) return;

        isBlinking = true;
        currentPlayerFace = Playerface.Blink;
        blinkDurationTimer = HasVisualProfile ? visualProfile.BlinkDuration : 0.15f;
        blinkDelayTimer = 0f;
    }

    private void UpdateBlink()
    {
        if (!isBlinking) return;

        blinkDurationTimer -= Time.deltaTime;

        if (blinkDurationTimer <= 0f)
        {
            isBlinking = false;
            if (currentPlayerFace != Playerface.Die) //added
                currentPlayerFace = Playerface.Idle;
        }
    }
    #endregion
    private void HandleUpdatedFace()
    {
        switch (currentPlayerFace)
        {
            case Playerface.Moving:
                spriteRenderer.sprite = faceData.moving;
                break;

            case Playerface.Dash:
                spriteRenderer.sprite = faceData.dash;
                break;

            case Playerface.JumpUp:
                spriteRenderer.sprite = faceData.jumpUp;
                break;

            case Playerface.JumpDown:
                spriteRenderer.sprite = faceData.jumpDown;
                break;

            case Playerface.FallDown_Impact:
                spriteRenderer.sprite = faceData.fallDownImpact;
                break;

            case Playerface.Blink:
                spriteRenderer.sprite = faceData.blink;
                break;

            case Playerface.Die:
                spriteRenderer.sprite = faceData.die;
                break;

            case Playerface.Idle:
                spriteRenderer.sprite = faceData.idle;
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
                SpawnVfx(transform.position);
                break;
        }
    }

    private void HandleGroundImpact(Vector2 position)
    {
        SpawnVfx(position);
    }

    private void SpawnVfx(Vector2 position)
    {
        smokePool?.Spawn(position);
    }

    #endregion

    #region Dashing Effects

    private void HandleDashEffects()
    {
        SquashPlayer(
            HasVisualProfile ? visualProfile.DashSquashAmount : 0.8f,
            HasVisualProfile ? visualProfile.DashSquashDuration : 0.2f);
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
            HandleJumpSquash(onAir, scaleAmount: HasVisualProfile ? visualProfile.JumpUpSquashAmount : 1.5f,
                scaleDuration: HasVisualProfile ? visualProfile.JumpUpSquashDuration : 0.2f,
                squashableObj: transform);
        else
            HandleJumpSquash(onAir, scaleAmount: HasVisualProfile ? visualProfile.LandSquashAmount : 0.7f,
                scaleDuration: HasVisualProfile ? visualProfile.LandSquashDuration : 0.2f,
                squashableObj: playerSquahTransform);
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
                        if (isDescending)
                        {
                            // Notify that the squash animation finished so the SFX can play || not used in the project
                            GameEventBus.TriggerPlayerJumpSquashComplete();
                        }
                        else
                        {
                            isLandingSquashActive = false;
                            UpdateFaceAfterAction();
                        }
                    });
            });
    }
}
