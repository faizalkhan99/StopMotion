using UnityEngine;

/// <summary>
/// Plain-class jump module — no MonoBehaviour. Owned and pumped by PlayerController.
/// Owns the coyote/buffer tolerances, the kinematic apex-descent face trigger, and the
/// variable-height jump cut. All tunables come from PlayerLocomotionProfileSO.
/// </summary>
public class JumpEngine
{
    readonly Rigidbody2D rb;
    readonly PlayerLocomotionProfileSO profile;
    readonly float initialJumpVelocity;

    // Jump Descending Detection
    private bool detectJump = false;
    private bool hasStartedDescending = false;

    // Zero-GC Timers
    private float coyoteTimer;
    private float jumpBufferTimer;

    public bool HasStartedDescending => hasStartedDescending;
    public bool DetectJump { get => detectJump; set => detectJump = value; }
    public bool HasCoyoteTime => coyoteTimer > 0f;

    public JumpEngine(Rigidbody2D rb, PlayerLocomotionProfileSO profile, float initialJumpVelocity)
    {
        this.rb = rb;
        this.profile = profile;
        this.initialJumpVelocity = initialJumpVelocity;
    }

    /// <summary>
    /// Apex crossing: rising → falling (same epsilon as ApplyDynamicGravity). Fire once per jump.
    /// Mirror of PlayerController.CheckJumpApex(); dash freezes gravity/face — don't stomp dash face.
    /// </summary>
    public void TickApex(bool isGrounded, bool isDashing)
    {
        if (isGrounded) return;
        if (isDashing) return;             // dash freezes gravity/face — don't stomp dash face
        if (hasStartedDescending) return;  // fire once per jump
        if (rb.linearVelocity.y < -0.01f)
        {
            hasStartedDescending = true;
            GameEventBus.TriggerPlayerFaceChange(Playerface.JumpDown);
        }
    }

    /// <summary>
    /// Coyote refill + buffer countdown + jump execution.
    /// Mirror of PlayerController.HandleJumpExecution(); run after the ground tick.
    /// </summary>
    public void Tick(bool isGrounded)
    {
        if (isGrounded)
        {
            coyoteTimer = profile.CoyoteTime;
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

    /// <summary>Buffer a fresh jump (mirrors PlayerController.OnJumpButtonPressed). SFX/face events stay with the caller.</summary>
    public void BufferJump()
    {
        jumpBufferTimer = profile.JumpBufferTime;
        detectJump = true;
    }

    /// <summary>
    /// If moving upward and the jump button is released early, cut vertical velocity immediately.
    /// Mirror of PlayerController.OnJumpButtonReleased (variable jump). JumpDown face is
    /// handled automatically by TickApex when velocity crosses apex.
    /// </summary>
    public void CutJump()
    {
        // JumpCutMultiplier: reduce vertical velocity by this percentage (0.5 = 50% cut)
        Vector2 currentVel = rb.linearVelocity;
        if (currentVel.y > 0f)
        {
            rb.linearVelocity = new Vector2(currentVel.x, currentVel.y * profile.JumpCutMultiplier);
        }
    }

    /// <summary>Clears the descent flag after a landing consumer handled the impact.</summary>
    public void ClearDescent()
    {
        hasStartedDescending = false;
    }

    /// <summary>Zeroes buffered input so no stale jump leaks into a new life (TeleportTo).</summary>
    public void Flush()
    {
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        hasStartedDescending = false;
        detectJump = false;
    }

    private void ExecuteJump()
    {
        // Apply vertical impulse and reset timers to prevent double-consuming the jump
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, initialJumpVelocity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }
}
