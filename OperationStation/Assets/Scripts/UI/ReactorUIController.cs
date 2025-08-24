using System.Collections;
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
    [SerializeField] TextMeshProUGUI txtUnitsHeld;
    [SerializeField] TextMeshProUGUI txtProjected;
    [SerializeField] TextMeshProUGUI txtEvery;
    [SerializeField] TextMeshProUGUI txtCountdown;
    [SerializeField] Image cooldownFill;

    [Header("Timing")]
    [SerializeField] float cooldownSeconds = 10f;

    int energyPerPolonium;
    bool running;
    float countdown;
    int unitsHeld;
    Coroutine loop;
    Coroutine initWait;

    int lastStoragePolonium = int.MinValue;

    public void Bind(UnitSO unitSo)
    {
        energyPerPolonium = Mathf.Max(0, unitSo.energyProductionAmount);
        if (ResourceManager.instance != null) ResetUI();
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

        if (initWait != null) StopCoroutine(initWait);
        initWait = StartCoroutine(WaitForResourceManagerThenInit());
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

        if (initWait != null) StopCoroutine(initWait);
        initWait = null;

        running = false;
    }

    IEnumerator WaitForResourceManagerThenInit()
    {
        while (ResourceManager.instance == null) yield return null;
        ResetUI();
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

    public void Toggle()
    {
        if (running) StopBurn();
        else StartBurn();
    }

    void StartBurn()
    {
        int storage = GetStoragePolonium();
        int desired = Mathf.Clamp(Mathf.RoundToInt(fuelSlider.value), 0, storage);

        if (desired > 0)
        {
            ResourceManager.instance.RemoveResource(ResourceSO.ResourceType.Polonium, desired);
            unitsHeld += desired;
        }
        else if (unitsHeld <= 0)
        {
            return;
        }

        running = true;
        LockLoadInputs(true);
        RefreshUnitsHeld();
        RefreshProjected();

        countdown = cooldownSeconds;
        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(cooldownSeconds)}s";
        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(BurnLoop());
        SetStartButtonText("Stop");
        btnStart.interactable = true;
    }

    void StopBurn()
    {
        running = false;
        if (loop != null) StopCoroutine(loop);
        loop = null;

        fuelSlider.value = 0;
        LockLoadInputs(false);
        RefreshProjected();
        UpdateLoadInteractivity();

        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        SetStartButtonText("Start Burn");
        btnStart.interactable = true;
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
        RefreshUnitsHeld();
        RefreshProjected();
        UpdateLoadInteractivity();

        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(cooldownSeconds)}s";
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;

        SetStartButtonText(running ? "Stop" : "Start Burn");
        btnStart.interactable = running || (unitsHeld > 0);
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
        int storage = lastStoragePolonium;
        int desired = Mathf.RoundToInt(fuelSlider.value);

        bool canAdjust = !running;
        btn10.interactable = canAdjust && storage >= 10;
        btn25.interactable = canAdjust && storage >= 25;
        btn50.interactable = canAdjust && storage >= 50;

        if (canAdjust && storage <= 0 && desired > 0)
        {
            fuelSlider.value = 0;
            desired = 0;
            RefreshProjected();
        }

        btnStart.interactable = running || (unitsHeld > 0) || (desired > 0 && desired <= storage);
    }

    void LockLoadInputs(bool on)
    {
        fuelSlider.interactable = !on;
        btn10.interactable = !on ? btn10.interactable : false;
        btn25.interactable = !on ? btn25.interactable : false;
        btn50.interactable = !on ? btn50.interactable : false;
    }

    void SetStartButtonText(string s)
    {
        var label = btnStart.GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.text = s;
    }

    int GetStoragePolonium()
    {
        var rm = ResourceManager.instance;
        if (rm == null) return 0;
        return rm.GetResource(ResourceSO.ResourceType.Polonium);
    }

    static string Format(float s)
    {
        int t = Mathf.CeilToInt(s);
        int mm = t / 60;
        int ss = t % 60;
        return (mm > 0) ? $"{mm:00}:{ss:00}" : $"00:{ss:00}";
    }
}
