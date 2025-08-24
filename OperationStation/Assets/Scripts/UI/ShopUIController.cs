using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        SolarPanelArray
    }

    [System.Serializable]
    public struct ResourceField
    {
        public ResourceSO.ResourceType type;
        public TMP_Text nameText;
        public TMP_Text valueText;
    }

    [System.Serializable]
    public struct ItemPanel
    {
        public ItemKey item;
        public string displayName;
        public TMP_Text titleText;
        public ResourceField[] costFields;
    }

    [SerializeField] private ObjectSpawner spawner;
    [SerializeField] private ItemPanel[] panels;

    void Start()
    {
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
        var lookup = new Dictionary<ResourceSO.ResourceType, int>();
        if (costs != null)
        {
            for (int i = 0; i < costs.Count; i++)
                lookup[costs[i].type] = costs[i].amount;
        }

        for (int i = 0; i < panel.costFields.Length; i++)
        {
            var f = panel.costFields[i];
            if (f.nameText != null) f.nameText.text = GetDisplayName(f.type);
            if (f.valueText != null) f.valueText.text = lookup.TryGetValue(f.type, out var amt) ? amt.ToString() : "0";
        }
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
        switch (key)
        {
            case ItemKey.BasicTurret: return spawner.basicTurretCosts;
            case ItemKey.NullSpaceFabricator: return spawner.nullSpaceFabricatorCosts;
            case ItemKey.Wall: return spawner.wallCosts;
            case ItemKey.GrapeJam: return spawner.grapeJamCosts;
            case ItemKey.PoloniumReactor: return spawner.poloniumReactorCosts;
            case ItemKey.Smelter: return spawner.smelterCosts;
            case ItemKey.SolarPanelArray: return spawner.solarPanelArrayCosts;
            default: return null;
        }
    }
}
