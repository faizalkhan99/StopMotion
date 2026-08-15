using UnityEditor.Rendering;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerDash playerDash;

    [Header("Movement")]
    [Tooltip("Maximum horizontal speed in units per second.")]
    [SerializeField] private float maxSpeed = 8f;
    [Tooltip("How fast the player reaches max speed.")]
    [SerializeField] private float acceleration = 50f;
    [Tooltip("How fast the player stops when no input is provided. High values give snappy shooter controls.")]
    [SerializeField] private float deceleration = 60f;


    [Header("Jump Architecture (Kinematic Math)")]
    [Tooltip("The exact peak height of the jump in Unity units.")]
    [SerializeField] private float maxJumpHeight = 3.5f;
    [Tooltip("Time in seconds to reach the jump apex. Lower values = faster, snappier jump.")]
    [SerializeField] private float timeToApex = 0.35f;
    [Tooltip("Multiplier applied to gravity when falling. > 1 means falling is faster than rising (heavy, realistic gravity).")]
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [Tooltip("Reduces vertical velocity by this percentage when the jump button is released early (0.5 = 50% cut).")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Juice & Responsiveness (Tolerances)")]
    [Tooltip("Time in seconds player can still jump after falling off a ledge.")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Time in seconds a jump input is remembered before hitting the ground.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Collision Checks")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Debug & Diagnostics")]
    [Tooltip("Enable to see exact reasons why jumps fail in the Console.")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool isGroundedDebugView; // Read-only view in Inspector

    // Internal Physics & Mechanics
    private Rigidbody2D rb;
    private float baseGravityScale;
    private float initialJumpVelocity;

    // Input State (Driven by public mobile methods)
    private float horizontalInput;
    private bool isGrounded;
    public bool isDashing  { get; private set; } =  false;

    // Zero-GC Timers
    private float coyoteTimer;
    private float jumpBufferTimer;

    // Zero-GC Physics Buffer (Pre-allocated array prevents runtime garbage)
    private readonly Collider2D[] groundHitBuffer = new Collider2D[1];
    private ContactFilter2D groundFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        EnforceZeroFriction();
        // 1. Calculate physics formulas based on designer metrics
        float calculatedGravity = -(2f * maxJumpHeight) / (timeToApex * timeToApex);
        baseGravityScale = calculatedGravity / Physics2D.gravity.y;
        initialJumpVelocity = Mathf.Abs(calculatedGravity) * timeToApex;
        rb.gravityScale = baseGravityScale;

        // 2. CACHE THE CONTACT FILTER (Unity 6 Standard)
        // Instead of building filters or passing masks every frame, we configure this struct ONCE at startup.
        groundFilter = new ContactFilter2D();
        groundFilter.useLayerMask = true;
        groundFilter.layerMask = groundLayer;
        groundFilter.useTriggers = false; // Ignore triggers so enemy/item zones don't count as ground
    }

    private void Start()
    {
        playerDash.InitDash( maxSpeed, this );
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
    private void Update()
    {
        // Keep filter synced in case you tweak LayerMasks in the Inspector during Play Mode!
        groundFilter.layerMask = groundLayer;

        // 1. Zero-GC Ground Check
        int hitCount = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundFilter, groundHitBuffer);
        isGrounded = hitCount > 0;
        isGroundedDebugView = isGrounded; // Exposes state to your Inspector

        // 2. Update Timers
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;

        // 3. Execute Jump if tolerances align
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            ExecuteJump();
        }
    }

    private void FixedUpdate()
    {
        if( !isDashing )
        {
            ApplyHorizontalMovement();
            ApplyDynamicGravity();
        }
        else
        {
            StopGravity();
        }
    }

    private void ApplyHorizontalMovement()
    {
        // if( !isDashing )
        // {
            // CACHING NATIVE CALLS: Read rb.linearVelocity ONCE into a local stack variable.
            // Every time you call 'rb.linearVelocity', Unity crosses the C# to C++ Native boundary.
            Vector2 currentVel = rb.linearVelocity;

            float targetSpeed = horizontalInput * maxSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            // MoveTowards provides crisp, snappy changes without the floatiness of Lerp
            float newVelocityX = Mathf.MoveTowards(currentVel.x, targetSpeed, accelRate * Time.fixedDeltaTime);

            // Write back to native property ONCE
            rb.linearVelocity = new Vector2(newVelocityX, currentVel.y);
        // }
    }

    private void ApplyDynamicGravity()
    {
        // Asymmetric Gravity: Make falling faster than rising for a weighty, responsive feel
        if (rb.linearVelocity.y < -0.01f)
        {
            rb.gravityScale = baseGravityScale * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }
    }

    private void StopGravity()
    {
        rb.gravityScale = 0f;
    }
    private void ExecuteJump()
    {
        // Apply vertical impulse and reset timers to prevent double-consuming the jump
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, initialJumpVelocity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }


    #region Mobile Touch Interface (Public API)

    /// <summary>
    /// Feed joystick or touch-button movement (-1.0 to 1.0). Call from UI EventTriggers.
    /// </summary>
    public void SetHorizontalInput(float input)
    {
        horizontalInput = Mathf.Clamp(input, -1f, 1f);
    }

    /// <summary>
    /// Call when the Mobile Jump Button is pressed DOWN (PointerDown).
    /// </summary>
    public void OnJumpButtonPressed()
    {
        jumpBufferTimer = jumpBufferTime;
        GameEventBus.TriggerPlaySFXCommand(SoundID.Jump);
        // DIAGNOSTIC CHECK: Tell the developer exactly why the jump might fail
        // if (showDebugLogs)
        // {
        //     if (!isGrounded && coyoteTimer <= 0f)
        //     {
        //         Debug.LogWarning("<b><color=red>[JUMP FAILED]</color></b> Jump pressed, but Player is NOT grounded! " +
        //                          "Check your 'Ground Layer' and ensure your GroundCheck Transform is at the bottom of the player's feet.");
        //     }
        //     else if (rb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionY))
        //     {
        //         Debug.LogError("<b><color=red>[JUMP FAILED]</color></b> Your Rigidbody2D has 'Freeze Position Y' checked in its Constraints!");
        //     }
        // }
    }

    /// <summary>
    /// Call when the Mobile Jump Button is RELEASED (PointerUp). Enables Mario-style variable jump.
    /// </summary>
    public void OnJumpButtonReleased()
    {
        // If moving upward and player releases screen, cut vertical velocity immediately
        Vector2 currentVel = rb.linearVelocity;
        if (currentVel.y > 0f)
        {
            rb.linearVelocity = new Vector2(currentVel.x, currentVel.y * jumpCutMultiplier);
        }
    }
    public void StopMovement()
    {
        horizontalInput = 0f;
    }

    public void ApplyDash(float dashSpeed)
    {
        Vector2 currentVelocity = rb.linearVelocity;
 
        if (currentVelocity.magnitude > 0.1f)
        {
            isDashing = true;
            Vector2 direction = currentVelocity.normalized;
            if(direction.y != 1)
            {
                rb.linearVelocity = dashSpeed * direction;
            }
        }
    }

    public void UnDash()
    {
        isDashing = false;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}