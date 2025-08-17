using System.Data;
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
    public static bool awaitingPlacement;

    public void DeathCatSpawn()
    {
        awaitingPlacement = true;
        objectToInstantiate = deathCat;
    }
    public void NullSpaceFabricatorSpawn()
    {
        awaitingPlacement = true;
        objectToInstantiate = nullSpaceFabricator;
    }
    public void MacroParticleSmelterSpawn()
    {
        awaitingPlacement = true;
        objectToInstantiate = macroParticleSmelter;
    }
    public void BasicTurretSpawn()
    {
        awaitingPlacement = true;
        objectToInstantiate = basicTurret;
    }

    public void WallSpawn()
    {
        awaitingPlacement = true;
        objectToInstantiate = wall;
    }

    public void GrapeJamSpawn()
    {
        awaitingPlacement = true;
        objectToInstantiate = grapeJam;
    }

    // Update is called once per frame
    void Update()
    {

        if (awaitingPlacement && Input.GetMouseButtonDown(0)) 
        { 
        
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;


            if(Physics.Raycast(ray, out hit))
            {

                GameObject hitObject = hit.collider.gameObject;
                Tile tile = hitObject.GetComponent<Tile>();
                GridSystem grid = tile.GetComponentInParent<GridSystem>();

                if (hitObject != null && tile != null)
                {
                    Vector3 Location = hitObject.transform.position;
                    grid.spawnAvailableSpaces();
                    spawnLocation = Location;
                    Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, hitObject.transform.parent);
                    awaitingPlacement = false;
                    Destroy(hitObject);
                }
            }
            else
            {
                if(objectToInstantiate == wall || objectToInstantiate == grapeJam || 
                    objectToInstantiate == basicTurret)
                {
                    GameObject foundDeathCat = GameObject.Find("DeathCat");

                    if (foundDeathCat != null)
                    {
                        Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, foundDeathCat.transform.parent);
                    }


                }
            }
        
        }

    }
}
