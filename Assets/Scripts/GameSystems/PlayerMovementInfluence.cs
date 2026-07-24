using UnityEngine;

public class PlayerMovementInfluence : MonoBehaviour, ITimerInfluence
{
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private float minMoveSpeed = 0.1f;

    private float minMoveSpeedSqr;

    private void Awake()
    {
        minMoveSpeedSqr = minMoveSpeed * minMoveSpeed;
    }

    private void OnValidate()
    {
        minMoveSpeedSqr = minMoveSpeed * minMoveSpeed;
    }

    public bool ShouldCountDown(float deltaTime)
    {
        return playerRb != null && playerRb.linearVelocity.sqrMagnitude > minMoveSpeedSqr;
    }
}