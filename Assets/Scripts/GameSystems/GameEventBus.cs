using System;
using UnityEngine;

/// <summary>
/// CENTRAL EVENT BUS: The single point of contact for all game-wide communication[cite: 7].
/// Uses native Unity runtime initialization to guarantee zero race conditions during scene boot.
/// </summary>
public static class GameEventBus
{
    // Macro Game State Events
    public static GameState CurrentGameState { get; private set; }
    public static event Action<GameState> OnGameStateChanged;
    public static void TriggerGameStateChanged(GameState newState)
    {
        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(newState);

        if (newState == GameState.MainMenu)
            ResetCachedState();
    }

    // Chrono Timer Micro-State Events
    public static ChronoState CurrentChronoState { get; private set; }
    public static event Action<ChronoState> OnChronoStateChanged;
    public static void TriggerChronoStateChanged(ChronoState newState)
    {
        CurrentChronoState = newState;
        OnChronoStateChanged?.Invoke(newState);
    }

    public static event Action<GameOverReason> OnGameOverTriggered;
    public static void TriggerGameOver(GameOverReason reason) => OnGameOverTriggered?.Invoke(reason);


    // Continuous Feedback & UI Events
    public static event Action<float> OnLevelTimerUpdated;
    public static void TriggerLevelTimerUpdated(float timeRemaining) => OnLevelTimerUpdated?.Invoke(timeRemaining);
    public static event Action OnCameraShake;
    public static void TriggerCameraShake() => OnCameraShake?.Invoke();

    /// <summary>Fires during a rule violation. Value is normalized 0.0 (safe) to 1.0 (explosion)[cite: 7].</summary>
    public static event Action<float> OnGracePeriodUpdated;
    public static void TriggerGracePeriodUpdated(float severity) => OnGracePeriodUpdated?.Invoke(severity);

    // Audio Events
    public static event Action<SoundID> OnPlaySFXCommand;
    public static void TriggerPlaySFXCommand(SoundID id) => OnPlaySFXCommand?.Invoke(id);

    // Level Won Event
    public static event Action OnLevelWon;
    public static void TriggerLevelWon() => OnLevelWon?.Invoke();

    #region Pause State
    private static bool isPaused;
    public static bool IsPaused => isPaused;
    #endregion

    #region Chrono Timer Convenience Methods
    public static void StartTimer()
    {
        isPaused = false;
        TriggerChronoStateChanged(ChronoState.Ticking);
    }

    public static void PauseTimer()
    {
        isPaused = true;
        TriggerChronoStateChanged(ChronoState.WarnStop);
    }

    public static void ResumeTimer()
    {
        isPaused = false;
        TriggerChronoStateChanged(ChronoState.WarnGo);
    }
    #endregion

    private static void ResetCachedState()
    {
        CurrentGameState = GameState.MainMenu;
        CurrentChronoState = ChronoState.Ticking;
        isPaused = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAllListeners()
    {
        OnGameStateChanged = null;
        OnChronoStateChanged = null;
        OnGameOverTriggered = null;
        OnLevelTimerUpdated = null;
        OnGracePeriodUpdated = null;
        OnLevelWon = null;
        OnCameraShake = null;
        OnPlaySFXCommand = null;
        ResetCachedState();
    }
}

public enum GameState
{

    MainMenu,
    Loading,
    Booting,
    Gameplay,
    Paused,
    GameOver,
    LevelComplete,
    GameComplete
}

public enum ChronoState
{
    Ticking,
    WarnStop,
    Frozen,
    WarnGo
}

public enum GameOverReason
{
    IdleBomb,
    MotionBomb,
    TimeExpired
}