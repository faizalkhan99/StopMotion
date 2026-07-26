using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoalTrigger : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log(" Player collision detected");
        ChronoState chrono = GameEventBus.CurrentChronoState;
        if (chrono != ChronoState.Ticking && chrono != ChronoState.WarnStop && chrono != ChronoState.WarnGo) return;

        if (GameEventBus.CurrentGameState != GameState.Gameplay) return;
        Debug.Log(" Player collision detected : ALl states are correct");

        GameEventBus.TriggerLevelWon();
        Debug.Log(" event sent ");
    }
}
