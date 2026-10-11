using System;
using System.Dynamic;
using NUnit.Framework;
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
    public static event Action OnReverseVines;
    public static void TriggerReverseVines() => OnReverseVines?.Invoke();


    // Continuous Feedback & UI Events
    public static event Action<float> OnLevelTimerUpdated;
    public static void TriggerLevelTimerUpdated(float timeRemaining) => OnLevelTimerUpdated?.Invoke(timeRemaining);
    public static event Action<float> OnLevelDurationUpdated;
    public static void TriggerLevelDurationUpdated(float timeRemaining) => OnLevelDurationUpdated?.Invoke(timeRemaining);
    public static event Action OnCameraShake;
    public static void TriggerCameraShake() => OnCameraShake?.Invoke();
    public static event Action OnDelayEnd;
    public static void StartShaderTimer() => OnDelayEnd?.Invoke();

    /// <summary>Fires during a rule violation. Value is normalized 0.0 (safe) to 1.0 (explosion)[cite: 7].</summary>
    public static event Action<float> OnGracePeriodUpdated;
    public static void TriggerGracePeriodUpdated(float severity) => OnGracePeriodUpdated?.Invoke(severity);

    // Audio Events
    public static event Action<SoundID> OnPlaySFXCommand;
    public static void TriggerPlaySFXCommand(SoundID id) => OnPlaySFXCommand?.Invoke(id);

    // Level Won Event
    public static event Action OnLevelWon;
    public static void TriggerLevelWon() => OnLevelWon?.Invoke();

    #region Lives & Checkpoints
    public static int CurrentLives { get; private set; } = 3;
    public static event Action<int> OnLivesChanged;
    public static void TriggerLivesChanged(int lives)
    {
        CurrentLives = lives;
        OnLivesChanged?.Invoke(lives);
    }
    public static event Action<Vector3> OnCheckpointReached;
    public static void TriggerCheckpointReached(Vector3 pos) => OnCheckpointReached?.Invoke(pos);
    public static event Action OnPlayerRespawnStarted;
    public static void TriggerPlayerRespawnStarted() => OnPlayerRespawnStarted?.Invoke();
    public static event Action OnPlayerRespawned;
    public static void TriggerPlayerRespawned() => OnPlayerRespawned?.Invoke();
    /// <summary>Fired when a hazard or fall should cost a life. LivesManager consumes it.</summary>
    public static event Action OnHazardHit;
    public static void TriggerHazardHit() => OnHazardHit?.Invoke();
    /// <summary>Fired by LivesManager when lives remain. RespawnManager consumes it.</summary>
    public static event Action OnRespawnRequested;
    public static void TriggerRespawnRequested() => OnRespawnRequested?.Invoke();
    /// <summary>Fired by RespawnManager when post-respawn i-frames end.</summary>
    public static event Action OnPlayerVulnerable;
    public static void TriggerPlayerVulnerable() => OnPlayerVulnerable?.Invoke();

    #endregion

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

    #region Player Dash
    public static event Action OnPlayerContactWithItem;
    public static void TriggerPlayerContactWithItem() => OnPlayerContactWithItem?.Invoke();
    public static event Action OnPlayerDash;
    public static void TriggerPlayerDash() => OnPlayerDash?.Invoke();
    #endregion

    // Jump squash start – called when the player presses Jump (keyboard) to show the anticipation squash
    public static event Action<bool> OnPlayerJumpSquash;
    public static void TriggerPlayerJumpSquash(bool isDescending) => OnPlayerJumpSquash?.Invoke(isDescending);

    #region Player Face
    public static event Action<Playerface> OnPlayerFaceChange;
    public static void TriggerPlayerFaceChange(Playerface playerface) => OnPlayerFaceChange?.Invoke(playerface);
    // OnPlayerIdle removed — idle is now visual-only polling via PlayerController.IsIdle in PlayerVisuals.LateUpdate()
    public static event Action OnPlayerBlinkComplete;
    public static void TriggerOnPlayerBlinkComplete() => OnPlayerBlinkComplete?.Invoke();

    // Jump squash complete – fires after the jump‑anticipation squash animation ends
    public static event Action OnPlayerJumpSquashComplete;
    public static void TriggerPlayerJumpSquashComplete() => OnPlayerJumpSquashComplete?.Invoke();
    #endregion
#region  VFX
    public static event Action<Vector2> OnPlayerGroundImpact;
    public static void TriggerGroundImpact(Vector2 pos) => OnPlayerGroundImpact?.Invoke(pos);
#endregion
    private static void ResetCachedState()
    {
        CurrentGameState = GameState.MainMenu;
        CurrentChronoState = ChronoState.Ticking;
        CurrentLives = 3;
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
        OnPlayerJumpSquash = null;
        OnPlayerJumpSquashComplete = null;
        OnPlayerFaceChange = null;
        OnPlayerBlinkComplete = null;
        OnPlayerContactWithItem = null;
        OnPlayerDash = null;
        OnDelayEnd = null;
        OnReverseVines = null;
        OnLevelDurationUpdated = null;
        OnLivesChanged = null;
        OnCheckpointReached = null;
        OnPlayerRespawnStarted = null;
        OnPlayerRespawned = null;
        OnHazardHit = null;
        OnRespawnRequested = null;
        OnPlayerVulnerable = null;
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
    TimeExpired,
    Hazard
}

public enum Playerface
{
    Idle,
    Moving,
    Dash,
    JumpUp,
    JumpDown,
    FallDown_Impact,
    Die,
    Blink

}