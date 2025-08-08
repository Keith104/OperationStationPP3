using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour, IDamage
{
    [SerializeField] Renderer model;
    [SerializeField] int health;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform shootPos;
    [SerializeField] GameObject bullet;

    float shootTimer;

    private GameObject station;
    private Color colorOG;

    void Start()
    {
        //This isn't final I'll fix/change this when I know how we're implementing the station
        station = GameObject.FindWithTag("Player");
        colorOG = model.material.color;
    }

    void Update()
    {
        Attack();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        StartCoroutine(FlashRed());

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    void Attack()
    {
        agent.SetDestination(station.transform.position);
        shootTimer += Time.deltaTime;

        if (agent.remainingDistance < 10 && shootTimer >= 5)
        {
            shootTimer = 0;
            Instantiate(bullet, shootPos.position, transform.rotation);
        }
    }

    IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOG;
    }
}
