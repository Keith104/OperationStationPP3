using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "OperationStation/New Resource")]
public class ResourceSO : ScriptableObject
{
    public enum ResourceType
    {
        Tritium,
        Silver,
        Polonium,
        TritiumIngot,
        SilverCoin,
        PoloniumCrystal,
        Energy
    }
    public ResourceType resourceType;
}
