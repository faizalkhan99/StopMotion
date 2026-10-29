using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDashProfile", menuName = "StopMotion/Player Dash Profile")]
public class PlayerDashProfileSO : ScriptableObject
{
    [Header(" Dash Settings ")]
    [Tooltip("Master switch for the dash mechanic. Off = collectables gather but dash never fires.")]
    [SerializeField] private bool canDash = true;

    [SerializeField] private float dashSpeed = 20f;

    [Tooltip("Seconds the dash burst lasts before the facing/gravity hand back to normal locomotion.")]
    [SerializeField] private float dashDuration = 0.2f;

    [Tooltip("Seconds between dash starts. New dashes can only fire after the cooldown fully clears.")]
    [SerializeField] private float dashCoolDownTime = 1f;

    [Header(" Dash Collectable ")]
    [Tooltip("Max dash charges held from pickups (CollisionDetection with item = DashCharge).")]
    [SerializeField] private int maxDashCollectableCount = 3;

    [Tooltip("Charges granted at level start. 0 = must collect DashCharge pickups first.")]
    [SerializeField] private int startingDashCharges = 3;

    [Header("Echo Effect")]
    [SerializeField] DashEchoEffect echoPrefab;
    [SerializeField] float echoSpawnInterval = 0.05f;
    [SerializeField] int echoPoolSize = 10;

    public bool CanDash => canDash;
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCoolDownTime => dashCoolDownTime;
    public int MaxDashCollectableCount => maxDashCollectableCount;
    public int StartingDashCharges => startingDashCharges;
    public DashEchoEffect EchoPrefab => echoPrefab;
    public float EchoSpawnInterval => echoSpawnInterval;
    public int EchoPoolSize => echoPoolSize;
}
