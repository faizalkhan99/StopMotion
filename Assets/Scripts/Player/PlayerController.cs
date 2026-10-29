using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Config Profiles (ScriptableObjects)")]
    [SerializeField] private PlayerLocomotionProfileSO locomotionProfile;
    [SerializeField] private PlayerDashProfileSO dashProfile;

    [SerializeField] PlayerInputRouter playerInput;

    [Header("Scene References")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform dashEchoParent;

    [Header("Collision Checks")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Debug & Diagnostics")]
    [Tooltip("Enable to see exact reasons why jumps fail in the Console.")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool isGroundedDebugView; // Read-only view in Inspector

    private Rigidbody2D rb;
    private float baseGravityScale;

    // ---- Plain-class modules (no MonoBehaviour) — wired once in Awake ----
    private GroundSensor groundSensor;
    private JumpEngine jumpEngine;
    private LocomotionEngine locomotion;
    private DashEngine dashEngine;
    private EchoTrailSpawner echoSpawner;
    private FallDampingRelay fallDamping;

    // Input State (Driven by public mobile methods)
    private float horizontalInput;

    public bool isDashing => dashEngine != null && dashEngine.IsDashing;
    public float DashDirectionX => dashEngine != null ? dashEngine.DirectionX : 1f;

    // Idle query - raw grounded (no coyote) for visual idle detection
    public bool IsIdle => IsGroundedRaw && Mathf.Abs(horizontalInput) < 0.01f;
    public bool IsGroundedRaw => groundSensor != null && groundSensor.IsGroundedRaw;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (locomotionProfile == null)
        {
            Debug.LogError("<b>[PlayerController]</b> Locomotion profile not assigned — player disabled.");
            enabled = false;
            return;
        }

        if (dashProfile == null)
        {
            Debug.LogError("<b>[PlayerController]</b> Dash profile not assigned — player disabled.");
            enabled = false;
            return;
        }

        EnforceZeroFriction();
        WireModules();
        groundSensor.OnLanded += GroundImpactDetection;
    }

    /// <summary>
    /// 1. Calculate physics formulas based on designer metrics, then wire every plain-class
    /// module. Profiles carry the inspector-controllable numbers; this behaviour stays a
    /// thin pump + scene-reference holder.
    /// </summary>
    private void WireModules()
    {
        float calculatedGravity = -(2f * locomotionProfile.MaxJumpHeight) / (locomotionProfile.TimeToApex * locomotionProfile.TimeToApex);
        baseGravityScale = calculatedGravity / Physics2D.gravity.y;
        float initialJumpVelocity = Mathf.Abs(calculatedGravity) * locomotionProfile.TimeToApex;
        rb.gravityScale = baseGravityScale;

        groundSensor = new GroundSensor(groundCheck, groundLayer, locomotionProfile);
        jumpEngine = new JumpEngine(rb, locomotionProfile, initialJumpVelocity);
        locomotion = new LocomotionEngine(rb, locomotionProfile, baseGravityScale);
        echoSpawner = new EchoTrailSpawner(dashProfile.EchoPrefab, dashEchoParent, transform, dashProfile.EchoSpawnInterval, dashProfile.EchoPoolSize);
        dashEngine = new DashEngine(rb, dashProfile, echoSpawner);
        fallDamping = new FallDampingRelay(rb, showDebugLogs);
    }

    /// <summary>
    /// Prevents Unity's physics solver from calculating impact friction when hitting the floor.
    /// This stops the character from stuttering or halting for a frame upon landing.
    /// </summary>
    private void EnforceZeroFriction()
    {
        // If the developer hasn't manually assigned a custom frictionless material in the Editor, make one!
        if (rb.sharedMaterial == null || rb.sharedMaterial.friction > 0f)
        {
            PhysicsMaterial2D zeroFrictionMat = new PhysicsMaterial2D("Runtime_ZeroFriction")
            {
                friction = 0f,
                bounciness = 0f
            };

            rb.sharedMaterial = zeroFrictionMat;

            // Also apply to all attached colliders to ensure complete coverage
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].sharedMaterial = zeroFrictionMat;
            }

            if (showDebugLogs)
            {
                Debug.Log("<b><color=cyan>[PHYSICS]</color></b> Auto-applied Zero-Friction Material to Player to prevent landing stutters.");
            }
        }
    }

    private void Start()
    {
        // Was PlayerDash.InitDash(this) — echo pool creation + collectable subscription
        // now live in the plain-class DashEngine.
        dashEngine.Initialize();
    }

    private void OnDestroy()
    {
        dashEngine?.Dispose();
    }

    private void Update()
    {
        isGroundedDebugView = IsGroundedRaw; // Exposes state to your Inspector

        groundSensor.SyncLayerMask(groundLayer); // Keep filter synced for Play Mode Inspector tweaks
        groundSensor.Tick();                     // was UpdateGroundDetection
        jumpEngine.TickApex(IsGroundedRaw, isDashing); // was CheckJumpApex
        jumpEngine.Tick(IsGroundedRaw);                // was HandleJumpExecution
        fallDamping.Tick(IsGroundedRaw);               // was UpdateFallDamping
        dashEngine.Tick();                             // was PlayerDash.Update
        echoSpawner.Tick();                            // was PlayerDash.SpawnEchoTrail coroutine
    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            locomotion.MoveFixed(Time.fixedDeltaTime, horizontalInput);
        }
        else
        {
            locomotion.SuspendGravity();
        }
    }

    private void GroundImpactDetection()
    {
        jumpEngine.ClearDescent();
        GameEventBus.TriggerPlayerJumpSquash(false);
        GameEventBus.TriggerGroundImpact(transform.position);

        // Landing mid-dash: don't let the impact face stomp the dash face.
        // isDashing is this component's own authoritative state — no lookup
        // into another system needed. Fixes #14.
        if (!isDashing)
        {
            GameEventBus.TriggerPlayerFaceChange(Playerface.FallDown_Impact);
        }

        if (playerInput != null)
            playerInput.SetIsJumpFalse();
    }

    #region Mobile Touch Interface (Public API)

    /// <summary>
    /// Call when the jump input fires — keyboard press (routed directly, no longer gated
    /// behind the squash animation) or mobile tap. Buffers the physics jump immediately
    /// so the animation can never delay or eat the input.
    /// </summary>
    public void OnJumpButtonPressed()
    {
        jumpEngine.BufferJump();
        GameEventBus.TriggerPlaySFXCommand(SoundID.Jump);
        GameEventBus.TriggerPlayerFaceChange(Playerface.JumpUp);
    }

    /// <summary>
    /// Call when the Mobile Jump Button is RELEASED (PointerUp). Enables Mario-style variable jump.
    /// </summary>
    public void OnJumpButtonReleased()
    {
        // If moving upward and player releases screen, cut vertical velocity immediately
        jumpEngine.CutJump();
        // JumpDown face is now handled automatically by TickApex() when velocity crosses apex
    }
    public void StopMovement()
    {
        horizontalInput = 0f;
    }

    /// <summary>
    /// Feed joystick or touch-button movement (-1.0 to 1.0). Call from UI EventTriggers.
    /// </summary>
    public void SetHorizontalInput(float input)
    {
        horizontalInput = Mathf.Clamp(input, -1f, 1f);
    }

    #endregion

    #region Dash Effect

    /// <summary>Router entry point — replaced PlayerDash.TriggerDash with a charge + movement gate.</summary>
    public void TriggerDash()
    {
        dashEngine.Request(IsPlayerMoving());
    }

    public void UnDash()
    {
        dashEngine.EndDash();
    }

    public bool IsPlayerMoving()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Instantly relocates the player (checkpoint respawn). Zeroes velocity,
    /// clears dash/jump buffers so no stale input leaks into the new life.
    /// </summary>
    public void TeleportTo(Vector2 target)
    {
        StopMovement();
        dashEngine.Flush();
        jumpEngine.Flush();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.MovePosition(target);
        }
        transform.position = new Vector3(target.x, target.y, transform.position.z);
        rb.gravityScale = baseGravityScale;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null && locomotionProfile != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, locomotionProfile.Ground.gizmoRadius);
        }
    }
    public bool IsGrounded()
    {
        return IsGroundedRaw || jumpEngine.HasCoyoteTime;
    }
}
