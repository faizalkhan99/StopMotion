using UnityEngine;

/// <summary>
/// Owns the 3-lives rule. Consumes <see cref="GameEventBus.OnHazardHit"/>:
/// lives &gt; 0 stays in Gameplay and fires
/// <see cref="GameEventBus.OnRespawnRequested"/>, lives == 0 fires the
/// existing GameOver flow (gameOverPanel + Retry).
/// Timer/vine deaths bypass this and go straight to GameOver.
/// No statics: all communication goes through <see cref="GameEventBus"/>.
/// </summary>
[DisallowMultipleComponent]
public class LivesManager : MonoBehaviour
{
    [Header("Lives Settings")]
    [Tooltip("Lives granted on each level attempt (scene load).")]
    [SerializeField] private int maxLives = 3;

    public int CurrentLives { get; private set; }
    public int MaxLives => maxLives;

    // Re-entrancy gate covering the whole death window (respawn + i-frames).
    // Set when a life is lost, cleared by OnPlayerVulnerable.
    private bool isResolvingDeath;

    private void Awake()
    {
        CurrentLives = maxLives;
    }

    private void Start()
    {
        // Late broadcast so LivesHUD (possibly enabled after us) syncs.
        GameEventBus.TriggerLivesChanged(CurrentLives);
    }

    private void OnEnable()
    {
        GameEventBus.OnHazardHit += LoseLife;
        GameEventBus.OnPlayerVulnerable += HandlePlayerVulnerable;
    }

    private void OnDisable()
    {
        GameEventBus.OnHazardHit -= LoseLife;
        GameEventBus.OnPlayerVulnerable -= HandlePlayerVulnerable;
    }

    /// <summary>
    /// Scene-setup guarantee only: makes sure a LivesManager exists so Hazard
    /// prefabs work in levels that forgot one. Stores nothing, returns nothing.
    /// </summary>
    public static void EnsurePresent()
    {
        if (Object.FindAnyObjectByType<LivesManager>() != null) return;
        new GameObject("LivesManager (Auto)").AddComponent<LivesManager>();
    }

    private void HandlePlayerVulnerable()
    {
        isResolvingDeath = false;
    }

    /// <summary>Costs one life. Driven by OnHazardHit; guards re-entrancy.</summary>
    private void LoseLife()
    {
        if (GameEventBus.CurrentGameState != GameState.Gameplay) return;
        if (CurrentLives <= 0) return;
        if (isResolvingDeath) return;

        CurrentLives = Mathf.Max(0, CurrentLives - 1);
        GameEventBus.TriggerLivesChanged(CurrentLives); // LiveHUD UI call
        GameEventBus.TriggerPlaySFXCommand(SoundID.Damage); //Sound
        GameEventBus.TriggerCameraShake(); // Camer Shake
        GameEventBus.TriggerPlayerFaceChange(Playerface.Die); // Face Change

        if (CurrentLives <= 0)
        {
            GameEventBus.TriggerGameOver(GameOverReason.Hazard);
            return;
        }

        isResolvingDeath = true;
        GameEventBus.TriggerRespawnRequested();
    }

    /// <summary>Manual reset (e.g. debug). Scene reload already resets via Awake.</summary>
    public void ResetLives()
    {
        CurrentLives = maxLives;
        GameEventBus.TriggerLivesChanged(CurrentLives);
    }
}
