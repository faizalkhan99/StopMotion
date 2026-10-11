using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lives HUD driven by child icons under a container (e.g. Lives HUD -> 1, 2, 3).
/// Drag the container Transform in; on Start its children are cached, and on
/// every lives change the first N stay on while the rest are turned off.
/// Syncs via OnLivesChanged; re-syncs on Gameplay enter for late-enablers.
/// </summary>
[DisallowMultipleComponent]
public class LivesHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent holding one child per life icon (e.g. Lives HUD -> 1, 2, 3).")]
    [SerializeField] private Transform livesContainer;

    private readonly List<GameObject> lifeIcons = new();

    private void Awake()
    {
        CollectIcons();
    }

    private void OnValidate()
    {
        CollectIcons();
    }

    private void OnEnable()
    {
        GameEventBus.OnLivesChanged += RefreshLives;
        GameEventBus.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEventBus.OnLivesChanged -= RefreshLives;
        GameEventBus.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        CollectIcons();
        RefreshLives(GameEventBus.CurrentLives > 0 ? GameEventBus.CurrentLives : lifeIcons.Count);
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Gameplay)
            RefreshLives(GameEventBus.CurrentLives > 0 ? GameEventBus.CurrentLives : lifeIcons.Count);
    }

    private void CollectIcons()
    {
        lifeIcons.Clear();
        if (livesContainer == null) return;
        foreach (Transform child in livesContainer)
        {
            if (child != null) lifeIcons.Add(child.gameObject);
        }
    }

    private void RefreshLives(int lives)
    {
        if (livesContainer == null)
        {
            Debug.LogError("<b>[LivesHUD]</b> Lives container is not assigned.");
            return;
        }

        int lit = Mathf.Clamp(lives, 0, lifeIcons.Count);
        for (int i = 0; i < lifeIcons.Count; i++)
        {
            if (lifeIcons[i] != null) lifeIcons[i].SetActive(i < lit);
        }
    }
}
