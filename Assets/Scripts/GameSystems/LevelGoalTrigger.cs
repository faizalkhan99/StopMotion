using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoalTrigger : MonoBehaviour
{
    [Tooltip("When true, the portal can only open after every key is collected.")]
    [SerializeField] bool hasLock;
    [SerializeField] float _lockedGateRadius;
    [SerializeField, Range(0.1f, 0.5f)] private float _openGateRadius = 0.5f;
    [Tooltip("Minimap icon for this portal. Falls back to a marker on this GameObject.")]
    [SerializeField] private MinimapMarker gateMarker;

    private Material gateShader;
    private static readonly int PortalRadius = Shader.PropertyToID("_PortalRadius");

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        gateShader = GetComponent<Renderer>().material;
        if (gateMarker == null) gateMarker = GetComponent<MinimapMarker>();

        // Locked portals spawn without their minimap blip. All Awakes finish
        // before any Start, so the marker derives its initial visibility from
        // this flag regardless of script execution order.
        if (hasLock && gateMarker != null)
            gateMarker.StartHidden = true;
    }

    private void OnEnable()
    {
        if (hasLock)
            GameEventBus.OnAllKeysCollected += OpenGate;
    }

    private void OnDisable()
    {
        GameEventBus.OnAllKeysCollected -= OpenGate;
    }

    private void Start()
    {
        if (hasLock)
            CloseGate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (hasLock && !GameEventBus.AllKeysCollected) return;

        // ChronoState chrono = GameEventBus.CurrentChronoState;
        // if (chrono != ChronoState.Ticking && chrono != ChronoState.WarnStop && chrono != ChronoState.WarnGo) return;

        if (GameEventBus.CurrentGameState != GameState.Gameplay) return;

        GameEventBus.TriggerLevelWon();
    }

    private void CloseGate()
    {
        gateShader.SetFloat(PortalRadius, _lockedGateRadius);
        if (gateMarker != null) gateMarker.SetMarkerActive(false);
    }

    private void OpenGate()
    {
        gateShader.SetFloat(PortalRadius, _openGateRadius);
        if (gateMarker != null) gateMarker.SetMarkerActive(true);
    }
}
