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
    [SerializeField] int costPerShip = 100;
    [SerializeField] float cooldownSeconds = 10f;
    [SerializeField] ResourceSO.ResourceType costResource = ResourceSO.ResourceType.Energy;
    [SerializeField] int defaultCapacity = 15;

    NullSpaceFabricator target;
    int toMake;
    int batchLeft;
    bool running;
    bool paidForBatch;
    bool arrowsLocked;
    float countdown;

    int MaxCap => target ? target.capacity : defaultCapacity;
    int RemainingCap => Mathf.Clamp(MaxCap - (target ? target.totalShips : 0), 0, MaxCap);

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
        paidForBatch = false;
        arrowsLocked = false;
    }

    void Update()
    {
        AutoBindFromSelection();
        if (!target)
        {
            if (btnLeft) btnLeft.interactable = false;
            if (btnRight) btnRight.interactable = false;
            UpdateStartButtonState();
            return;
        }

        if (txtShipsHeld) txtShipsHeld.text = target.totalShips.ToString();

        if (!paidForBatch && toMake > RemainingCap) { toMake = RemainingCap; UpdateToMakeText(); }
        if (paidForBatch && batchLeft > RemainingCap) batchLeft = RemainingCap;

        if (!arrowsLocked) UpdateArrowInteractivity();
        UpdateStartButtonState();

        if (!running) return;

        countdown -= Time.unscaledDeltaTime;
        if (countdown < 0f) countdown = 0f;
        if (txtCountdown) txtCountdown.text = Format(countdown);
        if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / cooldownSeconds);

        if (countdown <= 0f)
        {
            if (batchLeft > 0 && target.totalShips < MaxCap)
            {
                target.SpawnMiningShip();
                batchLeft--;
                UpdateToMakeText();
            }

            if (batchLeft <= 0) { FinalizeBatchComplete(); return; }
            if (target.totalShips >= MaxCap) { PauseKeepBatch(); return; }
            countdown = cooldownSeconds;
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
        paidForBatch = false;
        arrowsLocked = false;
        if (txtShipsHeld) txtShipsHeld.text = target ? target.totalShips.ToString() : "0";
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        UpdateToMakeText();
        UpdateStartButtonState();
        SetStartText("Start Fabricate");
        ToggleInputs(true);
        UpdateArrowInteractivity();
    }

    public void SetCostResource(ResourceSO.ResourceType type)
    {
        costResource = type;
        if (!arrowsLocked) UpdateArrowInteractivity();
        UpdateStartButtonState();
    }

    void Adjust(int delta)
    {
        if (!target || paidForBatch) return;
        toMake = Mathf.Clamp(toMake + delta, 0, RemainingCap);
        UpdateToMakeText();
        if (!arrowsLocked) UpdateArrowInteractivity();
        UpdateStartButtonState();
    }

    void Toggle()
    {
        if (!target) return;
        if (running) PauseKeepBatch();
        else
        {
            if (paidForBatch && batchLeft > 0) ResumeBatch();
            else StartNewBatch();
        }
    }

    void StartNewBatch()
    {
        if (!target || toMake <= 0 || ResourceManager.instance == null) return;
        if (RemainingCap <= 0) return;
        int need = toMake * costPerShip;
        int have = ResourceManager.instance.GetResource(costResource);
        if (have < need) return;
        ResourceManager.instance.RemoveResource(costResource, need);
        batchLeft = Mathf.Min(toMake, RemainingCap);
        paidForBatch = true;
        running = true;
        countdown = cooldownSeconds;
        SetStartText("Stop");
        ToggleInputs(false);
        UpdateToMakeText();
        UpdateStartButtonState();
    }

    void ResumeBatch()
    {
        if (batchLeft <= 0 || RemainingCap <= 0) { UpdateStartButtonState(); return; }
        running = true;
        SetStartText("Stop");
        ToggleInputs(false);
        UpdateStartButtonState();
    }

    void PauseKeepBatch()
    {
        running = false;
        SetStartText(batchLeft > 0 ? "Resume Fabricate" : "Start Fabricate");
        ToggleInputs(batchLeft == 0);
        if (txtCountdown) txtCountdown.text = Format(countdown);
        if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / cooldownSeconds);
        UpdateStartButtonState();
    }

    void FinalizeBatchComplete()
    {
        running = false;
        paidForBatch = false;
        batchLeft = 0;
        toMake = 0;
        SetStartText("Start Fabricate");
        ToggleInputs(true);
        countdown = cooldownSeconds;
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        UpdateToMakeText();
        UpdateArrowInteractivity();
        UpdateStartButtonState();
    }

    void ResetUI()
    {
        toMake = 0;
        batchLeft = 0;
        paidForBatch = false;
        running = false;
        arrowsLocked = false;
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (txtShipsHeld) txtShipsHeld.text = "0";
        UpdateToMakeText();
        UpdateArrowInteractivity();
        UpdateStartButtonState();
        SetStartText("Start Fabricate");
        ToggleInputs(true);
    }

    void UpdateToMakeText()
    {
        int shown = paidForBatch ? batchLeft : toMake;
        if (txtToMake) txtToMake.text = shown.ToString();
    }

    void UpdateStartButtonState()
    {
        bool can = false;
        if (target)
        {
            if (running) can = true;
            else
            {
                if (paidForBatch) can = batchLeft > 0 && RemainingCap > 0;
                else if (toMake > 0 && RemainingCap > 0 && ResourceManager.instance != null)
                {
                    int need = toMake * costPerShip;
                    int have = ResourceManager.instance.GetResource(costResource);
                    can = have >= need;
                }
            }
        }
        if (btnStart) btnStart.interactable = can;
    }

    void UpdateArrowInteractivity()
    {
        if (!btnLeft || !btnRight) return;
        if (!target || paidForBatch || arrowsLocked)
        {
            btnLeft.interactable = false;
            btnRight.interactable = false;
            return;
        }
        btnLeft.interactable = toMake > 0;
        btnRight.interactable = toMake < 15;
    }

    void ToggleInputs(bool on)
    {
        arrowsLocked = !on;
        if (!btnLeft || !btnRight) return;
        if (arrowsLocked)
        {
            btnLeft.interactable = false;
            btnRight.interactable = false;
        }
        else
        {
            UpdateArrowInteractivity();
        }
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
