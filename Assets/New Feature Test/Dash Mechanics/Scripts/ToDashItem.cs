using UnityEngine;

public class ToDashItem : MonoBehaviour
{
    private readonly int dashPoint = 1;

    public void TriggerFunction()
    {
        GameEventBus.TriggerPlayerContactWithItem();
        //Destroy this component after firing event
        Destroy( this.gameObject , 2f );
    }
}