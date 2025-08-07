using UnityEngine;

public class Damage : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] int regSpeed;

    public int damageAmount;
    public int destoryTime;

    void Start()
    {
        Destroy(gameObject, destoryTime);

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
