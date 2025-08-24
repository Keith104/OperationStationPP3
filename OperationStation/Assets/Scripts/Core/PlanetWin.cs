using UnityEngine;

public class PlanetWin : MonoBehaviour
{
    private bool deathImenete = false;

    private void OnTriggerEnter(Collider other)
    {
        deathImenete = true;
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
