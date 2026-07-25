using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This helper class lets us create an editable list in the Inspector.
[System.Serializable]
public class Sound
{
    public SoundID id;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmMainMenuSource;
    [SerializeField] private AudioSource _bgmGameplaySource;
    [SerializeField] private AudioSource[] _sfxSources;

    [Header("Audio Settings")]
    [Tooltip("The maximum volume for the BGM tracks when fully faded in.")]
    [Range(0f, 1f)] public float maxBgmVolume = 1f;
    [Tooltip("How long crossfades take in seconds.")]
    public float fadeDuration = 1.5f;

    [Header("Audio Clips Library")]
    [SerializeField] private Sound[] _sfxLibrary;

    private Dictionary<SoundID, AudioClip> _sfxDictionary;

    private Coroutine _currentFadeRoutine;
    private GameState _previousState;

    private void Awake()
    {
        // Absolute Decoupling: No Singletons. Just survive the scene load.
        DontDestroyOnLoad(gameObject);

        // Populate the Dictionary
        _sfxDictionary = new Dictionary<SoundID, AudioClip>();
        foreach (var sound in _sfxLibrary)
        {
            _sfxDictionary[sound.id] = sound.clip;
        }
    }
    void Start()
    {

        if (_bgmMainMenuSource != null && !_bgmMainMenuSource.isPlaying)
        {
            _bgmMainMenuSource.volume = maxBgmVolume;
            _bgmMainMenuSource.Play();
        }
    }

    private void OnEnable()
    {
        GameEventBus.OnGameStateChanged += HandleStateChange;
        GameEventBus.OnPlaySFXCommand += PlaySFX;
    }

    private void OnDisable()
    {
        GameEventBus.OnGameStateChanged -= HandleStateChange;
        GameEventBus.OnPlaySFXCommand -= PlaySFX;
    }

    // ==========================================
    // BGM STATE MACHINE
    // ==========================================
    private void HandleStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                _bgmMainMenuSource.pitch = 1f; // Reset pitch just in case
                TransitionBGM(fadeInSource: _bgmMainMenuSource, fadeOutSource: _bgmGameplaySource);
                break;

            case GameState.Gameplay:
                _bgmGameplaySource.pitch = 1f; // Ensure normal speed
                _bgmGameplaySource.UnPause(); // In case we were paused by an ad, unpause to ensure it starts fading in audibly

                if (_previousState == GameState.MainMenu || _previousState == GameState.GameOver)
                {
                    // Coming from the menu or a fresh restart: Crossfade to Gameplay BGM
                    TransitionBGM(fadeInSource: _bgmGameplaySource, fadeOutSource: _bgmMainMenuSource);
                }
                // If coming from SecondChance (Revived) or Paused, it just resumes normal pitch.
                break;
            case GameState.Paused:
                // Slow down the gameplay track by half!
                if (_bgmGameplaySource != null) _bgmGameplaySource.Pause();
                break;

            case GameState.GameOver:
                // Fade out the gameplay music, fade in NOTHING.
                TransitionBGM(fadeInSource: null, fadeOutSource: _bgmGameplaySource);
                break;
        }

        _previousState = state;
    }

    // ==========================================
    // CROSSFADE LOGIC (TimeScale Independent)
    // ==========================================
    private void TransitionBGM(AudioSource fadeInSource, AudioSource fadeOutSource)
    {
        if (_currentFadeRoutine != null)
        {
            StopCoroutine(_currentFadeRoutine);
        }
        _currentFadeRoutine = StartCoroutine(CrossfadeRoutine(fadeInSource, fadeOutSource, fadeDuration));
    }

    private IEnumerator CrossfadeRoutine(AudioSource fadeInSource, AudioSource fadeOutSource, float duration)
    {
        float time = 0;
        float fadeOutStartVol = fadeOutSource != null ? fadeOutSource.volume : 0;

        if (fadeInSource != null)
        {
            fadeInSource.volume = 0;
            if (!fadeInSource.isPlaying) fadeInSource.Play();
        }

        while (time < duration)
        {
            // WE MUST USE UNSCALED DELTA TIME because Time.timeScale is 0 during Menus and Game Over!
            time += Time.unscaledDeltaTime;
            float t = time / duration;

            if (fadeOutSource != null) fadeOutSource.volume = Mathf.Lerp(fadeOutStartVol, 0, t);
            if (fadeInSource != null) fadeInSource.volume = Mathf.Lerp(0, maxBgmVolume, t);

            yield return null; // Wait for next frame
        }

        // Snap to final values to prevent math floating point errors
        if (fadeOutSource != null)
        {
            fadeOutSource.volume = 0;
            fadeOutSource.Stop();
        }
        if (fadeInSource != null)
        {
            fadeInSource.volume = maxBgmVolume;
        }
    }

    // ==========================================
    // SFX CONTROLS
    // ==========================================
    private void PlaySFX(SoundID id)
    {

        if (!_sfxDictionary.ContainsKey(id))
        {
            Debug.LogWarning("AudioManager: Sound ID not found in library: " + id);
            return;
        }

        AudioClip clipToPlay = _sfxDictionary[id];

        // 1. Try to use a pre-made source
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            if (_sfxSources[i] != null && !_sfxSources[i].isPlaying)
            {
                _sfxSources[i].PlayOneShot(clipToPlay);
                return;
            }
        }

        // 2. If all are busy, create a temporary one
        StartCoroutine(CreateTemporarySourceAndPlay(clipToPlay));
    }

    private IEnumerator CreateTemporarySourceAndPlay(AudioClip clip)
    {
        GameObject tempGO = new("TempAudio_" + clip.name);
        tempGO.transform.SetParent(this.transform);
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();

        if (_sfxSources.Length > 0 && _sfxSources[0] != null)
        {
            tempSource.outputAudioMixerGroup = _sfxSources[0].outputAudioMixerGroup;
            tempSource.spatialBlend = _sfxSources[0].spatialBlend;
        }

        tempSource.PlayOneShot(clip);
        // Use Realtime so SFX can finish playing even if the game pauses mid-sound
        yield return new WaitForSecondsRealtime(clip.length);
        Destroy(tempGO);
    }
}