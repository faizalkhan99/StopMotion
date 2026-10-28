using UnityEngine;

/// <summary>
/// Plain-class camera feel module — no MonoBehaviour. Owned and pumped by PlayerController.Update().
/// Hollow Knight-style camera feel: when falling fast, hand the camera a lighter Y damping (fall
/// pan) so it lags behind the player, then lerps it back on landing. CameraManager lerps
/// Damping.y itself on another CinemachineCamera's Composer via LerpYDamping(true/false).
/// Scenes without a CameraManager (main menu, Test_Level) are safe — guarded no-op.
/// </summary>
public class FallDampingRelay
{
    readonly Rigidbody2D rb;
    readonly bool showDebugLogs;

    public FallDampingRelay(Rigidbody2D rb, bool showDebugLogs)
    {
        this.rb = rb;
        this.showDebugLogs = showDebugLogs;
    }

    /// <summary>Run from PlayerController.Update() last (same slot as the old UpdateFallDamping).</summary>
    public void Tick(bool isGrounded)
    {
        CameraManager cameraManager = CameraManager.instance;
        if (cameraManager == null) return;

        float velocityY = rb.linearVelocity.y;

        // 1. Fast fall detected: engage fall damping once (not mid-lerp, not already flagged)
        if (velocityY < cameraManager._fallSpeedYDampingChangeThreshold
            && !cameraManager.IsLerpingYDamping
            && !cameraManager.LerpedFromPlayerFalling)
        {
            if (showDebugLogs)
            {
                Debug.Log($"<b><color=cyan>[CAMERA]</color></b> Fast fall {velocityY:0.0} < {cameraManager._fallSpeedYDampingChangeThreshold} → LerpYDamping(true)");
            }
            cameraManager.LerpYDamping(true);
        }

        // 2. Rising / standing / grounded: fully restore damping when the flag is live
        if ((velocityY >= 0f || isGrounded) && cameraManager.LerpedFromPlayerFalling)
        {
            if (showDebugLogs)
            {
                Debug.Log($"<b><color=cyan>[CAMERA]</color></b> Fall ended (v={velocityY:0.0}, grounded={isGrounded}) → LerpYDamping(false)");
            }
            cameraManager.LerpedFromPlayerFalling = false;
            cameraManager.LerpYDamping(false);
        }
    }
}
