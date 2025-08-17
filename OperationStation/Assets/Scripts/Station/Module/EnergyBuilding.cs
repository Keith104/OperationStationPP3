using UnityEngine;

public class EnergyBuilding : MonoBehaviour, ISelectable, IModule, IDamage
{
    [SerializeField] string menuToActivate;

    public void ModuleDie()
    {
        Destroy(gameObject);
    }

    public void TakeControl()
    {
        switch (menuToActivate)
        {
            case "ReactorMenu":
                Debug.Log("Activating Menu!");
                UnitUIManager.instance.DisableAllMenus();
                UnitUIManager.instance.unitMenu.SetActive(true);
                UnitUIManager.instance.reactorMenu.SetActive(true);
                UnitUIManager.instance.tmpUnitName.text = GetComponent<Module>().name;
                var controller = UnitUIManager.instance.reactorMenu.GetComponent<ReactorUIController>();

                if (controller)
                    controller.Bind(GetComponent<Module>().stats);
                Debug.Log($"Menu: {UnitUIManager.instance.reactorMenu} has been activated");
                break;
            case "UnitMenu":
                UnitUIManager.instance.unitMenu.SetActive(true);
                break;
        }
        UnitUIManager.instance.currUnit = gameObject;
    }

    public void TakeDamage(float damage)
    {

    }
}
