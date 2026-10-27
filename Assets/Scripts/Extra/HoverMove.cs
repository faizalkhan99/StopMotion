using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

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
    [SerializeField] private float duration = 0.15f;

    private Vector3 originalPosition;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MoveTo(GetTargetPosition());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MoveTo(originalPosition);
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 target = originalPosition;

        switch (axis)
        {
            case MoveAxis.X:
                target.x += distance;
                break;

            case MoveAxis.Y:
                target.y += distance;
                break;

            case MoveAxis.Z:
                target.z += distance;
                break;
        }

        return target;
    }

    private void MoveTo(Vector3 target)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        Vector3 start = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            // Smooth ease in/out
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition = Vector3.Lerp(
                start,
                target,
                t
            );

            yield return null;
        }

        transform.localPosition = target;
        moveCoroutine = null;
    }
}