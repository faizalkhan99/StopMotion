using UnityEngine;

/// <summary>
/// Hazard trigger: fires <see cref="GameEventBus.OnHazardHit"/> on Player
/// contact. LivesManager consumes it (life loss, re-entrancy gate included).
/// Fire-only: holds no references.
/// Follows the CollisionDetection pattern: BoxCollider2D trigger + Player tag check.
/// Vines/timer deaths are untouched (they go straight to GameOver via TimerController).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class Hazard : MonoBehaviour
{
    [Header("Hazard Settings")]
    [Tooltip("Per-instance re-trigger cooldown to avoid multi-hits on a single contact.")]
    [SerializeField] private float retriggerCooldown = 0.5f;

    private float lastTriggerTime = -100f;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // Scene-setup guarantee so dropping the prefab in a level just works.
        LivesManager.EnsurePresent();
        RespawnManager.EnsurePresent();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameEventBus.CurrentGameState != GameState.Gameplay) return;
        if (Time.time - lastTriggerTime < retriggerCooldown) return;

        // Re-entrancy (respawn + i-frames) is gated by LivesManager, so a
        // hit here is always safe to broadcast.
        lastTriggerTime = Time.time;
        GameEventBus.TriggerHazardHit();
    }
}
