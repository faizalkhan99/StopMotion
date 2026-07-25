using System.Collections;
using UnityEngine;

public class CameraShakeSystem : MonoBehaviour
{
    [Header("Hit Shake Settings")]
    [SerializeField] private float hitDuration = 0.2f;
    [SerializeField] private float hitMagnitude = 0.3f;

    [Header("Death Shake Settings")]
    [SerializeField] private float deathDuration = 0.5f;
    [SerializeField] private float deathMagnitude = 0.8f;

    private Vector3 _originalLocalPosition;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        _originalLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        GameEventBus.OnCameraShake += TriggerCameraShake;
    }

    private void OnDisable()
    {
        GameEventBus.OnCameraShake -= TriggerCameraShake;
    }

    private void TriggerCameraShake() => StartShake(deathDuration, deathMagnitude);

    private void StartShake(float duration, float magnitude)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = _originalLocalPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            // Decay magnitude over time for a smoother settle
            magnitude = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            yield return null;
        }

        transform.localPosition = _originalLocalPosition;
    }
}