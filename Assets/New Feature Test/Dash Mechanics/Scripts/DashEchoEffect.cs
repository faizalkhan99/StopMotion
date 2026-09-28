using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(SpriteRenderer))]
public class DashEchoEffect : MonoBehaviour
{
    public event Action<DashEchoEffect> ReturnToPool;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Mirrors the echo sprite to match the dash direction.
    /// Same convention as PlayerVisuals: flipX when moving left.
    /// </summary>
    public void SetFacing(float directionX)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.flipX = directionX < 0f;
    }

    public void SetFacing(bool facingLeft)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.flipX = facingLeft;
    }
    public void OnAnimationDone()
    {
        // TurnOff();
        ReturnToPool?.Invoke(this);
    }


}
