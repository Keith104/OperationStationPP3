using UnityEngine;
using TMPro;

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

        tritiumText.text = ResourceManager.instance.GetResource(ResourceSO.ResourceType.Tritium).ToString();
        silverText.text = ResourceManager.instance.GetResource(ResourceSO.ResourceType.Silver).ToString();
        poloniumText.text = ResourceManager.instance.GetResource(ResourceSO.ResourceType.Polonium).ToString();

        tritiumIngotText.text = ResourceManager.instance.GetResource(ResourceSO.ResourceType.TritiumIngot).ToString();
        silverCoinsText.text = ResourceManager.instance.GetResource(ResourceSO.ResourceType.SilverCoin).ToString();
        poloniumCrystalText.text = ResourceManager.instance.GetResource(ResourceSO.ResourceType.PoloniumCrystal).ToString();

        energyText.text = ResourceManager.instance.GetResource(ResourceSO.ResourceType.Energy).ToString();
    }
}
