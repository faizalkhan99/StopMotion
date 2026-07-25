using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (GameEventBus.IsPaused)
                GameEventBus.ResumeTimer();
            else
                GameEventBus.PauseTimer();
        }
    }

    public bool ShouldCountDown(float deltaTime)
    {
        return currentChronoState == ChronoState.Ticking;
    }
}