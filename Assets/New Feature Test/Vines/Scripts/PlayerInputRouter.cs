using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerInputRouter : MonoBehaviour
{
    [Tooltip("The player controller this router drives. Auto-found on this GameObject if left null.")]
    [SerializeField] private PlayerController playerController;

    [Tooltip(" The Dash Mechanic Script")]
    [SerializeField] private PlayerDash dashController;

    [Header("Screen Split")]
    [Tooltip("Fraction of screen width. Columns left of this boundary = movement zone, right = jump/dash zone. 0.5 = split down the middle.")]
    [Range(0f, 1f)]
    [SerializeField] private float screenSplitRatio = 0.5f;

    [Header("Movement Drag")]
    [Tooltip("Horizontal drag distance (in screen pixels) that maps to full left/right movement.")]
    [SerializeField] private float maxDragDistance = 150f;

    [Header("Tap / Multi-Tap")]
    [Tooltip("How long to wait after a tap to see if a second tap arrives before committing to jump vs dash.")]
    [SerializeField] private float tapDelay = 0.2f;

    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction jumpAction; // Keyboard-only jump (Space)
    [SerializeField] private InputAction dashAction; 

    private float horizontalInput;
    private bool anyMovementAnchorActive;

    // Tap-debounce state (right-half touch taps only)
    private int tapCount;
    private Coroutine tapWindowRoutine;

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

        // Jump: Space only (desktop). Touch jump/dash is handled via EnhancedTouch below.
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.Enable();
        dashAction.Enable();
    }

    private void OnEnable()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        jumpAction.started += OnKeyboardJumpPressed;
        jumpAction.canceled += OnKeyboardJumpReleased;
        dashAction.started += OnKeyboardDashTriggered;
        // Touch.onFingerDown += HandleFingerDown;
    }

    private void OnDisable()
    {
        playerController.StopMovement();
        horizontalInput = 0f;
        anyMovementAnchorActive = false;

        moveAction.canceled -= OnMove;
        jumpAction.started -= OnKeyboardJumpPressed;
        jumpAction.canceled -= OnKeyboardJumpReleased;
        dashAction.started -= OnKeyboardDashTriggered;
        // Touch.onFingerDown -= HandleFingerDown;

        if (tapWindowRoutine != null)
        {
            StopCoroutine(tapWindowRoutine);
            tapWindowRoutine = null;
        }
        tapCount = 0;
    }

    private void Update()
    {
        // float keyboardX = moveAction.ReadValue<Vector2>().x;
        // float touchX = ReadLeftHalfDrag();

        // horizontalInput = Mathf.Clamp(keyboardX + touchX, -1f, 1f);
        // playerController.SetHorizontalInput(horizontalInput);
    }
    private void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            float keyboardX = context.ReadValue<Vector2>().x;
            horizontalInput = Mathf.Clamp(keyboardX, -1f, 1f);

            GameEventBus.TriggerPlayerFaceChange(Playerface.Moving);
        }
        else if (context.canceled)
        {
            horizontalInput = 0f;
            GameEventBus.TriggerPlayerFaceChange(Playerface.Idle);
        }

        playerController.SetHorizontalInput(horizontalInput);
    }
    private float ReadLeftHalfDrag()
    {
        bool touched = false;
        float dragX = 0f;

        foreach (Touch touch in Touch.activeTouches)
        {
            if (touch.screenPosition.x >= Screen.width * screenSplitRatio)
                continue; // Right half belongs to jump/dash.

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

    // ---------------------------------------------------------------
    // Touch: right-half tap = jump, right-half double-tap = dash
    // ---------------------------------------------------------------

    private void HandleFingerDown(Finger finger)
    {
        if (finger.screenPosition.x < Screen.width * screenSplitRatio)
            return; // Left half is movement, not taps.

        tapCount++;

        if (tapWindowRoutine != null)
            StopCoroutine(tapWindowRoutine);

        tapWindowRoutine = StartCoroutine(TapWindowRoutine());
    }

    private IEnumerator TapWindowRoutine()
    {
        yield return new WaitForSeconds(tapDelay);

        if (tapCount >= 2)
        {
            Debug.Log("Dash performed via MultiTap!");
            dashController.TriggerDash();
        }
        else
        {
            playerController.OnJumpButtonPressed();
            Debug.Log("Jump performed after tap delay!");
        }

        tapCount = 0;
        tapWindowRoutine = null;
    }

    // ---------------------------------------------------------------
    // Keyboard: Space is an immediate jump, no debounce/dash on desktop
    // ---------------------------------------------------------------
    public float squashAmount;
    public float squashDuration;
    private void OnKeyboardJumpPressed(InputAction.CallbackContext context)
    {
        transform.DOScaleY( squashAmount ,  squashDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScaleY(1f, squashDuration);
                playerController.OnJumpButtonPressed(); 
            }); 
    }

    private void OnKeyboardJumpReleased(InputAction.CallbackContext context)
    {
        playerController.OnJumpButtonReleased();
    }

    private void OnKeyboardDashTriggered(InputAction.CallbackContext context)
    {
        dashController.TriggerDash();
    }
    private void OnDestroy()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
    }
}