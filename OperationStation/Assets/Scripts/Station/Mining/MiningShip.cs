using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;

public class MiningShip : MonoBehaviour, ISelectable, IDamage
{
    [SerializeField] UnitSO stats;
    [SerializeField] Renderer model;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Camera playerCam;

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
            //Mine
        }
    }

    public void ShipMove()
    {
        Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            
            agent.SetDestination(hit.point);
        }
    }

    private IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        model.material.color = colorOG;
    }
}
