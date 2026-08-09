using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace StopMotion.Debugging
{
    /// <summary>
    /// Lightweight on-device performance overlay: FPS, frame time (ms), allocated memory.
    /// Uses OnGUI (no Canvas/TMP dependency) so it can be dropped into any scene standalone.
    ///
    /// Toggle:
    ///   - Editor / desktop: F1 key
    ///   - Mobile: tap the top-right corner of the screen
    ///
    /// Only compiled into Development Builds and the Editor. Stripped entirely from release
    /// builds, so there's zero runtime cost or accidental exposure in shipping builds.
    /// </summary>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public class PerformanceStatsHUD : MonoBehaviour
    {
        [Header("Sampling")]
        [SerializeField] private float fpsUpdateInterval = 0.5f;   // how often the FPS text refreshes
        [SerializeField] private int memorySampleEveryNFrames = 30; // memory query is heavier, sample less often

        [Header("Layout")]
        [SerializeField] private int fontSize = 28;
        [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);
        [SerializeField] private float cornerTapZoneSize = 120f; // px, top-right tap target on mobile

        [Header("Start State")]
        [SerializeField] private bool visibleOnStart = true;

        private bool _visible;
        private float _accumTime;
        private int _accumFrames;
        private float _currentFps;
        private float _currentFrameTimeMs;
        private long _lastMemoryBytes;
        private int _frameCounter;

        private readonly StringBuilder _sb = new StringBuilder(128);
        private string _cachedDisplayText = string.Empty;

        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;
        private Texture2D _boxTexture;
        private bool _stylesBuilt;

        private void Awake()
        {
            _visible = visibleOnStart;
        }

        private void Update()
        {
            HandleToggleInput();

            if (!_visible)
                return;

            // Rolling FPS accumulator - avoids per-frame GC from string work, only refreshes text on interval.
            _accumTime += Time.unscaledDeltaTime;
            _accumFrames++;
            _frameCounter++;

            if (_accumTime >= fpsUpdateInterval)
            {
                _currentFps = _accumFrames / _accumTime;
                _currentFrameTimeMs = (_accumTime / _accumFrames) * 1000f;
                _accumTime = 0f;
                _accumFrames = 0;

                // Memory query is relatively costly - sample on a slower cadence than FPS text.
                if (_frameCounter % memorySampleEveryNFrames == 0)
                {
                    _lastMemoryBytes = Profiler.GetTotalAllocatedMemoryLong();
                }

                RebuildDisplayText();
            }
        }

        private void HandleToggleInput()
        {
            // Keyboard toggle (editor / desktop / Android with attached keyboard)
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                _visible = !_visible;
                return;
            }

            // Touch toggle: tap the top-right corner (Input System touch, not legacy Input)
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    Vector2 pos = touch.position.ReadValue();
                    bool inCorner = pos.x >= Screen.width - cornerTapZoneSize &&
                                     pos.y >= Screen.height - cornerTapZoneSize;
                    if (inCorner)
                    {
                        _visible = !_visible;
                    }
                }
            }
        }

        private void RebuildDisplayText()
        {
            _sb.Clear();
            _sb.Append("FPS: ").Append(_currentFps.ToString("F1"));
            _sb.Append("\nFrame: ").Append(_currentFrameTimeMs.ToString("F2")).Append(" ms");
            _sb.Append("\nMem: ").Append((_lastMemoryBytes / 1048576f).ToString("F1")).Append(" MB");
            _cachedDisplayText = _sb.ToString();
        }

        private void EnsureStylesBuilt()
        {
            if (_stylesBuilt)
                return;

            _boxTexture = new Texture2D(1, 1);
            _boxTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            _boxTexture.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _boxTexture }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft
            };

            _stylesBuilt = true;
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            EnsureStylesBuilt();

            // Size the box to fit three lines at the configured font size.
            float boxWidth = 260f;
            float boxHeight = (fontSize + 6f) * 3f + 16f;

            Rect boxRect = new Rect(screenPadding.x, screenPadding.y, boxWidth, boxHeight);
            GUI.Box(boxRect, GUIContent.none, _boxStyle);

            Rect labelRect = new Rect(
                boxRect.x + 12f,
                boxRect.y + 8f,
                boxRect.width - 24f,
                boxRect.height - 16f);

            GUI.Label(labelRect, _cachedDisplayText, _labelStyle);
        }

        private void OnDestroy()
        {
            if (_boxTexture != null)
            {
                Destroy(_boxTexture);
            }
        }
    }
#endif
}
