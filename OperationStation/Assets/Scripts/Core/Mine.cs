using UnityEngine;

public class Mine : MonoBehaviour
{
    [SerializeField] GameObject boom;

    private void OnTriggerEnter(Collider other)
    {
        Instantiate(boom, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}