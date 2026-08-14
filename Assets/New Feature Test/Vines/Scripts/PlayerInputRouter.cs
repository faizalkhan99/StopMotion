using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class PlayerInputRouter : MonoBehaviour
{
    [Tooltip("The player controller this router drives. Auto-found on this GameObject if left null.")]
    [SerializeField] private PlayerController playerController;

    [Header("Screen Split")]
    [Tooltip("Fraction of screen width. Columns left of this boundary = movement zone, right = jump zone. 0.5 = split down the middle.")]
    [Range(0f, 1f)]
    [SerializeField] private float screenSplitRatio = 0.5f;

    [Header("Movement Drag")]
    [Tooltip("Horizontal drag distance (in screen pixels) that maps to full left/right movement.")]
    [SerializeField] private float maxDragDistance = 150f;

    private InputAction moveAction;
    private InputAction jumpAction;

    private float horizontalInput;
    private bool anyMovementAnchorActive;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        EnhancedTouchSupport.Enable();

        // Left/Right movement: desktop keyboard via a Vector2 composite.
        // Touch drag on the left half is mixed in during Update().
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.Enable();

        // Jump: Space (desktop) OR a touch. Right-half filtering happens in Update().
        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.Enable();
    }

    private void Update()
    {
        float keyboardX = moveAction.ReadValue<Vector2>().x;
        float touchX = ReadLeftHalfDrag();

        horizontalInput = Mathf.Clamp(keyboardX + touchX, -1f, 1f);
        playerController.SetHorizontalInput(horizontalInput);

        HandleJumpInput();
    }

    private float ReadLeftHalfDrag()
    {
        bool touched = false;
        float dragX = 0f;

        foreach (Touch touch in Touch.activeTouches)
        {
            if (touch.screenPosition.x >= Screen.width * screenSplitRatio)
                continue; // Right half belongs to jump.

            touched = true;
            float delta = touch.screenPosition.x - touch.startScreenPosition.x;
            dragX = Mathf.Clamp(delta / maxDragDistance, -1f, 1f);
            break; // Use the first left-half finger.
        }

        if (touched)
        {
            anyMovementAnchorActive = true;
            return dragX;
        }

        anyMovementAnchorActive = false;
        return 0f;
    }

    private void HandleJumpInput()
    {
        // Desktop: Space via the Jump InputAction.
        if (Keyboard.current != null)
        {
            if (jumpAction.WasPressedThisFrame())
                playerController.OnJumpButtonPressed();
            else if (jumpAction.WasReleasedThisFrame())
                playerController.OnJumpButtonReleased();
        }

        // Mobile: right-half touch taps.
        foreach (var touch in Touch.activeTouches)
        {
            bool onRightHalf = touch.screenPosition.x >= Screen.width * screenSplitRatio;
            if (touch.phase == TouchPhase.Began && onRightHalf)
            {
                playerController.OnJumpButtonPressed();
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && onRightHalf)
            {
                playerController.OnJumpButtonReleased();
            }
        }
    }

    private void OnDisable()
    {
        playerController.StopMovement();
        horizontalInput = 0f;
        anyMovementAnchorActive = false;
    }

    private void OnDestroy()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
    }
}