using System;
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

    public bool IsKeyItem => item == InteractableItems.Key;

    public static event Action OnKeyCollected;

    // Self-registration so gates can read the total without FindObjectsOfType.
    private static readonly System.Collections.Generic.HashSet<CollisionDetection> keyRegistry = new();
    public static int TotalKeyCount => keyRegistry.Count;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D collider2D;
    private bool alreadyCollected;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collider2D = GetComponent<BoxCollider2D>();
        collider2D.isTrigger = true;
    }

    private void OnEnable()
    {
        if (IsKeyItem)
            keyRegistry.Add(this);
    }

    private void OnDisable()
    {
        if (IsKeyItem)
            keyRegistry.Remove(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (alreadyCollected) return;

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
        if (visuals != null) visuals.AddKey();
        alreadyCollected = true;
        OnKeyCollected?.Invoke();
    }
    private void TurnOffItem()
    {
        collider2D.enabled = false;
        spriteRenderer.enabled = false;
    }
}

