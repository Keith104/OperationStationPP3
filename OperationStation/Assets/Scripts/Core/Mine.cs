using UnityEngine;

public class Mine : MonoBehaviour
{
    [SerializeField] GameObject boom;

    private void OnTriggerEnter(Collider other)
    {
        GameObject explostion = Instantiate(boom, transform.position, transform.rotation);
        explostion.transform.localScale = new Vector3(5, 5, 5);
        Destroy(gameObject);
    }
}