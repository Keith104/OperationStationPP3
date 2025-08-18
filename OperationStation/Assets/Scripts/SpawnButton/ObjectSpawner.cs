using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject deathCat;
    public GameObject basicTurret;
    public GameObject nullSpaceFabricator;
    public GameObject macroParticleSmelter;
    public GameObject wall;
    public GameObject grapeJam;

    private GameObject objectToInstantiate;

    private Vector3 spawnLocation;
    public static bool awaitingPlacement = false;

    ResourceManager resourceManager = ResourceManager.instance;

    public void DeathCatSpawn(int resourcePrice)
    {
        ResourceSO.ResourceType resource = ResourceSO.ResourceType.PoloniumCrystal;
        if (resourceManager.poloniumCrystal >= resourcePrice)
        {
            resourceManager.RemoveResource(resource, resourcePrice);
            awaitingPlacement = true;
            objectToInstantiate = deathCat;

        }
    }
    public void NullSpaceFabricatorSpawn(int resourcePrice)
    {
        ResourceSO.ResourceType resource = ResourceSO.ResourceType.PoloniumCrystal;
        if (resourceManager.poloniumCrystal >= resourcePrice)
        {
            resourceManager.RemoveResource(resource, resourcePrice);
            awaitingPlacement = true;
            objectToInstantiate = nullSpaceFabricator;
        }
    }
    public void MacroParticleSmelterSpawn(int resourcePrice)
    {
        ResourceSO.ResourceType resource = ResourceSO.ResourceType.PoloniumCrystal;
        if (resourceManager.poloniumCrystal >= resourcePrice)
        {
            resourceManager.RemoveResource(resource, resourcePrice);
            awaitingPlacement = true;
            objectToInstantiate = macroParticleSmelter;
        }
    }
    public void BasicTurretSpawn(int resourcePrice)
    {
        ResourceSO.ResourceType resource = ResourceSO.ResourceType.PoloniumCrystal;
        if (resourceManager.poloniumCrystal >= resourcePrice)
        {
            resourceManager.RemoveResource(resource, resourcePrice);
            awaitingPlacement = true;
            objectToInstantiate = basicTurret;
        }
    }

    public void WallSpawn(int resourcePrice)
    {
        ResourceSO.ResourceType resource = ResourceSO.ResourceType.PoloniumCrystal;
        if (resourceManager.poloniumCrystal >= resourcePrice)
        {
            resourceManager.RemoveResource(resource, resourcePrice);
            awaitingPlacement = true;
            objectToInstantiate = wall;
        }
    }

    public void GrapeJamSpawn(int resourcePrice)
    {
        ResourceSO.ResourceType resource = ResourceSO.ResourceType.PoloniumCrystal;
        if (resourceManager.poloniumCrystal >= resourcePrice)
        {
            resourceManager.RemoveResource(resource, resourcePrice);
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

<<<<<<< HEAD
            if (hitObject != null)
=======

            if(Physics.Raycast(ray, out hit))
>>>>>>> parent of a33fb57 (Update before checking my stash)
            {
                if(objectToInstantiate == deathCat || objectToInstantiate == nullSpaceFabricator || 
                    objectToInstantiate == macroParticleSmelter)
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
}
