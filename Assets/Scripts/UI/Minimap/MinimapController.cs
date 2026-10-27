using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the circular 2D side-scroller minimap. Lives on the minimap's root
/// RectTransform inside an independent (screen-space) Canvas — NOT parented
/// to the player. <see cref="MinimapMarker"/> components on world objects
/// self-register here on enable; their UI icons are pooled, never
/// instantiated/destroyed per frame.
///
/// Tracking model: every registered marker's position is recomputed each
/// LateUpdate (after player movement). UpdateIcon skips redundant UI writes.
///
/// Coordinate mapping: minimap icon position = (target - player) * worldToMinimapScale,
/// circularly clamped to <see cref="minimapRadius"/> (inset by half the icon size);
/// offscreen icons sit on the perimeter rotated toward the true target.
/// </summary>
[DisallowMultipleComponent]
public class MinimapController : MonoBehaviour
{
    /// <summary>Active controller for the loaded scene. Cleared on destroy.</summary>
    public static MinimapController Instance { get; private set; }

    [Header("Active State")]
    [SerializeField] private bool isVisibleOnStart = false;

    [Header("Player")]
    [Tooltip("Transform the minimap is centered on. Auto-found by tag in Awake if left empty.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Tag used to auto-find the player when Player Transform is empty.")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("How often to re-search for the player if it was destroyed (seconds).")]
    [SerializeField] private float playerRefindInterval = 0.5f;

    [Header("Minimap Geometry")]
    [Tooltip("RectTransform the icons are positioned relative to (icons are children of this rect). " +
             "Defaults to this object's RectTransform. Anchors/pivot should be 0.5, 0.5.")]
    [SerializeField] private RectTransform minimapCenter;
    [Tooltip("Visible radius of the circular minimap in UI pixels (roughly half the rect size, minus padding).")]
    [SerializeField] private float minimapRadius = 90f;
    [Tooltip("World units -> minimap pixels multiplier. Rule of thumb: minimapRadius / visibleWorldRadius " +
             "(e.g. 100 px radius showing 30 world units -> ~3.33).")]
    [SerializeField] private float worldToMinimapScale = 3.33f;

    [Header("Icon Pooling")]
    [Tooltip("Optional UI Image prefab for marker icons (anchors 0.5/0.5, pivot 0.5/0.5). " +
             "If empty, a plain centered Image is created at runtime.")]
    [SerializeField] private GameObject iconPrefab;

    // Active icons: marker -> pooled UI image + cached write state (skips redundant UI updates).
    private sealed class IconEntry
    {
        public Image Icon;
        public RectTransform Rect;
        public Vector2 LastAnchoredPos;
        public float LastAngle;
        public bool HasLastState;
    }

    private readonly Dictionary<MinimapMarker, IconEntry> activeIcons = new Dictionary<MinimapMarker, IconEntry>(64);
    private readonly Stack<Image> iconPool = new Stack<Image>(64);
    private readonly List<MinimapMarker> markers = new List<MinimapMarker>(48);

    private float nextPlayerRefindTime;

    private void Awake()
    {
        if(!isVisibleOnStart)
        {
            this.gameObject.SetActive(false);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"<b>[MinimapController]</b> Duplicate controller on '{name}' — destroying this one.", this);
            Destroy(this);
            return;
        }

        Instance = this;

        if (minimapCenter == null)
            minimapCenter = transform as RectTransform;

        if (minimapCenter == null)
            Debug.LogError($"<b>[MinimapController]</b> '{name}' must sit on a RectTransform (UI) to act as the minimap center.", this);

        CachePlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        if (playerTransform == null)
        {
            if (Time.unscaledTime < nextPlayerRefindTime) return;

            CachePlayer();
            nextPlayerRefindTime = Time.unscaledTime + Mathf.Max(0.05f, playerRefindInterval);

            if (playerTransform == null) return; // still gone: keep last icon layout, do nothing
        }

        Vector3 playerPos = playerTransform.position;

        for (int i = markers.Count - 1; i >= 0; i--)
        {
            MinimapMarker marker = markers[i];
            if (marker == null)
            {
                // Destroyed without a clean disable.
                markers.RemoveAt(i);
                continue;
            }

            if (!marker.isActiveAndEnabled) continue;

            UpdateIcon(marker, playerPos);
        }
    }

    // ---- Registration -------------------------------------------------------

    /// <summary>
    /// Registers a marker (called by <see cref="MinimapMarker"/> on enable or
    /// re-show). Acquires a pooled icon. Idempotent per marker.
    /// </summary>
    public void Register(MinimapMarker marker)
    {
        if (marker == null) return;

        // Already fully registered and visible -> nothing to do.
        if (activeIcons.ContainsKey(marker)) return;

        IconEntry entry = AcquireIcon(marker);
        activeIcons[marker] = entry;
        ApplyIconLook(marker, entry);

        // Compute immediately so the icon never flashes at the origin.
        if (playerTransform != null)
            UpdateIcon(marker, playerTransform.position, entry);

        if (!markers.Contains(marker))
            markers.Add(marker);
    }

    /// <summary>
    /// Unregisters a marker (called by <see cref="MinimapMarker"/> on disable/destroy)
    /// and returns its icon to the pool.
    /// </summary>
    public void Unregister(MinimapMarker marker)
    {
        if (marker == null) return;
        ReleaseIcon(marker);
        markers.Remove(marker);
    }

    /// <summary>
    /// Hides a marker's icon without unregistering the marker's data
    /// (used by <see cref="MinimapMarker.SetMarkerActive"/> with false).
    /// The marker is removed from the update list so it costs nothing per frame;
    /// Register (via SetMarkerActive(true)) restores it.
    /// </summary>
    public void SetMarkerVisible(MinimapMarker marker, bool visible)
    {
        if (marker == null) return;

        if (visible)
        {
            Register(marker);
            return;
        }

        ReleaseIcon(marker);
        markers.Remove(marker);
    }

    // ---- Icon math ----------------------------------------------------------

    private void UpdateIcon(MinimapMarker marker, Vector3 playerPos, IconEntry entry = null)
    {
        if (entry == null && !activeIcons.TryGetValue(marker, out entry)) return;
        if (entry == null || entry.Icon == null) return;

        Vector2 dir = (Vector2)marker.TargetPosition - (Vector2)playerPos;

        // Inset by half the icon so the whole icon stays inside the circle.
        float iconHalf = Mathf.Max(marker.IconSize.x, marker.IconSize.y) * 0.5f;
        float clampRadius = Mathf.Max(0f, minimapRadius - iconHalf);

        Vector2 scaled = dir * worldToMinimapScale;
        Vector2 newPos;
        float newAngle;

        if (scaled.sqrMagnitude <= clampRadius * clampRadius)
        {
            // Onscreen: free placement, upright.
            newPos = scaled;
            newAngle = 0f;
        }
        else
        {
            // Offscreen: clamp to the perimeter, rotate toward the true target.
            // scaled is nonzero here, so normalizing is safe.
            newPos = scaled.normalized * clampRadius;
            newAngle = marker.RotateWhenOffscreen
                ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + marker.RotationOffset
                : 0f;
        }

        // Skip redundant UI writes (RectTransform changes trigger canvas rebuilds).
        if (entry.HasLastState
            && (entry.LastAnchoredPos - newPos).sqrMagnitude < 0.0001f
            && Mathf.Abs(Mathf.DeltaAngle(entry.LastAngle, newAngle)) < 0.01f)
            return;

        entry.Rect.anchoredPosition = newPos;
        if (!Mathf.Approximately(entry.LastAngle, newAngle) || !entry.HasLastState)
            entry.Rect.localEulerAngles = new Vector3(0f, 0f, newAngle);

        entry.LastAnchoredPos = newPos;
        entry.LastAngle = newAngle;
        entry.HasLastState = true;
    }

    private void ApplyIconLook(MinimapMarker marker, IconEntry entry)
    {
        entry.Icon.sprite = marker.IconSprite;
        entry.Icon.color = marker.IconColor;
        entry.Rect.sizeDelta = marker.IconSize;
    }

    // ---- Pooling ------------------------------------------------------------

    private IconEntry AcquireIcon(MinimapMarker marker)
    {
        Image icon = null;

        // Pop until we find a pooled icon that hasn't been destroyed externally.
        while (icon == null && iconPool.Count > 0)
            icon = iconPool.Pop();

        if (icon == null)
        {
            GameObject go;
            if (iconPrefab != null)
            {
                go = Instantiate(iconPrefab, minimapCenter);
            }
            else
            {
                go = new GameObject("MinimapIcon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(minimapCenter, false);
            }

            icon = go.GetComponent<Image>();
            if (go.transform.parent != minimapCenter)
                go.transform.SetParent(minimapCenter, false);

            // Centered, non-interactive minimap blip.
            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            icon.raycastTarget = false;
        }

        icon.gameObject.SetActive(true);

        return new IconEntry
        {
            Icon = icon,
            Rect = icon.rectTransform,
            HasLastState = false
        };
    }

    private void ReleaseIcon(MinimapMarker marker)
    {
        if (!activeIcons.TryGetValue(marker, out IconEntry entry)) return;

        activeIcons.Remove(marker);

        if (entry.Icon == null) return;

        entry.Icon.gameObject.SetActive(false);
        iconPool.Push(entry.Icon);
    }

    // ---- Helpers ------------------------------------------------------------

    private void CachePlayer()
    {
        if (playerTransform != null) return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            playerTransform = player.transform;
    }
}