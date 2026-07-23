using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
public class ChronoController : MonoBehaviour
{
    [System.Serializable]
    public struct PacingProfile
    {
        public string name;
        [Tooltip("Minimum duration (seconds) player must move.")]
        public float minMoveTime;
        [Tooltip("Maximum duration (seconds) player must move[cite: 4].")]
        public float maxMoveTime;
        [Tooltip("Minimum duration (seconds) player is frozen[cite: 4].")]
        public float minStopTime;
        [Tooltip("Maximum duration (seconds) player is frozen[cite: 4].")]
        public float maxStopTime;
        [Tooltip("Relative chance of this profile being selected[cite: 4].")]
        [Range(0f, 1f)] public float selectionWeight;
    }

    [Header("Level Meta")]
    [SerializeField] private float levelCountdownTimer = 60.0f;
    [SerializeField] private float warningDuration = 0.75f;
    [SerializeField] private float minMoveSpeed = 0.1f;
    [SerializeField] private float idleGracePeriod = 0.3f;

    [Header("Dynamic Rhythm Engine (Weighted Profiles)")]
    [SerializeField] private PacingProfile[] profiles;

    [Header("Debug View (Read Only)")]
    [SerializeField] private ChronoState currentState;
    [SerializeField] private GameState currentGameState = GameState.Gameplay;
    [SerializeField] private string currentProfileName;
    [SerializeField] private float stateTimer;
    [SerializeField] private float graceTimer;

    // Internal Zero-GC Cache
    private Rigidbody2D rb;
    private RigidbodyConstraints2D originalConstraints;
    private float minMoveSpeedSqr;
    private float currentMoveDuration;
    private float currentStopDuration;
    private bool isGameOver;

    private void Reset()
    {
        warningDuration = 0.75f;
        minMoveSpeed = 0.1f;
        idleGracePeriod = 0.3f;
        levelCountdownTimer = 60f;

        profiles = new PacingProfile[3]
        {
            new PacingProfile { name = "The Sprint", minMoveTime = 4.0f, maxMoveTime = 6.0f, minStopTime = 1.0f, maxStopTime = 1.5f, selectionWeight = 0.4f },
            new PacingProfile { name = "The Stutter", minMoveTime = 1.5f, maxMoveTime = 2.5f, minStopTime = 1.0f, maxStopTime = 2.0f, selectionWeight = 0.3f },
            new PacingProfile { name = "The Breather", minMoveTime = 3.0f, maxMoveTime = 4.0f, minStopTime = 3.0f, maxStopTime = 4.0f, selectionWeight = 0.3f }
        };
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalConstraints = rb.constraints;
        RecalculateFastMath();
    }

    private void OnValidate()
    {
        RecalculateFastMath();
    }

    private void OnEnable()
    {
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void RecalculateFastMath()
    {
        minMoveSpeedSqr = minMoveSpeed * minMoveSpeed;
    }

    private void Start()
    {
        SelectNewProfile();
        TransitionTo(ChronoState.Ticking, currentMoveDuration);
    }

    private void Update()
    {
        if (isGameOver || currentGameState != GameState.Gameplay) return;

        UpdateLevelTimer();
        UpdateFSM();
        EvaluateEnforcement();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        currentGameState = newState;
    }

    private void UpdateLevelTimer()
    {
        if (currentState != ChronoState.Frozen)
        {
            levelCountdownTimer -= Time.deltaTime;
            GameEventBus.TriggerLevelTimerUpdated(levelCountdownTimer);

            if (levelCountdownTimer <= 0f)
            {
                TriggerGameOver(GameOverReason.TimeExpired);
            }
        }
    }

    private void UpdateFSM()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            AdvanceState();
        }
    }

    private void AdvanceState()
    {
        switch (currentState)
        {
            case ChronoState.Ticking:
                TransitionTo(ChronoState.WarnStop, warningDuration);
                break;
            case ChronoState.WarnStop:
                TransitionTo(ChronoState.Frozen, currentStopDuration);
                break;
            case ChronoState.Frozen:
                TransitionTo(ChronoState.WarnGo, warningDuration);
                break;
            case ChronoState.WarnGo:
                SelectNewProfile();
                TransitionTo(ChronoState.Ticking, currentMoveDuration);
                break;
        }
    }

    private void TransitionTo(ChronoState newState, float duration)
    {
        if (currentState == ChronoState.Frozen)
        {
            rb.constraints = originalConstraints;
        }

        currentState = newState;
        stateTimer = duration;
        graceTimer = 0f;
        GameEventBus.TriggerGracePeriodUpdated(0f);

        if (currentState == ChronoState.Frozen)
        {
            rb.linearVelocity = Vector2.zero;
            originalConstraints = rb.constraints;
            rb.constraints |= RigidbodyConstraints2D.FreezePositionY;
        }

        GameEventBus.TriggerChronoStateChanged(currentState);
    }

    private void EvaluateEnforcement()
    {
        float currentSpeedSqr = rb.linearVelocity.sqrMagnitude;
        bool isMoving = currentSpeedSqr > minMoveSpeedSqr;

        switch (currentState)
        {
            case ChronoState.Ticking:
                if (!isMoving)
                    IncrementGracePeriod(GameOverReason.IdleBomb);
                else if (graceTimer > 0f)
                    ResetGracePeriod();
                break;

            case ChronoState.Frozen:
                if (isMoving)
                    IncrementGracePeriod(GameOverReason.MotionBomb);
                else
                {
                    if (graceTimer > 0f) ResetGracePeriod();
                    if (currentSpeedSqr > 0f) rb.linearVelocity = Vector2.zero;
                }
                break;

            case ChronoState.WarnStop:
            case ChronoState.WarnGo:
                break;
        }
    }

    private void IncrementGracePeriod(GameOverReason reason)
    {
        graceTimer += Time.deltaTime;
        float normalizedGrace = Mathf.Clamp01(graceTimer / idleGracePeriod);
        GameEventBus.TriggerGracePeriodUpdated(normalizedGrace);

        if (graceTimer >= idleGracePeriod)
        {
            TriggerGameOver(reason);
        }
    }

    private void ResetGracePeriod()
    {
        graceTimer = 0f;
        GameEventBus.TriggerGracePeriodUpdated(0f);
    }

    private void SelectNewProfile()
    {
        if (profiles == null || profiles.Length == 0) return;

        float totalWeight = 0f;
        for (int i = 0; i < profiles.Length; i++)
        {
            totalWeight += profiles[i].selectionWeight;
        }

        float randomVal = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < profiles.Length; i++)
        {
            cumulative += profiles[i].selectionWeight;
            if (randomVal <= cumulative)
            {
                currentProfileName = profiles[i].name;
                currentMoveDuration = Random.Range(profiles[i].minMoveTime, profiles[i].maxMoveTime);
                currentStopDuration = Random.Range(profiles[i].minStopTime, profiles[i].maxStopTime);
                return;
            }
        }
    }

    private void TriggerGameOver(GameOverReason reason)
    {
        isGameOver = true;
        rb.linearVelocity = Vector2.zero;
        GameEventBus.TriggerGameOver(reason);
    }
}