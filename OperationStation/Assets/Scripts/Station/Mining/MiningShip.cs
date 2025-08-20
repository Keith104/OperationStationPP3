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
    private bool getHurt;
    private bool foundAsteroid;
    private GameObject curAsteroid;

    void Start()
    {
        health = stats.unitHealth;
        colorOG = model.material.color;
        idlePos = transform.position;
        playerControlled = false;
        noControl = false;
        foundAsteroid = false;
        goHere.gameObject.SetActive(false);
        curAsteroid = null;
        getHurt = true;
    }

    void Update()
    {
        if (foundAsteroid && curAsteroid != null)
        {
            GetThatAsteroid(curAsteroid);
        }

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
        if (getHurt)
        {
            soundModulation.ModulateSound(Random.Range(0.8f, 1.2f));
            damageSource.Play();

            health -= damage;

            StartCoroutine(FlashRed());

            if (health <= 0)
                Destroy(gameObject);
        }
        else
            getHurt = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.tag == "Asteroid" && playerControlled)
        {
            playerControlled = false;
            noControl = true;

            agent.SetDestination(transform.position);

            other.transform.root.transform.SetParent(transform, true);
            
            other.transform.root.GetComponent<Asteroid>().canMove = false;

            Debug.Log(other);

            StartCoroutine(Mine(other));

            noControl = false;
        }
    }

    private void ShipMove()
    {
        if (Input.GetMouseButtonDown(0))
        {
            getHurt = false;
            goHere.gameObject.SetActive(true);

            Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.CompareTag("Asteroid"))
                {
                    foundAsteroid = true;
                    curAsteroid = hit.collider.transform.parent.gameObject;
                }
                
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
        IDamage dmg = asteroid.transform.root.GetComponent<IDamage>();

        do
        {
            yield return new WaitForSeconds(1);
            if (dmg != null)
            {
                dmg.TakeDamage(stats.miningDamage);

                if (asteroid.transform.root.GetComponent<Asteroid>() != null && asteroid.transform.root.GetComponent<Asteroid>().health <= 10)
                {
                    asteroid.transform.parent.transform.SetParent(null);
                }
            }
        } while (asteroid.transform.root.GetComponent<Asteroid>().health > 0);

        agent.SetDestination(idlePos);
    }

    private void GetThatAsteroid(GameObject asteroid)
    {
        goHere = asteroid.transform;
        agent.SetDestination(goHere.position);

        return;
    }
}
