using UnityEngine;
using UnityEngine.EventSystems;

public class HoverMove : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum MoveAxis
    {
        X,
        Y,
        Z
    }

    [Header("Movement")]
    [SerializeField] private MoveAxis axis = MoveAxis.Y;
    [SerializeField] private float distance = 20f;
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 velocity;
    private bool isHovered;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        targetPosition = originalPosition;
    }

    private void Update()
    {
        Vector3 desiredPosition = originalPosition;

        if (isHovered)
        {
            switch (axis)
            {
                case MoveAxis.X:
                    desiredPosition += Vector3.right * distance;
                    break;

                case MoveAxis.Y:
                    desiredPosition += Vector3.up * distance;
                    break;

                case MoveAxis.Z:
                    desiredPosition += Vector3.forward * distance;
                    break;
            }
        }

        targetPosition = desiredPosition;

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPosition,
            ref velocity,
            smoothTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}