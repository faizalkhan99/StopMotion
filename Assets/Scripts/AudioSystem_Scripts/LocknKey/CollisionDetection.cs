using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CollisionDetection : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D collider2D;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collider2D = GetComponent<BoxCollider2D>();
        collider2D.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        AddKeyToPlayer(other);
        collider2D.enabled = false;
        spriteRenderer.enabled = false;
    }

    private void AddKeyToPlayer(Collider2D other)
    {
        PlayerVisuals visuals = other.GetComponentInChildren<PlayerVisuals>();
        if(visuals != null) visuals.ShowKeyInUI();
    }
}
