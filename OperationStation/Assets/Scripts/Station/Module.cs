using System.Collections;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class Module : MonoBehaviour, ISelectable, IDamage
{
    [SerializeField] UnitSO stats;
    [SerializeField] Renderer model;
    [SerializeField] ResourceCost[] resourceCosts;
    public int[] costsLeft;

    private float localHealth;
    private Color origColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        origColor = model.material.color;
        localHealth = stats.unitHealth;
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
    public void TakeDamage(float damage)
    {
        Debug.Log(gameObject.name + " has taken damage");
        StartCoroutine(FlashRed());
        if (stats.isBase == false)
        {
            localHealth -= damage;

            if (localHealth <= 0)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            int costIndex = 0;
            foreach (ResourceCost resourceCost in resourceCosts)
            {
                ResourceSO resourceSO = resourceCost.resource;
                if (costsLeft[costIndex] + (int)damage > resourceCost.cost)
                    costsLeft[costIndex] = resourceCost.cost;
                else
                    costsLeft[costIndex] += (int)damage;
                costIndex++;
            }

            SetCost();
        }
    }
    private IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        model.material.color = origColor;
    }
}
