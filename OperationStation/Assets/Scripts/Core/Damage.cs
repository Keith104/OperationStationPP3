using System.Collections;
using UnityEngine;

public class Damage : MonoBehaviour
{
    public enum damageType { bullet, explosion }
    [SerializeField] damageType type;
    [SerializeField] Rigidbody rb;
    [SerializeField] int regSpeed;

    public int damageAmount;
    public float destroyTime;

    void Start()
    {
        Destroy(gameObject, destroyTime);

        rb.linearVelocity = transform.forward * regSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        DealDamage(other);
    }

    void DealDamage(Collider other)
    {
       Debug.Log("Check");
       IDamage dmg = other.GetComponent<IDamage>();
       
        if (dmg != null)
       {
            dmg.TakeDamage(damageAmount);
       }
    }
}
