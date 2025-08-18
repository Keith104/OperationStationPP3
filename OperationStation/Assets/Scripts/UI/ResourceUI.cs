using UnityEngine;
using TMPro; // Make sure TextMeshPro is imported

public class ResourceUI : MonoBehaviour
{
    [Header("Basic Resources")]
    [SerializeField] private TMP_Text tritiumText;
    [SerializeField] private TMP_Text silverText;
    [SerializeField] private TMP_Text poloniumText;

    [Header("Smelted Resources")]
    [SerializeField] private TMP_Text tritiumIngotText;
    [SerializeField] private TMP_Text silverCoinsText;
    [SerializeField] private TMP_Text poloniumCrystalText;

    [Header("Special Resources")]
    [SerializeField] private TMP_Text energyText;

    private void Update()
    {
        if (ResourceManager.instance == null) return;

        // Basic
        tritiumText.text = $"Tritium: {ResourceManager.instance.GetResource(ResourceSO.ResourceType.Tritium)}";
        silverText.text = $"Silver: {ResourceManager.instance.GetResource(ResourceSO.ResourceType.Silver)}";
        poloniumText.text = $"Polonium: {ResourceManager.instance.GetResource(ResourceSO.ResourceType.Polonium)}";

        // Smelted
        tritiumIngotText.text = $"Tritium Ingot: {ResourceManager.instance.GetResource(ResourceSO.ResourceType.TritiumIngot)}";
        silverCoinsText.text = $"Silver Coins: {ResourceManager.instance.GetResource(ResourceSO.ResourceType.SilverCoin)}";
        poloniumCrystalText.text = $"Polonium Crystal: {ResourceManager.instance.GetResource(ResourceSO.ResourceType.PoloniumCrystal)}";

        // Special
        energyText.text = $"Energy: {ResourceManager.instance.GetResource(ResourceSO.ResourceType.Energy)}";
    }
}
