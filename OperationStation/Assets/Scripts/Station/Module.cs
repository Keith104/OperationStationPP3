using UnityEngine;

public class Module : MonoBehaviour, ISelectable
{
    [SerializeField] UnitSO stats;
    [SerializeField] ResourceCost[] resourceCosts;
    public int[] costsLeft;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resourceCosts = stats.cost;
        for (int i = 0; i < resourceCosts.Length; i++)
            costsLeft[i] = resourceCosts[i].cost;
    }

    // Update is called once per frame
    void Update()
    {
        ReduceCost();
    }

    public void TakeControl()
    {
        Debug.Log("Selected Unit");
        UnitUIManager.instance.unitMenu.SetActive(true);



        UnitUIManager.instance.tmpUnitName.text = stats.unitName;
        UnitUIManager.instance.tmpUnitDesc.text = stats.unitDescription;
        SetCost();

    }

    void SetCost()
    {
        UnitUIManager.instance.tmpUnitCost.text = "";
        int costIndex = 0;
        foreach (ResourceCost resourceCost in resourceCosts)
        {
            ResourceSO resourceSO = resourceCost.resource;
            UnitUIManager.instance.tmpUnitCost.text +=
                resourceSO.resourceType.ToString() + ": " +
                costsLeft[costIndex] + "\n";
            costIndex++;
        }
    }

    void ReduceCost()
    {
        int currIndex = UnitUIManager.instance.buttonNum;
        if (currIndex != -1)
        {
            if (costsLeft[currIndex] > 0)
                costsLeft[currIndex]--;
        }

        SetCost();
        UnitUIManager.instance.buttonNum = -1;
    }
}
