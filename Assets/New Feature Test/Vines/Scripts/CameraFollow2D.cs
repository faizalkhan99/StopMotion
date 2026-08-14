using UnityEngine;
using UnityEngine.PlayerLoop;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target; // Drag your player here
    public float smoothSpeed = 5.0f; // Speed of the camera catch-up
    public Vector3 offset; // Distance offset (keep Z at -10)
    public float delayTime;

    private float timeRemaining;
    private bool canCamMove = false;

    void Start()
    {
        timeRemaining = delayTime;
    }

    void LateUpdate()
    {
        if (target == null) return;
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
}

