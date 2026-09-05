using UnityEngine;

public class EdgeVinesGrowthController : MonoBehaviour
{
    [SerializeField] private Material edgeVinesMaterial;
    [SerializeField] private float levelDuration = 60f; // match your level's starting time

    private static readonly int GrowthAmountID = Shader.PropertyToID("_GrowthAmount");

    private void OnEnable()  => GameEventBus.OnLevelTimerUpdated += HandleTimerUpdated;
    private void OnDisable() => GameEventBus.OnLevelTimerUpdated -= HandleTimerUpdated;

    private void HandleTimerUpdated(float timeRemaining)
    {
        float elapsedFraction = 1f - Mathf.Clamp01(timeRemaining / levelDuration);
        edgeVinesMaterial.SetFloat(GrowthAmountID, elapsedFraction);
    }
}