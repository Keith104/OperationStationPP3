using UnityEngine;
[CreateAssetMenu(fileName = "NewUnit", menuName = "OperationStation/New Unit")]
public class UnitSO : ScriptableObject
{
    public enum UnitType
    {
        Modules,
        Defense,
        Mining
    }
    public UnitType unitType;
    public GameObject unitOutline;
    public string unitName;
    public string unitDescription;
    public float unitHealth;

    [Header("Module Data")]
    public float builtPercent;
    public bool isBase;
    [Header("Defense Data")]
    public float attackDamage;
    [Header("Mining Data")]
    public float miningDamage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
