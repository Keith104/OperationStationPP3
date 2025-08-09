using UnityEngine;

public class WaveManager : MonoBehaviour
{
    //This one's written by Christian so expect poor quality
    public static WaveManager instance;

    [SerializeField] float spawnTime;
    [SerializeField] int maxEnemies;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] GameObject enemyBow;
    [SerializeField] GameObject enemyVert;
    //[SerializeField] GameObject enemyDOG;
    //[SerializeField] GameObject enemySUPDOG;

    int curEnemies;
    private float timer;
    private float spawnTimeOG;
    private int randSpawn;
    
    void Start()
    {
        instance = this;
        spawnTimeOG = spawnTime;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnTime)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        timer = 0;

        RandomizeSpawn();

        //If the max enemies it less than or equal to current amount of enemies they won't spawn anymore
        if(maxEnemies <= curEnemies)
        {
            return;
        }

        //It makes a BowFighter ups the current enemies and lowers spawn time by 10
        Instantiate(enemyBow, spawnPoints[randSpawn]);
        curEnemies++;
        spawnTime = spawnTime - 10;

        //If spawn time is less then 15 it'll spawn a Verticle fighter and reset the spawn timer
        //When DOG and Super DOG destroyers get added they'd reset spawn time and Vert will do something else instead
        // +3 to the spawn timer? that way 3 Verticle bombers can spawn before a DOG?
        if(spawnTime < 15 && maxEnemies > curEnemies)
        {
            RandomizeSpawn();

            Instantiate(enemyVert, spawnPoints[randSpawn]);
            curEnemies++;
        }
            spawnTime = spawnTimeOG;
    }

    //Keeps track of dead enemies with EnemyAI script
    public void DeadEnemy()
    {
        curEnemies--;
    }

    void RandomizeSpawn()
    {
        randSpawn = Random.Range(0, spawnPoints.Length);
    }
}
