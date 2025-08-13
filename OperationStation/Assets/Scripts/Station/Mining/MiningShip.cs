using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class MiningShip : MonoBehaviour, ISelectable, IDamage
{
    [SerializeField] UnitSO stats;
    [SerializeField] Renderer model;
    [SerializeField] NavMeshAgent agent;

    private float health;
    private Color colorOG;
    private Vector3 idlePos;

    void Start()
    {
        health = stats.unitHealth;
        colorOG = model.material.color;
        idlePos = transform.position;
    }

    void Update()
    {
        
    }

    public void TakeControl()
    {
        throw new System.NotImplementedException();
    }

    public void TakeDamage(float damage)
    {
        health -= damage;

        StartCoroutine(FlashRed());

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Asteroid")
        {
            //Mine
        }
    }

    private IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        model.material.color = colorOG;
    }
}
