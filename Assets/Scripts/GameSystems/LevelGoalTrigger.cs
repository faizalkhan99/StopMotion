using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoalTrigger : MonoBehaviour
{
    [SerializeField] bool hasLock;
    [SerializeField] float _lockedGateRadius;
    [SerializeField, Range(0.1f, 0.5f)] private float _openGateRadius = 0.5f;
    private Material gateShader;
    private bool key;
    private int totalKeys;
    private int collectedKeys;
    private static readonly int PortalRadius = Shader.PropertyToID("_PortalRadius");

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        gateShader = GetComponent<Renderer>().material;
    }

    private void OnEnable()
    {
        if (hasLock)
            CollisionDetection.OnKeyCollected += HandleKeyCollected;
    }

    private void OnDisable()
    {
        CollisionDetection.OnKeyCollected -= HandleKeyCollected;
    }

    private void Start()
    {
        if (hasLock)
        {
            // Single read from the self-registering key registry — no scene search.
            // Keys enabled after us are picked up by the lazy repair in HandleKeyCollected.
            totalKeys = CollisionDetection.TotalKeyCount;
            collectedKeys = 0;
            CloseGate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if(hasLock)
        {
            key = CheckPlayerForKey(other);
        }

        if(!hasLock || key)
        {
            // ChronoState chrono = GameEventBus.CurrentChronoState;
            // if (chrono != ChronoState.Ticking && chrono != ChronoState.WarnStop && chrono != ChronoState.WarnGo) return;

            if (GameEventBus.CurrentGameState != GameState.Gameplay) return;

            GameEventBus.TriggerLevelWon();
        }
    }

    private bool CheckPlayerForKey(Collider2D other)
    {
        // Gate owns the lock state. PlayerVisuals is view-only (key icon),
        // so this never queries it for counts — scoped lookup is only for UI.
        if (totalKeys > 0 && collectedKeys >= totalKeys)
        {
            PlayerVisuals visuals = other.GetComponentInChildren<PlayerVisuals>();
            if (visuals != null)
                visuals.HideKeyInUI();
            OpenGate();
            return true;
        }

        return false;
    }

    private void HandleKeyCollected()
    {
        collectedKeys++;

        // Lazy repair: Start() can run before keys enable (script execution
        // order / spawned later), which left totalKeys at 0. Re-read the
        // registry — O(1), no scene search — instead of FindObjectsOfType.
        if (totalKeys <= 0)
            totalKeys = CollisionDetection.TotalKeyCount;

        if (totalKeys > 0 && collectedKeys >= totalKeys)
            OpenGate();
    }

    private void CloseGate()
    {
        gateShader.SetFloat(PortalRadius,_lockedGateRadius);
    }
    private void OpenGate()
    {
        gateShader.SetFloat(PortalRadius,_openGateRadius);
    }
}
