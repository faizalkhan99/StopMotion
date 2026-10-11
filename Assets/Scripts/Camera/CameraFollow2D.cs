using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target; // Drag your player here
    [SerializeField] private float smoothSpeed = 5.0f; // Speed of the camera catch-up
    [SerializeField] private Vector3 offset; // Distance offset (keep Z at -10)
    [SerializeField] private float delayTime;
    [SerializeField] private bool followPlayer;

    private float timeRemaining;
    private bool canCamMove = false;
    private bool followSuspended;

    void Start()
    {
        timeRemaining = delayTime;
    }

    private void OnEnable()
    {
        GameEventBus.OnPlayerRespawnStarted += HandleRespawnStarted;
        GameEventBus.OnPlayerRespawned += HandleRespawned;
    }

    private void OnDisable()
    {
        GameEventBus.OnPlayerRespawnStarted -= HandleRespawnStarted;
        GameEventBus.OnPlayerRespawned -= HandleRespawned;
    }

    void LateUpdate()
    {
        if (target == null) return;
        // Respawn freeze holds the full death window and also guards the
        // delay-timer block so StartShaderTimer can never refire mid-respawn.
        if (followSuspended) return;
        if(!followPlayer) return;
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else if(!canCamMove)
        {
            canCamMove = true;
            GameEventBus.StartShaderTimer();
        }

        if (canCamMove)
        {
            // Define the target position including the offset
            Vector3 desiredPosition = target.position + offset;

            // Smoothly interpolate between current position and desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Update the camera position
            transform.position = smoothedPosition;
        }
    }

    private void HandleRespawnStarted()
    {
        // Respawn freeze holds the death window; followPlayer is untouched.
        followSuspended = true;
    }

    private void HandleRespawned()
    {
        // Resume lerp: existing smoothing glides from the death framing
        // to the teleported player — no snap, no extra tuning.
        followSuspended = false;
    }

    public void SetPos(Vector2 pos)
    {
        transform.position = pos;
    }
}
