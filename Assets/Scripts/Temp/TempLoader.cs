using UnityEngine;

public class TempLoader : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        GameEventBus.TriggerGameStateChanged(GameState.GameComplete);
    }
}
