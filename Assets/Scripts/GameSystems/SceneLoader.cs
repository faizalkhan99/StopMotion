using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles asynchronous scene loading with visual transitions.
/// Guarantees that the gameplay state is only broadcast after the screen is fully visible to the player.
/// </summary>
[DisallowMultipleComponent]
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup loadingPanelCanvasGroup;
    [SerializeField] private TextMeshProUGUI loadingText; // Upgrade to TextMeshProUGUI for production!
    
    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minimumLoadingTime = 1.5f;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure loading screen is hidden and non-blocking on startup
        if (loadingPanelCanvasGroup != null)
        {
            loadingPanelCanvasGroup.alpha = 0f;
            loadingPanelCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Initiates an asynchronous scene load with fade transitions.
    /// </summary>
    public void LoadScene(int buildIndex, GameState targetState)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"<b>[SceneLoader]</b> Build index {buildIndex} is out of range! (Total scenes: {SceneManager.sceneCountInBuildSettings})");
            return;
        }
        StartCoroutine(LoadSceneRoutine(buildIndex, targetState));
    }

    private IEnumerator LoadSceneRoutine(int buildIndex, GameState targetState)
    {
        // 1. Immediately switch state to Loading to freeze game loops and hide UI
        ChangeState(GameState.Loading);

        // 2. Fade In Loading Screen to obscure the screen during unloading/loading
        yield return StartCoroutine(FadeLoadingScreen(1f, fadeDuration));

        // 3. Begin Async Loading
        float startTime = Time.time;
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(buildIndex);
        asyncOperation.allowSceneActivation = false;

        // 4. Track and display progress while loading
        while (asyncOperation.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            UpdateLoadingText(progress);
            yield return null;
        }

        // 5. Enforce minimum loading time to prevent jarring flashes on fast hardware[cite: 7]
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            float remainingTime = minimumLoadingTime - elapsedTime;
            float timer = 0f;
            while (timer < remainingTime)
            {
                timer += Time.deltaTime;
                UpdateLoadingText(Mathf.Lerp(0.9f, 1f, timer / remainingTime));
                yield return null;
            }
        }

        UpdateLoadingText(1f);

        // 6. Allow Unity to activate the new scene in the background[cite: 7]
        asyncOperation.allowSceneActivation = true;

        // Wait until Unity finishes the heavy scene instantiation and initialization frame[cite: 7]
        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        // ------------------------------------------------------------------------
        // CRITICAL TIMING FIX:
        // We do NOT change the state here. The new scene is loaded, but the 
        // loading screen CanvasGroup is still at alpha 1.0 (pitch black).
        // ------------------------------------------------------------------------

        // 7. Fade Out Loading Screen FIRST so the player can actually see the world[cite: 7]
        yield return StartCoroutine(FadeLoadingScreen(0f, fadeDuration));

        // 8. NOW broadcast the target state (e.g., GameState.Gameplay)[cite: 7].
        // This fires frame 1 of actual gameplay right as the screen becomes 100% clear!
        ChangeState(targetState);
    }

    private void UpdateLoadingText(float progress)
    {
        if (loadingText == null) return;
        int percentage = Mathf.RoundToInt(progress * 100f);
        loadingText.text = $"LOADING... {percentage}%"; 
    }

    private IEnumerator FadeLoadingScreen(float targetAlpha, float duration)
    {
        if (loadingPanelCanvasGroup == null) yield break;

        loadingPanelCanvasGroup.blocksRaycasts = true; // Block input during transitions[cite: 7]
        float startAlpha = loadingPanelCanvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            loadingPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        loadingPanelCanvasGroup.alpha = targetAlpha;
        if (targetAlpha == 0f)
        {
            loadingPanelCanvasGroup.blocksRaycasts = false; // Unblock input when fully hidden[cite: 7]
        }
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        GameEventBus.TriggerGameStateChanged(CurrentState);
        Debug.Log($"<b><color=green>[SceneLoader]</color></b> Game State Broadcasted: {CurrentState}");
    }
}