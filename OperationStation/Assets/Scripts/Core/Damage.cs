using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Damage : MonoBehaviour
{
    public enum damageType { bullet, explosion, collision }
    [SerializeField] damageType type;
    [SerializeField] Rigidbody rb;
    [SerializeField] int regSpeed;

    public float damageAmount;
    public float destroyTime;

    void Start()
    {
        if (type != damageType.collision)
        {
            Destroy(gameObject, destroyTime);

            rb.linearVelocity = transform.forward * regSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        MiningShip ship = other.GetComponent<MiningShip>();

        if (ship != null && ship.playerControlled)
            return;

        DealDamage(other);
        if (type != damageType.collision)
            Destroy(gameObject);
    }

    void DealDamage(Collider other)
    {
       IDamage dmg = other.GetComponent<IDamage>();
       
        if (dmg != null)
        {
            dmg.TakeDamage(damageAmount);
        }
    }
}
