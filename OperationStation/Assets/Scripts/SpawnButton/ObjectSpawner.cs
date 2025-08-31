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
    public GameObject enableBuilding;

    [SerializeField] Transform worldBuildParent;

    private GameObject objectToInstantiate;
    private Vector3 spawnLocation;
    private List<ResourceCostSpawner> pendingCosts;

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

    bool placingLock;

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
        if (buildMenu == null) return;
        if (buildMenu.activeSelf) CloseBuildMenu();
        else LevelUIManager.instance.SetActiveMenu(buildMenu);
    }

    void CloseBuildMenu()
    {
        LevelUIManager.instance.SetActiveMenu(enableBuilding);
        awaitingPlacement = false;
        isDefence = false;
        objectToInstantiate = null;
        pendingCosts = null;
        if (viewing != null) viewing.isDefenceBuildActive = false;
    }

    public void DeathCatSpawn() { BeginPlacementIfAffordable(deathCat, deathCatCosts, false, null); }
    public void NullSpaceFabricatorSpawn() { BeginPlacementIfAffordable(nullSpaceFabricator, nullSpaceFabricatorCosts, false, null); }
    public void PoloniumReactorSpawn() { BeginPlacementIfAffordable(poloniumReactor, poloniumReactorCosts, false, null); }
    public void SmelterSpawn() { BeginPlacementIfAffordable(smelter, smelterCosts, false, null); }
    public void SolarPanelArraySpawn() { BeginPlacementIfAffordable(solarPanelArray, solarPanelArrayCosts, false, null); }
    public void BasicTurretSpawn() { BeginPlacementIfAffordable(basicTurret, basicTurretCosts, true, () => viewing?.PreviewDefence(basicTurret)); }
    public void LaserTurretSpawn() { BeginPlacementIfAffordable(laserTurret, laserTurretCosts, true, () => viewing?.PreviewDefence(laserTurret)); }
    public void WallSpawn() { BeginPlacementIfAffordable(wall, wallCosts, true, () => viewing?.PreviewDefence(wall)); }
    public void GrapeJamSpawn() { BeginPlacementIfAffordable(grapeJam, grapeJamCosts, true, () => viewing?.PreviewDefence(grapeJam)); }

    void BeginPlacementIfAffordable(GameObject prefab, List<ResourceCostSpawner> costs, bool defence, Action afterBegin)
    {
        if (!CanAfford(costs)) return;
        awaitingPlacement = true;
        isDefence = defence;
        objectToInstantiate = prefab;
        pendingCosts = (costs != null) ? new List<ResourceCostSpawner>(costs) : null;
        afterBegin?.Invoke();
    }

    void Update()
    {
        if (!(awaitingPlacement && Input.GetMouseButtonDown(0) && objectToInstantiate != null)) return;
        if (placingLock) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool gotHit = Physics.Raycast(ray, out hit, Mathf.Infinity, buildLayer);
        if (!gotHit) gotHit = Physics.Raycast(ray, out hit, Mathf.Infinity);
        if (!gotHit) return;

        var hitObject = hit.collider.gameObject;
        var tile = hitObject.GetComponent<Tile>();
        var defence = hitObject.GetComponent<Defence>();

        bool isFactory = objectToInstantiate == deathCat
                      || objectToInstantiate == nullSpaceFabricator
                      || objectToInstantiate == poloniumReactor
                      || objectToInstantiate == smelter
                      || objectToInstantiate == solarPanelArray;

        if (isFactory)
        {
            if (tile == null) return;
            if (!TrySpend(pendingCosts)) return;

            awaitingPlacement = false;
            placingLock = true;

            var grid = tile.GetComponentInParent<GridSystem>();
            Vector3 location = hitObject.transform.position;
            if (grid != null) grid.spawnAvailableSpaces();
            spawnLocation = location;

            Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, hitObject.transform.parent);
            pendingCosts = null;
            Destroy(hitObject);
            StartCoroutine(ReleasePlacingLockEndOfFrame());
        }
        else
        {
            if (defence != null || tile != null) return;
            if (!TrySpend(pendingCosts)) return;

            awaitingPlacement = false;
            placingLock = true;

            spawnLocation = hit.point + hit.normal * 0.5f;

            Transform parent = worldBuildParent;
            if (parent == null)
            {
                GameObject foundDeathCat = GameObject.Find("DeathCat");
                parent = foundDeathCat != null ? foundDeathCat.transform.parent : null;
            }

            Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, parent);
            isDefence = false;
            pendingCosts = null;
            if (viewing != null) viewing.isDefenceBuildActive = false;

            StartCoroutine(ReleasePlacingLockEndOfFrame());
        }
    }

    IEnumerator ReleasePlacingLockEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        objectToInstantiate = null;
        placingLock = false;
    }

    bool TrySpend(List<ResourceCostSpawner> costs)
    {
        if (!CanAfford(costs)) return false;
        if (costs == null) return true;
        foreach (var c in costs) ResourceManager.instance.RemoveResource(c.type, c.amount);
        return true;
    }

    bool CanAfford(List<ResourceCostSpawner> costs)
    {
        if (costs == null) return true;
        foreach (var c in costs) if (GetAmount(c.type) < c.amount) return false;
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
