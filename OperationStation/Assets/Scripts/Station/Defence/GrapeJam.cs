using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GrapeJam : MonoBehaviour
{
    [Header("Timers")]
    [SerializeField] float jamDelay;
    [SerializeField] float upTime; // time in seconds, if nothing is set then timer is set to 5 min

    [Header("Fragment")]
    [SerializeField] GameObject fragmentModel;

    [Header("Jam")]
    [SerializeField] GameObject jamModel;
    [SerializeField] GameObject[] otherModels;
    private Material jamMaterial;
    [SerializeField] float fadeInDuration;
    [SerializeField] float fadeOutDuration; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        jamMaterial = jamModel.GetComponent<Renderer>().material;
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
        
        yield return new WaitForSeconds(jamDelay);

        foreach (GameObject model in otherModels)
            model.SetActive(false);

        fragmentModel.SetActive(true);
        jamModel.SetActive(true);
        StartCoroutine(FadeJamIn());
    }

    IEnumerator FadeJamIn()
    {        
        Color jamColor = jamMaterial.color;

        jamColor.a = 0;
        jamMaterial.color = jamColor;

        float timePassed = 0;
        while (timePassed < fadeInDuration)
        {
            timePassed += Time.deltaTime;
            jamColor.a = Mathf.Lerp(0, 1, timePassed / fadeInDuration);
            jamMaterial.color = jamColor;
            yield return null;
        }

        StartCoroutine(FadeJamOut());
    }

    IEnumerator FadeJamOut()
    {
        Color jamColor = jamMaterial.color;

        jamColor.a = 0;
        jamMaterial.color = jamColor;

        float timePassed = 0;
        while (timePassed < fadeOutDuration)
        {
            timePassed += Time.deltaTime;
            jamColor.a = Mathf.Lerp(1, 0, timePassed / fadeOutDuration);
            jamMaterial.color = jamColor;
            yield return null; 
        }

        Destroy(gameObject);
    }


    private void OnTriggerEnter(Collider other)
    {
        NavMeshAgent enemyNAv = other.GetComponent<NavMeshAgent>();
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemyNAv != null && enemy.isSlowed == false)
        {
            enemyNAv.speed /= 2;
            enemy.isSlowed = true;
        }

    }


    private void OnTriggerExit(Collider other)
    {
        NavMeshAgent enemyNAv = other.GetComponent<NavMeshAgent>();
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemyNAv != null && enemy.isSlowed == true)
        {
            enemyNAv.speed *= 2;
            enemy.isSlowed = false;

        }
    }
}
