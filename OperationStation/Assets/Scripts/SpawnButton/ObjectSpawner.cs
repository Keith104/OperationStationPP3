using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public struct ResourceCostSpawner
{
    public ResourceSO.ResourceType type;
    public int amount;
}

public class ObjectSpawner : MonoBehaviour
{
    public GameObject deathCat;
    public GameObject basicTurret;
    public GameObject nullSpaceFabricator;
    public GameObject wall;
    public GameObject grapeJam;

    public GameObject poloniumReactor;
    public GameObject smelter;
    public GameObject solarPanelArray;

    private GameObject objectToInstantiate;

    private Vector3 spawnLocation;
    public static bool awaitingPlacement = false;

    public List<ResourceCostSpawner> deathCatCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> basicTurretCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> nullSpaceFabricatorCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> wallCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> grapeJamCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> poloniumReactorCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> smelterCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> solarPanelArrayCosts = new List<ResourceCostSpawner>();

    public void DeathCatSpawn()
    {
        if (TrySpend(deathCatCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = deathCat;
        }
    }
    public void NullSpaceFabricatorSpawn()
    {
        if (TrySpend(nullSpaceFabricatorCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = nullSpaceFabricator;
        }
    }

    public void PoloniumReactorSpawn()
    {
        if (TrySpend(poloniumReactorCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = poloniumReactor;
        }
    }

    public void SmelterSpawn()
    {
        if (TrySpend(smelterCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = smelter;
        }
    }
    public void SolarPanelArraySpawn()
    {
        if (TrySpend(solarPanelArrayCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = solarPanelArray;
        }
    }
    public void BasicTurretSpawn()
    {
        if (TrySpend(basicTurretCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = basicTurret;
        }
    }

    public void WallSpawn()
    {
        if (TrySpend(wallCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = wall;
        }
    }

    public void GrapeJamSpawn()
    {
        if (TrySpend(grapeJamCosts))
        {
            awaitingPlacement = true;
            objectToInstantiate = grapeJam;
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (awaitingPlacement && Input.GetMouseButtonDown(0)) 
        { 
        
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit);
            GameObject hitObject = hit.collider.gameObject;
            Tile tile = hitObject.GetComponent<Tile>();
            Defence defence = hitObject.GetComponent<Defence>();

            if (hitObject != null)
            {
                if(objectToInstantiate == deathCat || objectToInstantiate == nullSpaceFabricator ||
                    objectToInstantiate == poloniumReactor || objectToInstantiate == smelter ||
                    objectToInstantiate == solarPanelArray)
                {
                    if (tile != null)
                    {
                        GridSystem grid = tile.GetComponentInParent<GridSystem>();
                        Vector3 Location = hitObject.transform.position;
                        grid.spawnAvailableSpaces();
                        spawnLocation = Location;
                        Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, hitObject.transform.parent);
                        awaitingPlacement = false;
                        Destroy(hitObject);

                    }

                }else
                { 
                    if(defence == null && tile == null)
                    {
                        GameObject foundDeathCat = GameObject.Find("DeathCat");
                        spawnLocation = hit.point + hit.normal * 0.5f;
                        if (foundDeathCat != null)
                        {
                            Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, foundDeathCat.transform.parent);
                            awaitingPlacement = false;
                        }

                    }
                }
            }
        
        }

    }

    // Helpers
    private bool TrySpend(List<ResourceCostSpawner> costs)
    {
        if (!CanAfford(costs)) return false;

        // Spend all costs atomically
        foreach(var c in costs)
        {
            ResourceManager.instance.RemoveResource(c.type, c.amount);
        }
        return true;
    }

    private bool CanAfford(List<ResourceCostSpawner> costs)
    {
        if (costs == null) return true;

        foreach(var c in costs)
        {
            if (GetAmount(c.type) < c.amount) return false;
        }
        return true;
    }

    private int GetAmount(ResourceSO.ResourceType type)
    {
        var rm = ResourceManager.instance;

        switch (type)
        {
            case ResourceSO.ResourceType.Tritium: return rm.tritium;
            case ResourceSO.ResourceType.Silver: return rm.silver;
            case ResourceSO.ResourceType.Polonium: return rm.polonium;
            case ResourceSO.ResourceType.TritiumIngot: return rm.tritiumIngot;
            case ResourceSO.ResourceType.SilverCoin: return rm.silverCoins;
            case ResourceSO.ResourceType.PoloniumCrystal: return rm.poloniumCrystal;
            case ResourceSO.ResourceType.Energy: return rm.energy;
            default: return 0;

        }
    }
}
