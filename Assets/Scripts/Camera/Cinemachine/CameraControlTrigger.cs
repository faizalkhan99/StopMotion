using UnityEngine;

public enum PanDirection
{
    Up,
    Down,
    Left,
    Right
}


public class CameraControlTrigger : MonoBehaviour
{
    [SerializeField] private PanDirection panDirection;
    [SerializeField] private float panDistance = 3f;
    [SerializeField] private float panTime = 0.35f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CameraManager.instance.PanCamera(
                panDistance,
                panTime,
                panDirection,
                false
            );
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
                CameraManager.instance.PanCamera(
                    panDistance,
                    panTime,
                    panDirection,
                    true
                );
        
        }
    }
}
