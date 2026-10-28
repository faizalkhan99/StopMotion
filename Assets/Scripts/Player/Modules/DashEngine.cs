using UnityEngine;

/// <summary>
/// Plain-class dash state machine — no MonoBehaviour. Owned and pumped by PlayerController.
/// Defines the wrapper-level sprite facing (DirectionX), velocity burst, cooldown and
/// collectable bookkeeping that used to live in PlayerDash + PlayerController.CanDash.
/// EchoING visuals live in EchoTrailSpawner; profile comes from PlayerDashProfileSO.
/// </summary>
public class DashEngine
{
    readonly Rigidbody2D rb;
    readonly PlayerDashProfileSO profile;
    readonly EchoTrailSpawner echoSpawner;

    public bool IsDashing { get; private set; } = false;

    /// <summary>Authoritative horizontal dash direction: +1 = right, -1 = left.</summary>
    public float DirectionX { get; private set; } = 1f;

    public bool IsInitialized { get; private set; }

    private float lastDashTime;
    private bool triggerDash = false;
    private int collectableCount;

    public DashEngine(Rigidbody2D rb, PlayerDashProfileSO profile, EchoTrailSpawner echoSpawner)
    {
        this.rb = rb;
        this.profile = profile;
        this.echoSpawner = echoSpawner;
    }

    /// <summary>
    /// One-time init (was PlayerDash.InitDash + OnEnable/Start). The echo pool always comes
    /// up (same overhead profile as before); the collectable listener only subscribes when
    /// the dash feature itself is enabled in the profile, mirroring the old enabled-gate.
    /// Charges start at the profile's startingDashCharges (was the PlayerDash testDashCount temp hack).
    /// </summary>
    public void Initialize()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        collectableCount = profile.StartingDashCharges;

        if (profile.CanDash)
        {
            GameEventBus.OnPlayerContactWithItem += AddDashCount;
        }
    }

    /// <summary>Router entry (was PlayerDash.TriggerDash). Only accepts while charges remain and the player is moving.</summary>
    public void Request(bool isPlayerMoving)
    {
        if (!profile.CanDash) return;
        if (collectableCount > 0 && isPlayerMoving)
        {
            triggerDash = true;
        }
    }

    /// <summary>Pump from PlayerController.Update() (was PlayerDash.Update).</summary>
    public void Tick()
    {
        if (profile.CanDash == false || !IsInitialized)
        {
            // Feature toggled off mid-burst: hand gravity/face back instead of wedging the player airborne.
            if (!profile.CanDash && IsDashing) EndDash();
            return;
        }

        bool dashTimeElapsed = Time.time >= lastDashTime + profile.DashDuration;

        bool cooldownElapsed = Time.time >= lastDashTime + profile.DashCoolDownTime;

        // End the current dash burst once its own (shorter) duration is up.
        // Face revert now handled by PlayerVisuals.UpdateLocomotionFace() (LateUpdate)
        // based on IsGroundedRaw + IsIdle, so no forced Moving here (fixes airborne Moving flicker).
        if (dashTimeElapsed && IsDashing)
        {
            EndDash();
        }

        // Only start a new dash once the cooldown has actually cleared.
        if (cooldownElapsed && triggerDash)
        {
            AttemptDash();
        }
    }

    /// <summary>Was PlayerController.CanDash(float) — burst velocity + facing bookkeeping.</summary>
    private void AttemptDash()
    {
        Vector2 currentVelocity = rb.linearVelocity;

        if (currentVelocity.magnitude > 0.1f)
        {
            IsDashing = true;
            Vector2 lastKnowDir = currentVelocity.normalized;

            if (lastKnowDir.x > 0)
            {
                DirectionX = 1f;
                rb.linearVelocity = new Vector2(profile.DashSpeed * 1f, 0f);
                OnDashStarted();
            }
            else if (lastKnowDir.x < 0)
            {
                DirectionX = -1f;
                rb.linearVelocity = new Vector2(profile.DashSpeed * -1f, 0f);
                OnDashStarted();
            }
        }
        triggerDash = false;
    }

    /// <summary>Count/side-effects for a committed burst (was PlayerDash.CanPerformDash success path).</summary>
    private void OnDashStarted()
    {
        lastDashTime = Time.time;
        collectableCount--;

        // Was PlayerDash.TriggerDashEffect: squash+face handled by PlayerVisuals via the bus,
        // echo trail spawned by the caller-owned spawner.
        GameEventBus.TriggerPlayerDash();
        echoSpawner.Begin(DirectionX, profile.DashDuration);
    }

    public void EndDash()
    {
        IsDashing = false;
        GameEventBus.TriggerPlayerFaceChange(Playerface.Idle);
    }

    private void AddDashCount()
    {
        if (collectableCount < profile.MaxDashCollectableCount)
        {
            collectableCount++;
        }
    }

    /// <summary>Zeroes dash/burst state so nothing leaks into a new life (TeleportTo).</summary>
    public void Flush()
    {
        IsDashing = false;
        DirectionX = 1f;
        triggerDash = false;
    }

    /// <summary>Unsubscribe the collectable listener — called from PlayerController.OnDestroy.</summary>
    public void Dispose()
    {
        GameEventBus.OnPlayerContactWithItem -= AddDashCount;
        IsInitialized = false;
    }
}
