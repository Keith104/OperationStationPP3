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
    public GameObject smelterMenu;
    public GameObject nullSpaceMenu;
    public int buttonNum;

    [SerializeField] ButtonFunctions buttonFunctions;

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    public void DisableAllMenus()
    {
        if (costMenu) costMenu.SetActive(false);
        if (reactorMenu) reactorMenu.SetActive(false);
        if (smelterMenu) smelterMenu.SetActive(false);
        if (unitMenu) unitMenu.SetActive(false);
        if(nullSpaceMenu) nullSpaceMenu.SetActive(false);
    }

    public void OnSpendClick(int num)
    {
        if (buttonFunctions) buttonFunctions.PlayClick();
        buttonNum = num;
    }
}
