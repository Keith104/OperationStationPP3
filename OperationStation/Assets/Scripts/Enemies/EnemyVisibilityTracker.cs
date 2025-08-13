using UnityEngine;

public class EnemyVisibilityTracker : MonoBehaviour
{
    private bool enemyAdded = false;
    private bool enemySubed = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Renderer>().isVisible && enemyAdded == false)
        {
            MusicManager.instance.enemiesSeen++;
            enemyAdded = true;
            enemySubed = false;
        }
        if (!GetComponent<Renderer>().isVisible && enemySubed == false)
        {
            MusicManager.instance.enemiesSeen--;
            enemySubed = true;
            enemyAdded = false;
        }

    }
}
