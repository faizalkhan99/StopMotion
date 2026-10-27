using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private bool isVisibleOnStart = false;

    public List<Image> lifeIcons = new();

    private void Awake()
    {
        if(!isVisibleOnStart)
        {
            livesContainer.gameObject.SetActive(false);
            return;
        }
        // CollectIcons();
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
        // if(!isVisibleOnStart)
        // {
        //     livesContainer.gameObject.SetActive(false);
        //     return;
        // }

        // CollectIcons();
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
            if (child != null)
            {
                lifeIcons.Add( child.GetChild(0).GetComponent<Image>() );
            } 
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
            if (lifeIcons[i] != null) lifeIcons[i].enabled = (i < lit);
        }
    }
}
