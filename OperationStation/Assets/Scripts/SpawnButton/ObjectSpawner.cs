using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public GameObject laserTurret;
    public GameObject nullSpaceFabricator;
    public GameObject wall;
    public GameObject grapeJam;

    public GameObject poloniumReactor;
    public GameObject smelter;
    public GameObject solarPanelArray;

    public LayerMask buildLayer;

    public GameObject buildMenu;

    private GameObject objectToInstantiate;
    private Vector3 spawnLocation;

    public static bool awaitingPlacement = false;
    public static bool isDefence = false;

    public List<ResourceCostSpawner> deathCatCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> basicTurretCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> laserTurretCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> nullSpaceFabricatorCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> wallCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> grapeJamCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> poloniumReactorCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> smelterCosts = new List<ResourceCostSpawner>();
    public List<ResourceCostSpawner> solarPanelArrayCosts = new List<ResourceCostSpawner>();

    public DefencePreview viewing;

    PlayerInput controls;

    void Awake()
    {
        controls = new PlayerInput();
    }

    void OnEnable()
    {
        controls.Player.BuildMode.performed += OnBuildMode;
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Player.BuildMode.performed -= OnBuildMode;
        controls.Disable();
    }

    void OnBuildMode(InputAction.CallbackContext _)
    {
        LevelUIManager.instance.SetActiveMenu(buildMenu);
    }

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
            isDefence = true;
            objectToInstantiate = basicTurret;
            viewing.PreviewDefence(basicTurret);
        }
    }

    public void LaserTurretSpawn()
    {
        if (TrySpend(laserTurretCosts))
        {
            awaitingPlacement = true;
            isDefence = true;
            objectToInstantiate = laserTurret;
            viewing.PreviewDefence(laserTurret);
        }
    }

    public void WallSpawn()
    {
        if (TrySpend(wallCosts))
        {
            awaitingPlacement = true;
            isDefence = true;
            objectToInstantiate = wall;
            viewing.PreviewDefence(wall);
        }
    }

    public void GrapeJamSpawn()
    {
        if (TrySpend(grapeJamCosts))
        {
            awaitingPlacement = true;
            isDefence = true;
            objectToInstantiate = grapeJam;
            viewing.PreviewDefence(grapeJam);
        }
    }

    void Update()
    {
        if (awaitingPlacement == true && Input.GetMouseButtonDown(0) && objectToInstantiate != null)
        {
            GameObject hitObject = null;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, buildLayer))
            {
                hitObject = hit.collider.gameObject;
            }
            else
            {
                return;
            }
            Tile tile = hitObject.GetComponent<Tile>();
            Defence defence = hitObject.GetComponent<Defence>();


            if (hitObject != null)
            {
                if (objectToInstantiate == deathCat || objectToInstantiate == nullSpaceFabricator ||
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
                }
                else
                {
                    if (defence == null && tile == null)
                    {
                        GameObject foundDeathCat = GameObject.Find("DeathCat");
                        spawnLocation = hit.point + hit.normal * 0.5f;
                        if (foundDeathCat != null)
                        {
                            Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, foundDeathCat.transform.parent);
                            awaitingPlacement = false;
                            isDefence = false;
                            viewing.isDefenceBuildActive = false;
                        }
                    }
                }
            }
        }
    }

    bool TrySpend(List<ResourceCostSpawner> costs)
    {
        if (!CanAfford(costs)) return false;
        foreach (var c in costs)
            ResourceManager.instance.RemoveResource(c.type, c.amount);
        return true;
    }

    bool CanAfford(List<ResourceCostSpawner> costs)
    {
        if (costs == null) return true;
        foreach (var c in costs)
        {
            if (GetAmount(c.type) < c.amount) return false;
        }
        return true;
    }

    int GetAmount(ResourceSO.ResourceType type)
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
