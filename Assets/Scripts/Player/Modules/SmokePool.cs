using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Plain-class pooled jump/land smoke VFX — no MonoBehaviour. Owned by PlayerVisuals.
/// The pool stores the ParticleSystem component itself; liveSmoke tracks currently-playing
/// puffs so the visuals' LateUpdate can Tick() releases for finished ones back into the pool.
/// </summary>
public class SmokePool
{
    readonly GameObject smokeVFXPrefab;
    readonly int poolSize;

    ObjectPool<ParticleSystem> smokePool;
    readonly List<ParticleSystem> liveSmoke = new List<ParticleSystem>(8);

    public SmokePool(GameObject smokeVFXPrefab, int poolSize)
    {
        this.smokeVFXPrefab = smokeVFXPrefab;
        this.poolSize = poolSize;
    }

    /// <summary>
    /// Created lazily from PlayerVisuals.Awake (pool creation needs the prefab + VFX sanity checks).
    /// Returns false (puffs disabled) when the smoke VFX reference is missing or malformed.
    /// </summary>
    public bool TryCreate()
    {
        if (smokeVFXPrefab == null)
        {
            Debug.LogWarning("<b>[PlayerVisuals]</b> Smoke VFX not assigned — jump/land puffs disabled!");
            return false;
        }

        if (smokeVFXPrefab.GetComponent<ParticleSystem>() == null)
        {
            Debug.LogError("<b>[PlayerVisuals]</b> Smoke VFX prefab has no ParticleSystem on its root — puffs disabled!");
            return false;
        }

        smokePool = new ObjectPool<ParticleSystem>(
            CreateSmokeInstance,
            OnTakeFromSmokePool,
            OnReturnToSmokePool,
            null,
            collectionCheck: true,
            defaultCapacity: poolSize
        );

        // Prewarm: prime the stack so the first jumps never pay Instantiate cost (mirrors PlayerDash.CreatePool).
        var warmupBuffer = new ParticleSystem[poolSize];
        for (int i = 0; i < poolSize; i++)
            warmupBuffer[i] = smokePool.Get();
        for (int i = 0; i < poolSize; i++)
            smokePool.Release(warmupBuffer[i]);

        return true;
    }

    public void Spawn(Vector2 position)
    {
        if (smokePool == null) return;

        ParticleSystem particles = smokePool.Get();
        if (particles == null) return;

        particles.transform.position = position;
        particles.Play();
        liveSmoke.Add(particles);
    }

    /// <summary>Pump from PlayerVisuals.LateUpdate — releases finished puffs back into the pool.</summary>
    public void Tick()
    {
        for (int i = liveSmoke.Count - 1; i >= 0; i--)
        {
            ParticleSystem particles = liveSmoke[i];
            if (particles != null && particles.IsAlive(true)) continue;

            // Dead puffs come off the live list; only surviving references re-enter the pool.
            liveSmoke.RemoveAt(i);
            if (particles != null && smokePool != null) smokePool.Release(particles);
        }
    }

    public void ReleaseAll()
    {
        for (int i = liveSmoke.Count - 1; i >= 0; i--)
        {
            ParticleSystem particles = liveSmoke[i];
            liveSmoke.RemoveAt(i);
            if (particles != null && smokePool != null) smokePool.Release(particles);
        }
    }

    private ParticleSystem CreateSmokeInstance()
    {
        GameObject instance = Object.Instantiate(smokeVFXPrefab);
        instance.SetActive(false);
        return instance.GetComponent<ParticleSystem>();
    }

    private void OnTakeFromSmokePool(ParticleSystem particles)
    {
        particles.gameObject.SetActive(true);
    }

    private void OnReturnToSmokePool(ParticleSystem particles)
    {
        particles.gameObject.SetActive(false);
    }
}
