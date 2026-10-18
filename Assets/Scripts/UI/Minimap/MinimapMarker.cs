using UnityEngine;

/// <summary>
/// Attach to any world object that should appear on the circular minimap
/// (chests, enemies, quest points, checkpoints). Registers/unregisters with
/// the scene's <see cref="MinimapController"/> on enable/disable; holds only
/// marker *data* — all UI instantiation and positioning lives in the controller.
/// Use <see cref="SetMarkerActive"/> to hide/show the marker at runtime
/// without disabling the component or the GameObject.
/// </summary>
[DisallowMultipleComponent]
public class MinimapMarker : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("World transform tracked on the minimap. Falls back to this transform if left empty.")]
    [SerializeField] private Transform targetTransform;

    [Header("Icon")]
    [Tooltip("Sprite drawn on the minimap for this marker.")]
    [SerializeField] private Sprite iconSprite;
    [Tooltip("Tint applied to the minimap icon.")]
    [SerializeField] private Color iconColor = Color.white;
    [Tooltip("Minimap icon size in UI pixels.")]
    [SerializeField] private Vector2 iconSize = new Vector2(12f, 12f);

    [Header("Offscreen Indicator")]
    [Tooltip("When the target is outside the minimap radius, rotate the icon to point " +
             "toward the true target (arrow-style indicators). Leave off for upright " +
             "icons like skulls/dots.")]
    [SerializeField] private bool rotateWhenOffscreen = true;
    [Tooltip("Degrees added to the direction angle to match the sprite's default facing. " +
             "Use -90 for sprites that point UP by default, 0 for sprites that point RIGHT.")]
    [SerializeField] private float rotationOffset = -90f;

    /// <summary>Live world position of the tracked target.</summary>
    public Vector3 TargetPosition => targetTransform != null ? targetTransform.position : transform.position;

    /// <summary>Sprite drawn on the minimap for this marker.</summary>
    public Sprite IconSprite => iconSprite;

    /// <summary>Tint applied to the minimap icon.</summary>
    public Color IconColor => iconColor;

    /// <summary>Minimap icon size in UI pixels.</summary>
    public Vector2 IconSize => iconSize;

    /// <summary>True if the icon should rotate to point at the target while offscreen.</summary>
    public bool RotateWhenOffscreen => rotateWhenOffscreen;

    /// <summary>Angle offset compensating for the icon sprite's default facing direction.</summary>
    public float RotationOffset => rotationOffset;

    /// <summary>Whether this marker currently shows an icon on the minimap.</summary>
    public bool IsMarkerActive { get; private set; } = true;

    // Registration happens in Start (so MinimapController.Awake has run);
    // OnEnable only re-registers after that first Start.
    private bool started;

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform;
    }

    private void Start()
    {
        started = true;
        IsMarkerActive = true;

        MinimapController controller = MinimapController.Instance;
        if (controller == null)
        {
            Debug.LogWarning($"<b>[MinimapMarker]</b> No active MinimapController in scene — '{name}' cannot register. Disabling marker.", this);
            enabled = false;
            return;
        }

        controller.Register(this);
    }

    private void OnEnable()
    {
        if (!started) return; // first registration is handled by Start

        // Re-enabling the component always restores a visible marker.
        IsMarkerActive = true;
        MinimapController.Instance?.Register(this);
    }

    private void OnDisable()
    {
        // Instance may already be gone during scene teardown; Unregister is a no-op then.
        MinimapController.Instance?.Unregister(this);
    }

    /// <summary>
    /// Shows or hides this marker on the minimap without disabling the
    /// component or GameObject. Hiding releases the pooled UI icon (zero cost);
    /// re-showing re-acquires an icon and repositions it immediately.
    /// </summary>
    public void SetMarkerActive(bool active)
    {
        if (IsMarkerActive == active) return;

        IsMarkerActive = active;

        MinimapController controller = MinimapController.Instance;
        if (controller == null) return;

        if (active)
        {
            // Only re-register if the component is actually enabled;
            // OnEnable will register when it next runs.
            if (isActiveAndEnabled)
                controller.Register(this);
        }
        else
        {
            controller.SetMarkerVisible(this, false);
        }
    }

    private void OnValidate()
    {
        if (targetTransform == null)
            targetTransform = transform;

        if (iconSize.x < 1f) iconSize.x = 1f;
        if (iconSize.y < 1f) iconSize.y = 1f;
        if (iconColor.a < 0f) iconColor.a = 0f;
    }
}