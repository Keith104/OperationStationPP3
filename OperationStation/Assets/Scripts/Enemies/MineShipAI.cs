using System.Collections;
using UnityEngine;

public class MineShipAI : EnemyAI
{
    [Header("Mine Ship")]
    [SerializeField] GameObject mine;
    [SerializeField] float destroyTime;

    private Transform wanderArea;
    private Vector3 startingPos;
    private float xOffset;
    private float zOffset;

    void Start()
    {
        station = GameObject.FindWithTag("Player");
        colorOG = model.material.color;
        wanderArea = station.transform;

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

            if(shootTimer % 5 == 0)
            {
                Instantiate(mine, shootPos.position, shootPos.rotation);
                Wander();
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
        xOffset += Random.Range(-20, 21);
        zOffset += Random.Range(-20, 21);

        wanderArea.position = new Vector3(startingPos.x + xOffset, station.transform.position.y, startingPos.z + zOffset);

        agent.SetDestination(wanderArea.position);
    }
}
