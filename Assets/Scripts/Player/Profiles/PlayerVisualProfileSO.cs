using UnityEngine;

[CreateAssetMenu(fileName = "PlayerVisualProfile", menuName = "StopMotion/Player Visual Profile")]
public class PlayerVisualProfileSO : ScriptableObject
{
    [Header("Player Squash Config")]
    [SerializeField] private float dashSquashAmount = 0.8f;
    [SerializeField] private float dashSquashDuration = 0.2f;

    [SerializeField] private float jumpUpSquashAmount = 1.5f;
    [SerializeField] private float jumpUpSquashDuration = 0.2f;

    [SerializeField] private float landSquashAmount = 0.7f;
    [SerializeField] private float landSquashDuration = 0.2f;

    [Header("Player's Eye Blinking")]
    [SerializeField] private float blinkDelay = 2f;
    [SerializeField] private float blinkDuration = 0.15f;
    [SerializeField] private float idleDelay = 2f;

    [Header("Player's Key UI")]
    [SerializeField] private float keyFadeDuration = 0.35f;

    [Header("VFX Pools")]
    [Tooltip("Prewarmed puff instances. Spawned puffs return here (instead of leaking clones) once their particles die.")]
    [SerializeField, Min(1)] private int smokePoolSize = 6;

    public float DashSquashAmount => dashSquashAmount;
    public float DashSquashDuration => dashSquashDuration;
    public float JumpUpSquashAmount => jumpUpSquashAmount;
    public float JumpUpSquashDuration => jumpUpSquashDuration;
    public float LandSquashAmount => landSquashAmount;
    public float LandSquashDuration => landSquashDuration;
    public float BlinkDelay => blinkDelay;
    public float BlinkDuration => blinkDuration;
    public float IdleDelay => idleDelay;
    public float KeyFadeDuration => keyFadeDuration;
    public int SmokePoolSize => smokePoolSize;
}
