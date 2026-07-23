using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CENTRAL STATE MANAGER: Single source of truth for macro game flow.
/// Guarantees an unconditional initial broadcast to prevent initialization deadlocks.
/// </summary>
[DisallowMultipleComponent]
public class GameStateManager : MonoBehaviour
{
    [Header("Read-Only State View")]
    [SerializeField] private GameState currentGameState = GameState.Booting;

    [Header("Performance & Target Settings")]
    [Tooltip("Locks frame rate to conserve battery and prevent thermal throttling on mobile[cite: 6].")]
    [SerializeField] private int targetFrameRate = 60;

    public GameState CurrentState => currentGameState;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
    }

    private void OnEnable()
    {
        GameEventBus.OnGameOverTriggered += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEventBus.OnGameOverTriggered -= HandleGameOver;
        Time.timeScale = 1.0f;
    }

    private void Start()
    {
        // Unconditional Bootstrapping: Force the initial broadcast without guard clauses
        // This guarantees all subscribers (Visuals, HUD, AI) synchronize immediately on frame 1.
        ForceBootState(GameState.Gameplay);
    }

    /// <summary>
    /// Forces an initial state transition and broadcast during system startup.
    /// </summary>
    private void ForceBootState(GameState initialState)
    {
        currentGameState = initialState;
        ApplyStateSystemRules(initialState);
        GameEventBus.TriggerGameStateChanged(initialState);
    }

    /// <summary>
    /// The ONLY method authorized to change the macro game state during gameplay[cite: 6].
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        currentGameState = newState;
        ApplyStateSystemRules(newState);
        GameEventBus.TriggerGameStateChanged(newState);
    }

    private void ApplyStateSystemRules(GameState state)
    {
        switch (state)
        {
            case GameState.Gameplay:
                Time.timeScale = 1.0f;
                AudioListener.pause = false;
                break;

            case GameState.Paused:
                Time.timeScale = 0.0f;
                AudioListener.pause = true;
                break;

            case GameState.GameOver:
                Time.timeScale = 1.0f;
                break;

            case GameState.LevelComplete:
                Time.timeScale = 0.5f;
                break;

            default:
                Time.timeScale = 1.0f;
                break;
        }
    }

    private void HandleGameOver(GameOverReason reason)
    {
        Debug.Log($"<b><color=red>[GAME OVER]</color></b> Triggered by: {reason}");
        SetGameState(GameState.GameOver);
    }

    #region Public UI Hookups
    public void TogglePause()
    {
        if (currentGameState == GameState.Gameplay)
            SetGameState(GameState.Paused);
        else if (currentGameState == GameState.Paused)
            SetGameState(GameState.Gameplay);
    }

    public void RetryLevel()
    {
        Time.timeScale = 1.0f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1.0f;
        AudioListener.pause = false;
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextSceneIndex);
        else
            Debug.LogWarning("<b>[GameStateManager]</b> No further scenes in Build Settings!");
    }
    #endregion
}