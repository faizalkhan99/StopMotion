using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLocomotionProfile", menuName = "StopMotion/Player Locomotion Profile")]
public class PlayerLocomotionProfileSO : ScriptableObject
{
    [System.Serializable]
    public class GroundCheckConfig
    {
        [Tooltip("Width of the grounded overlap box. Matches foot width, catches edges even when only partially over ground.")]
        public float checkWidth = 0.8f;

        [Tooltip("Height of the grounded overlap box.")]
        public float checkHeight = 0.1f;

        [Tooltip("Scene-view gizmo only.")]
        public float gizmoRadius = 0.2f;
    }

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
    [Tooltip("Min continuous airtime before a re-ground counts as a landing. Filters 1-frame separation flicker on moving platforms.")]
    [SerializeField, Range(0.03f, 1f)] private float landingMinAirTime = 0.1f;
    [SerializeField] private GroundCheckConfig groundCheck = new GroundCheckConfig();

    public float MaxSpeed => maxSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public float MaxJumpHeight => maxJumpHeight;
    public float TimeToApex => timeToApex;
    public float FallGravityMultiplier => fallGravityMultiplier;
    public float JumpCutMultiplier => jumpCutMultiplier;
    public float CoyoteTime => coyoteTime;
    public float JumpBufferTime => jumpBufferTime;
    public float LandingMinAirTime => landingMinAirTime;
    public GroundCheckConfig Ground => groundCheck;
}
