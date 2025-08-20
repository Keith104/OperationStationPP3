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

    [Header("Sound")]
    [SerializeField] SoundModulation soundModulation;
    [SerializeField] AudioSource damageSource;

    private bool playerControlled;
    private float health;
    private Color colorOG;
    private Vector3 idlePos;
    private bool noControl;

    public NullSpaceFabricator nullScript;
    

    void Start()
    {
        this.name = nullScript.DesignatedName();
        health = stats.unitHealth;
        colorOG = model.material.color;
        idlePos = transform.position;
        playerControlled = false;
        noControl = false;
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
        if (!noControl)
        {
            playerControlled = !playerControlled;
            Debug.Log(playerControlled);
        }
        else
            return;
    }

    public void TakeDamage(float damage)
    {
        soundModulation.ModulateSound(Random.Range(0.8f, 1.2f));
        damageSource.Play();

        health -= damage;

        StartCoroutine(FlashRed());

        if (health <= 0)
        {
            Destroy(gameObject);
            if(nullScript.totalShips > 0)
            {

                nullScript.totalShips--;

            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Asteroid" && playerControlled)
        {
            playerControlled = false;
            noControl = true;
            agent.SetDestination(transform.position);

            other.transform.parent.transform.SetParent(transform, true);
            other.GetComponentInParent<Asteroid>().canMove = false;

            StartCoroutine(Mine(other));

            noControl = false;
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
