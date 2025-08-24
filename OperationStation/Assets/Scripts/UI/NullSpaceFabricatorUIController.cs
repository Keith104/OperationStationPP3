using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NullSpaceFabricatorUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Button btnLeft;
    [SerializeField] Button btnRight;
    [SerializeField] Button btnStart;
    [SerializeField] TextMeshProUGUI txtShipsHeld;
    [SerializeField] TextMeshProUGUI txtToMake;
    [SerializeField] TextMeshProUGUI txtCost;
    [SerializeField] TextMeshProUGUI txtEvery;
    [SerializeField] TextMeshProUGUI txtCountdown;
    [SerializeField] Image cooldownFill;

    [Header("Settings")]
    [SerializeField] int maxPerFabricator = 15;
    [SerializeField] int costPerShip = 100;
    [SerializeField] float cooldownSeconds = 10f;
    [SerializeField] ResourceSO.ResourceType costResource = ResourceSO.ResourceType.Energy;

    NullSpaceFabricator target;
    int toMake;
    int batchLeft;
    bool running;
    float countdown;

    void OnEnable()
    {
        btnLeft.onClick.AddListener(() => Adjust(-1));
        btnRight.onClick.AddListener(() => Adjust(1));
        btnStart.onClick.AddListener(Toggle);
        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(cooldownSeconds)}s";
        if (txtCost) txtCost.text = costPerShip.ToString();
        ResetUI();
    }

    void OnDisable()
    {
        btnLeft.onClick.RemoveAllListeners();
        btnRight.onClick.RemoveAllListeners();
        btnStart.onClick.RemoveAllListeners();
        running = false;
        target = null;
    }

    void Update()
    {
        AutoBindFromSelection();
        if (!target) return;

        if (txtShipsHeld) txtShipsHeld.text = target.totalShips.ToString();

        int cap = Mathf.Clamp(maxPerFabricator - target.totalShips, 0, maxPerFabricator);
        if (toMake > cap) { toMake = cap; UpdateToMakeText(); }
        UpdateStartButtonState();

        if (!running) return;

        countdown -= Time.unscaledDeltaTime;
        if (countdown < 0f) countdown = 0f;
        if (txtCountdown) txtCountdown.text = Format(countdown);
        if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / cooldownSeconds);

        if (countdown <= 0f)
        {
            if (batchLeft > 0 && target.totalShips < maxPerFabricator)
            {
                target.SpawnMiningShip();
                batchLeft--;
            }
            if (batchLeft <= 0 || target.totalShips >= maxPerFabricator) StopRun();
            else countdown = cooldownSeconds;
        }
    }

    void AutoBindFromSelection()
    {
        var ui = UnitUIManager.instance;
        if (!ui) return;
        var cu = ui.currUnit ? ui.currUnit.GetComponentInParent<NullSpaceFabricator>() : null;
        if (cu && cu != target) Bind(cu);
    }

    public void Bind(NullSpaceFabricator fab)
    {
        target = fab;
        running = false;
        toMake = 0;
        batchLeft = 0;
        if (txtShipsHeld) txtShipsHeld.text = target ? target.totalShips.ToString() : "0";
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        UpdateToMakeText();
        UpdateStartButtonState();
        SetStartText("Start Fabricate");
    }

    void Adjust(int delta)
    {
        if (!target) return;
        int cap = Mathf.Clamp(maxPerFabricator - target.totalShips, 0, maxPerFabricator);
        toMake = Mathf.Clamp(toMake + delta, 0, cap);
        UpdateToMakeText();
        UpdateStartButtonState();
    }

    void Toggle()
    {
        if (!target) return;
        if (running) StopRun();
        else StartRun();
    }

    void StartRun()
    {
        if (!target || toMake <= 0 || ResourceManager.instance == null) return;
        int need = toMake * costPerShip;
        int have = ResourceManager.instance.GetResource(costResource);
        if (have < need) return;
        ResourceManager.instance.RemoveResource(costResource, need);
        batchLeft = toMake;
        running = true;
        countdown = cooldownSeconds;
        SetStartText("Stop");
        ToggleInputs(false);
    }

    void StopRun()
    {
        running = false;
        SetStartText("Start Fabricate");
        ToggleInputs(true);
        countdown = cooldownSeconds;
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        toMake = 0;
        UpdateToMakeText();
        UpdateStartButtonState();
    }

    void ResetUI()
    {
        toMake = 0;
        batchLeft = 0;
        running = false;
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (txtShipsHeld) txtShipsHeld.text = "0";
        UpdateToMakeText();
        UpdateStartButtonState();
    }

    void UpdateToMakeText()
    {
        if (txtToMake) txtToMake.text = toMake.ToString();
    }

    void UpdateStartButtonState()
    {
        bool can = target && toMake > 0 && ResourceManager.instance != null;
        if (can)
        {
            int need = toMake * costPerShip;
            int have = ResourceManager.instance.GetResource(costResource);
            can = have >= need && target.totalShips < maxPerFabricator;
        }
        if (btnStart) btnStart.interactable = can;
    }

    void ToggleInputs(bool on)
    {
        if (btnLeft) btnLeft.interactable = on;
        if (btnRight) btnRight.interactable = on;
    }

    void SetStartText(string s)
    {
        var label = btnStart ? btnStart.GetComponentInChildren<TextMeshProUGUI>() : null;
        if (label) label.text = s;
    }

    static string Format(float s)
    {
        int t = Mathf.CeilToInt(s);
        int mm = t / 60;
        int ss = t % 60;
        return (mm > 0) ? $"{mm:00}:{ss:00}" : $"00:{ss:00}";
    }
}
