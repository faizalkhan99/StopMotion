using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class UnityRemoteTouchValidator : MonoBehaviour
{
    [Header("UI Diagnostics")]
    [Tooltip("Drag your EventSystem here to validate its setup at runtime.")]
    [SerializeField] private EventSystem eventSystem;
    [Tooltip("If true, draws a debug circle on screen where Unity detects a touch.")]
    [SerializeField] private bool visualizeTouches = true;

    private void Awake()
    {
        // Auto-assign EventSystem if not manually linked
        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
        }

        ValidateMobileSetup();
    }

    private void ValidateMobileSetup()
    {
        if (eventSystem == null)
        {
            Debug.LogError("<b><color=red>[REMOTE ERROR]</color></b> No EventSystem found in the scene! UI buttons cannot receive touch inputs without one.");
            return;
        }

        // Ensure the Input Module is ready for mobile touches
        var inputModule = eventSystem.GetComponent<BaseInputModule>();
        if (inputModule == null)
        {
            Debug.LogError("<b><color=red>[REMOTE ERROR]</color></b> Your EventSystem is missing an Input Module (e.g., StandaloneInputModule or InputSystemUIInputModule).");
        }
        else
        {
            Debug.Log($"<b><color=green>[REMOTE OK]</color></b> EventSystem active using: {inputModule.GetType().Name}");
        }

        // Warn if simulate touch is not active in standard input
        if (!Input.touchSupported && !Application.isEditor)
        {
            Debug.LogWarning("<b><color=yellow>[REMOTE WARNING]</color></b> Touch is not supported on this device/configuration.");
        }
    }

    private void Update()
    {
        if (!visualizeTouches) return;

        // Loop through all active touches sent by Unity Remote or device
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Check if this touch is hitting a UI element
            bool hitUI = eventSystem != null && eventSystem.IsPointerOverGameObject(touch.fingerId);

            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log($"<b>Touch Detected:</b> Finger {touch.fingerId} at {touch.position} | <b>Hit UI:</b> {hitUI}");
            }
        }
    }

    private void OnGUI()
    {
        if (!visualizeTouches || Input.touchCount == 0) return;

        // Draw a visual circle on the Editor screen for every touch Unity Remote sends
        foreach (Touch touch in Input.touches)
        {
            // Flip Y coordinate for OnGUI screen space
            Vector2 guiPosition = new Vector2(touch.position.x, Screen.height - touch.position.y);
            
            GUI.color = new Color(0f, 1f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(guiPosition.x - 25f, guiPosition.y - 25f, 50f, 50f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f);
        }
    }
}