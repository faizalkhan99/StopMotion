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

        ChronoState chrono = GameEventBus.CurrentChronoState;
        if (chrono != ChronoState.Ticking && chrono != ChronoState.WarnStop && chrono != ChronoState.WarnGo) return;

        if (GameEventBus.CurrentGameState != GameState.Gameplay) return;

        GameEventBus.TriggerLevelWon();
    }
}
