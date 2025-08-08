using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] int speed;

    [SerializeField] GameObject explosion;
    public int destroyTime;

    void Start()
    {
        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, destroyTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Explode();
        Destroy(gameObject);
    }

    void Explode()
    {
        Instantiate(explosion, transform.position, Quaternion.identity);
    }
}
