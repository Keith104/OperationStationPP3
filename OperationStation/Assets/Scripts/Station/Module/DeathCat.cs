using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DeathCat : MonoBehaviour, IModule
{
    [SerializeField] Module module;
    [SerializeField] int totalCostsLeft;
    
    private bool deathCatFired = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        totalCostsLeft = 0;
        foreach (int cost in module.costsLeft)
        {
            totalCostsLeft += cost;
        }

        if(totalCostsLeft <= 0 && deathCatFired == false)
        {
            FireDeathCat();
            deathCatFired = true;
        }
    }

    void FireDeathCat()
    {
        Debug.Log("Cat The Death Cat has been fired");
        LevelUIManager.instance.menuWin.SetActive(true);
    }

    public void ModuleDie()
    {
        LevelUIManager.instance.menuLose.SetActive(true);
    }
}