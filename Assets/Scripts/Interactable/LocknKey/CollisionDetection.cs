using System.Data.Common;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CollisionDetection : MonoBehaviour
{
    [SerializeField] private InteractableItems item;
    public enum InteractableItems
    {
        Key,
        ReverseTimeAbility
    }
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

        switch (item)
        {
            case InteractableItems.Key:
                AddKeyToPlayer(other);
                TurnOffItem();
            break;

            case InteractableItems.ReverseTimeAbility:
                GameEventBus.TriggerReverseVines();
                TurnOffItem();
            break;
        }
    }

    private void AddKeyToPlayer(Collider2D other)
    {
        PlayerVisuals visuals = other.GetComponentInChildren<PlayerVisuals>();
        if(visuals != null) visuals.ShowKeyInUI();
    }
    private void TurnOffItem()
    {
        collider2D.enabled = false;
        spriteRenderer.enabled = false;
    }
}

