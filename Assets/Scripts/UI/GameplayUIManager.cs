using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Centralized switchboard for Gameplay UI. Mirrors the animated panel architecture of the Main Menu
/// and binds precise SFX triggers strictly to player-initiated actions.
/// </summary>
[DisallowMultipleComponent]
public class GameplayUIManager : MonoBehaviour
{
    [Header("State Manager")]
    [SerializeField] private GameStateManager stateManager;
    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Input Settings")]
    [Tooltip("Primary key used to pause/unpause the game.")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [Tooltip("Alternative key used to pause/unpause the game.")]
    [SerializeField] private KeyCode altPauseKey = KeyCode.Q;
    [Tooltip("Key used to restart the level from gameplay, pause, or game over.")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [Tooltip("Key used to return to the main menu.")]
    [SerializeField] private KeyCode mainMenuKey = KeyCode.M;

    [Header("Panel References (UIPanelAnimators)")]
    [SerializeField] private UIPanelAnimator gameplayPanel;
    [SerializeField] private UIPanelAnimator pauseMenuPanel;
    [SerializeField] private UIPanelAnimator gameOverPanel;
    [SerializeField] private UIPanelAnimator gameWinPanel;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Button Hooks")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private List<Button> mainMenuButton;
    [SerializeField] private Button restartButtonTwo;
    // [SerializeField] private Button mainMenuButtonTwo;

    private UIPanelAnimator currentActivePanel;
    private GameState currentGameState = GameState.Booting;

    private void Awake()
    {
        // Snap-hide all panels instantly on startup without playing closing animations
        HideAllPanelsImmediate();
        ShowPanelAnimated(gameplayPanel);
        // Bind the single serialized button if assigned[cite: 16]
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        // if (mainMenuButton != null)
        //     mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (restartButtonTwo != null)
            restartButtonTwo.onClick.AddListener(OnRestartClicked);
    //     if (mainMenuButtonTwo != null)
    //         mainMenuButtonTwo.onClick.AddListener(OnMainMenuClicked);

        AddOnClick(mainMenuButton);
    }

    private void OnEnable()
    {
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;

        if (pauseButton != null)
            pauseButton.onClick.RemoveAllListeners();
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    /// <summary>
    /// Monitors hardware input and routes actions contextually based on the macro game state[cite: 16].
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 1. Pause / Unpause Input (Escape or Q)[cite: 16]
        if (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(altPauseKey))
        {
            if (currentGameState == GameState.Gameplay)
                OnPauseClicked();
            else if (currentGameState == GameState.Paused)
                OnResumeClicked();
        }

        // 2. Restart Input (R) - Available during Gameplay, Pause, or Game Over[cite: 16]
        if (Input.GetKeyDown(restartKey))
        {
            if (currentGameState == GameState.Gameplay || currentGameState == GameState.Paused || currentGameState == GameState.GameOver || currentGameState == GameState.GameComplete)
                OnRestartClicked();
        }

        // 3. Main Menu Input (M) - Available during Gameplay, Pause, or Game Over[cite: 16]
        if (Input.GetKeyDown(mainMenuKey))
        {
            if (currentGameState == GameState.Gameplay || currentGameState == GameState.Paused || currentGameState == GameState.GameOver || currentGameState == GameState.GameComplete)
                OnMainMenuClicked();
        }
    }
    private void AddOnClick(List<Button> buttonList)
    {
        foreach (var item in buttonList)
        {
            item.onClick.AddListener(OnMainMenuClicked);
        }
    }
    #region Event Bus Receivers

    private void HandleGameStateChanged(GameState newState)
    {
        currentGameState = newState;

        // Automatically switch panels based on system state changes[cite: 16].
        // NOTE: No SFX is triggered here, preventing false audio when Game Over pops automatically[cite: 16]!
        switch (newState)
        {
            case GameState.Gameplay:
                SwitchPanel(gameplayPanel);
                break;

            case GameState.Paused:
                SwitchPanel(pauseMenuPanel);
                break;

            case GameState.GameOver:
                SwitchPanel(gameOverPanel);
                break;
            case GameState.GameComplete:
                SwitchPanel(gameWinPanel);
                break;
        }
    }

    private void HandleTimerUpdated(float timeRemaining)
    {
        if (timerText == null || currentGameState != GameState.Gameplay) return;

        float clampedTime = Mathf.Max(0f, timeRemaining);
        int minutes = (int)(clampedTime / 60);
        int seconds = (int)(clampedTime % 60);

        timerText.SetText("{0:00}:{1:00}", minutes, seconds);
    }

    #endregion

    #region Button & Input Actions (Public for Inspector Wiring)
    public void OnPauseButtonClicked()
    {
        if (currentGameState == GameState.Gameplay)
            OnPauseClicked();
        else if (currentGameState == GameState.Paused)
            OnResumeClicked();
    }

    public void OnPauseClicked()
    {
        PlayButtonClickAudio();

        if (stateManager != null)
            stateManager.TogglePause();
        else
            Debug.LogWarning($" State Manager Missing");
    }

    public void OnResumeClicked()
    {
        PlayButtonClickAudio();

        if (stateManager != null)
            stateManager.TogglePause();
        else
            Debug.LogWarning($" State Manager Missing");
    }

    public void OnRestartClicked()
    {
        PlayButtonClickAudio();

        Time.timeScale = 1.0f;
        AudioListener.pause = false;

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(currentSceneIndex, GameState.Gameplay);
        else
            SceneManager.LoadScene(currentSceneIndex);
    }

    public void OnMainMenuClicked()
    {
        PlayButtonClickAudio();

        Time.timeScale = 1.0f;
        AudioListener.pause = false;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(0, GameState.MainMenu);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Helper method to guarantee click audio only fires on deliberate user actions[cite: 16].
    /// </summary>
    private void PlayButtonClickAudio()
    {
        GameEventBus.TriggerPlaySFXCommand(SoundID.ButtonClick);
    }

    #endregion

    #region Panel Management (Mirrored from MainMenuUIManager)

    public void SwitchPanel(UIPanelAnimator targetPanel)
    {
        if (targetPanel == null || targetPanel == currentActivePanel) return;

        if (currentActivePanel != null)
        {
            HidePanel(currentActivePanel, immediate: false);
        }

        ShowPanelAnimated(targetPanel);
    }

    private void ShowPanelAnimated(UIPanelAnimator panel)
    {
        if (panel == null) return;
        panel.AnimateShow();
        currentActivePanel = panel;
    }

    private void HidePanel(UIPanelAnimator panel, bool immediate = false)
    {
        if (panel == null) return;
        panel.AnimateHide(immediate);
    }

    private void HideAllPanelsImmediate()
    {
        // Explicitly passing 'true' so panels snap shut instantly on startup[cite: 16]!
        HidePanel(gameplayPanel, immediate: true);
        HidePanel(pauseMenuPanel, immediate: true);
        HidePanel(gameOverPanel, immediate: true);
        HidePanel(gameWinPanel, immediate: true);
    }

    #endregion
}