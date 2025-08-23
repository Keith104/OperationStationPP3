using System.Collections.Generic;
using UnityEngine;

public class Fragment : MonoBehaviour
{
    private void Awake()
    {
        transform.SetParent(null);        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        ReduceFragments();
    }

    void ReduceFragments()
    {
        foreach (Transform fragment in transform)
        {
            fragment.localScale -= new Vector3(
                Random.Range(0.1f, 0.01f), 
                Random.Range(0.1f, 0.01f), 
                Random.Range(0.1f, 0.01f));
            if (fragment.localScale.x <= 0
                || fragment.localScale.y <= 0
                || fragment.localScale.z <= 0)
                Destroy(fragment.gameObject);
        }
        if(transform.childCount == 0)
            Destroy(gameObject);
    }
}
