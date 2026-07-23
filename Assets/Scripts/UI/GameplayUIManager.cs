using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUIManager : MonoBehaviour
{
    [Header("--- UI PANELS ---")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("--- HUD PANEL ELEMENTS ---")]
    [SerializeField] private TextMeshProUGUI currentCoinsText;
    [Tooltip("Drag the Heart Image GameObjects from the HUD hierarchy here, in left-to-right order.")]
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Button pauseButton;

    [Header("--- PAUSE MENU BUTTONS ---")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseMenuButton;

    [Header("--- GAME OVER ELEMENTS ---")]
    [SerializeField] private Button gameOverRestartButton;
    [SerializeField] private Button gameOverMenuButton;
    [Tooltip("Displays the score achieved during this specific run.")]
    [SerializeField] private TextMeshProUGUI gameOverCoinsText;

    [Header("--- LIVES UI SETTINGS ---")]
    [SerializeField] private Sprite filledHeartSprite;
    [SerializeField] private Sprite hollowHeartSprite;
    [Tooltip("Color of a heart when the player still has that life.")]
    [SerializeField] private Color fullHeartColor = Color.white;
    [Tooltip("Color of a heart when that life is lost.")]
    [SerializeField] private Color emptyHeartColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        private int _cachedTotalCoins;

private void OnEnable()
    {
        // Subscribe to state changes and gameplay events
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnLivesUpdated += UpdateHearts;
        GameEvents.OnTotalCoinsUpdated += UpdateCoins;

        // Button listeners - HUD & Pause
        if (pauseButton) pauseButton.onClick.AddListener(OnPauseClicked);
        if (resumeButton) resumeButton.onClick.AddListener(OnResumeClicked);
        if (pauseRestartButton) pauseRestartButton.onClick.AddListener(OnRestartClicked);
        if (pauseMenuButton) pauseMenuButton.onClick.AddListener(OnMenuClicked);

        // Button listeners - Game Over
        if (gameOverRestartButton) gameOverRestartButton.onClick.AddListener(OnRestartClicked);
        if (gameOverMenuButton) gameOverMenuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnLivesUpdated -= UpdateHearts;
        GameEvents.OnTotalCoinsUpdated -= UpdateCoins;
    

        // Cleanup button listeners
        if (pauseButton) pauseButton.onClick.RemoveAllListeners();
        if (resumeButton) resumeButton.onClick.RemoveAllListeners();
        if (pauseRestartButton) pauseRestartButton.onClick.RemoveAllListeners();
        if (pauseMenuButton) pauseMenuButton.onClick.RemoveAllListeners();
        if (gameOverRestartButton) gameOverRestartButton.onClick.RemoveAllListeners();
        if (gameOverMenuButton) gameOverMenuButton.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        currentCoinsText.SetText("0"); // Initialize coin display
        // Force the UI into the correct initial state when the scene starts
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    // --- STATE MANAGEMENT ---
    private void HandleGameStateChanged(GameState newState)
    {
        if (hudPanel) hudPanel.SetActive(newState == GameState.Playing);
        if (pausePanel) pausePanel.SetActive(newState == GameState.Paused);
        if (gameOverPanel) gameOverPanel.SetActive(newState == GameState.GameOver);

        if(newState == GameState.GameOver && gameOverCoinsText != null)
        {
            gameOverCoinsText.SetText("Coins CoLlected: {0}", _cachedTotalCoins); // Display the final score on Game Over
        }
    }

    // --- UI UPDATE LOGIC ---
    private void UpdateHearts(int currentLives)
    {
        if (heartImages == null) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            bool hasLife = i < currentLives;

            // Apply sprite swapping if sprites are assigned in Inspector
            if (filledHeartSprite != null && hollowHeartSprite != null)
            {
                heartImages[i].sprite = hasLife ? filledHeartSprite : hollowHeartSprite;
            }

            // Apply color tinting/dimming for added visual feedback
            heartImages[i].color = hasLife ? fullHeartColor : emptyHeartColor;
        }
    }


    private void UpdateCoins(int total)
    {
        _cachedTotalCoins = total;
        if (currentCoinsText != null)
            currentCoinsText.SetText("{0}", total);
    }

    // --- BUTTON ACTIONS ---
    private void OnPauseClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.Paused);
    }

    private void OnResumeClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.Playing);
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadGameSceneAsync();
    }

    private void OnMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenuAsync();
    }
}