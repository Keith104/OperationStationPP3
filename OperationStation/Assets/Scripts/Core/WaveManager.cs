using UnityEngine;

public class WaveManager : MonoBehaviour
{
    //This one's written by Christian so expect poor quality
    public static WaveManager instance;

    [SerializeField] float spawnTime;
    [SerializeField] public int maxEnemies;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] EnemiesSO[] enemies;

    public int curEnemies;
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

    //I don't like how I have the spawning so I'm going to probably change it after I finish my tasks
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
        Instantiate(enemies[0], spawnPoints[randSpawn]);
        curEnemies++;
        spawnTime = spawnTime - 5;

        //If spawn time is less then half of it's orignial, it'll spawn a Verticle wing
        if(spawnTime < spawnTimeOG % 2 && maxEnemies > curEnemies)
        {
            RandomizeSpawn();

            Instantiate(enemies[1], spawnPoints[randSpawn]);
            curEnemies++;
        }

        //If spawn time is less then a quater of it's orignial, it'll spawn a DOG
        if (spawnTime < spawnTimeOG % 4 && maxEnemies > curEnemies)
        {
            RandomizeSpawn();

            Instantiate(enemies[2], spawnPoints[randSpawn]);
            curEnemies++;
        }

        //If spawn time is less then 10, it'll spawn a Super DOG
        if (spawnTime < 10 && maxEnemies > curEnemies + 2)
        {
            RandomizeSpawn();

            Instantiate(enemies[3], spawnPoints[randSpawn]);
            curEnemies += 3;
        }
        spawnTime = spawnTimeOG;
    }

    //Keeps track of dead enemies with EnemyAI script
    public void DeadEnemy()
    {
        curEnemies--;
    }

    //Randomizes the spawnpoints of enemies
    void RandomizeSpawn()
    {
        randSpawn = Random.Range(0, spawnPoints.Length);
    }
}
