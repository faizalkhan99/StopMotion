// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// Controls Main Menu presentation. Exposes a customizable Back key in the Inspector
// /// to return to the root panel from any sub-menu.
// /// </summary>
// [DisallowMultipleComponent]
// public class MainMenuUIManager : MonoBehaviour
// {
//     [Header("Scene Configuration")]
//     [Tooltip("The exact build name of your gameplay scene.")]
//     [SerializeField] private string gameSceneName = "GameScene";

//     [Header("Input Settings")]
//     [Tooltip("The keyboard key used to return to the main root panel from sub-menus.")]
//     [SerializeField] private KeyCode backKey = KeyCode.Escape;

//     [Header("Panel References (CanvasGroups)")]
//     [SerializeField] private CanvasGroup mainMenuPanel;
//     [SerializeField] private CanvasGroup howToPlayPanel;
//     [SerializeField] private CanvasGroup creditsPanel;

//     [Header("Button Hooks")]
//     [SerializeField] private Button playButton;
//     [SerializeField] private Button howToPlayButton;
//     [SerializeField] private Button creditsButton;
//     [SerializeField] private Button quitButton;

//     private CanvasGroup currentActivePanel;

//     private void Awake()
//     {
//         HideAllPanelsImmediate();
//         ShowPanelImmediate(mainMenuPanel);

//         if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
//         if (howToPlayButton != null) howToPlayButton.onClick.AddListener(() => SwitchPanel(howToPlayPanel));
//         if (creditsButton != null) creditsButton.onClick.AddListener(() => SwitchPanel(creditsPanel));
//         if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
//     }

//     private void Update()
//     {
//         // Evaluates the customizable Inspector key to trigger navigation back to the root menu
//         if (Input.GetKeyDown(backKey))
//         {
//             if (currentActivePanel != null && currentActivePanel != mainMenuPanel && currentActivePanel.interactable)
//             {
//                 SwitchPanel(mainMenuPanel);
//             }
//         }
//     }

//     private void OnDestroy()
//     {
//         if (playButton != null) playButton.onClick.RemoveAllListeners();
//         if (howToPlayButton != null) howToPlayButton.onClick.RemoveAllListeners();
//         if (creditsButton != null) creditsButton.onClick.RemoveAllListeners();
//         if (quitButton != null) quitButton.onClick.RemoveAllListeners();
//     }

//     private void OnPlayClicked()
//     {
//         if (currentActivePanel != null)
//         {
//             currentActivePanel.interactable = false;
//         }

//         if (AsyncSceneLoader.Instance != null)
//         {
//             AsyncSceneLoader.Instance.LoadScene(gameSceneName, GameState.Gameplay);
//         }
//         else
//         {
//             Debug.LogError("<b>[MainMenuUIManager]</b> AsyncSceneLoader.Instance is missing! Make sure it exists in your startup scene.");
//         }
//     }

//     private void OnQuitClicked()
//     {
//         Debug.Log("<b>[MainMenuUIManager]</b> Application Quit requested.");
// #if UNITY_EDITOR
//         UnityEditor.EditorApplication.isPlaying = false;
// #else
//         Application.Quit();
// #endif
//     }

//     #region Panel Management (Zero-GC CanvasGroup Architecture)

//     public void SwitchPanel(CanvasGroup targetPanel)
//     {
//         if (targetPanel == null || targetPanel == currentActivePanel) return;

//         if (currentActivePanel != null)
//         {
//             HidePanelImmediate(currentActivePanel);
//         }

//         ShowPanelImmediate(targetPanel);
//     }

//     private void ShowPanelImmediate(CanvasGroup panel)
//     {
//         if (panel == null) return;
//         panel.alpha = 1f;
//         panel.interactable = true;
//         panel.blocksRaycasts = true;
//         currentActivePanel = panel;
//     }

//     private void HidePanelImmediate(CanvasGroup panel)
//     {
//         if (panel == null) return;
//         panel.alpha = 0f;
//         panel.interactable = false;
//         panel.blocksRaycasts = false;
//     }

//     private void HideAllPanelsImmediate()
//     {
//         HidePanelImmediate(mainMenuPanel);
//         HidePanelImmediate(howToPlayPanel);
//         HidePanelImmediate(creditsPanel);
//     }

//     #endregion
// }





















// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// /// <summary>
// /// Controls Main Menu presentation. Fully hides all CanvasGroups upon initiating 
// /// a scene load to prevent visual bleed-through during async loading transitions.
// /// </summary>
// [DisallowMultipleComponent]
// public class MainMenuUIManager : MonoBehaviour
// {
//     [Header("Scene Configuration")]
//     [Tooltip("The exact build name of your gameplay scene.")]
//     [SerializeField] private string gameSceneName = "GameScene";

//     [Header("Input Settings")]
//     [Tooltip("The keyboard key used to return to the main root panel from sub-menus.")]
//     [SerializeField] private KeyCode backKey = KeyCode.Escape;

//     [Header("Panel References (CanvasGroups)")]
//     [SerializeField] private UIPanelAnimator mainMenuPanel;
//     [SerializeField] private UIPanelAnimator howToPlayPanel;
//     [SerializeField] private UIPanelAnimator creditsPanel;

//     [Header("Button Hooks")]
//     [SerializeField] private Button playButton;
//     [SerializeField] private Button howToPlayButton;
//     [SerializeField] private Button creditsButton;
//     [SerializeField] private Button quitButton;

//     private UIPanelAnimator currentActivePanel;
//     private bool isTransitioning = false;

//     private void Awake()
//     {
//         isTransitioning = false;
//         HideAllPanelsImmediate();
//         ShowPanelImmediate(mainMenuPanel);

//         if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
//         if (howToPlayButton != null) howToPlayButton.onClick.AddListener(() => SwitchPanel(howToPlayPanel));
//         if (creditsButton != null) creditsButton.onClick.AddListener(() => SwitchPanel(creditsPanel));
//         if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
//     }

//     private void Update()
//     {
//         // Ignore back input if we are currently loading the gameplay scene
//         if (isTransitioning) return;

//         if (Input.GetKeyDown(backKey) || Input.GetKeyDown(KeyCode.Q)) // Allow Q as an alternative back key for convenience in webGL builds.
//         {
//             // if (currentActivePanel != null && currentActivePanel != mainMenuPanel && currentActivePanel.interactable)
//             {
//                 SwitchPanel(mainMenuPanel);
//             }
//         }
//     }

//     private void OnDestroy()
//     {
//         if (playButton != null) playButton.onClick.RemoveAllListeners();
//         if (howToPlayButton != null) howToPlayButton.onClick.RemoveAllListeners();
//         if (creditsButton != null) creditsButton.onClick.RemoveAllListeners();
//         if (quitButton != null) quitButton.onClick.RemoveAllListeners();
//     }

//     private void OnPlayClicked()
//     {
//         if (isTransitioning) return;
//         isTransitioning = true;

//         // 1. Immediately hide all panels (sets alpha = 0, interactable = false, blocksRaycasts = false)
//         // This guarantees menu buttons won't bleed through or sit on top of the loading screen!
//         HideAllPanelsImmediate();

//         // 2. Trigger the async transition
//         if (SceneLoader.Instance != null)
//         {
//             SceneLoader.Instance.LoadScene(gameSceneName, GameState.Gameplay);
//         }
//         else
//         {
//             Debug.LogError("<b>[MainMenuUIManager]</b> SceneLoader.Instance is missing! Make sure it exists in your startup scene.");
//         }
//     }

//     private void OnQuitClicked()
//     {
//         if (isTransitioning) return;

//         Debug.Log("<b>[MainMenuUIManager]</b> Application Quit requested.");
// #if UNITY_EDITOR
//         UnityEditor.EditorApplication.isPlaying = false;
// #else
//         Application.Quit();
// #endif
//     }

//     #region Panel Management (Zero-GC CanvasGroup Architecture)

//     public void SwitchPanel(UIPanelAnimator targetPanel)
//     {
//         if (isTransitioning || targetPanel == null || targetPanel == currentActivePanel) return;

//         if (currentActivePanel != null)
//         {
//             HidePanelImmediate(currentActivePanel);
//         }

//         ShowPanelImmediate(targetPanel);
//     }

//     private void ShowPanelImmediate(UIPanelAnimator panel)
//     {
//         if (panel == null) return;
//         panel.AnimateShow(); // Triggers the bouncy pop or slide!
//         currentActivePanel = panel;
//     }

//     private void HidePanelImmediate(UIPanelAnimator panel, bool immediate = false)
//     {
//         if (panel == null) return;
//         panel.AnimateHide(immediate); // Smoothly transitions out!
//     }

//     private void HideAllPanelsImmediate()
//     {
//         HidePanelImmediate(mainMenuPanel);
//         HidePanelImmediate(howToPlayPanel);
//         HidePanelImmediate(creditsPanel);
//     }

//     #endregion
// }















using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls Main Menu presentation. Fully hides all panels immediately upon initiating 
/// a scene load to prevent visual bleed-through during async loading transitions[cite: 13].
/// </summary>
[DisallowMultipleComponent]
public class MainMenuUIManager : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("The keyboard key used to return to the main root panel from sub-menus.")]
    [SerializeField] private KeyCode backKey = KeyCode.Escape;

    [Header("Panel References (UIPanelAnimators)")]
    [SerializeField] private UIPanelAnimator mainMenuPanel;
    [SerializeField] private UIPanelAnimator howToPlayPanel;
    [SerializeField] private UIPanelAnimator creditsPanel;

    [Header("Button Hooks")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    private UIPanelAnimator currentActivePanel;
    private bool isTransitioning = false;

    private void Awake()
    {
        isTransitioning = false;

        // Snap-hide all panels instantly on frame 1 without playing exit animations
        HideAllPanelsImmediate();
        ShowPanelAnimated(mainMenuPanel);

        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (howToPlayButton != null) howToPlayButton.onClick.AddListener(() => SwitchPanel(howToPlayPanel));
        if (creditsButton != null) creditsButton.onClick.AddListener(() => SwitchPanel(creditsPanel));
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void Update()
    {
        if (isTransitioning) return;

        // Allow Q as an alternative back key for convenience in WebGL/Editor builds[cite: 13]
        if (Input.GetKeyDown(backKey) || Input.GetKeyDown(KeyCode.Q))
        {
            // RESTORED GUARD: Only go back if we are on a sub-panel and not currently animating
            if (currentActivePanel != null && currentActivePanel != mainMenuPanel)
            {
                SwitchPanel(mainMenuPanel);
            }
        }
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (howToPlayButton != null) howToPlayButton.onClick.RemoveAllListeners();
        if (creditsButton != null) creditsButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
    }

    private void OnPlayClicked()
    {
        GameEventBus.TriggerPlaySFXCommand(SoundID.ButtonClick);
        if (isTransitioning) return;
        isTransitioning = true;

        // Instantly hide all UI so nothing bleeds over the loading screen
        HideAllPanelsImmediate();

        if (SceneLoader.Instance != null)
        {
            int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
            SceneLoader.Instance.LoadScene(nextBuildIndex, GameState.Gameplay);
        }
        else
        {
            Debug.LogError("<b>[MainMenuUIManager]</b> SceneLoader.Instance is missing! Make sure it exists in your startup scene[cite: 13].");
        }
    }

    private void OnQuitClicked()
    {
        GameEventBus.TriggerPlaySFXCommand(SoundID.ButtonClick);
        if (isTransitioning) return;

        Debug.Log("<b>[MainMenuUIManager]</b> Application Quit requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #region Panel Management

    public void SwitchPanel(UIPanelAnimator targetPanel)
    {
        GameEventBus.TriggerPlaySFXCommand(SoundID.ButtonClick);

        if (isTransitioning || targetPanel == null || targetPanel == currentActivePanel) return;

        if (currentActivePanel != null)
        {
            // Play smooth closing animation for the departing panel
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
        // FIXED: Explicitly passing 'true' so panels snap shut instantly without coroutines!
        HidePanel(mainMenuPanel, immediate: true);
        HidePanel(howToPlayPanel, immediate: true);
        HidePanel(creditsPanel, immediate: true);
    }

    #endregion
}