using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance { get; private set; }

    [Header("Basic Resources")]
    [SerializeField] public int tritium;
    [SerializeField] public int silver;
    [SerializeField] public int polonium;

    [Header("Smelted Resources")]
    [SerializeField] public int tritiumIngot;
    [SerializeField] public int silverCoins;
    [SerializeField] public int poloniumCrystal;

    [Header("Special Resources")]
    [SerializeField] public int energy;

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

    public int GetResource(ResourceSO.ResourceType resource)
    {
        return resource switch
        {
            ResourceSO.ResourceType.Tritium => tritium,
            ResourceSO.ResourceType.Silver => silver,
            ResourceSO.ResourceType.Polonium => polonium,
            ResourceSO.ResourceType.TritiumIngot => tritiumIngot,
            ResourceSO.ResourceType.SilverCoin => silverCoins,
            ResourceSO.ResourceType.PoloniumCrystal => poloniumCrystal,
            ResourceSO.ResourceType.Energy => energy,
            _ => 0
        };
    }

}
