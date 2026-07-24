using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKeyboardInput : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    private Keyboard keyboard;

    private void Awake()
    {
        keyboard = Keyboard.current;
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        float horizontalInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontalInput = -1f;
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontalInput = 1f;

        playerController.SetHorizontalInput(horizontalInput);

        if (keyboard.spaceKey.wasPressedThisFrame)
            playerController.OnJumpButtonPressed();

        if (keyboard.spaceKey.wasReleasedThisFrame)
            playerController.OnJumpButtonReleased();
    }
}