using UnityEngine;

/// <summary>
/// Plain-class horizontal movement + gravity module — no MonoBehaviour. Owned and pumped by
/// PlayerController.FixedUpdate(). All tunables come from PlayerLocomotionProfileSO.
/// </summary>
public class LocomotionEngine
{
    readonly Rigidbody2D rb;
    readonly PlayerLocomotionProfileSO profile;
    readonly float baseGravityScale;

    public LocomotionEngine(Rigidbody2D rb, PlayerLocomotionProfileSO profile, float baseGravityScale)
    {
        this.rb = rb;
        this.profile = profile;
        this.baseGravityScale = baseGravityScale;
    }

    /// <summary>Restore the profile-derived gravity scale (used after teleport / dash end consumers).</summary>
    public float BaseGravityScale => baseGravityScale;

    public void MoveFixed(float dt, float horizontalInput)
    {
        ApplyHorizontalMovement(dt, horizontalInput);
        ApplyDynamicGravity();
    }

    /// <summary>Dash freeze: gravity is handed over to the dash burst (velocity is dash-owned).</summary>
    public void SuspendGravity()
    {
        rb.gravityScale = 0f;
    }

    public void RestoreGravity()
    {
        rb.gravityScale = baseGravityScale;
    }

    private void ApplyHorizontalMovement(float dt, float horizontalInput)
    {
        // CACHING NATIVE CALLS: Read rb.linearVelocity ONCE into a local stack variable.
        // Every time you call 'rb.linearVelocity', Unity crosses the C# to C++ Native boundary.
        Vector2 currentVel = rb.linearVelocity;

        float targetSpeed = horizontalInput * profile.MaxSpeed;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? profile.Acceleration : profile.Deceleration;

        // MoveTowards provides crisp, snappy changes without the floatiness of Lerp
        float newVelocityX = Mathf.MoveTowards(currentVel.x, targetSpeed, accelRate * dt);

        // Write back to native property ONCE
        rb.linearVelocity = new Vector2(newVelocityX, currentVel.y);
    }

    private void ApplyDynamicGravity()
    {
        // Asymmetric Gravity: Make falling faster than rising for a weighty, responsive feel
        if (rb.linearVelocity.y < -0.01f)
        {
            rb.gravityScale = baseGravityScale * profile.FallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }
    }
}
