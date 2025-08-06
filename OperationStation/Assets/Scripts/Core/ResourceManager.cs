using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance { get; private set; }

    [Header("Basic Resources")]
    [SerializeField] int tritium;
    [SerializeField] int silver;
    [SerializeField] int polonium;

    [Header("Smelted Resources")]
    [SerializeField] int tritiumIngot;
    [SerializeField] int silverCoins;
    [SerializeField] int poloniumCrystal;

    [Header("Special Resources")]
    [SerializeField] int energy;

    [Header("Debug Tools")]
    [SerializeField] bool debug;
    [SerializeField] int amountToDebug;
    [SerializeField] ResourceSO.ResourceType resourceType;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if(debug)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                AddResource(resourceType, amountToDebug);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                RemoveResource(resourceType, amountToDebug);
            }
        }

    }

    public void AddResource(ResourceSO.ResourceType resource, int amount)
    {
        switch (resource)
        {
            case ResourceSO.ResourceType.Tritium:
                tritium += amount;
                break;
            case ResourceSO.ResourceType.Silver:
                silver += amount;
                break;
            case ResourceSO.ResourceType.Polonium:
                polonium += amount;
                break;
            case ResourceSO.ResourceType.TritiumIngot:
                tritiumIngot += amount;
                break;
            case ResourceSO.ResourceType.SilverCoin:
                silverCoins += amount;
                break;
            case ResourceSO.ResourceType.PoloniumCrystal:
                poloniumCrystal += amount;
                break;
            case ResourceSO.ResourceType.Energy:
                energy += amount; 
                break;

        }
    }
    public void RemoveResource(ResourceSO.ResourceType resource, int amount)
    {
        switch (resource)
        {
            case ResourceSO.ResourceType.Tritium:
                tritium -= amount;
                break;
            case ResourceSO.ResourceType.Silver:
                silver -= amount;
                break;
            case ResourceSO.ResourceType.Polonium:
                polonium -= amount;
                break;
            case ResourceSO.ResourceType.TritiumIngot:
                tritiumIngot -= amount;
                break;
            case ResourceSO.ResourceType.SilverCoin:
                silverCoins -= amount;
                break;
            case ResourceSO.ResourceType.PoloniumCrystal:
                poloniumCrystal -= amount;
                break;
            case ResourceSO.ResourceType.Energy:
                energy -= amount;
                break;

        }
    }
}
