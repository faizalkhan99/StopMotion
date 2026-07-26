using UnityEngine;

public class LockKeySystem : MonoBehaviour
{
    [SerializeField] private GameObject _respectiveGate;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _respectiveGate.SetActive(false);
            gameObject.SetActive(false);
            //play sfx
        }
    }
}
