using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathCatUIController : MonoBehaviour
{
    [Serializable]
    public class ResourceRow
    {
        public ResourceSO.ResourceType resourceType;
        public Slider slider;
        public TextMeshProUGUI sliderValueText;
        public TextMeshProUGUI totalText;
        [HideInInspector] public int lastPlayerHas = int.MinValue;
        [HideInInspector] public int lastRemaining = int.MinValue;
    }
    [SerializeField] DeathCat cat;
    [SerializeField] ResourceRow[] rows;
    [SerializeField] Button spendButton;

    [Header("Center Object Scale")]
    [SerializeField] Transform centerObject;
    [SerializeField] Vector3 minScale = new Vector3(0.2f, 0.2f, 0.2f);
    [SerializeField] Vector3 maxScale = new Vector3(1f, 1f, 1f);
    [SerializeField] float scaleLerpSpeed = 6f;

    int initialTotalCost;

    void OnEnable()
    {
        foreach (var r in rows)
        {
            if (!r.slider) continue;
            r.slider.wholeNumbers = true;
            r.slider.onValueChanged.AddListener(_ =>
            {
                if (r.sliderValueText) r.sliderValueText.text = Mathf.RoundToInt(r.slider.value).ToString();
                RefreshSpendButton();
            });
            if (r.sliderValueText) r.sliderValueText.text = "0";
        }
        if (spendButton) spendButton.onClick.AddListener(Spend);
        EnsureRuntimeCosts();
        CacheInitialTotalCost();
        FullRefresh();
        ApplyScaleImmediate();
    }

    void OnDisable()
    {
        foreach (var r in rows) if (r.slider) r.slider.onValueChanged.RemoveAllListeners();
        if (spendButton) spendButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        bool anyChange = false;
        for (int i = 0; i < rows.Length; i++)
        {
            var r = rows[i];
            int playerHas = SafeGetPlayerHas(r.resourceType);
            int remaining = SafeGetRemaining(r.resourceType);
            if (playerHas != r.lastPlayerHas || remaining != r.lastRemaining)
            {
                r.lastPlayerHas = playerHas;
                r.lastRemaining = remaining;
                RefreshRow(i);
                anyChange = true;
            }
        }
        if (anyChange) RefreshSpendButton();
        UpdateCenterScale();
    }

    public void Bind(Module m)
    {
        cat.module = m;
        EnsureRuntimeCosts();
        CacheInitialTotalCost();
        FullRefresh();
        ApplyScaleImmediate();
    }

    void EnsureRuntimeCosts()
    {
        if (cat.module == null || cat.module.stats == null || cat.module.stats.cost == null) return;
        int n = cat.module.stats.cost.Length;
        if (cat.module.costsLeft == null || cat.module.costsLeft.Length != n)
        {
            cat.module.costsLeft = new int[n];
            for (int i = 0; i < n; i++) cat.module.costsLeft[i] = Mathf.Max(0, cat.module.stats.cost[i].cost);
        }
    }

    void CacheInitialTotalCost()
    {
        initialTotalCost = 0;
        if (cat.module != null && cat.module.stats != null && cat.module.stats.cost != null)
        {
            for (int i = 0; i < cat.module.stats.cost.Length; i++)
                initialTotalCost += Mathf.Max(0, cat.module.stats.cost[i].cost);
        }
    }

    int CurrentRemainingTotal()
    {
        if (cat.module == null || cat.module.costsLeft == null) return 0;
        int s = 0;
        for (int i = 0; i < cat.module.costsLeft.Length; i++) s += Mathf.Max(0, cat.module.costsLeft[i]);
        return s;
    }

    float BuildProgress()
    {
        if (initialTotalCost <= 0) return 1f;
        int remaining = CurrentRemainingTotal();
        float done = Mathf.Clamp01(1f - (remaining / (float)initialTotalCost));
        return done;
    }

    void UpdateCenterScale()
    {
        if (!centerObject) return;
        float t = BuildProgress();
        Vector3 target = Vector3.Lerp(minScale, maxScale, t);
        centerObject.localScale = Vector3.Lerp(centerObject.localScale, target, Time.unscaledDeltaTime * Mathf.Max(0f, scaleLerpSpeed));
    }

    void ApplyScaleImmediate()
    {
        if (!centerObject) return;
        float t = BuildProgress();
        centerObject.localScale = Vector3.Lerp(minScale, maxScale, t);
    }

    void FullRefresh()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].lastPlayerHas = SafeGetPlayerHas(rows[i].resourceType);
            rows[i].lastRemaining = SafeGetRemaining(rows[i].resourceType);
            RefreshRow(i, true);
        }
        RefreshSpendButton();
    }

    void RefreshRow(int i, bool resetSliderValueToZero = false)
    {
        var r = rows[i];
        if (!r.slider) return;
        int playerHas = SafeGetPlayerHas(r.resourceType);
        int remaining = SafeGetRemaining(r.resourceType);
        int max = Mathf.Max(0, Mathf.Min(playerHas, remaining));
        r.slider.maxValue = max;
        int newValue = Mathf.Clamp(Mathf.RoundToInt(r.slider.value), 0, max);
        if (resetSliderValueToZero) newValue = 0;
        if (!Mathf.Approximately(newValue, r.slider.value)) r.slider.SetValueWithoutNotify(newValue);
        r.slider.interactable = (playerHas > 0 && remaining > 0);
        if (r.sliderValueText) r.sliderValueText.text = newValue.ToString();
        if (r.totalText) r.totalText.text = Mathf.Max(0, remaining).ToString("N0");
    }

    void RefreshSpendButton()
    {
        bool canSpend = false;
        foreach (var r in rows) if (Mathf.RoundToInt(r.slider.value) > 0) { canSpend = true; break; }
        if (spendButton) spendButton.interactable = canSpend;
    }

    void Spend()
    {
        if (ResourceManager.instance == null || cat.module == null || cat.module.costsLeft == null || cat.module.stats == null) return;

        foreach (var r in rows)
        {
            if (!r.slider) continue;
            int want = Mathf.RoundToInt(r.slider.value);
            if (want <= 0) continue;

            int idx = FindCostIndex(r.resourceType);
            if (idx < 0) continue;

            int playerHas = SafeGetPlayerHas(r.resourceType);
            int remaining = cat.module.costsLeft[idx];
            int spend = Mathf.Clamp(want, 0, Mathf.Min(playerHas, remaining));
            if (spend <= 0) continue;

            ResourceManager.instance.RemoveResource(r.resourceType, spend);
            cat.module.costsLeft[idx] = Mathf.Max(0, remaining - spend);

            r.slider.SetValueWithoutNotify(0);
            if (r.sliderValueText) r.sliderValueText.text = "0";
        }

        FullRefresh();
        UpdateCenterScale();

        if(CurrentRemainingTotal() <= 0)
        {
            OnCostCompleted();
        }
    }

    int FindCostIndex(ResourceSO.ResourceType type)
    {
        if (cat.module == null || cat.module.stats == null || cat.module.stats.cost == null) return -1;
        for (int i = 0; i < cat.module.stats.cost.Length; i++)
        {
            if (cat.module.stats.cost[i] != null && cat.module.stats.cost[i].resource != null &&
                cat.module.stats.cost[i].resource.resourceType == type) return i;
        }
        return -1;
    }

    int SafeGetPlayerHas(ResourceSO.ResourceType type)
    {
        return (ResourceManager.instance == null) ? 0 : ResourceManager.instance.GetResource(type);
    }

    int SafeGetRemaining(ResourceSO.ResourceType type)
    {
        int idx = FindCostIndex(type);
        if (idx < 0 || cat.module == null || cat.module.costsLeft == null || idx >= cat.module.costsLeft.Length) return 0;
        return cat.module.costsLeft[idx];
    }

    void OnCostCompleted()
    {
        cat.StartWinSequence();
    }
}
