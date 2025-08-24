using UnityEngine;
using System;
using System.Drawing;
using System.Collections;

public class NullSpaceFabricator : MonoBehaviour, ISelectable, IModule
{

    [SerializeField] GameObject miningPrefab;

    public int totalShips;

    public enum MiningDesignations
    {
        Heracles, Perseus, Theseus, Helen_Of_Troy, Achilles, 
        Hippolyta, Jason_Grace, Percy_Jackson, Aeneas, Bellerophon,
        Amphion, Aphrodite, Ares, Castor, Pollux

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        totalShips = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //if(totalShips <= 14)
        //{
        //    StartCoroutine(FlashRed());

        //}
    }


    private IEnumerator FlashRed()
    {
        yield return new WaitForSeconds(0.5f);
        SpawnMiningShip();
    }
    public void SpawnMiningShip()
    {
        if(totalShips <= 14)
        {
            var newMiningShip = Instantiate(miningPrefab, new Vector3(transform.position.x, transform.position.y + .5f,
                transform.position.z), Quaternion.identity, transform);
            newMiningShip.name = DesignatedName();
            totalShips++;
            
        }
    }

    public string DesignatedName()
    {
        string Designation;
        switch (totalShips)
        {
            case 0: Designation = MiningDesignations.Heracles.ToString(); break;
            case 1: Designation = MiningDesignations.Perseus.ToString(); break;
            case 2: Designation = MiningDesignations.Theseus.ToString(); break;
            case 3: Designation = MiningDesignations.Helen_Of_Troy.ToString(); break;
            case 4: Designation = MiningDesignations.Achilles.ToString(); break;
            case 5: Designation = MiningDesignations.Hippolyta.ToString(); break;
            case 6: Designation = MiningDesignations.Jason_Grace.ToString(); break;
            case 7: Designation = MiningDesignations.Percy_Jackson.ToString(); break;
            case 8: Designation = MiningDesignations.Aeneas.ToString(); break;
            case 9: Designation = MiningDesignations.Bellerophon.ToString(); break;
            case 10:Designation = MiningDesignations.Amphion.ToString(); break;
            case 11:Designation = MiningDesignations.Aphrodite.ToString(); break;
            case 12:Designation = MiningDesignations.Ares.ToString(); break;
            case 13:Designation = MiningDesignations.Castor.ToString(); break;
            case 14:Designation = MiningDesignations.Pollux.ToString(); break;
            default: Designation = "Null"; break;
        }

        return "Mining Ship_-_" + Designation.ToString();
    }

    public void TakeControl()
    {
        var ui = UnitUIManager.instance;
        ui.DisableAllMenus();
        ui.unitMenu.SetActive(true);
        ui.nullSpaceMenu.SetActive(true);

        var controller = ui.nullSpaceMenu.GetComponentInChildren<NullSpaceFabricatorUIController>(true);
        if (controller) controller.Bind(this);
    }


    public void ModuleDie()
    {
        Destroy(gameObject);
    }
}
