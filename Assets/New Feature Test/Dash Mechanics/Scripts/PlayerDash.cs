using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;

public class PlayerDash : MonoBehaviour
{
    [Header(" Dash Settings ")]
    [SerializeField] private float dashCoolDownTime;
    [SerializeField] private float dashDuration;
    [SerializeField] private int testDashCount;

    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private bool canDash = false;

    [Header(" Dash Collectable ")]
    [SerializeField] private int maxDashCollectableCount = 3;
    [SerializeField] private int currentDashCollectableCount;

    [Header(" Echo Effect ")]
    [SerializeField] DashEchoEffect echoPrefab;
    [SerializeField] Transform parentTransform;
    [SerializeField] float echoSpawnInterval = 0.05f;
    [SerializeField] int poolSize = 10;
    // private int maxPoolSize;
    ObjectPool<DashEchoEffect> echoPool;


    private PlayerController playerController;
    private PlayerVisuals playerVisuals;
    private float lastDashTime;
    private bool triggerDash = false;
    private bool isInit = false;

#region Temp Code 
void Start()
{
    currentDashCollectableCount = testDashCount;
}
#endregion

    void OnEnable()
    {
        currentDashCollectableCount = 0;
        GameEventBus.OnPlayerContactWithItem += AddDashCount;
    }
    void OnDisable()
    {
        GameEventBus.OnPlayerContactWithItem -= AddDashCount;
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if( collision.TryGetComponent<ToDashItem>( out ToDashItem item ) )
        {
            item.TriggerFunction();
        }
    }
    void Update()
    {
        if( canDash && isInit )
        {
            bool dashTimeElapsed = Time.time >= lastDashTime + dashDuration;

            bool cooldownElapsed = Time.time >= lastDashTime + dashCoolDownTime;
    
            // End the current dash burst once its own (shorter) duration is up.
            // Face revert now handled by PlayerVisuals.UpdateLocomotionFace() (LateUpdate)
            // based on IsGroundedRaw + IsIdle, so no forced Moving here (fixes airborne Moving flicker).
            if (dashTimeElapsed && playerController.isDashing)
            {
                playerController.UnDash();
            }

            // Only start a new dash once the cooldown has actually cleared.
            if (cooldownElapsed &&  triggerDash )
            {
                PerformDash();
                TriggerDashEffect();
            }
        }
    }

    public void InitDash( PlayerController controller)
    {
        playerController = controller;
        CreatePool();
        isInit = true;
    }

    public void TriggerDash()
    {
        bool cooldownElapsed = Time.time >= lastDashTime + dashCoolDownTime;

        if( currentDashCollectableCount > 0 && playerController.IsPlayerMoving() && cooldownElapsed )
        {
            triggerDash = true;
        }
    }    

    private void AddDashCount()
    {
        if( currentDashCollectableCount < maxDashCollectableCount)
        {
            currentDashCollectableCount++;
        }
    }
    private void PerformDash()
    {
        if( playerController == null) return;

        lastDashTime = Time.time;
        triggerDash = false;
        currentDashCollectableCount--;
        
        playerController.ApplyDash( dashSpeed );
    }

#region Object Pool

    private void TriggerDashEffect()
    {
        GameEventBus.TriggerPlayerDash();
        StartCoroutine(SpawnEchoTrail());
        // GameEventBus.TriggerPlayerFaceChange(Playerface.Dash);
        // playerVisuals.TriggerSquashEffect();
    }
 
    private IEnumerator SpawnEchoTrail()
    {
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            DashEchoEffect echo = echoPool.Get();
            echo.transform.position = playerController.transform.position;
 
            elapsed += echoSpawnInterval;
            yield return new WaitForSeconds(echoSpawnInterval);
        }
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
            // maxSize: maxPoolSize
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

        DashEchoEffect echo = Instantiate( echoPrefab, Vector3.zero, Quaternion.identity );
        echo.transform.SetParent(parentTransform, true);
        echo.ReturnToPool +=  ReturnObjectToPool;
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
        // Instance.transform.SetParent(parentTransform, true);
    }

    private void OnReturnToPool(DashEchoEffect Instance)
    {
        Instance.gameObject.SetActive(false);
    }

    private void OnDestroyObject(DashEchoEffect Instance)
    {
        if( Instance != null )
        {
            Destroy(Instance.gameObject);
        }
    }
#endregion
}