using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SolarUIController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txtEvery;
    [SerializeField] TextMeshProUGUI txtCountdown;
    [SerializeField] Image cooldownFill;

    EnergyBuilding target;
    Module targetModule;
    float interval = 10f;
    int energyPerTick = 0;
    float countdown;

    void OnEnable()
    {
        AutoBindFromSelection();
        SetupUI();
    }

    void OnDisable()
    {
        target = null;
        targetModule = null;
    }

    void Update()
    {
        AutoBindFromSelection();
        if (!target) return;

        countdown -= Time.unscaledDeltaTime;
        if (countdown < 0f) countdown = 0f;

        if (txtCountdown) txtCountdown.text = Format(countdown);
        if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / Mathf.Max(0.01f, interval));

        if (countdown <= 0f) countdown = interval;
    }

    void AutoBindFromSelection()
    {
        var ui = UnitUIManager.instance;
        if (!ui) return;
        var eb = ui.currUnit ? ui.currUnit.GetComponentInParent<EnergyBuilding>() : null;
        if (eb && eb != target) Bind(eb);
    }

    public void Bind(EnergyBuilding eb)
    {
        target = eb;
        targetModule = eb ? eb.GetComponent<Module>() : null;
        interval = ReadIntervalSeconds(eb);
        energyPerTick = (targetModule && targetModule.stats) ? Mathf.Max(0, targetModule.stats.energyProductionAmount) : 0;
        countdown = interval;
        SetupUI();
    }

    void SetupUI()
    {
        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(interval)}s";
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(interval):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
    }

    float ReadIntervalSeconds(EnergyBuilding eb)
    {
        if (!eb) return 10f;
        var f = typeof(EnergyBuilding).GetField("autoIntervalSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
        if (f != null)
        {
            object v = f.GetValue(eb);
            if (v is float s) return Mathf.Max(0.01f, s);
        }
        return 10f;
    }

    static string Format(float s)
    {
        int t = Mathf.CeilToInt(s);
        int mm = t / 60;
        int ss = t % 60;
        return (mm > 0) ? $"{mm:00}:{ss:00}" : $"00:{ss:00}";
    }
}
