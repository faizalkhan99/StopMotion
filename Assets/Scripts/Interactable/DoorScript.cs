using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DoorScript : MonoBehaviour
{
    [SerializeField] private float forceAmount = 5f;
    [SerializeField] private float openAlpha = 0.5f;
    [SerializeField] private float closedAlpha = 1f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private BoxCollider2D _boxCollider;
    [SerializeField] private BoxCollider2D _entryboxCollider;

    private SpriteRenderer _spriteRen;
    private Transform _selfTransform;
    private Vector2 _pushForce;
    private Tweener _fadeTween;

    private void Awake()
    {
        _spriteRen = GetComponent<SpriteRenderer>();
        _selfTransform = transform;
        _pushForce = Vector2.right * forceAmount;
    }

    private void OnValidate()
    {
        _pushForce = Vector2.right * forceAmount;
    }

    private void OnDisable()
    {
        KillFade();
    }

    private void OnDestroy()
    {
        KillFade();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (!IsPlayerOnRightSide(collision.transform.position.x)) return;

        Rigidbody2D rb = collision.rigidbody;
        if (rb != null)
        {
            rb.AddForce(_pushForce, ForceMode2D.Impulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (_entryboxCollider.IsTouching(collision))
        {
            PlayFade(openAlpha);
            // Play Animation and SOund
            _boxCollider.isTrigger = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if(IsPlayerOnRightSide(collision.transform.position.x))
        {
            PlayFade(closedAlpha);
            // Play Animation and SOund
            _boxCollider.isTrigger = false;            
        }
    }

    private bool IsPlayerOnRightSide(float playerX)
    {
        return playerX > _selfTransform.position.x;
    }

    private void PlayFade(float targetAlpha)
    {
        if (_spriteRen == null) return;
        KillFade();
        _fadeTween = _spriteRen.DOFade(targetAlpha, fadeDuration);
    }

    private void KillFade()
    {
        if (_fadeTween != null && _fadeTween.IsActive())
        {
            _fadeTween.Kill();
        }
        _fadeTween = null;
    }
}
