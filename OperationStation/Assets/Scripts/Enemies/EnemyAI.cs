using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour, IDamage
{
    [SerializeField] Renderer model;
    [SerializeField] float health;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform shootPos;
    [SerializeField] GameObject bullet;
    [SerializeField] float attackCooldown;

    float shootTimer;

    private GameObject station;
    private Color colorOG;

    [Header("Debug")]
    [SerializeField] bool debug;

    void Start()
    {
        //This isn't final I'll fix/change this when I know how we're implementing the station
        station = GameObject.FindWithTag("Player");
        colorOG = model.material.color;
    }

    void Update()
    {
        Attack();

        //Debug damage
        if (debug && Input.GetKeyDown(KeyCode.F5))
        {
            TakeDamage(1f);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        StartCoroutine(FlashRed());

        if (health <= 0)
        {
            WaveManager.instance.DeadEnemy();
            Destroy(gameObject);
        }
    }

    //Once the enemy is spawned they'll b-line to the player to attack
    void Attack()
    {
        if (station != null)
        {
            agent.SetDestination(station.transform.position);
            shootTimer += Time.deltaTime;

            if (agent.remainingDistance < 10 && shootTimer >= attackCooldown)
            {
                shootTimer = 0;
                Instantiate(bullet, shootPos.position, transform.rotation);
            }
        }
        else
        {
            return;
        }
    }

    //Feedback
    IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOG;
    }
}
