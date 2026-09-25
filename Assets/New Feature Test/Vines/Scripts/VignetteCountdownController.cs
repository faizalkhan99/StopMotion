using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StopMotion.Visuals
{
    /// <summary>
    /// Drives CityBuster/V9's countdown-based growth on a UI Image. The
    /// shader computes growth itself from _Time.y via
    /// lerp(_PausedGrowth, runningGrowth, _IsRunning) — this script only
    /// needs to fire on Play/Pause/Resume/Stop/ReverseGrowth, not every
    /// frame (except while a reverse animation is in flight).
    ///
    /// _PausedGrowth is a plain 0-1 "how grown" fraction, not a radius —
    /// there is no start/end radius to interpolate between.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class VignetteCountdownController : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float reverseDuration = 0.75f;

        private static readonly int DurationID     = Shader.PropertyToID("_Duration");
        private static readonly int StartTimeID    = Shader.PropertyToID("_StartTime");
        private static readonly int PausedGrowthID = Shader.PropertyToID("_PausedGrowth");
        private static readonly int IsRunningID    = Shader.PropertyToID("_IsRunning");

        private Material _instancedMaterial;
        private float _pausedElapsed; // seconds already consumed when paused
        private bool _isRunning;      // mirrors _IsRunning; guards CaptureElapsed()
        private Coroutine _reverseRoutine;

        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            // Instance the material once so we don't edit the shared asset.
            // This does mean this Image can't be UI-batched with siblings
            // that share the original material — expected/fine for a
            // single fullscreen vignette layer.
            _instancedMaterial = new Material(targetImage.material);
            targetImage.material = _instancedMaterial;

            _instancedMaterial.SetFloat(DurationID, duration);
        }
        private void OnEnable()
        {
            GameEventBus.OnDelayEnd += Play;
            GameEventBus.OnLevelDurationUpdated += SetLevelDuration;
            GameEventBus.OnGameStateChanged += HandleEventChanges;
            GameEventBus.OnReverseVines += HandleReverseVines;
        }
        private void OnDisable()
        {
            GameEventBus.OnDelayEnd -= Play;
            GameEventBus.OnLevelDurationUpdated -= SetLevelDuration;
            GameEventBus.OnGameStateChanged += HandleEventChanges;
            GameEventBus.OnReverseVines -= HandleReverseVines;
            CancelReverse();
        }
        private void OnDestroy()
        {
            // Clean up the instanced material to avoid leaking it.
            if (_instancedMaterial != null)
                Destroy(_instancedMaterial);
        }

#region State Change
        private void HandleReverseVines()
        {
            ReverseGrowth( 0.18f );
        }
        private void HandleEventChanges(GameState state)
        {
            switch (state)
            {
                case GameState.LevelComplete :
                    Pause();
                break;
            }
        }
#endregion
        /// <summary>Begin (or restart) the countdown growth from scratch.</summary>
        public void Play()
        {
            CancelReverse();
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

        /// <summary>Resume after a Pause(), preserving elapsed progress.</summary>
        public void Resume()
        {
            CancelReverse();
            _isRunning = true;
            _instancedMaterial.SetFloat(StartTimeID, Time.time - _pausedElapsed);
            _instancedMaterial.SetFloat(IsRunningID, 1f);
        }

        /// <summary>Freeze the vines at their current growth amount.</summary>
        public void Pause()
        {
            CancelReverse();
            CaptureElapsed();
            _isRunning = false;

            float currentGrowth = GetGrowthFromElapsed(_pausedElapsed);
            _instancedMaterial.SetFloat(PausedGrowthID, currentGrowth);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
        }

        /// <summary>Stop entirely and reset back to fully hidden (growth = 0).</summary>
        public void Stop()
        {
            CancelReverse();
            _pausedElapsed = 0f;
            _isRunning = false;
            _instancedMaterial.SetFloat(PausedGrowthID, 0f);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
        }

        /// <summary>
        /// Freeze at the current growth value, then retreat by
        /// <paramref name="amount"/> (0-1, same scale as growth itself) —
        /// e.g. 0.3 pulls a 90%-grown effect back to 60%. Clamped so it
        /// can never go below 0.
        /// </summary>
        public void ReverseGrowth(float amount)
        {
            CancelReverse();
            CaptureElapsed();
            _isRunning = false;

            float current = GetGrowthFromElapsed(_pausedElapsed);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
            _instancedMaterial.SetFloat(PausedGrowthID, current);

            float target = Mathf.Clamp01(current - amount);
            _reverseRoutine = StartCoroutine(ReverseRoutine(current, target));
        }

        private IEnumerator ReverseRoutine(float from, float target)
        {
            float elapsed = 0f;
            float value = from;

            // Unscaled so a reverse triggered right as the game pauses still finishes.
            while (elapsed < reverseDuration && !Mathf.Approximately(value, target))
            {
                elapsed += Time.unscaledDeltaTime;
                value = Mathf.Lerp(from, target, elapsed / reverseDuration);
                _instancedMaterial.SetFloat(PausedGrowthID, value);
                yield return null;
            }

            value = target;
            _instancedMaterial.SetFloat(PausedGrowthID, value);

            // Keep _pausedElapsed consistent with the new frozen growth value
            // so a later Resume() picks back up from the right point.
            _pausedElapsed = value * Mathf.Max(duration, 0.0001f);

            _reverseRoutine = null;

            Resume();
        }

        private void CaptureElapsed()
        {
            // Only meaningful if we were actually running — otherwise
            // _StartTime is stale from the last Play()/Resume() and would
            // overcount elapsed time.
            if (!_isRunning) return;

            float storedStartTime = _instancedMaterial.GetFloat(StartTimeID);
            _pausedElapsed = Mathf.Clamp(Time.time - storedStartTime, 0f, duration);
        }
        private float GetGrowthFromElapsed(float elapsedSeconds)
        {
            return duration > 0f ? Mathf.Clamp01(elapsedSeconds / duration) : 1f;
        }
        private void CancelReverse()
        {
            if (_reverseRoutine != null)
            {
                StopCoroutine(_reverseRoutine);
                _reverseRoutine = null;
            }
        }
    }
}