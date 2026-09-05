using UnityEngine;
using UnityEngine.UI;

namespace StopMotion.Visuals
{
    /// <summary>
    /// Drives RadiusVignette's countdown-based radius shrink on a UI Image.
    /// The shader computes the shrink itself from _Time.y — this script
    /// only needs to fire on Play/Pause/Resume/Stop, not every frame.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class VignetteCountdownController : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private float startRadius = 0.6f;
        [SerializeField] private float endRadius = 0.05f;
        [SerializeField] private float duration = 10f;

        private static readonly int StartRadiusID  = Shader.PropertyToID("_StartRadius");
        private static readonly int EndRadiusID    = Shader.PropertyToID("_EndRadius");
        private static readonly int DurationID     = Shader.PropertyToID("_Duration");
        private static readonly int StartTimeID    = Shader.PropertyToID("_StartTime");
        private static readonly int PausedRadiusID = Shader.PropertyToID("_PausedRadius");
        private static readonly int IsRunningID    = Shader.PropertyToID("_IsRunning");

        private Material _instancedMaterial;
        private float _pausedElapsed; // seconds already consumed when paused

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

            _instancedMaterial.SetFloat(StartRadiusID, startRadius);
            _instancedMaterial.SetFloat(EndRadiusID, endRadius);
            _instancedMaterial.SetFloat(DurationID, duration);
        }
        private void OnEnable()
        {
            GameEventBus.OnDelayEnd += Play;
        }
        private void OnDestroy()
        {
            // Clean up the instanced material to avoid leaking it.
            if (_instancedMaterial != null)
                Destroy(_instancedMaterial);

            GameEventBus.OnDelayEnd -= Play;
        }

        /// <summary>Begin (or restart) the countdown shrink from scratch.</summary>
        public void Play()
        {
            _pausedElapsed = 0f;
            _instancedMaterial.SetFloat(StartTimeID, Time.time);
            _instancedMaterial.SetFloat(IsRunningID, 1f);
        }

        /// <summary>Resume after a Pause(), preserving elapsed progress.</summary>
        public void Resume()
        {
            _instancedMaterial.SetFloat(StartTimeID, Time.time - _pausedElapsed);
            _instancedMaterial.SetFloat(IsRunningID, 1f);
        }

        /// <summary>Freeze the vignette at its current radius.</summary>
        public void Pause()
        {
            float storedStartTime = _instancedMaterial.GetFloat(StartTimeID);
            _pausedElapsed = Mathf.Clamp(Time.time - storedStartTime, 0f, duration);

            float t = duration > 0f ? _pausedElapsed / duration : 1f;
            float frozenRadius = Mathf.Lerp(startRadius, endRadius, t);

            _instancedMaterial.SetFloat(PausedRadiusID, frozenRadius);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
        }

        /// <summary>Stop entirely and reset back to the full start radius.</summary>
        public void Stop()
        {
            _pausedElapsed = 0f;
            _instancedMaterial.SetFloat(PausedRadiusID, startRadius);
            _instancedMaterial.SetFloat(IsRunningID, 0f);
        }
    }
}