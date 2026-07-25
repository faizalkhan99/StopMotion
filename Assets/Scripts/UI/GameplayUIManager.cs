using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Centralized switchboard for Gameplay UI. Exposes a customizable Back/Toggle key in the Inspector
/// to pause, resume, or return to menu based on the macro GameState.
/// </summary>
[DisallowMultipleComponent]
public class GameplayUIManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Input Settings")]
    [Tooltip("The keyboard key used to pause gameplay, resume from pause, or exit from Game Over.")]
    [SerializeField] private KeyCode actionKey = KeyCode.Q;

    [Header("Panel References (CanvasGroups)")]
    [SerializeField] private CanvasGroup gameplayPanel;
    [SerializeField] private CanvasGroup pauseMenuPanel;
    [SerializeField] private CanvasGroup gameOverPanel;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Button Hooks")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button[] restartButtons;   
    [SerializeField] private Button[] mainMenuButtons;  

    private CanvasGroup currentActivePanel;
    private GameState currentGameState = GameState.Booting;

    private void Awake()
    {
        HideAllPanelsImmediate();
        ShowPanelImmediate(gameplayPanel);

        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);

        for (int i = 0; i < restartButtons.Length; i++)
        {
            if (restartButtons[i] != null) restartButtons[i].onClick.AddListener(OnRestartClicked);
        }

        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] != null) mainMenuButtons[i].onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void OnEnable()
    {
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
        GameEventBus.OnLevelTimerUpdated += HandleTimerUpdated;
    }

    private void OnDisable()
    {
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
        GameEventBus.OnLevelTimerUpdated -= HandleTimerUpdated;

        if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
        if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
        
        for (int i = 0; i < restartButtons.Length; i++)
        {
            if (restartButtons[i] != null) restartButtons[i].onClick.RemoveAllListeners();
        }

        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] != null) mainMenuButtons[i].onClick.RemoveAllListeners();
        }
    }

    private void Update()
    {
        // Uses your Inspector-assigned key to route actions contextually
        if (Input.GetKeyDown(actionKey))
        {
            switch (currentGameState)
            {
                case GameState.Gameplay:
                    OnPauseClicked();
                    break;

                case GameState.Paused:
                    OnResumeClicked();
                    break;

                case GameState.GameOver:
                    OnMainMenuClicked();
                    break;
            }
        }
    }

    #region Event Bus Receivers

    private void HandleGameStateChanged(GameState newState)
    {
        currentGameState = newState;

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

    #region Button Actions

    private void OnPauseClicked()
    {
        var stateManager = FindFirstObjectByType<GameStateManager>();
        if (stateManager != null)
        {
            stateManager.TogglePause();
        }
        else
        {
            GameEventBus.TriggerGameStateChanged(GameState.Paused);
        }
    }

    private void OnResumeClicked()
    {
        var stateManager = FindFirstObjectByType<GameStateManager>();
        if (stateManager != null)
        {
            stateManager.TogglePause();
        }
        else
        {
            GameEventBus.TriggerGameStateChanged(GameState.Gameplay);
        }
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1.0f;
        AudioListener.pause = false;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(currentSceneName, GameState.Gameplay);
        }
        else
        {
            SceneManager.LoadScene(currentSceneName);
        }
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1.0f;
        AudioListener.pause = false;

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(mainMenuSceneName, GameState.MainMenu);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    #endregion

    #region Panel Management (Zero-GC CanvasGroup Architecture)

    public void SwitchPanel(CanvasGroup targetPanel)
    {
        if (targetPanel == null || targetPanel == currentActivePanel) return;

        if (currentActivePanel != null)
        {
            HidePanelImmediate(currentActivePanel);
        }

        ShowPanelImmediate(targetPanel);
    }

    private void ShowPanelImmediate(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
        currentActivePanel = panel;
    }

    private void HidePanelImmediate(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
    }

    private void HideAllPanelsImmediate()
    {
        HidePanelImmediate(gameplayPanel);
        HidePanelImmediate(pauseMenuPanel);
        HidePanelImmediate(gameOverPanel);
    }

    #endregion
}