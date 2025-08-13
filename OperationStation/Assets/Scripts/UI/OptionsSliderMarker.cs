using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsSliderMarker : MonoBehaviour
{
    [Header("Represents this audio channel")]
    public OptionsManager.Channel channel;

    [Header("Optional explicit refs (will auto-find if left empty)")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text percentLabel;

    // -------- Static registry so the manager can find us --------
    static readonly List<OptionsSliderMarker> _instances = new();

    void OnEnable()
    {
        if (!_instances.Contains(this)) _instances.Add(this);
        // Try auto-resolve if fields are not set
        ResolveLocalRefs();
    }

    void OnDisable()
    {
        _instances.Remove(this);
    }

    public static OptionsSliderMarker FindActive(OptionsManager.Channel ch)
    {
        // Prefer active in hierarchy
        for (int i = 0; i < _instances.Count; i++)
        {
            var m = _instances[i];
            if (m && m.channel == ch && m.gameObject.activeInHierarchy)
                return m;
        }
        // Fallback to any instance (in case none are active yet)
        for (int i = 0; i < _instances.Count; i++)
        {
            var m = _instances[i];
            if (m && m.channel == ch)
                return m;
        }
        return null;
    }

    public Slider GetSlider()
    {
        if (!slider) ResolveLocalRefs();
        return slider;
    }

    public TMP_Text GetPercentLabel()
    {
        if (!percentLabel) ResolveLocalRefs();
        return percentLabel;
    }

    void ResolveLocalRefs()
    {
        if (!slider)
            slider = GetComponentInChildren<Slider>(true);

        if (!percentLabel)
        {
            // Prefer a child explicitly named "PercentLabel"
            var t = transform.Find("PercentLabel");
            if (t) percentLabel = t.GetComponent<TMP_Text>();

            // Fallback to first TMP_Text in children
            if (!percentLabel)
                percentLabel = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
