using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    public void Update()
    {
        if(Keyboard.current.gKey.wasPressedThisFrame)
        {
            ChangeGameState();
            Debug.Log(" GameState changed to Gameplaye");
        }
    }
    private void ChangeGameState()
    {
        GameEventBus.TriggerGameStateChanged(GameState.Gameplay);
    }
}