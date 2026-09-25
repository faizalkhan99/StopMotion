using UnityEngine;
using UnityEngine.InputSystem;

public class TimerController : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float totalTime = 60f;
    [SerializeField] private bool autoStart = true;

    [SerializeField] private float timeRemaining;
    private bool isRunning;
    private bool wasStarted = false;
    private ITimerInfluence influence;

    public float TimeRemaining => timeRemaining;
    public float NormalizedProgress => Mathf.Clamp01(timeRemaining / totalTime);
    public bool IsRunning => isRunning;

    private void Awake()
    {
        influence = GetComponent<ITimerInfluence>();
        timeRemaining = totalTime;
    }
    private void OnEnable()
    {
        GameEventBus.OnDelayEnd += StartTimer;
        GameEventBus.OnGameStateChanged += HandleGameStateChange;
        GameEventBus.OnReverseVines += HandleReverseVines;
    }
    private void OnDisable()
    {
        GameEventBus.OnDelayEnd -= StartTimer;
        GameEventBus.OnGameStateChanged -= HandleGameStateChange;
        GameEventBus.OnReverseVines -= HandleReverseVines;
    }
    private void Start()
    {
        // if (autoStart) StartTimer();

        // GameEventBus.TriggerLevelDurationUpdated(totalTime);
    }

    private void Update()
    {
#region Test

        if(Keyboard.current.yKey.wasPressedThisFrame)
        {
            GameEventBus.TriggerReverseVines();
        }
#endregion        
        if (!isRunning) return;

        if (influence == null || influence.ShouldCountDown(Time.deltaTime))
        {
            timeRemaining -= Time.deltaTime;
        }

        // GameEventBus.TriggerLevelTimerUpdated(timeRemaining);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            GameEventBus.TriggerGameOver(GameOverReason.TimeExpired);
        }
    }

    public void StartTimer() => isRunning = true;
    public void PauseTimer() => isRunning = false;
    public void ResumeTimer() => isRunning = true;
    public void ResetTimer(float? newTotal = null)
    {
        if (newTotal.HasValue) totalTime = newTotal.Value;
        timeRemaining = totalTime;
        isRunning = false;
    }
    /// <summary>
    /// Rewinds the timer by a fraction of the time already elapsed.
    /// ReverseTimer(0.3f) = undo 30% of the elapsed time.
    /// </summary>
    public void ReverseTimer(float percent)
    {
        if (timeRemaining <= 0f) return; // already expired, don't revive a finished run

        percent = Mathf.Clamp01(percent);
        float elapsed = totalTime - timeRemaining;

        timeRemaining = Mathf.Min(totalTime, timeRemaining + elapsed * percent);
    }
    private void HandleReverseVines()
    {
        ReverseTimer( 0.18f );
    }
    private void HandleGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.LevelComplete :
                PauseTimer();
            break;

            case GameState.Gameplay :
                if ( autoStart && !wasStarted) 
                {
                    StartTimer();
                    GameEventBus.TriggerLevelDurationUpdated(totalTime);
                    wasStarted = true;
                }
                else if(wasStarted)
                {
                    ResumeTimer();
                }
            Debug.Log($" [TimerController] State Changed event fired!! State:{state}");
            break;
        }
    }

}