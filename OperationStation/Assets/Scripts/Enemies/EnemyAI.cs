using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class EnemyAI : MonoBehaviour, IDamage
{
    [SerializeField] WaveManager waveManager;
    [SerializeField] Renderer model;

    [Header("Enemy Data")]
    [SerializeField] EnemiesSO enemy;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] GameObject enemyToSpawn;

    [Header("Damage Data")]
    [SerializeField] Transform shootPos;
    [SerializeField] bool spinFire;

    [Header("Debug")]
    [SerializeField] bool debug;
    
    float shootTimer;

    private GameObject station;
    private Color colorOG;
    private float shootY;

    private float health;


    public void Initialized(EnemiesSO enemyData)
    {
        enemy = enemyData;
    }

    void Start()
    {
        //This isn't final I'll fix/change this when I know how we're implementing the station
        station = GameObject.FindWithTag("Player");
        colorOG = model.material.color;

        health = enemy.health;

        if (enemy.bullet.GetComponent<Damage>() != null)
        {
            enemy.bullet.GetComponent<Damage>().damageAmount = enemy.damageAmount;
        }
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
            if(enemyToSpawn != null && waveManager.maxEnemies > waveManager.curEnemies + 1)
            {
                Instantiate(enemyToSpawn, shootPos.position, transform.rotation);
                Instantiate(enemyToSpawn, transform.position, transform.rotation);
            }

            WaveManager.instance.DeadEnemy();
            Destroy(gameObject);
        }
    }

    //Once the enemy is spawned they'll b-line to the player to attack
    public void Attack()
    {
        if (station != null)
        {
            agent.SetDestination(station.transform.position);
            shootTimer += Time.deltaTime;

            if (agent.remainingDistance < 20 && shootTimer >= enemy.attackCooldown)
            {
                shootTimer = 0;

                if (spinFire)
                {
                    shootY = Random.rotation.y;
                    shootPos.rotation = new Quaternion(shootPos.rotation.x, shootY, shootPos.rotation.z, shootPos.rotation.w);
                }

                Instantiate(enemy.bullet, shootPos.position, shootPos.rotation);
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
