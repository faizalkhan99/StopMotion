using UnityEngine;

public class ToDashItem : MonoBehaviour
{
    private readonly int dashPoint = 1;
    [SerializeField] private bool destroy = false;
    public void TriggerFunction()
    {
        GameEventBus.TriggerPlayerContactWithItem();
        //Destroy this component after firing event
        if( destroy )
        {
            Destroy( this.gameObject , 2f );
        }
    }
}