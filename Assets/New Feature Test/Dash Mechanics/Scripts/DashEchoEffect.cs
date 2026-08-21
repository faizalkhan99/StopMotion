using System;
using UnityEngine;
using UnityEngine.UIElements;

public class DashEchoEffect : MonoBehaviour
{
    public event Action<DashEchoEffect> ReturnToPool;
    public void OnAnimationDone()
    {
        // TurnOff();
        ReturnToPool?.Invoke(this);
    }


}
