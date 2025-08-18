using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmelterUIController : MonoBehaviour
{
    [Header("Raw Inputs")]
    [SerializeField] Slider sldTritium;
    [SerializeField] Slider sldSilver;
    [SerializeField] Slider sldPolonium;
    [SerializeField] TextMeshProUGUI txtTritiumValue;   // shows SLIDER (raw to smelt per tick)
    [SerializeField] TextMeshProUGUI txtSilverValue;
    [SerializeField] TextMeshProUGUI txtPoloniumValue;

    [Header("Projected Outputs (per tick)")]
    [SerializeField] TextMeshProUGUI txtProjTritiumIngot;
    [SerializeField] TextMeshProUGUI txtProjSilverCoin;
    [SerializeField] TextMeshProUGUI txtProjPoloniumCrystal;

    [Header("Cooldown UI")]
    [SerializeField] TextMeshProUGUI txtEvery;
    [SerializeField] TextMeshProUGUI txtCountdown;
    [SerializeField] Image cooldownFill;

    [Header("Controls")]
    [SerializeField] Button btnStart;

    [Header("Config")]
    [SerializeField] float cooldownSeconds = 10f;

    // ratios: raw -> 1 output
    const int RATIO_TRITIUM = 2;   // 2 Tritium  -> 1 Ingot
    const int RATIO_SILVER = 2;   // 2 Silver   -> 1 Coin
    const int RATIO_POLONIUM = 5;   // 5 Polonium -> 1 Crystal

    bool running;
    float countdown;
    Coroutine loop;

    static FieldInfo fTritium, fSilver, fPolonium, fIngot, fCoin, fCrystal;

    void Awake() { CacheFields(); }

    void OnEnable()
    {
        sldTritium.onValueChanged.AddListener(_ => OnSliderChanged());
        sldSilver.onValueChanged.AddListener(_ => OnSliderChanged());
        sldPolonium.onValueChanged.AddListener(_ => OnSliderChanged());
        btnStart.onClick.AddListener(Toggle);

        InitUI();
        RefreshAll();
    }

    void OnDisable()
    {
        sldTritium.onValueChanged.RemoveAllListeners();
        sldSilver.onValueChanged.RemoveAllListeners();
        sldPolonium.onValueChanged.RemoveAllListeners();
        btnStart.onClick.RemoveAllListeners();

        if (loop != null) StopCoroutine(loop);
        loop = null;
        running = false;
    }

    void Update()
    {
        if (!running) RefreshStorageAndInteractivity();

        if (running)
        {
            countdown -= Time.unscaledDeltaTime;
            if (countdown < 0f) countdown = 0f;
            if (txtCountdown) txtCountdown.text = Format(countdown);
            if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / cooldownSeconds);
        }
    }

    void InitUI()
    {
        if (txtEvery) txtEvery.text = $"Every {Mathf.RoundToInt(cooldownSeconds)}s";
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        countdown = cooldownSeconds;
        SetStartText("Start Smelt");
    }

    void RefreshAll()
    {
        RefreshStorageAndInteractivity();
        RefreshProjected();
        RefreshSliderValueLabels();
    }

    void RefreshStorageAndInteractivity()
    {
        int tStore = Get(ResourceSO.ResourceType.Tritium);
        int sStore = Get(ResourceSO.ResourceType.Silver);
        int pStore = Get(ResourceSO.ResourceType.Polonium);

        // Clamp sliders to available storage (slider = desired RAW per tick)
        ClampSliderToStorage(sldTritium, tStore);
        ClampSliderToStorage(sldSilver, sStore);
        ClampSliderToStorage(sldPolonium, pStore);

        RefreshProjected();
        RefreshSliderValueLabels();

        int wantT = Mathf.RoundToInt(sldTritium.value);
        int wantS = Mathf.RoundToInt(sldSilver.value);
        int wantP = Mathf.RoundToInt(sldPolonium.value);

        // Can start if at least one resource will produce ≥ 1 output with current want & storage
        bool canMakeIngot = Mathf.FloorToInt(Mathf.Min(wantT, tStore) / (float)RATIO_TRITIUM) >= 1;
        bool canMakeCoin = Mathf.FloorToInt(Mathf.Min(wantS, sStore) / (float)RATIO_SILVER) >= 1;
        bool canMakeCrystal = Mathf.FloorToInt(Mathf.Min(wantP, pStore) / (float)RATIO_POLONIUM) >= 1;

        btnStart.interactable = running || canMakeIngot || canMakeCoin || canMakeCrystal;
    }

    void OnSliderChanged()
    {
        RefreshProjected();
        RefreshSliderValueLabels();
        if (!running) RefreshStorageAndInteractivity();
    }

    void RefreshSliderValueLabels()
    {
        if (txtTritiumValue) txtTritiumValue.text = Mathf.RoundToInt(sldTritium.value).ToString("00");
        if (txtSilverValue) txtSilverValue.text = Mathf.RoundToInt(sldSilver.value).ToString("00");
        if (txtPoloniumValue) txtPoloniumValue.text = Mathf.RoundToInt(sldPolonium.value).ToString("00");
    }

    void RefreshProjected()
    {
        int tStore = Get(ResourceSO.ResourceType.Tritium);
        int sStore = Get(ResourceSO.ResourceType.Silver);
        int pStore = Get(ResourceSO.ResourceType.Polonium);

        int wantT = Mathf.RoundToInt(sldTritium.value);
        int wantS = Mathf.RoundToInt(sldSilver.value);
        int wantP = Mathf.RoundToInt(sldPolonium.value);

        // Outputs per tick based on ratios
        int outIngots = Mathf.FloorToInt(Mathf.Min(wantT, tStore) / (float)RATIO_TRITIUM);
        int outCoins = Mathf.FloorToInt(Mathf.Min(wantS, sStore) / (float)RATIO_SILVER);
        int outCrystals = Mathf.FloorToInt(Mathf.Min(wantP, pStore) / (float)RATIO_POLONIUM);

        if (txtProjTritiumIngot) txtProjTritiumIngot.text = outIngots.ToString("00");
        if (txtProjSilverCoin) txtProjSilverCoin.text = outCoins.ToString("00");
        if (txtProjPoloniumCrystal) txtProjPoloniumCrystal.text = outCrystals.ToString("00");
    }

    void ClampSliderToStorage(Slider sld, int storage)
    {
        if (!sld) return;
        int clampMax = Mathf.Max(0, storage);
        if (sld.maxValue != clampMax) sld.maxValue = clampMax;
        if (sld.value > clampMax) sld.value = clampMax;
    }

    public void Toggle()
    {
        if (running) StopSmelt();
        else StartSmelt();
    }

    void StartSmelt()
    {
        running = true;
        SetSlidersInteractable(false);
        SetStartText("Stop");
        btnStart.interactable = true;

        countdown = cooldownSeconds;
        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(SmeltLoop());
    }

    void StopSmelt()
    {
        running = false;
        if (loop != null) StopCoroutine(loop);
        loop = null;

        SetSlidersInteractable(true);
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (txtCountdown) txtCountdown.text = $"00:{Mathf.RoundToInt(cooldownSeconds):00}";
        SetStartText("Start Smelt");

        RefreshAll();
    }

    IEnumerator SmeltLoop()
    {
        while (running)
        {
            while (countdown > 0f && running)
            {
                countdown -= Time.unscaledDeltaTime;
                if (txtCountdown) txtCountdown.text = Format(countdown);
                if (cooldownFill) cooldownFill.fillAmount = 1f - (countdown / cooldownSeconds);
                yield return null;
            }
            if (!running) yield break;

            TickSmelt();

            // If NOTHING can produce at least 1 output given current sliders, stop.
            int tStore = Get(ResourceSO.ResourceType.Tritium);
            int sStore = Get(ResourceSO.ResourceType.Silver);
            int pStore = Get(ResourceSO.ResourceType.Polonium);

            int wantT = Mathf.RoundToInt(sldTritium.value);
            int wantS = Mathf.RoundToInt(sldSilver.value);
            int wantP = Mathf.RoundToInt(sldPolonium.value);

            bool canMakeIngot = Mathf.FloorToInt(Mathf.Min(wantT, tStore) / (float)RATIO_TRITIUM) >= 1;
            bool canMakeCoin = Mathf.FloorToInt(Mathf.Min(wantS, sStore) / (float)RATIO_SILVER) >= 1;
            bool canMakeCrystal = Mathf.FloorToInt(Mathf.Min(wantP, pStore) / (float)RATIO_POLONIUM) >= 1;

            if (!canMakeIngot && !canMakeCoin && !canMakeCrystal)
            {
                StopSmelt();
                yield break;
            }

            countdown = cooldownSeconds;
            if (cooldownFill) cooldownFill.fillAmount = 0f;
        }
    }

    void TickSmelt()
    {
        int tStore = Get(ResourceSO.ResourceType.Tritium);
        int sStore = Get(ResourceSO.ResourceType.Silver);
        int pStore = Get(ResourceSO.ResourceType.Polonium);

        int wantT = Mathf.RoundToInt(sldTritium.value);
        int wantS = Mathf.RoundToInt(sldSilver.value);
        int wantP = Mathf.RoundToInt(sldPolonium.value);

        // How many outputs we can actually make this tick given sliders + storage
        int makeIngots = Mathf.FloorToInt(Mathf.Min(wantT, tStore) / (float)RATIO_TRITIUM);
        int makeCoins = Mathf.FloorToInt(Mathf.Min(wantS, sStore) / (float)RATIO_SILVER);
        int makeCrystals = Mathf.FloorToInt(Mathf.Min(wantP, pStore) / (float)RATIO_POLONIUM);

        // Raw to burn is produced * ratio
        int burnT = makeIngots * RATIO_TRITIUM;
        int burnS = makeCoins * RATIO_SILVER;
        int burnP = makeCrystals * RATIO_POLONIUM;

        if (burnT > 0)
        {
            Remove(ResourceSO.ResourceType.Tritium, burnT);
            Add(ResourceSO.ResourceType.TritiumIngot, makeIngots);
        }
        if (burnS > 0)
        {
            Remove(ResourceSO.ResourceType.Silver, burnS);
            Add(ResourceSO.ResourceType.SilverCoin, makeCoins);
        }
        if (burnP > 0)
        {
            Remove(ResourceSO.ResourceType.Polonium, burnP);
            Add(ResourceSO.ResourceType.PoloniumCrystal, makeCrystals);
        }

        RefreshAll();
    }

    void SetSlidersInteractable(bool on)
    {
        if (sldTritium) sldTritium.interactable = on;
        if (sldSilver) sldSilver.interactable = on;
        if (sldPolonium) sldPolonium.interactable = on;
    }

    void SetStartText(string s)
    {
        var label = btnStart.GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.text = s;
    }

    // -------- Resource helpers (uses public getter if present; else reflection) --------
    int Get(ResourceSO.ResourceType t)
    {
        var rm = ResourceManager.instance;
        if (rm == null) return 0;

        var getter = rm.GetType().GetMethod("GetResource", BindingFlags.Public | BindingFlags.Instance);
        if (getter != null) return (int)getter.Invoke(rm, new object[] { t });

        return t switch
        {
            ResourceSO.ResourceType.Tritium => (int)(fTritium?.GetValue(rm) ?? 0),
            ResourceSO.ResourceType.Silver => (int)(fSilver?.GetValue(rm) ?? 0),
            ResourceSO.ResourceType.Polonium => (int)(fPolonium?.GetValue(rm) ?? 0),
            ResourceSO.ResourceType.TritiumIngot => (int)(fIngot?.GetValue(rm) ?? 0),
            ResourceSO.ResourceType.SilverCoin => (int)(fCoin?.GetValue(rm) ?? 0),
            ResourceSO.ResourceType.PoloniumCrystal => (int)(fCrystal?.GetValue(rm) ?? 0),
            _ => 0
        };
    }

    void Add(ResourceSO.ResourceType t, int amt)
    {
        if (amt <= 0) return;
        ResourceManager.instance.AddResource(t, amt);
    }

    void Remove(ResourceSO.ResourceType t, int amt)
    {
        if (amt <= 0) return;
        ResourceManager.instance.RemoveResource(t, amt);
    }

    void CacheFields()
    {
        var rmType = typeof(ResourceManager);
        fTritium = rmType.GetField("tritium", BindingFlags.NonPublic | BindingFlags.Instance);
        fSilver = rmType.GetField("silver", BindingFlags.NonPublic | BindingFlags.Instance);
        fPolonium = rmType.GetField("polonium", BindingFlags.NonPublic | BindingFlags.Instance);
        fIngot = rmType.GetField("tritiumIngot", BindingFlags.NonPublic | BindingFlags.Instance);
        fCoin = rmType.GetField("silverCoins", BindingFlags.NonPublic | BindingFlags.Instance);
        fCrystal = rmType.GetField("poloniumCrystal", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    static string Format(float s)
    {
        int t = Mathf.CeilToInt(s);
        int mm = t / 60;
        int ss = t % 60;
        return (mm > 0) ? $"{mm:00}:{ss:00}" : $"00:{ss:00}";
    }
}
