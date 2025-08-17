using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SolarUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI txtProjected;   // energy per tick
    [SerializeField] TextMeshProUGUI txtEvery;       // "Every 10s"
    [SerializeField] TextMeshProUGUI txtCountdown;   // "00:08"
    [SerializeField] Image cooldownFill;             // optional radial fill

    [Header("Timing")]
    [SerializeField] float cooldownSeconds = 10f;

    [Header("Behavior")]
    public bool generateEnergy = true;     // turn OFF if something else already generates (e.g., EnergyBuilding.autoGenerate)

    int energyPerTick;        // from UnitSO
    float countdown;
    Coroutine loop;

    public void Bind(UnitSO unitSo)
    {
        energyPerTick = Mathf.Max(0, unitSo.energyProductionAmount);
        InitUI();
        StartLoop();
    }

    void OnEnable()
    {
        if (loop == null && energyPerTick > 0)
        {
            InitUI();
            StartLoop();
        }
    }

    void OnDisable()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    void InitUI()
    {
        if (txtProjected) txtProjected.text = energyPerTick.ToString("N0");
        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(cooldownSeconds)}s";
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        countdown = cooldownSeconds;
    }

    void StartLoop()
    {
        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(TickLoop());
    }

    IEnumerator TickLoop()
    {
        while (true)
        {
            // per-frame countdown + UI
            while (countdown > 0f)
            {
                countdown -= Time.unscaledDeltaTime;
                if (countdown < 0f) countdown = 0f;

                if (txtCountdown) txtCountdown.text = Format(countdown);
                if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / cooldownSeconds);
                yield return null;
            }

            // tick
            if (generateEnergy && ResourceManager.instance != null)
                ResourceManager.instance.AddResource(ResourceSO.ResourceType.Energy, energyPerTick);

            countdown = cooldownSeconds;
            if (cooldownFill) cooldownFill.fillAmount = 0f;
        }
    }

    static string Format(float s)
    {
        int t = Mathf.CeilToInt(s);
        int mm = t / 60;
        int ss = t % 60;
        return (mm > 0) ? $"{mm:00}:{ss:00}" : $"00:{ss:00}";
    }
}
