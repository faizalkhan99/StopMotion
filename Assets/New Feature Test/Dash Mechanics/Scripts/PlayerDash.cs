using JetBrains.Annotations;
using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [Header(" Dash Settings ")]
    [SerializeField] private float dashCoolDownTime;
    [SerializeField] private float dashDuration;

    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private bool canDash = false;

    [Header(" Dash Collectable ")]
    [SerializeField] private int maxDashCollectableCount = 3;
    [SerializeField] private int currentDashCollectableCount;

    private PlayerController playerController;
    private float lastDashTime;
    private bool triggerDash = false;
    private bool isInit = false;


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
            if (dashTimeElapsed && playerController.isDashing)
            {
                playerController.UnDash();
            }

            // Only start a new dash once the cooldown has actually cleared.
            if (cooldownElapsed &&  triggerDash && currentDashCollectableCount > 0)
            {
                PreformDash();
            }
        }
    }

    public void InitDash( PlayerController controller)
    {
        playerController = controller;
        isInit = true;
    }

    public void TriggerDash()
    {
        if( currentDashCollectableCount > 0)
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
    private void PreformDash()
    {
        lastDashTime = Time.time;
        triggerDash = false;
        currentDashCollectableCount--;
        playerController.ApplyDash( dashSpeed );
    }

}