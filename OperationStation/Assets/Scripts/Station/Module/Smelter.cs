using UnityEngine;

public class Smelter : MonoBehaviour, ISelectable, IModule
{
    [SerializeField] Module module;

    public void ModuleDie() { Destroy(gameObject); }

    public void TakeControl()
    {
        var ui = UnitUIManager.instance;
        ui.DisableAllMenus();

        ui.unitMenu.SetActive(true);
        ui.smelterMenu.SetActive(true);

        ui.tmpUnitName.text = module ? module.name : "Smelter";
        ((ISelectable)module).TakeControl();
        ui.currUnit = gameObject;
    }


}
