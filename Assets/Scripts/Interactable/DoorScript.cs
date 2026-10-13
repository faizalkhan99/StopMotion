using DG.Tweening;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    private SpriteRenderer spriteRen;
    private BoxCollider2D boxCollider;

    void Start()
    {
        spriteRen = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if(!collision.gameObject.CompareTag("Player")) return;

        if(IsPlayerOnLeftSide( collision ))
        {
            Debug.Log($" left ");
            boxCollider.isTrigger = true;
            spriteRen.DOFade(0.1f ,0.5f);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.gameObject.CompareTag("Player")) return;

        if(IsPlayerOnRightSide( collision ))
        {
            Debug.Log($" right ");
            boxCollider.isTrigger = false;
            spriteRen.DOFade(1f ,0.5f);
        }
    }

    private bool IsPlayerOnLeftSide(Collision2D collision)
    {
        // bool leftContactFound = false;
        // for (int i = 0; i < collision.contactCount; i++)
        // {
        //     Vector2 normal = collision.GetContact(i).normal;

        //     if (normal.x > 0.5f && Mathf.Abs(normal.y) < 0.5f)
        //         leftContactFound = true;
        // }

        // if (!leftContactFound)
        //     return false;

        return collision.transform.position.x < transform.position.x;
    }
    private bool IsPlayerOnRightSide(Collider2D collision)
    {
        // bool leftContactFound = false;
        // for (int i = 0; i < collision.contactCount; i++)
        // {
        //     Vector2 normal = collision.GetContact(i).normal;

        //     if (normal.x > 0.5f && Mathf.Abs(normal.y) < 0.5f)
        //         leftContactFound = true;
        // }

        // if (!leftContactFound)
        //     return false;

        return collision.transform.position.x > transform.position.x;
    }
}