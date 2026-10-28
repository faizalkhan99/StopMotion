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

    [Header("Touch Config")]
    [Tooltip("Inspector-controllable touch tuning, grouped in one serializable config.")]
    [SerializeField] private TouchInputConfig touchConfig = new TouchInputConfig();

    // Input boundary gate. The router never enables/disables actions on state
    // changes — every sink below just refuses to consume input outside live
    // gameplay (same convention as Hazard/Checkpoint/LivesManager/etc.).
    private static bool IsGameplay => GameEventBus.CurrentGameState == GameState.Gameplay;

    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction jumpAction; // Keyboard-only jump (Space)
    [SerializeField] private InputAction dashAction;  // Keyboard-only dash (<Keyboard>/f) — parity with touch double-tap

    // Movement input, tracked separately so keyboard (event-driven) and touch
    // (polled every frame) can be combined without either one stomping the other.
    private float keyboardX;
    private float touchX;
    private float horizontalInput;
    private bool anyMovementAnchorActive;
    private bool anyJumpAnchorActive;
    private bool isJumping = false;

    // Prevents the upward-drag trigger from firing every frame while the
    // finger stays past the threshold. Clears when the right-half finger lifts.
    private bool upwardDragConsumed;

    // Tap-debounce state (right-half touch taps only)
    private int tapCount;
    private Coroutine tapWindowRoutine;

    private enum ScreenHalf { Left, Right }
    private enum DragAxis { Horizontal, Vertical }

    public void SetIsJumpFalse()
    {
        isJumping = false;
    }

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

        // Dash: <Keyboard>/f (desktop). The action binding is serialized with the component,
        // but freshly-added components have no serialized action — construct the same default
        // binding here so Enable() can never null-throw (was a runtime NRE before the guard).
        // Touch dash does NOT go through this action; it's driven by the tap debounce
        // in TapWindowRoutine() below.
        if (dashAction == null)
        {
            dashAction = new InputAction("Dash", InputActionType.Button, "<Keyboard>/f");
        }
        dashAction.Enable();
    }

    private void OnEnable()
    {
        GameEventBus.OnGameStateChanged += OnGameStateChanged;
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        jumpAction.started += OnKeyboardJumpPressed;
        jumpAction.canceled += OnKeyboardJumpReleased;
        dashAction.started += OnKeyboardDashTriggered;
        // Touch.onFingerDown += HandleFingerDown;
        Touch.onFingerUp += HandleFingerUp;
    }

    private void OnDisable()
    {
        GameEventBus.OnGameStateChanged -= OnGameStateChanged;
        playerController.StopMovement();
        keyboardX = 0f;
        touchX = 0f;
        horizontalInput = 0f;
        anyMovementAnchorActive = false;
        anyJumpAnchorActive = false;
        upwardDragConsumed = false;

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        jumpAction.started -= OnKeyboardJumpPressed;
        jumpAction.canceled -= OnKeyboardJumpReleased;
        dashAction.started -= OnKeyboardDashTriggered;
        // Touch.onFingerDown -= HandleFingerDown;
        Touch.onFingerUp -= HandleFingerUp;

        if (tapWindowRoutine != null)
        {
            StopCoroutine(tapWindowRoutine);
            tapWindowRoutine = null;
        }
        tapCount = 0;
    }

    private void Update()
    {
        // Touch drag (left half) is polled every frame since EnhancedTouch has no
        // "performed" callback equivalent for continuous drag. Keyboard input is
        // event-driven via OnMove() and cached in keyboardX. Combine both here.
        touchX = ReadHalfDrag(ScreenHalf.Left, DragAxis.Horizontal, out anyMovementAnchorActive);

        // Forward movement only during live gameplay. Outside Gameplay (Paused,
        // GameOver, LevelComplete) this pushes a persistent 0 so the controller
        // never inherits stale cached input from a pre-transition frame.
        horizontalInput = IsGameplay ? Mathf.Clamp(keyboardX + touchX, -1f, 1f) : 0f;
        playerController.SetHorizontalInput(horizontalInput);

        if (IsGameplay)
            UpdateRightHalfUpwardDrag();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (!IsGameplay)
            return;

        if (context.performed)
        {
            keyboardX = Mathf.Clamp(context.ReadValue<Vector2>().x, -1f, 1f);
        }
        else if (context.canceled)
        {
            keyboardX = 0f;
        }
    }
    #region Touch Controls
    // ---------------------------------------------------------------
    // Shared drag reader — either half, either axis. Returns the clamped
    // -1..1 drag value for the first finger found on that half, and reports
    // via 'touched' whether any finger is currently anchoring that half.
    // ---------------------------------------------------------------

    private float ReadHalfDrag(ScreenHalf half, DragAxis axis, out bool touched)
    {
        touched = false;

        // EnhancedTouchSupport gets reset by domain reloads and can be toggled
        // externally; polling while disabled throws every frame, so treat it as
        // "no touches" instead. Awake() enables it at session start.
        if (!EnhancedTouchSupport.enabled)
            return 0f;

        float dragValue = 0f;

        foreach (Touch touch in Touch.activeTouches)
        {
            bool isLeftSide = touch.screenPosition.x < Screen.width * touchConfig.screenSplitRatio;

            if (half == ScreenHalf.Left && !isLeftSide)
                continue;
            if (half == ScreenHalf.Right && isLeftSide)
                continue;

            touched = true;

            float delta = axis == DragAxis.Horizontal
                ? touch.screenPosition.x - touch.startScreenPosition.x
                : touch.screenPosition.y - touch.startScreenPosition.y;

            dragValue = Mathf.Clamp(delta / touchConfig.maxDragDistance, -1f, 1f);
            break; // Use the first finger found on that half.
        }

        return touched ? dragValue : 0f;
    }

    // ---------------------------------------------------------------
    // Right half: upward drag trigger (fires once per gesture, resets
    // when the right-half finger lifts).
    // ---------------------------------------------------------------

    private void UpdateRightHalfUpwardDrag()
    {
        float dragY = ReadHalfDrag(ScreenHalf.Right, DragAxis.Vertical, out anyJumpAnchorActive);

        if (!anyJumpAnchorActive)
        {
            upwardDragConsumed = false;
            return;
        }

        // Screen Y increases upward in Unity, so a positive dragY is already "up".
        if (!upwardDragConsumed && dragY >= touchConfig.upwardDragThreshold)
        {
            upwardDragConsumed = true;
            OnRightHalfUpwardDrag();
        }
    }
    // ---------------------------------------------------------------
    // Touch: right-half drag up  = jump
    // ---------------------------------------------------------------
    private void OnRightHalfUpwardDrag()
    {
        // TODO: hook up whatever the upward drag should do (jump, dash, etc.)
        // Debug.Log("Upward drag detected on right half!");
        TriggerTouchJump();
    }

    // ---------------------------------------------------------------
    // Touch: right-half tap = dash
    // ---------------------------------------------------------------

    private void HandleFingerUp(Finger finger)
    {
        if (!IsGameplay)
            return;

        if (finger.screenPosition.x < Screen.width * touchConfig.screenSplitRatio)
            return; // Left half is movement, not taps.    

        if (!upwardDragConsumed)
        {
            playerController.TriggerDash();
        }
    }
    private void HandleFingerDown(Finger finger)
    {
        if (finger.screenPosition.x < Screen.width * touchConfig.screenSplitRatio)
            return; // Left half is movement, not taps.

        // playerController.TriggerDash();
        // tapCount++;

        // if (tapWindowRoutine != null)
        //     StopCoroutine(tapWindowRoutine);

        // tapWindowRoutine = StartCoroutine(TapWindowRoutine());
    }

    private IEnumerator TapWindowRoutine()
    {
        yield return new WaitForSeconds(touchConfig.tapDelay);

        if (tapCount >= 2)
        {
            // Debug.Log("Dash performed via MultiTap!");
            playerController.TriggerDash();
        }
        else if (playerController.IsGrounded() && !isJumping)
        {
            isJumping = true;

            // Same buffered-jump + cosmetic-squash pattern as the keyboard path,
            // so touch jump doesn't skip the squash animation or fire mid-air.
            playerController.OnJumpButtonPressed();
            GameEventBus.TriggerPlayerJumpSquash(true);

            // Debug.Log("Jump performed after tap delay!");
        }

        tapCount = 0;
        tapWindowRoutine = null;
    }
    #endregion

    #region Keyboard Controls
    // ---------------------------------------------------------------
    // Keyboard: Space is an immediate jump, no debounce/dash on desktop
    // ---------------------------------------------------------------

    private void OnKeyboardJumpPressed(InputAction.CallbackContext context)
    {
        if (!IsGameplay)
            return;

        if (playerController.IsGrounded() && !isJumping)
        {
            isJumping = true;

            // Buffer the real jump immediately — physics no longer waits on the
            // squash animation, so a jump can never be silently eaten if the
            // tween runs past the coyote window.
            playerController.OnJumpButtonPressed();

            // Squash is now purely cosmetic and can't block or delay the jump.
            GameEventBus.TriggerPlayerJumpSquash(true);
        }
    }

    private void OnKeyboardJumpReleased(InputAction.CallbackContext context)
    {
        if (!IsGameplay)
            return;

        playerController.OnJumpButtonReleased();

        // Reset on key release rather than only on landing, so a stuck grounded
        // press can never wedge keyboard jump for the rest of the scene.
        isJumping = false;
    }

    private void OnKeyboardDashTriggered(InputAction.CallbackContext context)
    {
        if (!IsGameplay)
            return;

        playerController.TriggerDash();
    }
    #endregion

    private void TriggerTouchJump()
    {
        if (!IsGameplay || !playerController.IsGrounded() || isJumping)
            return;

        isJumping = true;
        playerController.OnJumpButtonPressed();
        GameEventBus.TriggerPlayerJumpSquash(true);

        // Debug.Log("Jump performed via swipe!");
    }
    private void OnDestroy()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        dashAction?.Disable();
        EnhancedTouchSupport.Disable();
    }

    private void OnGameStateChanged(GameState newState)
    {
        // Flush buffered/transient input on every state transition so nothing
        // bleeds across a pause, game over, respawn, restart, or level load.
        // Actions stay enabled — sinks gate on IsGameplay instead, so input
        // can never be accidentally left disabled by a missed transition.
        keyboardX = 0f;
        touchX = 0f;
        horizontalInput = 0f;
        anyMovementAnchorActive = false;
        anyJumpAnchorActive = false;
        isJumping = false;
        upwardDragConsumed = false;

        if (tapWindowRoutine != null)
        {
            StopCoroutine(tapWindowRoutine);
            tapWindowRoutine = null;
        }
        tapCount = 0;

        if (playerController != null)
            playerController.StopMovement();
    }

    /// <summary>
    /// Inspector-controllable touch tuning, exposed through the router behaviour as one
    /// serializable config (rule: modules with tunables don't need their own components).
    /// </summary>
    [System.Serializable]
    public class TouchInputConfig
    {
        [Header("Screen Split")]
        [Tooltip("Fraction of screen width. Columns left of this boundary = movement zone, right = jump/dash zone. 0.5 = split down the middle.")]
        [Range(0f, 1f)] public float screenSplitRatio = 0.5f;

        [Header("Movement Drag")]
        [Tooltip("Horizontal drag distance (in screen pixels) that maps to full left/right movement.")]
        public float maxDragDistance = 150f;

        [Header("Upward Drag (right half)")]
        [Tooltip("Normalized drag distance (0-1, as a fraction of maxDragDistance) the right-half finger must move upward before it counts as an upward-drag trigger.")]
        [Range(0f, 1f)] public float upwardDragThreshold = 0.5f;

        [Header("Tap / Multi-Tap")]
        [Tooltip("How long to wait after a tap to see if a second tap arrives before committing to jump vs dash.")]
        public float tapDelay = 0.2f;
    }
}