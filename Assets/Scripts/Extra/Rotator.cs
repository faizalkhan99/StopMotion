using System.Collections;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float _waitForSeconds;
    [SerializeField] private float _rotationSpeed;
    [SerializeField] private Vector3 _rotationAxis;


    // Update is called once per frame
    private IEnumerator Start()
    {
        while (true)
        {

            transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime);
            yield return _waitForSeconds;
        }
    }
}
