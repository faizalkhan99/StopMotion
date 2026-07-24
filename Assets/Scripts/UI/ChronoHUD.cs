using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class ChronoHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag your TextMeshPro text component here to show the countdown[cite: 5].")]
    [SerializeField] private TextMeshProUGUI timerText;
    [Tooltip("A full-screen UI Image with a border/vignette sprite to frame the screen[cite: 5].")]
    [SerializeField] private Image screenBorderImage;

    [Header("Telegraph & State Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color warnStopColor = new Color(1f, 0.5f, 0f, 0.8f);
    [SerializeField] private Color frozenColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color warnGoColor = new Color(0f, 1f, 0.2f, 0.8f);

    [Header("Audio Juice")]
    [SerializeField] private AudioSource metronomeAudio;
    [SerializeField] private AudioSource heartbeatAudio;

    private Color targetBorderColor;
    private float colorTransitionSpeed = 15f;

    private void Awake()
    {
        if (screenBorderImage != null)
        {
            screenBorderImage.color = normalColor;
            targetBorderColor = normalColor;
        }

        if (heartbeatAudio != null) heartbeatAudio.Stop();
    }

    private void OnEnable()
    {
        GameEventBus.OnLevelTimerUpdated += HandleTimerUpdated;
        GameEventBus.OnChronoStateChanged += HandleChronoStateChanged;
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEventBus.OnLevelTimerUpdated -= HandleTimerUpdated;
        GameEventBus.OnChronoStateChanged -= HandleChronoStateChanged;
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        // Fallback sync to ensure HUD initializes correctly even if enabled late
        HandleChronoStateChanged(ChronoState.Ticking);
    }

    private void Update()
    {
        if (screenBorderImage != null && screenBorderImage.color != targetBorderColor)
        {
            screenBorderImage.color = Color.Lerp(screenBorderImage.color, targetBorderColor, Time.deltaTime * colorTransitionSpeed);
        }
    }

    private void HandleTimerUpdated(float timeRemaining)
    {
        if (timerText == null) return;

        float clampedTime = Mathf.Max(0f, timeRemaining);
        int minutes = (int)(clampedTime / 60);
        int seconds = (int)(clampedTime % 60);

        timerText.SetText("{0:00}:{1:00}", minutes, seconds);
    }

    private void HandleChronoStateChanged(ChronoState newState)
    {
        switch (newState)
        {
            case ChronoState.Ticking:
                targetBorderColor = normalColor;
                colorTransitionSpeed = 5f;

                if (metronomeAudio != null && !metronomeAudio.isPlaying) metronomeAudio.Play();
                if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
                break;

            case ChronoState.WarnStop:
                targetBorderColor = warnStopColor;
                colorTransitionSpeed = 25f;
                break;

            case ChronoState.Frozen:
                targetBorderColor = frozenColor;

                if (metronomeAudio != null && metronomeAudio.isPlaying) metronomeAudio.Pause();
                if (heartbeatAudio != null && !heartbeatAudio.isPlaying) heartbeatAudio.Play();
                break;

            case ChronoState.WarnGo:
                targetBorderColor = warnGoColor;
                colorTransitionSpeed = 25f;
                break;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Paused || newState == GameState.GameOver)
        {
            if (metronomeAudio != null) metronomeAudio.Pause();
            if (heartbeatAudio != null) heartbeatAudio.Pause();
        }
    }
}