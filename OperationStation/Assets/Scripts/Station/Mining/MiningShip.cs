using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class MiningShip : MonoBehaviour, ISelectable, IDamage
{
    //[SerializeField] Renderer model;
    [SerializeField] GameObject fragmentModel;
    [SerializeField] UnitSO stats;
    [SerializeField] float firstHealth;
    [SerializeField] bool doesntDie;

    [Header("Movement")]
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Camera playerCam;
    [SerializeField] Transform goHere;

    [Header("Sound")]
    [SerializeField] SoundModulation soundModulation;
    [SerializeField] AudioSource damageSource;

    public bool playerControlled;
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

        health += firstHealth;
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
        if (doesntDie) return;

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

            noControl = true;

            //agent.SetDestination(transform.position);

            other.transform.root.transform.SetParent(transform, true);
            
            other.transform.root.GetComponentInChildren<Asteroid>().canMove = false;

            other = other.transform.root.GetComponent<Collider>();

            //Debug.Log(other);

            StartCoroutine(Mine(other));
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

    private IEnumerator Mine(Collider asteroidCol)
    {
        // Block other control while mining
        noControl = true;

        Asteroid ast = null;
        if (asteroidCol)
            ast = asteroidCol.GetComponentInChildren<Asteroid>();

        while (ast != null && ast.health > 0f)
        {
            yield return new WaitForSeconds(1f);

            var dmg = ast.GetComponent<IDamage>();
            if (dmg != null)
                dmg.TakeDamage(stats.miningDamage);

            // If the asteroid object/component was destroyed by TakeDamage, break out
            if (ast == null) break;
        }

        // --- Clean up targeting state ---
        foundAsteroid = false;
        curAsteroid = null;
        goHere = goHereFallback;

        // --- Send the ship home cleanly ---
        playerControlled = true;
        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(idlePos);

        noControl = false;
    }


    private void GetThatAsteroid(GameObject asteroid)
    {
        if (curAsteroid == null || curAsteroid.Equals(null))
        {
            agent.ResetPath();
            agent.SetDestination(idlePos);
            return;
        }

        // Don’t rebind goHere; just steer there
        agent.SetDestination(asteroid.transform.position);
    }
}
