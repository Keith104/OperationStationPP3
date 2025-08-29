using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    //This one's written by Christian so expect poor quality
    public static WaveManager instance;

    [SerializeField] float spawnTime;
    [SerializeField] float startingGracePeriod;
    [SerializeField] public int maxEnemies;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] EnemiesSO[] enemies;

    public int curEnemies;
    private float timer;
    private int tier = 0;
    private int randSpawn;
    private bool waiting = false;
    
    void Start()
    {
        instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnTime)
        {
            Spawn();
        }
    }

    //I don't like how I have the spawning so I'm going to probably change it if I finish my tasks
    void Spawn()
    {
        Debug.Log("Tried enemy spawn");

        timer = 0;

        if (waiting)
            return;

        RandomizeSpawn();

        //If the max enemies it less than or equal to current amount of enemies they won't spawn anymore
        if(maxEnemies <= curEnemies)
        {
            return;
        }

        /*Debug.Log("Spawn Enemy 'BowFighter'");
        //It makes a BowFighter ups the current enemies and lowers spawn time by 10
        Instantiate(enemies[0].enemyObject, spawnPoints[randSpawn]);
        enemies[0].found = true;
        curEnemies++;
        spawnTime = spawnTime - 5;

        //If spawn time is less then half of it's orignial, it'll spawn a Verticle wing
        if(spawnTime < spawnTimeOG % 2 && maxEnemies > curEnemies)
        {
            RandomizeSpawn();

            Instantiate(enemies[1].enemyObject, spawnPoints[randSpawn]);
            enemies[1].found = true;
            curEnemies++;
        }

        //If spawn time is less then a quater of it's orignial, it'll spawn a DOG
        if (spawnTime < spawnTimeOG % 4 && maxEnemies > curEnemies)
        {
            RandomizeSpawn();

            Instantiate(enemies[2].enemyObject, spawnPoints[randSpawn]);
            enemies[2].found = true;
            curEnemies++;
        }

        //If spawn time is less then 10, it'll spawn a Super DOG
        if (spawnTime < 10 && maxEnemies > curEnemies + 2)
        {
            RandomizeSpawn();

            Instantiate(enemies[3].enemyObject, spawnPoints[randSpawn]);
            enemies[3].found = true;
            curEnemies += 3;
        }
        spawnTime = spawnTimeOG; */

        switch (tier)
        {
            case 0:
                StartCoroutine(Wait(startingGracePeriod));
                break;

            case 1:
                Instantiate(enemies[0].enemyObject, spawnPoints[randSpawn]);
                enemies[0].found = true;
                curEnemies++;
                break;

            case 2:
                Instantiate(enemies[1].enemyObject, spawnPoints[randSpawn]);
                enemies[1].found = true;
                curEnemies++;
                break;

            case 3:
                Instantiate(enemies[4].enemyObject, spawnPoints[randSpawn]);
                enemies[4].found = true;
                curEnemies++;
                break;

            case 4:
                Instantiate(enemies[2].enemyObject, spawnPoints[randSpawn]);
                enemies[2].found = true;
                curEnemies++;
                break;

            case 5:
                Instantiate(enemies[3].enemyObject, spawnPoints[randSpawn]);
                enemies[3].found = true;
                curEnemies += 3;
                break;

            case 6:
                tier = 0;
                break;
        }

        tier++;
    }

    //Keeps track of dead enemies with EnemyAI script
    public void DeadEnemy()
    {
        curEnemies--;
    }

    //Randomizes the spawnpoints of enemies
    private void RandomizeSpawn()
    {
        randSpawn = Random.Range(0, spawnPoints.Length);
    }

    private IEnumerator Wait(float waitTime)
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        waiting = false;
    }
}
