using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoalTrigger : MonoBehaviour
{
    [SerializeField] bool hasLock;
    [SerializeField] float _lockedGateRadius;

    private float _openGateRadius = 0.5f;
    private Material gateShader;
    private bool key;
    private static readonly int PortalRadius = Shader.PropertyToID("_PortalRadius");

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        gateShader = GetComponent<Renderer>().material;
    }
    private void Start()
    {
        if (hasLock)
        {
            CloseGate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log(" Player collision detected");

        key = CheckPlayerForKey(other);

        if(!hasLock || key)
        {
            ChronoState chrono = GameEventBus.CurrentChronoState;
            if (chrono != ChronoState.Ticking && chrono != ChronoState.WarnStop && chrono != ChronoState.WarnGo) return;

            if (GameEventBus.CurrentGameState != GameState.Gameplay) return;
            Debug.Log(" Player collision detected : ALl states are correct");

            GameEventBus.TriggerLevelWon();
            Debug.Log(" event sent ");
        }
    }

    private bool CheckPlayerForKey(Collider2D other)
    {
        PlayerVisuals visuals = other.GetComponentInChildren<PlayerVisuals>();
        if(visuals.CheckForKey())
        {
            OpenGate();
        }

        return visuals.CheckForKey();
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
