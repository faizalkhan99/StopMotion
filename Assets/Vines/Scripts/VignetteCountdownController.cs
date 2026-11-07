using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StopMotion.Visuals
{
    /// <summary>
    /// Drives the CityBuster edge-vines shader on a UI Image.
    ///
    /// Two modes:
    ///  - Time-based (triggerBased = false): shader ramps growth from _Time.y.
    ///  - Trigger-based (triggerBased = true): _IsRunning stays 0 and growth is
    ///    whatever this script writes into _PausedGrowth. Vines hold their size
    ///    and only step up via GrowBy() / down via ReverseGrowth().
    ///
    /// _PausedGrowth is a plain 0-1 "how grown" fraction, not a radius.
    ///
    /// Player tracking runs on three separate clocks:
    ///  - sample the player position often (playerSampleInterval)
    ///  - ease toward it EVERY frame (playerTrackEaseTime)
    ///  - grow on a slow tick (growthTickInterval) in trigger mode
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class VignetteCountdownController : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float reverseDuration = 0.75f;

        [Header("Trigger Mode")]
        [Tooltip("ON: vines only grow when GrowBy() is triggered. OFF: constant time-based growth.")]
        [SerializeField] private bool triggerBased = true;
        [Tooltip("Seconds a single GrowBy() step takes to animate.")]
        [SerializeField] private float growStepDuration = 0.5f;

        [Header("Growth Tick (trigger mode)")]
        [Tooltip("Seconds between automatic growth steps.")]
        [SerializeField, Min(0.1f)] private float growthTickInterval = 5f;
        [Tooltip("How much growth (0-1) each tick adds.")]
        [SerializeField] private float growDelay = 0.5f;

        [Header("Flash Burst (fires on every GrowBy)")]
        [SerializeField] private bool flashOnGrow = true;
        [Tooltip("Number of back-to-back flashes per trigger.")]
        [SerializeField, Range(1, 8)] private int flashCount = 3;
        [Tooltip("Length of ONE flash in seconds. Small = rapid.")]
        [SerializeField, Min(0.02f)] private float flashPulseDuration = 0.08f;

        [Header("Player Tracking")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera targetCamera;
        [Tooltip("How often the player's screen position is read. Keep small so the target is never stale.")]
        [SerializeField, Min(0.02f)] private float playerSampleInterval = 0.1f;
        [Tooltip("Smoothing time (applied every frame) toward the latest sample.")]
        [SerializeField, Min(0.01f)] private float playerTrackEaseTime = 0.2f;

        private static readonly int DurationID     = Shader.PropertyToID("_Duration");
        private static readonly int StartTimeID    = Shader.PropertyToID("_StartTime");
        private static readonly int PausedGrowthID = Shader.PropertyToID("_PausedGrowth");
        private static readonly int IsRunningID    = Shader.PropertyToID("_IsRunning");
        private static readonly int PlayerPosID    = Shader.PropertyToID("_PlayerPos");
        private static readonly int FlashTriggerTimeID   = Shader.PropertyToID("_FlashTriggerTime");
        private static readonly int FlashCountID         = Shader.PropertyToID("_FlashCount");
        private static readonly int FlashPulseDurationID = Shader.PropertyToID("_FlashPulseDuration");

        private float growthTickAmount;
        private Material _instancedMaterial;
        private float _pausedElapsed; // seconds already consumed when paused (time mode)
        private bool _isRunning;      // time mode: mirrors _IsRunning; guards CaptureElapsed()
        private Coroutine _growthRoutine;

        // Trigger mode state
        private float _currentGrowth; // 0-1, source of truth in trigger mode
        private bool _isActive;       // vines are live (tracking player) in trigger mode

        private float _sampleTimer;
        private float _growthTimer;
        private Vector2 _playerPosCurrent;
        private Vector2 _playerPosTarget;
        private bool _hasPlayerPos;

        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            _instancedMaterial = new Material(targetImage.material);
            targetImage.material = _instancedMaterial;

            ResolvePlayerReferences();

            _instancedMaterial.SetFloat(IsRunningID, 0f);
            _instancedMaterial.SetFloat(PausedGrowthID, 0f);
            ClearFlash();
        }

        private void OnEnable()
        {
            GameEventBus.OnLevelDurationUpdated += StartVineVisuals;
            GameEventBus.OnGameStateChanged += HandleEventChanges;
            GameEventBus.OnReverseVines += HandleReverseVines;
            // GameEventBus.OnGameOverTriggered +=
            // GameEventBus.OnGrowVines += GrowBy; // NEW: add this event to GameEventBus
        }

        private void OnDisable()
        {
            GameEventBus.OnLevelDurationUpdated -= StartVineVisuals;
            GameEventBus.OnGameStateChanged -= HandleEventChanges;
            GameEventBus.OnReverseVines -= HandleReverseVines;
            // GameEventBus.OnGameOverTriggered +=
            // GameEventBus.OnGrowVines -= GrowBy;
            CancelGrowthRoutine();
        }

        private void OnDestroy()
        {
            if (_instancedMaterial != null)
                Destroy(_instancedMaterial);
        }

#region State Change
        private void HandleReverseVines()
        {
            ReverseGrowth(0.18f);
        }

        private void HandleEventChanges(GameState state)
        {
            switch (state)
            {
                case GameState.LevelComplete:
                    Pause();
                    _isActive = false;
                break;

                case GameState.GameOver:
                _isActive = false;
                break;
            }
        }
#endregion

#region Player Tracking
        private void Update()
        {
            bool tracking = triggerBased ? _isActive : _isRunning;
            if (!tracking || _instancedMaterial == null) return;

            // 1) Sample the player often so the target is never stale.
            _sampleTimer += Time.deltaTime;
            if (_sampleTimer >= playerSampleInterval)
            {
                _sampleTimer -= playerSampleInterval;

                if (player == null || targetCamera == null)
                    ResolvePlayerReferences();

                if (TryGetPlayerUV(out Vector2 uv))
                {
                    _playerPosTarget = uv;
                    _hasPlayerPos = true;
                }
            }

            // 2) Ease EVERY frame (this is what makes k behave as intended).
            if (_hasPlayerPos)
            {
                float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(playerTrackEaseTime, 0.0001f));
                _playerPosCurrent = Vector2.Lerp(_playerPosCurrent, _playerPosTarget, k);

                _instancedMaterial.SetVector(PlayerPosID,
                    new Vector4(_playerPosCurrent.x, _playerPosCurrent.y, 0f, 0f));
            }

            // 3) Growth step on its own slow timer.
            if (triggerBased)
            {
                _growthTimer += Time.deltaTime;
                if (_growthTimer >= growthTickInterval)
                {
                    _growthTimer -= growthTickInterval;
                    GrowBy(growthTickAmount);
                }
            }
        }

        private void SnapPlayerTracking()
        {
            _sampleTimer = 0f;

            if (player == null || targetCamera == null)
                ResolvePlayerReferences();

            if (!TryGetPlayerUV(out Vector2 uv))
                return;

            _playerPosCurrent = uv;
            _playerPosTarget = uv;
            _hasPlayerPos = true;

            _instancedMaterial.SetVector(PlayerPosID,
                new Vector4(uv.x, uv.y, 0f, 0f));
        }

        private bool TryGetPlayerUV(out Vector2 uv)
        {
            uv = default;

            if (player == null || targetCamera == null)
                return false;

            Vector3 screenPosition = targetCamera.WorldToScreenPoint(player.position);

            if (screenPosition.z < 0f)
                return false;

            uv = new Vector2(
                Mathf.Clamp01(screenPosition.x / Mathf.Max(Screen.width, 1)),
                Mathf.Clamp01(screenPosition.y / Mathf.Max(Screen.height, 1)));

            return true;
        }

        private void ResolvePlayerReferences()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (player == null)
            {
                GameObject playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null)
                    player = playerObject.transform;
            }
        }

        private void OnValidate()
        {
            playerSampleInterval = Mathf.Max(playerSampleInterval, 0.02f);
        }
#endregion

        private void StartVineVisuals(float levelDuration)
        {
            SetLevelDuration(levelDuration);
            Play();
            growthTickAmount = growthTickInterval / Mathf.Max(duration, 0.0001f);
        }

        /// <summary>
        /// Time mode: begin the countdown growth from scratch.
        /// Trigger mode: show the vines at 0 growth and wait for GrowBy().
        /// </summary>
        public void Play()
        {
            Debug.Log("some log:)");
            CancelGrowthRoutine();
            ClearFlash(); // no flash at the beginning
            SnapPlayerTracking();
            _growthTimer = 0f; // first growth step lands a full interval after start

            if (triggerBased)
            {
                _currentGrowth = 0f;
                _isActive = true;
                _isRunning = false;
                _instancedMaterial.SetFloat(PausedGrowthID, 0f);
                _instancedMaterial.SetFloat(IsRunningID, 0f);
                return;
            }

            _pausedElapsed = 0f;
            _isRunning = true;
            _instancedMaterial.SetFloat(StartTimeID, Time.time);
            _instancedMaterial.SetFloat(IsRunningID, 1f);
        }

        /// <summary>Init Level Duration</summary>
        public void SetLevelDuration(float levelDuration)
        {
            duration = levelDuration;
            _instancedMaterial.SetFloat(DurationID, levelDuration);
        }

        /// <summary>Resume after a Pause(), preserving progress.</summary>
        public void Resume()
        {
            CancelGrowthRoutine();

            if (triggerBased)
            {
                _isActive = true;
                return;
            }

            _isRunning = true;
            _instancedMaterial.SetFloat(StartTimeID, Time.time - _pausedElapsed);
            _instancedMaterial.SetFloat(IsRunningID, 1f);
        }

        /// <summary>Freeze the vines at their current growth amount.</summary>
        public void Pause()
        {
            CancelGrowthRoutine();

            if (triggerBased)
            {
                // _PausedGrowth already holds the live value, so just stop tracking.
                _isActive = false;
                return;
            }

            CaptureElapsed();
            _isRunning = false;

            float currentGrowth = GetGrowthFromElapsed(_pausedElapsed);
            _instancedMaterial.SetFloat(PausedGrowthID, currentGrowth);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
        }

        /// <summary>Stop entirely and reset back to fully hidden (growth = 0).</summary>
        public void Stop()
        {
            CancelGrowthRoutine();
            _pausedElapsed = 0f;
            _currentGrowth = 0f;
            _isRunning = false;
            _isActive = false;
            ClearFlash();
            _instancedMaterial.SetFloat(PausedGrowthID, 0f);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
        }

#region Trigger-Based Growth
        /// <summary>
        /// Trigger mode: grow the vines by <paramref name="amount"/> (0-1, same
        /// scale as growth itself) from wherever they are now. e.g. 0.2 takes
        /// 40% -> 60%. Clamped at 1.
        /// </summary>
        public void GrowBy(float amount)
        {
            if (!triggerBased || !_isActive) return;

            CancelGrowthRoutine();
            float target = Mathf.Clamp01(_currentGrowth + amount);

            // Flash, flash, flash... THEN grow: hold the growth tween until the
            // last flash of the burst has finished.
            if (flashOnGrow)
            {
                TriggerFlash();
            }

            _growthRoutine = StartCoroutine(AnimateGrowth(target, growStepDuration, growDelay));
        }

        /// <summary>Start a burst of flashCount rapid flashes right now.</summary>
        private void TriggerFlash()
        {
            _instancedMaterial.SetFloat(FlashCountID, flashCount);
            _instancedMaterial.SetFloat(FlashPulseDurationID, flashPulseDuration);
            _instancedMaterial.SetFloat(FlashTriggerTimeID, Time.time);
        }

        /// <summary>Cancel any burst (sets the start time far in the past).</summary>
        private void ClearFlash()
        {
            _instancedMaterial.SetFloat(FlashTriggerTimeID, -1000f);
        }

        private IEnumerator AnimateGrowth(float target, float stepTime, float startDelay = 0f)
        {
            // Scaled wait on purpose: the flash burst runs on shader _Time.y
            // (scaled), so the delay stays in sync with it.
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            float from = _currentGrowth;
            float elapsed = 0f;

            // Unscaled so a step triggered as the game pauses still finishes.
            while (elapsed < stepTime && !Mathf.Approximately(_currentGrowth, target))
            {
                elapsed += Time.unscaledDeltaTime;
                _currentGrowth = Mathf.Lerp(from, target, elapsed / Mathf.Max(stepTime, 0.0001f));
                _instancedMaterial.SetFloat(PausedGrowthID, _currentGrowth);
                yield return null;
            }

            _currentGrowth = target;
            _instancedMaterial.SetFloat(PausedGrowthID, target);
            _growthRoutine = null;
        }
#endregion

        /// <summary>
        /// Retreat the vines by <paramref name="amount"/> (0-1, same scale as
        /// growth). Time mode: freezes, retreats, then resumes the countdown.
        /// Trigger mode: just retreats and holds; nothing grows until the next GrowBy().
        /// </summary>
        public void ReverseGrowth(float amount)
        {
            CancelGrowthRoutine();

            if (triggerBased)
            {
                float triggerTarget = Mathf.Clamp01(_currentGrowth - amount);
                _growthRoutine = StartCoroutine(AnimateGrowth(triggerTarget, reverseDuration));
                return;
            }

            CaptureElapsed();
            _isRunning = false;

            float current = GetGrowthFromElapsed(_pausedElapsed);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
            _instancedMaterial.SetFloat(PausedGrowthID, current);

            float target = Mathf.Clamp01(current - amount);
            _growthRoutine = StartCoroutine(ReverseRoutine(current, target));
        }

        private IEnumerator ReverseRoutine(float from, float target)
        {
            float elapsed = 0f;
            float value = from;

            while (elapsed < reverseDuration && !Mathf.Approximately(value, target))
            {
                elapsed += Time.unscaledDeltaTime;
                value = Mathf.Lerp(from, target, elapsed / reverseDuration);
                _instancedMaterial.SetFloat(PausedGrowthID, value);
                yield return null;
            }

            value = target;
            _instancedMaterial.SetFloat(PausedGrowthID, value);
            _pausedElapsed = value * Mathf.Max(duration, 0.0001f);

            _growthRoutine = null;

            Resume();
        }

        private void CaptureElapsed()
        {
            if (!_isRunning) return;

            float storedStartTime = _instancedMaterial.GetFloat(StartTimeID);
            _pausedElapsed = Mathf.Clamp(Time.time - storedStartTime, 0f, duration);
        }

        private float GetGrowthFromElapsed(float elapsedSeconds)
        {
            return duration > 0f ? Mathf.Clamp01(elapsedSeconds / duration) : 1f;
        }

        private void CancelGrowthRoutine()
        {
            if (_growthRoutine != null)
            {
                StopCoroutine(_growthRoutine);
                _growthRoutine = null;
            }
        }

        // private void HandleGameOver()
        // {
            
        // }
    }
}