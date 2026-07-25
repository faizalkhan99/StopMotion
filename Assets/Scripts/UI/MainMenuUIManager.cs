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





















using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls Main Menu presentation. Fully hides all CanvasGroups upon initiating 
/// a scene load to prevent visual bleed-through during async loading transitions.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuUIManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The exact build name of your gameplay scene.")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Input Settings")]
    [Tooltip("The keyboard key used to return to the main root panel from sub-menus.")]
    [SerializeField] private KeyCode backKey = KeyCode.Escape;

    [Header("Panel References (CanvasGroups)")]
    [SerializeField] private CanvasGroup mainMenuPanel;
    [SerializeField] private CanvasGroup howToPlayPanel;
    [SerializeField] private CanvasGroup creditsPanel;

    [Header("Button Hooks")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    private CanvasGroup currentActivePanel;
    private bool isTransitioning = false;

    private void Awake()
    {
        isTransitioning = false;
        HideAllPanelsImmediate();
        ShowPanelImmediate(mainMenuPanel);

        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (howToPlayButton != null) howToPlayButton.onClick.AddListener(() => SwitchPanel(howToPlayPanel));
        if (creditsButton != null) creditsButton.onClick.AddListener(() => SwitchPanel(creditsPanel));
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void Update()
    {
        // Ignore back input if we are currently loading the gameplay scene
        if (isTransitioning) return;

        if (Input.GetKeyDown(backKey) || Input.GetKeyDown(KeyCode.Q)) // Allow Q as an alternative back key for convenience in webGL builds.
        {
            if (currentActivePanel != null && currentActivePanel != mainMenuPanel && currentActivePanel.interactable)
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
        if (isTransitioning) return;
        isTransitioning = true;

        // 1. Immediately hide all panels (sets alpha = 0, interactable = false, blocksRaycasts = false)
        // This guarantees menu buttons won't bleed through or sit on top of the loading screen!
        HideAllPanelsImmediate();

        // 2. Trigger the async transition
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(gameSceneName, GameState.Gameplay);
        }
        else
        {
            Debug.LogError("<b>[MainMenuUIManager]</b> SceneLoader.Instance is missing! Make sure it exists in your startup scene.");
        }
    }

    private void OnQuitClicked()
    {
        if (isTransitioning) return;

        Debug.Log("<b>[MainMenuUIManager]</b> Application Quit requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #region Panel Management (Zero-GC CanvasGroup Architecture)

    public void SwitchPanel(CanvasGroup targetPanel)
    {
        if (isTransitioning || targetPanel == null || targetPanel == currentActivePanel) return;

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
        HidePanelImmediate(mainMenuPanel);
        HidePanelImmediate(howToPlayPanel);
        HidePanelImmediate(creditsPanel);
    }

    #endregion
}