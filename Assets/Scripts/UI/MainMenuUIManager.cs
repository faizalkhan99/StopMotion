using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The root container holding Play, How To Play, Credits, etc.")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _howToPlayPanel;
    [SerializeField] private GameObject _creditsPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _howToPlayButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _urlButton; // E.g., Studio Website, Discord, or Wishlist page
    [SerializeField] private Button _quitButton;

    [Header("Panel Close / Back Buttons")]
    [SerializeField] private Button _howToPlayBackButton;
    [SerializeField] private Button _creditsBackButton;

    [Header("External Links")]
    [SerializeField] private string defaultUrl;

    private void OnEnable()
    {
        // Main menu navigation
        _playButton.onClick.AddListener(OnPlayClicked);
        _howToPlayButton.onClick.AddListener(OnHowToPlayClicked);
        _creditsButton.onClick.AddListener(OnCreditsClicked);
        _urlButton.onClick.AddListener(OnDefaultUrlClicked);
        _quitButton.onClick.AddListener(OnQuitClicked);

        // Sub-panel back navigation
        if (_howToPlayBackButton != null) _howToPlayBackButton.onClick.AddListener(ShowMainMenu);
        if (_creditsBackButton != null) _creditsBackButton.onClick.AddListener(ShowMainMenu);
    }

    private void OnDisable()
    {
        _playButton.onClick.RemoveListener(OnPlayClicked);
        _howToPlayButton.onClick.RemoveListener(OnHowToPlayClicked);
        _creditsButton.onClick.RemoveListener(OnCreditsClicked);
        _urlButton.onClick.RemoveListener(OnDefaultUrlClicked);
        _quitButton.onClick.RemoveListener(OnQuitClicked);

        if (_howToPlayBackButton != null) _howToPlayBackButton.onClick.RemoveListener(ShowMainMenu);
        if (_creditsBackButton != null) _creditsBackButton.onClick.RemoveListener(ShowMainMenu);
    }

    private void Start()
    {
        // Ensure a predictable, clean initial state on boot
        ShowMainMenu();
    }

    // --- NAVIGATION STATE MACHINE ---

    private void ShowMainMenu()
    {
        SetPanelStates(showMain: true, showHowToPlay: false, showCredits: false);
    }

    public void OnPlayClicked()
    {
        // Defensive Design: Lock UI inputs instantly to prevent double-tap loading bugs
        SetAllButtonsInteractable(false);

        // Call the brain to do the heavy lifting
        GameManager.Instance.LoadGameSceneAsync();
    }

    public void OnHowToPlayClicked()
    {
        SetPanelStates(showMain: false, showHowToPlay: true, showCredits: false);
    }

    public void OnCreditsClicked()
    {
        SetPanelStates(showMain: false, showHowToPlay: false, showCredits: true);
    }

    // --- EXTERNAL LINK LOGIC ---

    private void OnDefaultUrlClicked()
    {
        OpenURL(defaultUrl);
    }

    /// <summary>
    /// Opens an external web link. Public so it can also be hooked up directly 
    /// via Unity Inspector OnClick() events for dynamic community/promo buttons.
    /// </summary>
    public void OpenURL(string urlToOpen)
    {
        if (string.IsNullOrEmpty(urlToOpen))
        {
            Debug.LogWarning("[MainMenuUIManager] Attempted to open an empty URL!");
            return;
        }

        Debug.Log($"[MainMenuUIManager] Opening external link: {urlToOpen}");
        Application.OpenURL(urlToOpen);
    }

    public void OnQuitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- UI ARCHITECTURE HELPERS ---

    /// <summary>
    /// Centralizes panel state management to prevent UI overlapping bugs and raycast blockers.
    /// </summary>
    private void SetPanelStates(bool showMain, bool showHowToPlay, bool showCredits)
    {
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(showMain);
        if (_howToPlayPanel != null) _howToPlayPanel.SetActive(showHowToPlay);
        if (_creditsPanel != null) _creditsPanel.SetActive(showCredits);
    }

    private void SetAllButtonsInteractable(bool state)
    {
        _playButton.interactable = state;
        _howToPlayButton.interactable = state;
        _creditsButton.interactable = state;
        _urlButton.interactable = state;
        _quitButton.interactable = state;
    }
}