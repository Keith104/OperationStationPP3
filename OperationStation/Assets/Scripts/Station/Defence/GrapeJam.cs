using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GrapeJam : MonoBehaviour
{
    [SerializeField] float upTime; // time in seconds, if nothing is set then timer is set to 5 min
    [SerializeField] GameObject fragmentModel;

    private bool hasItTriggeredOnce = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (upTime <= 0) upTime = 300f;
        
        if (fragmentModel != null)
        {
            StartCoroutine(DelayBeforeDestroy());
            
        }
        else
            Debug.Log("fragmentModel missing");

    }

    IEnumerator DelayBeforeDestroy()
    {
        
        yield return new WaitForSeconds(upTime);
        fragmentModel.SetActive(true);
        Destroy(gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {
        NavMeshAgent enemyNAv = other.GetComponent<NavMeshAgent>();
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemyNAv != null && enemy.isSlowed == false)
        {
            enemyNAv.speed = enemyNAv.speed / 2;
            enemy.isSlowed = true;
        }

    }


    private void OnTriggerExit(Collider other)
    {
        NavMeshAgent enemyNAv = other.GetComponent<NavMeshAgent>();
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemyNAv != null && enemy.isSlowed == true)
        {
            enemyNAv.speed = enemyNAv.speed * 2;
            enemy.isSlowed = false;

        }
    }
}
