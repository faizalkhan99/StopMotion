using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Settings")]
    [SerializeField] private string _mainMenuSceneName;
    [SerializeField] private string _gameSceneName;

    [Header("Loading Screen")]
    [SerializeField] private CanvasGroup _loadingScreen;
    [SerializeField] private float _fadeDuration = 0.5f;

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
        
        if (_loadingScreen)
        {
            _loadingScreen.alpha = 0f;
            _loadingScreen.interactable = false;
            _loadingScreen.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == _mainMenuSceneName)
            ChangeState(GameState.MainMenu);
        else
            ChangeState(GameState.Playing);
    }

    private void OnEnable()
    {
        // Fully unified: The manager listens exclusively to the universal death event
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
    } 

    private void OnDisable()
    {
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
    } 

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        
        switch (CurrentState)
        {
            case GameState.MainMenu: 
            case GameState.Playing: 
            case GameState.GameOver: 
                Time.timeScale = 1f; 
                break;
            case GameState.Paused: 
                Time.timeScale = 0f; 
                break;
        }

        GameEvents.TriggerGameStateChanged(CurrentState);
    }

    private void HandlePlayerDeath()
    {
        GameEvents.TriggerPlaySFXCommand(SoundID.GameOver);
        ChangeState(GameState.GameOver);

    }

    public void LoadGameSceneAsync()
    {
        StartCoroutine(TransitionRoutine(_gameSceneName, GameState.Playing));
    }

    public void ReturnToMainMenuAsync()
    {
        StartCoroutine(TransitionRoutine(_mainMenuSceneName, GameState.MainMenu));
    }

    private IEnumerator TransitionRoutine(string sceneName, GameState stateAfterLoad)
    {
        if (_loadingScreen)
        {
            _loadingScreen.blocksRaycasts = true;
            float t = 0;
            while (t < _fadeDuration)
            {
                t += Time.unscaledDeltaTime; 
                _loadingScreen.alpha = t / _fadeDuration;
                yield return null;
            }
            _loadingScreen.alpha = 1f;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Hold scene activation until Unity finishes loading the assets into memory
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
        yield return new WaitUntil(() => asyncLoad.isDone);
        
        ChangeState(stateAfterLoad);

        if (_loadingScreen)
        {
            float t = 0;
            while (t < _fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _loadingScreen.alpha = 1f - (t / _fadeDuration);
                yield return null;
            }
            _loadingScreen.alpha = 0f;
            _loadingScreen.blocksRaycasts = false;
        }
    }
}