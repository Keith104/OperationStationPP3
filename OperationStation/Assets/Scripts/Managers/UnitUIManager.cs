using TMPro;
using UnityEngine;

public class UnitUIManager : MonoBehaviour
{
    public static UnitUIManager instance { get; private set; }

    [Header("UI Elements")]
    public GameObject currUnit;
    public GameObject unitMenu;
    public TextMeshProUGUI tmpUnitName;
    public TextMeshProUGUI tmpUnitDesc;
    public TextMeshProUGUI tmpUnitCost;
    public int buttonNum;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSpendClick(int num)
    {
        Debug.Log("Spend Button Clicked");
        buttonNum = num;
    }
}
