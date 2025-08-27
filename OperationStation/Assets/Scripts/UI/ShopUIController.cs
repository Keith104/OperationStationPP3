using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    public enum ItemKey
    {
        BasicTurret,
        NullSpaceFabricator,
        Wall,
        GrapeJam,
        PoloniumReactor,
        Smelter,
        SolarPanelArray,
        LaserTurret
    }

    [System.Serializable]
    public struct ResourceField
    {
        public ResourceSO.ResourceType type;
        public TMP_Text nameText;
        public TMP_Text valueText; // shows REQUIRED amount; colored by affordability
    }

    [System.Serializable]
    public struct ItemPanel
    {
        public ItemKey item;
        public string displayName;
        public TMP_Text titleText;
        public ResourceField[] costFields;

        [Header("Spend / Build")]
        public Button spendButton; // disabled if not affordable
    }

    [SerializeField] private ObjectSpawner spawner;
    [SerializeField] private ItemPanel[] panels;

    void Start()
    {
        // Wire up each panel's spend button to the correct item
        for (int i = 0; i < panels.Length; i++)
        {
            int captured = i; // avoid closure issues
            if (panels[i].spendButton != null)
            {
                panels[i].spendButton.onClick.RemoveAllListeners();
                panels[i].spendButton.onClick.AddListener(() => OnSpendClicked(captured));
            }
        }

        RefreshAll();
    }

    public void RefreshAll()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            RefreshItem(ref panels[i]);
        }
    }

    void RefreshItem(ref ItemPanel panel)
    {
        if (panel.titleText != null)
            panel.titleText.text = string.IsNullOrEmpty(panel.displayName) ? panel.item.ToString() : panel.displayName;

        var costs = GetCosts(panel.item);

        // Build a quick lookup for required amounts by resource type
        var requiredLookup = new Dictionary<ResourceSO.ResourceType, int>();
        if (costs != null)
        {
            for (int i = 0; i < costs.Count; i++)
                requiredLookup[costs[i].type] = costs[i].amount;
        }

        bool allAffordable = true;

        // Update each displayed resource line
        for (int i = 0; i < panel.costFields.Length; i++)
        {
            var f = panel.costFields[i];

            if (f.nameText != null) f.nameText.text = GetDisplayName(f.type);

            int required = requiredLookup.TryGetValue(f.type, out var amt) ? amt : 0;
            if (f.valueText != null)
            {
                f.valueText.text = required.ToString();

                int have = GetAmount(f.type);
                bool canAffordThisResource = have >= required;

                // Colorize: white if affordable, red if not
                f.valueText.color = canAffordThisResource ? Color.white : Color.red;

                if (!canAffordThisResource) allAffordable = false;
            }
        }

        // Enable/disable the spend button based on overall affordability
        if (panel.spendButton != null)
            panel.spendButton.interactable = costs == null ? true : allAffordable;
    }

    // Button callback wired in Start()
    void OnSpendClicked(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= panels.Length || spawner == null) return;

        var item = panels[panelIndex].item;

        // Call the matching spawner method; those methods will do the actual TrySpend()
        switch (item)
        {
            case ItemKey.BasicTurret: spawner.BasicTurretSpawn(); break;
            case ItemKey.NullSpaceFabricator: spawner.NullSpaceFabricatorSpawn(); break;
            case ItemKey.Wall: spawner.WallSpawn(); break;
            case ItemKey.GrapeJam: spawner.GrapeJamSpawn(); break;
            case ItemKey.PoloniumReactor: spawner.PoloniumReactorSpawn(); break;
            case ItemKey.Smelter: spawner.SmelterSpawn(); break;
            case ItemKey.SolarPanelArray: spawner.SolarPanelArraySpawn(); break;
            case ItemKey.LaserTurret: spawner.LaserTurretSpawn(); break;
        }

        // After spending (or failing to), refresh UI to reflect new counts & button state
        RefreshAll();
    }

    string GetDisplayName(ResourceSO.ResourceType type)
    {
        switch (type)
        {
            case ResourceSO.ResourceType.TritiumIngot: return "Ingots";
            case ResourceSO.ResourceType.SilverCoin: return "Coins";
            case ResourceSO.ResourceType.PoloniumCrystal: return "Crystals";
            default: return type.ToString();
        }
    }

    List<ResourceCostSpawner> GetCosts(ItemKey key)
    {
        if (spawner == null) return null;
        switch (key)
        {
            case ItemKey.BasicTurret: return spawner.basicTurretCosts;
            case ItemKey.NullSpaceFabricator: return spawner.nullSpaceFabricatorCosts;
            case ItemKey.Wall: return spawner.wallCosts;
            case ItemKey.GrapeJam: return spawner.grapeJamCosts;
            case ItemKey.PoloniumReactor: return spawner.poloniumReactorCosts;
            case ItemKey.Smelter: return spawner.smelterCosts;
            case ItemKey.SolarPanelArray: return spawner.solarPanelArrayCosts;
            case ItemKey.LaserTurret: return spawner.laserTurretCosts;
            default: return null;
        }
    }

    // Mirror of ObjectSpawner's getter so we can colorize correctly
    int GetAmount(ResourceSO.ResourceType type)
    {
        var rm = ResourceManager.instance;
        if (rm == null) return 0;

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
