using UnityEngine;
using UnityEngine.InputSystem;

public class TEST : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.lKey.wasPressedThisFrame)
        {
            GameEventBus.TriggerGameStateChanged(GameState.GameComplete);
        }
    }
}
