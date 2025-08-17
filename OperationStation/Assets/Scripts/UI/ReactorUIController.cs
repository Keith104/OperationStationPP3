using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReactorUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Slider fuelSlider;
    [SerializeField] Button btn10;
    [SerializeField] Button btn25;
    [SerializeField] Button btn50;
    [SerializeField] Button btnStart;
    [SerializeField] TextMeshProUGUI txtUnitsHeld;   // reactor's internal fuel reserve
    [SerializeField] TextMeshProUGUI txtProjected;    // energy per tick (per 1 polonium)
    [SerializeField] TextMeshProUGUI txtEvery;
    [SerializeField] TextMeshProUGUI txtCountdown;
    [SerializeField] Image cooldownFill;

    [Header("Timing")]
    [SerializeField] float cooldownSeconds = 10f;

    int energyPerPolonium;      // from UnitSO
    bool running;
    float countdown;
    int unitsHeld;              // reactor-internal fuel reserve; decrements each tick
    Coroutine loop;

    int lastStoragePolonium = int.MinValue;

    public void Bind(UnitSO unitSo)
    {
        energyPerPolonium = Mathf.Max(0, unitSo.energyProductionAmount);
        ResetUI();
    }

    void OnEnable()
    {
        btn10.onClick.AddListener(() => QuickSet(10));
        btn25.onClick.AddListener(() => QuickSet(25));
        btn50.onClick.AddListener(() => QuickSet(50));
        btnStart.onClick.AddListener(Toggle);

        fuelSlider.onValueChanged.AddListener(_ =>
        {
            RefreshProjected();
            UpdateLoadInteractivity();
        });

        ResetUI();
    }

    void OnDisable()
    {
        btn10.onClick.RemoveAllListeners();
        btn25.onClick.RemoveAllListeners();
        btn50.onClick.RemoveAllListeners();
        btnStart.onClick.RemoveAllListeners();
        fuelSlider.onValueChanged.RemoveAllListeners();

        if (loop != null) StopCoroutine(loop);
        loop = null;
        running = false;
    }

    void Update()
    {
        if (!running)
        {
            int storage = GetStoragePolonium();
            if (storage != lastStoragePolonium)
            {
                lastStoragePolonium = storage;
                ClampSliderToStorage();
                RefreshProjected();
                UpdateLoadInteractivity();
            }
        }
        else
        {
            countdown -= Time.unscaledDeltaTime;
            if (countdown < 0f) countdown = 0f;

            if (txtCountdown) txtCountdown.text = Format(countdown);
            if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / cooldownSeconds);
        }
    }

    void Toggle()
    {
        if (running) StopBurn();
        else StartBurn();
    }

    void StartBurn()
    {
        int storage = GetStoragePolonium();
        int desired = Mathf.Clamp(Mathf.RoundToInt(fuelSlider.value), 0, storage);
        if (desired <= 0) return;

        ResourceManager.instance.RemoveResource(ResourceSO.ResourceType.Polonium, desired);
        unitsHeld = desired;

        running = true;
        LockLoadInputs(true);
        RefreshUnitsHeld();
        RefreshProjected();

        countdown = cooldownSeconds;
        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(cooldownSeconds)}s";
        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(BurnLoop());
        SetStartButtonText("Stop");
    }

    void StopBurn()
    {
        running = false;
        if (loop != null) StopCoroutine(loop);
        loop = null;

        LockLoadInputs(false);
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        SetStartButtonText("Start Burn");

        ResetUI();
    }

    IEnumerator BurnLoop()
    {
        while (running)
        {
            while (countdown > 0f && running) yield return null;
            if (!running) yield break;

            if (unitsHeld > 0)
            {
                ResourceManager.instance.AddResource(ResourceSO.ResourceType.Energy, energyPerPolonium);
                unitsHeld -= 1;
                RefreshUnitsHeld();
            }

            if (unitsHeld <= 0)
            {
                StopBurn();
                yield break;
            }

            countdown = cooldownSeconds;
        }
    }

    void QuickSet(int amount)
    {
        int storage = GetStoragePolonium();
        fuelSlider.value = Mathf.Clamp(amount, 0, storage);
        RefreshProjected();
        UpdateLoadInteractivity();
    }

    void ResetUI()
    {
        lastStoragePolonium = GetStoragePolonium();
        ClampSliderToStorage();
        RefreshUnitsHeld();            // shows reactor reserve (0 until loaded)
        RefreshProjected();
        UpdateLoadInteractivity();

        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(cooldownSeconds)}s";
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
    }

    void RefreshUnitsHeld()
    {
        if (txtUnitsHeld) txtUnitsHeld.text = unitsHeld.ToString();
    }

    void ClampSliderToStorage()
    {
        int storage = lastStoragePolonium;
        fuelSlider.maxValue = Mathf.Max(0, storage);
        if (fuelSlider.value > storage) fuelSlider.value = storage;
    }

    void RefreshProjected()
    {
        if (txtProjected) txtProjected.text = energyPerPolonium.ToString("N0");
    }

    void UpdateLoadInteractivity()
    {
        bool canAdjust = !running;
        int storage = lastStoragePolonium;
        int desired = Mathf.RoundToInt(fuelSlider.value);

        btn10.interactable = canAdjust && storage >= 10;
        btn25.interactable = canAdjust && storage >= 25;
        btn50.interactable = canAdjust && storage >= 50;

        if (canAdjust && storage <= 0)
        {
            fuelSlider.value = 0;
            RefreshProjected();
        }

        btnStart.interactable = canAdjust && desired > 0 && desired <= storage;
    }

    void LockLoadInputs(bool on)
    {
        fuelSlider.interactable = !on;
        btn10.interactable = !on ? btn10.interactable : false;
        btn25.interactable = !on ? btn25.interactable : false;
        btn50.interactable = !on ? btn50.interactable : false;
        btnStart.interactable = true;
    }

    void SetStartButtonText(string s)
    {
        var label = btnStart.GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.text = s;
    }

    int GetStoragePolonium()
    {
        var f = typeof(ResourceManager).GetField("polonium", BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? (int)f.GetValue(ResourceManager.instance) : 0;
    }

    static string Format(float s)
    {
        int t = Mathf.CeilToInt(s);
        int mm = t / 60;
        int ss = t % 60;
        return (mm > 0) ? $"{mm:00}:{ss:00}" : $"00:{ss:00}";
    }
}
