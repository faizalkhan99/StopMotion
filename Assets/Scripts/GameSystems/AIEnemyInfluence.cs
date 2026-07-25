using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AIEnemyInfluence : MonoBehaviour, ITimerInfluence
{
    [Header("Level Brain")]
    [Tooltip("Per-level scripted move/stop timeline. Assign the SO asset for this level here.")]
    [SerializeField] private LevelChronoProfileSO chronoProfile;

    [Header("Debug")]
    [Tooltip("If true, pressing P manually toggles Pause/Resume and cancels the scripted sequence. " +
             "Turn off for shipping builds so testers can't desync the brain.")]
    [SerializeField] private bool allowKeyboardOverride = true;

    private ChronoState currentChronoState = ChronoState.Ticking;
    private Coroutine sequenceRoutine;

    private void OnEnable()
    {
        GameEventBus.OnChronoStateChanged += HandleChronoStateChanged;
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEventBus.OnChronoStateChanged -= HandleChronoStateChanged;
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
    }

    private void Update()
    {
        if (!allowKeyboardOverride || Keyboard.current == null) return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            if (currentChronoState == ChronoState.Ticking)
                GameEventBus.PauseTimer();
            else
                GameEventBus.ResumeTimer();
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Gameplay)
        {
            if (sequenceRoutine == null && chronoProfile != null && chronoProfile.beats != null
                && chronoProfile.beats.Length > 0)
            {
                sequenceRoutine = StartCoroutine(RunChronoSequence());
            }
        }
        else if (state == GameState.GameOver || state == GameState.LevelComplete)
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }
        }
    }

    private void HandleChronoStateChanged(ChronoState state)
    {
        currentChronoState = state;
    }

    private IEnumerator RunChronoSequence()
    {
        do
        {
            foreach (var beat in chronoProfile.beats)
            {
                if (beat.isMoveBeat)
                    GameEventBus.ResumeTimer();
                else
                    GameEventBus.PauseTimer();

                yield return new WaitForSeconds(beat.duration);
            }
        }
        while (chronoProfile.loopSequence);

        sequenceRoutine = null;
    }

    public bool ShouldCountDown(float deltaTime)
    {
        return currentChronoState == ChronoState.Ticking;
    }
}
