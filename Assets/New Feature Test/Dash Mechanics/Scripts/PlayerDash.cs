using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [Header(" Dash Settings ")]
    [SerializeField] private float dashCoolDownTime;
    [SerializeField] private float dashDuration;

    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private bool canDash = false;

    [Header(" Dash Collectable ")]
    [SerializeField] private readonly int maxDashCollectableCount = 3;
    [SerializeField] private int currentDashCollectableCount;

    private PlayerController playerController;
    private float playerMaxSpeed;
    private float dashCoolDownTimer;
    private float lastDashTime;
    private bool triggerDash = false;


    public void InitDash(float playerMaxSpeed, PlayerController controller)
    {
        playerController = controller;
    }

    public void TriggerDash()
    {
        triggerDash = true;
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        
    }
    void Update()
    {
        if( canDash )
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

    private void PreformDash()
    {
        lastDashTime = Time.time;
        triggerDash = false;
        playerController.ApplyDash( dashSpeed );
    }

}