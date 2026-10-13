using System.Collections;
using UnityEngine;

/// <summary>
/// Stores level-start spawn + last one-shot checkpoint, teleports the player
/// there after a short delay with i-frames + blink feedback.
/// Consumes <see cref="GameEventBus.OnRespawnRequested"/> and
/// <see cref="GameEventBus.OnCheckpointReached"/>; fires
/// <see cref="GameEventBus.OnPlayerRespawnStarted"/>,
/// <see cref="GameEventBus.OnPlayerRespawned"/> and
/// <see cref="GameEventBus.OnPlayerVulnerable"/>.
/// Keys/gates are intentionally left untouched on respawn.
/// No statics: all communication goes through <see cref="GameEventBus"/>.
/// </summary>
[DisallowMultipleComponent]
public class RespawnManager : MonoBehaviour
{
    [Header("Player (auto-found by tag if empty)")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerTag = "Player";

    [Header("Respawn Feel")]
    [Tooltip("Delay before teleporting (lets death SFX/face read).")]
    [SerializeField] private float respawnDelay = 0.6f;
    [Tooltip("Invulnerability window after teleport. End is broadcast via OnPlayerVulnerable.")]
    [SerializeField] private float invulnerabilityDuration = 1.5f;
    [Tooltip("Blink rate during i-frames.")]
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Fall Death")]
    [SerializeField] private bool killOnFall = true;
    [SerializeField] private float killY = -15f;

    private bool isRespawning;

    private Vector3 initialSpawn;
    private bool hasInitialSpawn;
    private Vector3? lastCheckpoint;
    private Coroutine blinkRoutine;

    private Rigidbody2D playerRb;
    private PlayerController playerController;
    private PlayerKeyboardInput keyboardInput;
    private PlayerInputRouter inputRouter;
    private SpriteRenderer[] playerSprites;

    private void Awake()
    {
        CachePlayer();
    }

    private void Start()
    {
        if (playerTransform != null)
        {
            initialSpawn = playerTransform.position;
            hasInitialSpawn = true;
        }
        else
        {
            Debug.LogError("<b>[RespawnManager]</b> No Player found — respawn falls back to origin. Fix Level setup (Level_0 has no Player).");
            initialSpawn = Vector3.zero;
            hasInitialSpawn = true;
        }
    }

    private void OnEnable()
    {
        GameEventBus.OnCheckpointReached += HandleCheckpointReached;
        GameEventBus.OnRespawnRequested += RequestRespawn;
    }

    private void OnDisable()
    {
        GameEventBus.OnCheckpointReached -= HandleCheckpointReached;
        GameEventBus.OnRespawnRequested -= RequestRespawn;
    }

    /// <summary>
    /// Scene-setup guarantee only: makes sure a RespawnManager exists so Hazard
    /// / Checkpoint prefabs work in levels that forgot one. Stores nothing.
    /// </summary>
    public static void EnsurePresent()
    {
        if (Object.FindAnyObjectByType<RespawnManager>() != null) return;
        new GameObject("RespawnManager (Auto)").AddComponent<RespawnManager>();
    }

    private void Update()
    {
        if (!killOnFall || isRespawning) return;
        if (playerTransform == null) return;
        if (GameEventBus.CurrentGameState != GameState.Gameplay) return;

        if (playerTransform.position.y < killY)
            GameEventBus.TriggerHazardHit();
    }

    private void RequestRespawn()
    {
        if (isRespawning) return;
        if (!hasInitialSpawn) return;
        StartCoroutine(RespawnRoutine());
    }

    private void HandleCheckpointReached(Vector3 pos)
    {
        lastCheckpoint = pos;
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        CachePlayer(); // re-cache in case player was instantiated late
        SetInputEnabled(false);

        // Freeze the camera follow via bus (no direct camera reference —
        // managers stay wiring-free and auto-created instances just work).
        GameEventBus.TriggerPlayerRespawnStarted();

        if (playerController != null)
            playerController.StopMovement();


        yield return new WaitForSeconds(respawnDelay);

        Vector3 target = lastCheckpoint ?? initialSpawn;
        TeleportPlayer(target);

        GameEventBus.TriggerPlayerFaceChange(Playerface.Idle);
        GameEventBus.TriggerPlayerRespawned();
        GameEventBus.TriggerPlaySFXCommand(SoundID.Respawn);

        RestartBlink();
        SetInputEnabled(true);
        isRespawning = false;
    }

    private void TeleportPlayer(Vector3 target)
    {
        if (playerTransform == null) return;

        // Detach from moving platforms first (covers the no-PlayerController fallback
        // paths below; the PlayerController path detaches again inside TeleportTo).
        playerTransform.SetParent(null, true);

        if (playerController != null)
        {
            playerController.TeleportTo(target);
        }
        else if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.MovePosition(target);
            playerTransform.position = target;
        }
        else
        {
            playerTransform.position = target;
        }

        // Ensure sprites end visible even if a death effect hid them.
        if (playerSprites != null)
        {
            foreach (var s in playerSprites)
                if (s != null) s.enabled = true;
        }
    }

    private void SetInputEnabled(bool state)
    {
        if (keyboardInput != null) keyboardInput.enabled = state;
        if (inputRouter != null) inputRouter.enabled = state;
    }

    private void CachePlayer()
    {
        if (playerTransform == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) playerTransform = go.transform;
        }
        if (playerTransform == null) return;

        playerRb = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb == null) playerRb = playerTransform.GetComponentInChildren<Rigidbody2D>();
        playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController == null) playerController = playerTransform.GetComponentInChildren<PlayerController>();
        keyboardInput = playerTransform.GetComponent<PlayerKeyboardInput>();
        if (keyboardInput == null) keyboardInput = playerTransform.GetComponentInChildren<PlayerKeyboardInput>();
        inputRouter = playerTransform.GetComponent<PlayerInputRouter>();
        if (inputRouter == null) inputRouter = playerTransform.GetComponentInChildren<PlayerInputRouter>();
        playerSprites = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void RestartBlink()
    {
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        float end = Time.time + invulnerabilityDuration;
        bool visible = true;
        while (Time.time < end)
        {
            // Don't hide during an active death freeze.
            if (!isRespawning && playerSprites != null)
            {
                visible = !visible;
                foreach (var s in playerSprites)
                    if (s != null) s.enabled = visible;
            }
            yield return new WaitForSeconds(blinkInterval);
        }
        if (playerSprites != null)
        {
            foreach (var s in playerSprites)
                if (s != null) s.enabled = true;
        }
        blinkRoutine = null;
        // I-frames over: deaths count again from here.
        GameEventBus.TriggerPlayerVulnerable();
    }
}
