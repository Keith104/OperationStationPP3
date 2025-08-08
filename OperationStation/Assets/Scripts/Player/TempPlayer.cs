using UnityEngine;

public class TempPlayer : MonoBehaviour, IDamage
{
    //A useless temp script to help test if enemies did damage
    //once a proper station script is created feel free to delete

    public float health = 3;

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
