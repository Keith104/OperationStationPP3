using TMPro;
using UnityEngine;

public class UnitUIManager : MonoBehaviour
{
    public static UnitUIManager instance { get; private set; }

    [Header("UI Elements")]
    public GameObject currUnit;
    public GameObject unitMenu;
    public GameObject costMenu;
    public GameObject reactorMenu;
    public TextMeshProUGUI tmpUnitName;
    public TextMeshProUGUI tmpUnitDesc;
    public TextMeshProUGUI tmpUnitCost;
    public int buttonNum;

    [SerializeField] ButtonFunctions buttonFunctions;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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

    public void DisableAllMenus()
    {
        costMenu.SetActive(false);
        reactorMenu.SetActive(false);
    }

    public void OnSpendClick(int num)
    {
        buttonFunctions.PlayClick();
        Debug.Log("Spend Button Clicked");
        buttonNum = num;
    }
}
