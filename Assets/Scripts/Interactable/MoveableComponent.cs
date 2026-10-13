using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class MoveableComponent : MonoBehaviour
{
    public enum MoveAxis
    {
        X,
        Y
    }

    [Header("Movement")]
    [Tooltip("Axis to move along. X = horizontal, Y = vertical.")]
    [SerializeField] private MoveAxis direction = MoveAxis.X;
    [Tooltip("Movement speed in units per second.")]
    [SerializeField, Min(0.01f)] private float speed = 2f;
    [Tooltip("How far to travel. Signed: + moves along +X/+Y, - moves along -X/-Y.")]
    [SerializeField] private float distance = 3f;

    [Header("Behaviour")]
    [Tooltip("If true, movement starts automatically in Start(). Combine with IsTwoWay for a continuous loop from load.")]
    [SerializeField] private bool moveOnStart = false;
    [Tooltip("True = two-way (ping-pong between start and target, loops). False = one-way (go once and stop).")]
    [SerializeField] private bool isTwoWay = false;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private Vector2 targetPos;
    private Vector2 currentTarget;
    private bool isMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetRbConfig();

    }

    private void Start()
    {
        if (moveOnStart)
        {
            StartMovement();
        }
    }
    void SetRbConfig()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.useFullKinematicContacts = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        startPos = rb.position;
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        // Physics-driven move: the solver carries contacts (and the rider)
        // instead of teleporting a static collider through the player.
        Vector2 next = Vector2.MoveTowards(rb.position, currentTarget, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (next == currentTarget)
        {
            if (isTwoWay)
            {
                currentTarget = (currentTarget == targetPos) ? startPos : targetPos;
            }
            else
            {
                isMoving = false;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Top-only: ignore side brushes so the player is never glued to walls.
        if (!IsLandingOnTop(collision)) return;

        collision.transform.SetParent(transform, true);

        if (isMoving) return;

        // One-way platforms stay at the target after one trip — ignore repeat touches.
        if (!isTwoWay && Vector2.Distance(rb.position, targetPos) < 0.01f && targetPos != startPos)
            return;

        StartMovement();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (collision.transform.parent == transform)
            collision.transform.SetParent(null, true);
    }

    // NOTE: DetachRider() is intentionally NOT called from OnDisable. Unity forbids
    // SetParent while this GameObject is activating/deactivating, so detaching there
    // only spams "Cannot set the parent..." errors. Anything that deactivates this
    // platform must call DetachRider() first.

    /// <summary>
    /// Releases the player if this platform is currently carrying them.
    /// Called on exit/disable, and safe to call before teleports.
    /// </summary>
    public void DetachRider()
    {
        // Single-rider case: tag lookup + parent-equality check, no cached reference needed.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.transform.parent == transform)
            player.transform.SetParent(null, true);
    }

    private bool IsLandingOnTop(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            // Normal points from player toward platform here (~-1 when player is above).
            if (collision.GetContact(i).normal.y < -0.5f)
                return true;
        }
        return false;
    }
    /// <summary>
    /// Starts movement from the current position toward start + axis * distance.
    /// Safe to call from other scripts, UnityEvents, or OnCollisionEnter2D.
    /// </summary>
    public void StartMovement()
    {
        if (Mathf.Approximately(distance, 0f) || speed <= 0f) return;

        startPos = rb.position;
        targetPos = startPos + GetAxisVector() * distance;
        currentTarget = targetPos;
        isMoving = true;
    }

    public void StopMovement()
    {
        isMoving = false;
    }

    public void ResetPlatform()
    {
        DetachRider();
        isMoving = false;
        rb.position = startPos;
    }

    private Vector2 GetAxisVector()
    {
        return direction == MoveAxis.X ? Vector2.right : Vector2.up;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 from = Application.isPlaying ? startPos : (Vector2)transform.position;
        Vector2 to = from + (direction == MoveAxis.X ? Vector2.right : Vector2.up) * distance;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireCube(to, Vector3.one * 0.3f);
    }
}
