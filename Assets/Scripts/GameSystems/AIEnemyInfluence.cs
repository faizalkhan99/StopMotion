using UnityEngine;

public class AIEnemyInfluence : MonoBehaviour, ITimerInfluence
{
    private ChronoState currentChronoState;

    private void OnEnable()
    {
        GameEventBus.OnChronoStateChanged += HandleChronoStateChanged;
    }

    private void OnDisable()
    {
        GameEventBus.OnChronoStateChanged -= HandleChronoStateChanged;
    }

    private void HandleChronoStateChanged(ChronoState newState)
    {
        currentChronoState = newState;
    }

    public bool ShouldCountDown(float deltaTime)
    {
        return currentChronoState == ChronoState.Ticking;
    }
}