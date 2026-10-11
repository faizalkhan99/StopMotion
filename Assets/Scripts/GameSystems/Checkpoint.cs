using UnityEngine;

/// <summary>
/// One-shot checkpoint: first Player touch broadcasts the position as the
/// respawn point via <see cref="GameEventBus.OnCheckpointReached"/>.
/// RespawnManager consumes it. Re-touch does nothing.
/// Collected keys are intentionally kept on respawn (design choice).
/// Fire-only: holds no references.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    [Header("Feedback")]
    [Tooltip("Tint applied once this checkpoint is activated.")]
    [SerializeField] private Color activatedTint = new Color(0.2f, 1f, 0.5f, 1f);

    private bool activated;
    private SpriteRenderer spriteRenderer;
    private Vector3 punchScale;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        RespawnManager.EnsurePresent();
        spriteRenderer = GetComponent<SpriteRenderer>();
        punchScale = transform.localScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;
        if (GameEventBus.CurrentGameState != GameState.Gameplay) return;

        activated = true;

        GameEventBus.TriggerCheckpointReached(transform.position);
        GameEventBus.TriggerPlaySFXCommand(SoundID.Checkpoint);

        if (spriteRenderer != null)
            spriteRenderer.color = activatedTint;

        // Small activation pop (fire-and-forget, no DOTween dependency).
        StopAllCoroutines();
        StartCoroutine(PunchRoutine());
    }

    private System.Collections.IEnumerator PunchRoutine()
    {
        float t = 0f;
        const float duration = 0.25f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f + 0.3f * Mathf.Sin(Mathf.PI * (t / duration));
            transform.localScale = punchScale * k;
            yield return null;
        }
        transform.localScale = punchScale;
    }
}
