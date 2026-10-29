using System;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Plain-class pooled echo-trail renderer for the dash burst — no MonoBehaviour.
/// Was PlayerDash.StartCoroutine(SpawnEchoTrail); now pumped by PlayerController.Update()
/// with dt accumulation so the whole echo system survives without a component.
/// </summary>
public class EchoTrailSpawner
{
    readonly DashEchoEffect echoPrefab;
    readonly Transform parentTransform;
    readonly Transform playerTransform;
    readonly float echoSpawnInterval;
    readonly int poolSize;

    ObjectPool<DashEchoEffect> echoPool;

    // Trail pump state (driven by Tick)
    private float activeUntil;
    private float spawnTimer;
    private float activeDirectionX;

    public EchoTrailSpawner(DashEchoEffect echoPrefab, Transform parentTransform, Transform playerTransform, float echoSpawnInterval, int poolSize)
    {
        this.echoPrefab = echoPrefab;
        this.parentTransform = parentTransform;
        this.playerTransform = playerTransform;
        this.echoSpawnInterval = echoSpawnInterval;
        this.poolSize = poolSize;

        CreatePool();
    }

    /// <summary>Called once per dash burst (same call site as PlayerDash.TriggerDashEffect).</summary>
    public void Begin(float dashDirectionX, float dashDuration)
    {
        activeDirectionX = dashDirectionX;
        activeUntil = Time.time + dashDuration;
        spawnTimer = echoSpawnInterval;
        SpawnEcho();
    }

    /// <summary>Pump from PlayerController.Update() — replaces the WaitForSeconds coroutine.</summary>
    public void Tick()
    {
        if (Time.time >= activeUntil) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer += echoSpawnInterval;
        SpawnEcho();
    }

    private void SpawnEcho()
    {
        DashEchoEffect echo = echoPool.Get();
        echo.SetFacing(activeDirectionX);
        echo.transform.position = playerTransform.position;
        echo.transform.rotation = Quaternion.identity;
    }

    private void CreatePool()
    {
        echoPool = new ObjectPool<DashEchoEffect>(
            CreatePooledObject,
            OnTakeFromPool,
            OnReturnToPool,
            OnDestroyObject,
            collectionCheck: true,
            defaultCapacity: poolSize
        );

        // Prewarm: pull `poolSize` instances out (forcing CreatePooledObject
        // to fire each time, since the stack is empty until we release),
        // then hand them all back.
        var warmupBuffer = new DashEchoEffect[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            warmupBuffer[i] = echoPool.Get();
        }
        for (int i = 0; i < poolSize; i++)
        {
            echoPool.Release(warmupBuffer[i]);
        }
    }

    private DashEchoEffect CreatePooledObject()
    {
        DashEchoEffect echo = UnityEngine.Object.Instantiate(echoPrefab, Vector3.zero, Quaternion.identity);
        echo.transform.SetParent(parentTransform, true);
        echo.ReturnToPool += ReturnObjectToPool;
        echo.gameObject.SetActive(false);

        return echo;
    }

    private void ReturnObjectToPool(DashEchoEffect echo)
    {
        echoPool.Release(echo);
    }

    private void OnTakeFromPool(DashEchoEffect Instance)
    {
        Instance.gameObject.SetActive(true);
    }

    private void OnReturnToPool(DashEchoEffect Instance)
    {
        Instance.gameObject.SetActive(false);
    }

    private void OnDestroyObject(DashEchoEffect Instance)
    {
        if (Instance != null)
        {
            UnityEngine.Object.Destroy(Instance.gameObject);
        }
    }
}
