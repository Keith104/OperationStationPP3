using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[Serializable]
public struct ResourceCostSpawner
{
    public ResourceSO.ResourceType type;
    public int amount;
}

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] GameObject buildButton;
    [SerializeField] ButtonFunctions buttonFunctions;
    
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

    public float adjacencyRadius = 1.25f;
    public float armPlacementDelay = 0.25f;

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

    static bool globalPlacingLock;
    static ObjectSpawner placementOwner;

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
        if (placementOwner == this) placementOwner = null;
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
        StartCoroutine(DelayedPlacement(prefab, costs, defence, afterBegin));
    }

    IEnumerator DelayedPlacement(GameObject prefab, List<ResourceCostSpawner> costs, bool defence, Action afterBegin)
    {
        yield return new WaitForSeconds(armPlacementDelay);
        if (globalPlacingLock) yield break;
        if (placementOwner != null && placementOwner != this) yield break;
        if (awaitingPlacement) yield break;

        placementOwner = this;
        awaitingPlacement = true;
        isDefence = defence;
        objectToInstantiate = prefab;
        pendingCosts = (costs != null) ? new List<ResourceCostSpawner>(costs) : null;
        afterBegin?.Invoke();
    }

    void Update()
    {
        if (placementOwner != this) return;
        if (!awaitingPlacement || objectToInstantiate == null) return;
        if (globalPlacingLock) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool pressed = Mouse.current != null ? Mouse.current.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
        if (!pressed) return;

        if (!isDefence)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, Mathf.Infinity);
            float bestDist = float.MaxValue;
            Tile tile = null;
            GameObject tileGO = null;
            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].collider.GetComponent<Tile>();
                if (t != null && hits[i].distance < bestDist)
                {
                    bestDist = hits[i].distance;
                    tile = t;
                    tileGO = t.gameObject;
                }
            }
            if (tile == null) return;
            if (!TrySpend(pendingCosts)) return;

            globalPlacingLock = true;
            awaitingPlacement = false;

            spawnLocation = tileGO.transform.position;
            Transform parent = worldBuildParent != null ? worldBuildParent : tileGO.transform.parent;

            var instance = Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, parent);

            var clickedGrid = tileGO.GetComponentInParent<GridSystem>();
            if (clickedGrid) clickedGrid.ForceRefreshImmediate();

            var newGrid = instance.GetComponentInChildren<GridSystem>();
            if (newGrid) newGrid.ForceRefreshImmediate();

            pendingCosts = null;
            objectToInstantiate = null;
            if (viewing != null) viewing.isDefenceBuildActive = false;
            StartCoroutine(RemoveTileNextFrame(tileGO));
            StartCoroutine(ReleaseGlobalLockAfterMouseUp());
            placementOwner = null;
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, Mathf.Infinity, buildLayer)) return;
            if (!TrySpend(pendingCosts)) return;

            globalPlacingLock = true;
            awaitingPlacement = false;

            spawnLocation = hit.point + hit.normal * 0.5f;

            Transform parent = worldBuildParent != null ? worldBuildParent : null;

            Instantiate(objectToInstantiate, spawnLocation, Quaternion.identity, parent);
            isDefence = false;
            pendingCosts = null;

            objectToInstantiate = null;
            if (viewing != null) viewing.isDefenceBuildActive = false;
            StartCoroutine(ReleaseGlobalLockAfterMouseUp());
            placementOwner = null;
        }
    }

    IEnumerator RemoveTileNextFrame(GameObject tileGO)
    {
        yield return null;
        if (tileGO) Destroy(tileGO);
    }

    IEnumerator ReleaseGlobalLockAfterMouseUp()
    {
        while (Mouse.current != null ? Mouse.current.leftButton.isPressed : Input.GetMouseButton(0)) yield return null;
        yield return null;
        globalPlacingLock = false;
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
        buttonFunctions.SetActiveMenu(buildButton);
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
