using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using Unity.VisualScripting;

public class MiningShip : MonoBehaviour, ISelectable, IDamage
{
    [SerializeField] Renderer model;
    [SerializeField] UnitSO stats;

    [Header("Movement")]
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Camera playerCam;
    [SerializeField] Transform goHere;

    private bool playerControlled;
    private float health;
    private Color colorOG;
    private Vector3 idlePos;

    void Start()
    {
        health = stats.unitHealth;
        colorOG = model.material.color;
        idlePos = transform.position;
        playerControlled = false;
        goHere.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerControlled)
        {
            ShipMove();
        }
    }

    public void TakeControl()
    {
        playerControlled = !playerControlled;
        Debug.Log(playerControlled);
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
            playerControlled = false;
            agent.SetDestination(transform.position);

            other.transform.parent.transform.SetParent(transform, true);
            other.GetComponentInParent<Asteroid>().canMove = false;

            StartCoroutine(Mine(other));

            //agent.SetDestination(idlePos);
        }
    }

    private void ShipMove()
    {
        if (Input.GetMouseButtonDown(0))
        {
            goHere.gameObject.SetActive(true);

            Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                goHere.position = hit.point;
                agent.SetDestination(goHere.position);
            }
        }

        return;
    }

    private IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        model.material.color = colorOG;
    }

    private IEnumerator Mine(Collider asteroid)
    {
        IDamage dmg = asteroid.GetComponentInParent<IDamage>();

        do
        {
            yield return new WaitForSeconds(1);
            if (dmg != null)
            {
                dmg.TakeDamage(stats.miningDamage);

                if (asteroid.GetComponentInParent<Asteroid>().health <= 10)
                {
                    asteroid.transform.parent.transform.SetParent(null);
                }
            }
        } while (asteroid.GetComponentInParent<Asteroid>().health > 0);

        agent.SetDestination(idlePos);
    }
}
