using UnityEngine;

public class TimerController : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float totalTime = 60f;
    [SerializeField] private bool autoStart = true;

    private float timeRemaining;
    private bool isRunning;
    private ITimerInfluence influence;

    public float TimeRemaining => timeRemaining;
    public float NormalizedProgress => Mathf.Clamp01(timeRemaining / totalTime);
    public bool IsRunning => isRunning;

    private void Awake()
    {
        influence = GetComponent<ITimerInfluence>();
        timeRemaining = totalTime;
    }

    private void Start()
    {
        if (autoStart) StartTimer();
    }

    private void Update()
    {
        if (!isRunning) return;

        if (influence == null || influence.ShouldCountDown(Time.deltaTime))
        {
            timeRemaining -= Time.deltaTime;
        }

        GameEventBus.TriggerLevelTimerUpdated(timeRemaining);

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
}