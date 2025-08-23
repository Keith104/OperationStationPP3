using System.Collections;
using UnityEngine;

public class MineShipAI : EnemyAI
{
    [Header("Mine Ship Values")]
    [SerializeField] GameObject mine;
    [SerializeField] float destroyTime;

    private Vector3 startingPos;
    private float nextShootTime = 5f;
    private float xOffset;
    private float zOffset;

    void Start()
    {
        station = GameObject.FindWithTag("Player");
        colorOG = model.material.color;
        startingPos = station.transform.position;
        shootTimer = 0;

        health = enemy.health;

        if (enemy.bullet.GetComponent<Damage>() != null)
        {
            enemy.bullet.GetComponent<Damage>().damageAmount = enemy.damageAmount;
        }

        Wander();
    }

    public override void Attack()
    {
        if (station != null)
        {
            shootTimer += Time.deltaTime;

            if(shootTimer >= nextShootTime)
            {
                GameObject bomb = Instantiate(mine, shootPos.position, shootPos.rotation);
                bomb.transform.localPosition = new Vector3(bomb.transform.position.x, bomb.transform.position.y - 0.5f, bomb.transform.position.z);
                Wander();
                nextShootTime += 5f;
            }

            if(shootTimer >= 15)
            {
                StartCoroutine(Kamikaze(destroyTime));
            }
        }
        else
            return;
    }

    private IEnumerator Kamikaze(float time)
    {
        agent.SetDestination(station.transform.position);
        yield return new WaitForSeconds(time);
        Destroy(gameObject, time);
        Instantiate(enemy.bullet, shootPos.position, shootPos.rotation);
    }

    private void Wander()
    {
        xOffset = Random.Range(-20, 21);
        zOffset = Random.Range(-20, 21);

        Vector3 wanderArea = new Vector3(startingPos.x + xOffset, startingPos.y, startingPos.z + zOffset);

        agent.SetDestination(wanderArea);
    }
}
