using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class DeathCat : MonoBehaviour, ISelectable, IModule, IDamage
{
    public Module module;
    [SerializeField] int totalCostsLeft;
    [SerializeField] Image lowHealthIndicator;

    [Header("Win Sequence Settings")]
    [SerializeField] GameObject[] objectsToDeactivate;
    [SerializeField] GameObject[] objectsToActivate;
    [SerializeField] PlayableDirector timeline;

    public bool invincible;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lowHealthIndicator = GameObject.FindWithTag("LowHealthIndicator").GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        totalCostsLeft = 0;
        foreach (int cost in module.costsLeft)
        {
            totalCostsLeft += cost;
        }

        //if(totalCostsLeft <= 0 && deathCatFired == false)
        //{
        //    StartWinSequence();
        //    deathCatFired = true;
        //}
    }

    public void StartWinSequence()
    {
        foreach (var obj in objectsToDeactivate)
        {
            obj.SetActive(false);
        }
        foreach(var obj in objectsToActivate)
        {
            obj.SetActive(true);
        }

        timeline.Play();
    }

    public void FireDeathCat()
    {
        Debug.Log("Cat The Death Cat has been fired");
        UnlockNextDiff();
        LevelUIManager.instance.SetActiveMenu(LevelUIManager.instance.menuWin);
    }

    void UnlockNextDiff()
    {
        foreach (DifficultySO diff in DifficultyManager.instance.allDifficulties)
        {
            if (diff.isLocked == true)
            {
                diff.isLocked = false;
                break;
            }
        }
    }

    public void ModuleDie()
    {
        StartCoroutine(DelayDeath());
    }

    public void TakeDamage(float damage)
    {
        if (invincible == false)
        {
            ((IDamage)module).TakeDamage(damage);
            if (module.localHealth - damage < module.stats.unitHealth / 4 && lowHealthIndicator.color.a < 0.1f)
            {
                lowHealthIndicator.color += new Color(
                    lowHealthIndicator.color.r,
                    lowHealthIndicator.color.g,
                    lowHealthIndicator.color.b,
                    0.01f);
            }
        }
    }

    public void TakeControl()
    {
        UnitUIManager.instance.DisableAllMenus();
        UnitUIManager.instance.unitMenu.SetActive(true);
        UnitUIManager.instance.costMenu.SetActive(true);
        ((ISelectable)module).TakeControl();
    }

    private IEnumerator DelayDeath()
    {
        //Destroy(gameObject);
        yield return new WaitForSeconds(1);
        LevelUIManager.instance.menuLose.SetActive(true);
        LevelUIManager.instance.StatePause();
    }
}