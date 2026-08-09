// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;
// using TMPro;

// /// <summary>
// /// Centralized switchboard for Gameplay UI. Exposes a customizable Back/Toggle key in the Inspector
// /// to pause, resume, or return to menu based on the macro GameState[cite: 14].
// /// </summary>
// [DisallowMultipleComponent]
// public class GameplayUIManager : MonoBehaviour
// {
//     [Header("Scene Configuration")]
//     [SerializeField] private string mainMenuSceneName = "MainMenuScene";

//     [Header("Input Settings")]
//     [Tooltip("The keyboard key used to pause gameplay, resume from pause, or exit from Game Over[cite: 14].")]
//     [SerializeField] private KeyCode actionKey = KeyCode.Q;

//     [Header("Panel References (UIPanelAnimators)")]
//     [SerializeField] private UIPanelAnimator gameplayPanel;
//     [SerializeField] private UIPanelAnimator pauseMenuPanel;
//     [SerializeField] private UIPanelAnimator gameOverPanel;

//     [Header("HUD Elements")]
//     [SerializeField] private TextMeshProUGUI timerText;

//     [Header("Button Hooks")]
//     [SerializeField] private Button pauseButton;
//     [SerializeField] private Button resumeButton;
//     [SerializeField] private Button[] restartButtons;
//     [SerializeField] private Button[] mainMenuButtons;

//     private UIPanelAnimator currentActivePanel;
//     private GameState currentGameState = GameState.Booting;

//     private void Awake()
//     {
//         // Snap-hide all panels instantly on startup
//         HideAllPanelsImmediate();
//         ShowPanelAnimated(gameplayPanel);

//         if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
//         if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);

//         for (int i = 0; i < restartButtons.Length; i++)
//         {
//             if (restartButtons[i] != null) restartButtons[i].onClick.AddListener(OnRestartClicked);
//         }

//         for (int i = 0; i < mainMenuButtons.Length; i++)
//         {
//             if (mainMenuButtons[i] != null) mainMenuButtons[i].onClick.AddListener(OnMainMenuClicked);
//         }
//     }

//     private void OnEnable()
//     {
//         GameEventBus.OnGameStateChanged += HandleGameStateChanged;
//         GameEventBus.OnLevelTimerUpdated += HandleTimerUpdated;
//     }

//     private void OnDisable()
//     {
//         GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
//         GameEventBus.OnLevelTimerUpdated -= HandleTimerUpdated;

//         if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
//         if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();

//         for (int i = 0; i < restartButtons.Length; i++)
//         {
//             if (restartButtons[i] != null) restartButtons[i].onClick.RemoveAllListeners();
//         }

//         for (int i = 0; i < mainMenuButtons.Length; i++)
//         {
//             if (mainMenuButtons[i] != null) mainMenuButtons[i].onClick.RemoveAllListeners();
//         }
//     }

//     private void Update()
//     {
//         if (Input.GetKeyDown(actionKey) || Input.GetKeyDown(KeyCode.Q)) // Added a hardcoded fallback for the 'Q' key
//         {
//             switch (currentGameState)
//             {
//                 case GameState.Gameplay:
//                     OnPauseClicked();
//                     break;

//                 case GameState.Paused:
//                     OnResumeClicked();
//                     break;

//                 case GameState.GameOver:
//                     OnMainMenuClicked();
//                     break;
//             }
//         }
//     }

//     #region Event Bus Receivers

//     private void HandleGameStateChanged(GameState newState)
//     {
//         currentGameState = newState;

//         switch (newState)
//         {
//             case GameState.Gameplay:
//                 SwitchPanel(gameplayPanel);
//                 break;

//             case GameState.Paused:
//                 SwitchPanel(pauseMenuPanel);
//                 break;

//             case GameState.GameOver:
//                 SwitchPanel(gameOverPanel);
//                 break;
//         }
//     }

//     private void HandleTimerUpdated(float timeRemaining)
//     {
//         if (timerText == null || currentGameState != GameState.Gameplay) return;

//         float clampedTime = Mathf.Max(0f, timeRemaining);
//         int minutes = (int)(clampedTime / 60);
//         int seconds = (int)(clampedTime % 60);

//         timerText.SetText("{0:00}:{1:00}", minutes, seconds);
//     }

//     #endregion

//     #region Button Actions

//     private void OnPauseClicked()
//     {
//         var stateManager = FindFirstObjectByType<GameStateManager>();
//         if (stateManager != null)
//         {
//             stateManager.TogglePause();
//         }
//         else
//         {
//             GameEventBus.TriggerGameStateChanged(GameState.Paused);
//         }
//     }

//     private void OnResumeClicked()
//     {
//         var stateManager = FindFirstObjectByType<GameStateManager>();
//         if (stateManager != null)
//         {
//             stateManager.TogglePause();
//         }
//         else
//         {
//             GameEventBus.TriggerGameStateChanged(GameState.Gameplay);
//         }
//     }

//     private void OnRestartClicked()
//     {
//         Time.timeScale = 1.0f;
//         AudioListener.pause = false;

//         string currentSceneName = SceneManager.GetActiveScene().name;

//         // FIXED: Using AsyncSceneLoader to match your Singleton declaration[cite: 7]
//         if (SceneLoader.Instance != null)
//         {
//             SceneLoader.Instance.LoadScene(currentSceneName, GameState.Gameplay);
//         }
//         else
//         {
//             SceneManager.LoadScene(currentSceneName);
//         }
//     }

//     private void OnMainMenuClicked()
//     {
//         Time.timeScale = 1.0f;
//         AudioListener.pause = false;

//         if (SceneLoader.Instance != null)
//         {
//             SceneLoader.Instance.LoadScene(mainMenuSceneName, GameState.MainMenu);
//         }
//         else
//         {
//             SceneManager.LoadScene(mainMenuSceneName);
//         }
//     }

//     #endregion

//     #region Panel Management

//     public void SwitchPanel(UIPanelAnimator targetPanel)
//     {
//         if (targetPanel == null || targetPanel == currentActivePanel) return;

//         if (currentActivePanel != null)
//         {
//             HidePanel(currentActivePanel, immediate: false);
//         }

//         ShowPanelAnimated(targetPanel);
//     }

//     private void ShowPanelAnimated(UIPanelAnimator panel)
//     {
//         if (panel == null) return;
//         panel.AnimateShow();
//         currentActivePanel = panel;
//     }

//     private void HidePanel(UIPanelAnimator panel, bool immediate = false)
//     {
//         if (panel == null) return;
//         panel.AnimateHide(immediate);
//     }

//     private void HideAllPanelsImmediate()
//     {
//         // FIXED: Explicitly passing 'true' so panels snap shut instantly on startup!
//         HidePanel(gameplayPanel, immediate: true);
//         HidePanel(pauseMenuPanel, immediate: true);
//         HidePanel(gameOverPanel, immediate: true);
//     }

//     #endregion
// }































































using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Centralized switchboard for Gameplay UI. Mirrors the animated panel architecture of the Main Menu
/// and binds precise SFX triggers strictly to player-initiated actions.
/// </summary>
[DisallowMultipleComponent]
public class GameplayUIManager : MonoBehaviour
{
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
    [SerializeField] private UIPanelAnimator gameCompletePanel;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Button Hooks")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButtonTwo;
    [SerializeField] private Button mainMenuButtonTwo;

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
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (restartButtonTwo != null)
            restartButtonTwo.onClick.AddListener(OnRestartClicked);
        if (mainMenuButtonTwo != null)
            mainMenuButtonTwo.onClick.AddListener(OnMainMenuClicked);
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
                SwitchPanel(gameCompletePanel);
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

        // var stateManager = FindAnyObjectByType<GameStateManager>();
        // if (stateManager != null)
        //     stateManager.TogglePause();
        // else
            GameEventBus.TriggerGameStateChanged(GameState.Paused);
    }

    public void OnResumeClicked()
    {
        PlayButtonClickAudio();

        // var stateManager = FindAnyObjectByType<GameStateManager>();
        // if (stateManager != null)
        //     stateManager.TogglePause();
        // else
            GameEventBus.TriggerGameStateChanged(GameState.Gameplay);
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
        HidePanel(gameCompletePanel, immediate: true);
    }

    #endregion
}