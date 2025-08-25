using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class MiningShip : MonoBehaviour, ISelectable, IDamage
{
    //[SerializeField] Renderer model;
    [SerializeField] GameObject fragmentModel;
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
    private bool foundAsteroid;
    private GameObject curAsteroid;
    private Transform goHereFallback;

    //public NullSpaceFabricator nullScript;
    

    void Start()
    {
        //this.name = nullScript.DesignatedName();
        health = stats.unitHealth;
        //colorOG = model.material.color;
        idlePos = transform.position;
        goHereFallback = goHere;
        playerControlled = false;
        noControl = false;
        foundAsteroid = false;
        goHere.gameObject.SetActive(false);
        curAsteroid = null;

        if (playerCam == null)
            playerCam = Camera.main;
    }

    void Update()
    {
        if (!noControl)
        {
            if (goHere == null)
                goHere = goHereFallback;

            if (foundAsteroid && curAsteroid != null)
            {
                GetThatAsteroid(curAsteroid);
            }

            if (playerControlled)
            {
                ShipMove();
            }
        }
    }

    public void TakeControl()
    {
        playerControlled = !playerControlled;
        Debug.Log(playerControlled);
    }

    public void TakeDamage(float damage)
    {
            soundModulation.ModulateSound(Random.Range(0.8f, 1.2f));
            damageSource.Play();

            health -= damage;

            StartCoroutine(FlashRed());

            if (health <= 0)
            {
                if (fragmentModel != null)
                    fragmentModel.SetActive(true);
                else
                    Debug.Log("fragmentModel missing");

                Destroy(gameObject);

                //if(nullScript.totalShips > 0)
                //{

                //    nullScript.totalShips--;

                //}
            }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.tag == "Asteroid" && playerControlled)
        {
            agent.ResetPath();
            agent.isStopped = true;

            playerControlled = false;
            noControl = true;

            //agent.SetDestination(transform.position);

            other.transform.root.transform.SetParent(transform, true);
            
            other.transform.root.GetComponentInChildren<Asteroid>().canMove = false;

            other = other.transform.root.GetComponent<Collider>();

            //Debug.Log(other);

            StartCoroutine(Mine(other));

            noControl = false;
        }
    }

    private void ShipMove()
    {
        if (curAsteroid != null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            goHere.gameObject.SetActive(true);

            Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.CompareTag("Asteroid"))
                {
                    foundAsteroid = true;

                    if(curAsteroid == null)
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
        //model.material.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        //model.material.color = colorOG;
    }

    private IEnumerator Mine(Collider asteroid)
    {
        IDamage dmg = asteroid.GetComponentInChildren<Asteroid>().gameObject.GetComponent<IDamage>();

        do
        {
            yield return new WaitForSeconds(1);

            if (dmg != null)
                dmg.TakeDamage(stats.miningDamage);

        } while (asteroid.GetComponentInChildren<Asteroid>().health > 0);

        agent.isStopped = false;
        agent.SetDestination(idlePos);
    }

    private void GetThatAsteroid(GameObject asteroid)
    {
        goHere = asteroid.transform;
        agent.SetDestination(goHere.position);

        return;
    }
}
