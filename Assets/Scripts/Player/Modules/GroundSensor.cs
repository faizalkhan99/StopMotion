using System;
using UnityEngine;

/// <summary>
/// Plain-class ground detection module — no MonoBehaviour. Owned and pumped by PlayerController.
/// Zero-GC box overlap (pre-allocated collider buffer + cached contact filter) plus the
/// airtime-filtered landing event that used to live inline in PlayerController.UpdateGroundDetection().
/// </summary>
public class GroundSensor
{
    /// <summary>
    /// Fired the frame a genuine landing lands (raw grounded edge + landingMinAirTime satisfied).
    /// Consumers reset descent flags / fire squash + impact VFX exactly where
    /// PlayerController.GroundImpactDetection() ran before.
    /// </summary>
    public event Action OnLanded;

    readonly PlayerLocomotionProfileSO profile;

    // Zero-GC Physics Buffer (Pre-allocated array prevents runtime garbage)
    readonly Collider2D[] groundHitBuffer = new Collider2D[1];
    ContactFilter2D groundFilter;
    readonly Transform groundCheck;

    public bool IsGroundedRaw { get; private set; }

    private float airborneTimer;

    public GroundSensor(Transform groundCheck, LayerMask groundLayer, PlayerLocomotionProfileSO profile)
    {
        this.groundCheck = groundCheck;
        this.profile = profile;

        // CACHE THE CONTACT FILTER (Unity 6 Standard)
        // Instead of building filters or passing masks every frame, we configure this struct ONCE at startup.
        groundFilter = new ContactFilter2D();
        groundFilter.useLayerMask = true;
        groundFilter.layerMask = groundLayer;
        groundFilter.useTriggers = false; // Ignore triggers so enemy/item zones don't count as ground
    }

    /// <summary>Keep filter synced in case you tweak LayerMasks in the Inspector during Play Mode!</summary>
    public void SyncLayerMask(LayerMask groundLayer)
    {
        groundFilter.layerMask = groundLayer;
    }

    /// <summary>Run from PlayerController.Update(), first — others read IsGroundedRaw after this.</summary>
    public void Tick()
    {
        bool wasGrounded = IsGroundedRaw;

        // 1. Zero-GC Ground Check
        // Box matches foot width, catches edges even when only partially over ground
        Vector2 boxSize = new Vector2(profile.Ground.checkWidth, profile.Ground.checkHeight);
        int hitCount = Physics2D.OverlapBox(groundCheck.position, boxSize, 0f, groundFilter, groundHitBuffer);
        IsGroundedRaw = hitCount > 0;

        // Track continuous airtime so 1-frame separation flicker (e.g. riding a
        // transform-moved platform) never counts as a landing on its own.
        if (!IsGroundedRaw)
        {
            airborneTimer += Time.deltaTime;
        }

        // Fall detection — only after genuine airtime, not contact flicker.
        if (IsGroundedRaw && !wasGrounded && airborneTimer >= profile.LandingMinAirTime)
        {
            OnLanded?.Invoke();
            airborneTimer = 0f;
        }
    }
}
