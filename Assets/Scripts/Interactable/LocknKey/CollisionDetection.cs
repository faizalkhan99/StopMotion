using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CollisionDetection : MonoBehaviour
{
    [SerializeField] private InteractableItems item;
    public enum InteractableItems
    {
        Key,
        ReverseTimeAbility,
        DashCharge
    }

    public bool IsKeyItem => item == InteractableItems.Key;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D collider2D;
    private MinimapMarker minimapMarker;
    private bool alreadyCollected;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collider2D = GetComponent<BoxCollider2D>();
        collider2D.isTrigger = true;
        // Optional: pickups can carry a minimap blip; it's hidden on pickup.
        minimapMarker = GetComponent<MinimapMarker>();
    }

    private void OnEnable()
    {
        if (IsKeyItem)
            GameEventBus.RegisterKey(this);
    }

    private void OnDisable()
    {
        if (IsKeyItem)
            GameEventBus.UnregisterKey(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (alreadyCollected) return;

        switch (item)
        {
            case InteractableItems.Key:
                // The bus owns counting + the OnAllKeysCollected moment.
                alreadyCollected = true;
                GameEventBus.TriggerKeyCollected(this);
                TurnOffItem();
            break;

            case InteractableItems.ReverseTimeAbility:
                GameEventBus.TriggerReverseVines();
                TurnOffItem();
            break;

            case InteractableItems.DashCharge:
                // DashEngine listens on this bus (only while dashProfile.canDash) —
                // replaces the deleted ToDashItem component's contract.
                GameEventBus.TriggerPlayerContactWithItem();
                TurnOffItem();
            break;
        }
    }

    private void TurnOffItem()
    {
        if (collider2D != null) collider2D.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (minimapMarker != null) minimapMarker.SetMarkerActive(false);
    }
}
