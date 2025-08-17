using System.Collections;
using UnityEngine;

public class EnergyBuilding : MonoBehaviour, ISelectable, IModule, IDamage
{
    [SerializeField] string menuToActivate;
    [SerializeField] bool autoGenerate = false;
    [SerializeField] float autoIntervalSeconds = 10f;

    Module moduleRef;
    Coroutine autoLoop;

    public void Awake()
    {
        moduleRef = GetComponent<Module>();
    }

    void OnEnable()
    {
        if (autoGenerate && moduleRef && moduleRef.stats)
            autoLoop = StartCoroutine(AutoGenLoop());
    }

    void OnDisable()
    {
        if (autoLoop != null)
        {
            StopCoroutine(autoLoop);
            autoLoop = null;
        }
    }

    IEnumerator AutoGenLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.01f, autoIntervalSeconds));
        while (true)
        {
            yield return wait;
            if (ResourceManager.instance != null && moduleRef && moduleRef.stats)
                ResourceManager.instance.AddResource(ResourceSO.ResourceType.Energy, Mathf.Max(0, moduleRef.stats.energyProductionAmount));
        }
    }

    public void ModuleDie()
    {
        Destroy(gameObject);
    }

    public void TakeControl()
    {
        switch (menuToActivate)
        {
            case "ReactorMenu":
                UnitUIManager.instance.DisableAllMenus();
                UnitUIManager.instance.unitMenu.SetActive(true);
                UnitUIManager.instance.reactorMenu.SetActive(true);
                if (moduleRef) UnitUIManager.instance.tmpUnitName.text = moduleRef.name;
                var parent = UnitUIManager.instance.reactorMenu.gameObject.transform.parent.gameObject;
                var controller = parent.GetComponentInParent<ReactorUIController>();
                if (controller && moduleRef && moduleRef.stats) controller.Bind(moduleRef.stats);
                break;

            case "UnitMenu":
                UnitUIManager.instance.unitMenu.SetActive(true);
                break;
        }
        UnitUIManager.instance.currUnit = gameObject;
    }

    public void TakeDamage(float damage) { }
}
