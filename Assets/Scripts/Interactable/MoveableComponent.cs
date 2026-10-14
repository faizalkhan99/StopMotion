using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class MoveableComponent : MonoBehaviour
{
    [Header("Route")]
    [Tooltip("Ordered stops: index 0 = start, last = finish. Assign scene Transforms in travel order.")]
    [SerializeField] private List<Transform> waypoints = new();
    [Tooltip("Travel speed in units per second, constant across all legs.")]
    [SerializeField, Min(0.01f)] private float speed = 2f;
    [Tooltip("Seconds to wait on each arrival. 0 = constant motion, no stopping.")]
    [SerializeField, Min(0f)] private float waypointPause = 0f;

    [Header("Behaviour")]
    [Tooltip("If true, snaps to the first waypoint and starts the route automatically in Start().")]
    [SerializeField] private bool moveOnStart = false;
    [Tooltip("True = after the finish, travel back to waypoint 0 and repeat. False = stop at the finish.")]
    [SerializeField] private bool loopRoute = false;

    private Rigidbody2D rb;
    private int currentIndex = -1;
    private bool isMoving;
    private float pauseTimer;

    private const float ArrivalTolerance = 0.05f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetRbConfig();
    }

    private void Start()
    {
        int first = NextValidIndex(0);
        if (first < 0)
        {
            Debug.LogWarning($"[MoveableComponent] {name} has no valid waypoints — platform stays idle.", this);
            return;
        }

        rb.position = waypoints[first].position;
        currentIndex = NextValidIndex(first + 1);

        if (moveOnStart && currentIndex >= 0)
        {
            isMoving = true;
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
    }

    private void FixedUpdate()
    {
        if (!isMoving || currentIndex < 0) return;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        Transform target = waypoints[currentIndex];
        if (target == null)
        {
            Advance();
            return;
        }

        // Read live each step so the route follows moving waypoints.
        Vector2 goal = target.position;
        Vector2 next = Vector2.MoveTowards(rb.position, goal, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (next == goal || Vector2.Distance(next, goal) <= ArrivalTolerance)
        {
            pauseTimer = waypointPause;
            Advance();
        }
    }

    private void Advance()
    {
        int next = currentIndex >= 0 ? NextValidIndex(currentIndex + 1) : NextValidIndex(0);
        if (next >= 0)
        {
            currentIndex = next;
            return;
        }

        if (loopRoute)
        {
            currentIndex = NextValidIndex(0);
            if (currentIndex >= 0) return;
        }

        currentIndex = -1;
        isMoving = false;
    }

    private int NextValidIndex(int from)
    {
        if (waypoints == null) return -1;
        for (int i = from; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null) return i;
        }
        return -1;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Top-only: ignore side brushes so the player is never glued to walls.
        // Landing only parents the rider — it never starts the route (auto-start only).
        if (!IsLandingOnTop(collision)) return;

        collision.transform.SetParent(transform, true);
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
    /// Called on exit/reset, and safe to call before teleports.
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
    /// Starts (or resumes) the waypoint route. Restarts from the first waypoint
    /// if the route already finished. Safe to call from other scripts or UnityEvents.
    /// Landing on the platform does NOT call this — it only parents the rider.
    /// </summary>
    public void StartMovement()
    {
        if (speed <= 0f) return;
        if (NextValidIndex(0) < 0)
        {
            Debug.LogWarning($"[MoveableComponent] {name} has no valid waypoints — ignoring StartMovement.", this);
            return;
        }

        if (currentIndex < 0 || currentIndex >= waypoints.Count || waypoints[currentIndex] == null)
        {
            currentIndex = NextValidIndex(0);
        }

        pauseTimer = 0f;
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
        pauseTimer = 0f;

        int first = NextValidIndex(0);
        if (first >= 0)
        {
            rb.position = waypoints[first].position;
            currentIndex = NextValidIndex(first + 1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.cyan;
        Vector2? prev = null;
        Vector2 first = Vector2.zero;
        bool hasFirst = false;
        int validCount = 0;

        foreach (Transform wp in waypoints)
        {
            if (wp == null) continue;
            Vector2 p = wp.position;
            Gizmos.DrawWireSphere(p, 0.25f);
            if (!hasFirst)
            {
                first = p;
                hasFirst = true;
            }
            if (prev.HasValue) Gizmos.DrawLine(prev.Value, p);
            prev = p;
            validCount++;
        }

        if (loopRoute && hasFirst && prev.HasValue && validCount > 1)
            Gizmos.DrawLine(prev.Value, first);
    }
}
