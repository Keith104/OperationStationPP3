using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager instance;

    [Header("Audio Options")]
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Shared Preview (optional, used if a VolumeUI has no previewSource)")]
    [SerializeField] private AudioSource sharedPreviewSource; // put this on the manager object
    [SerializeField] private AudioClip sharedPreviewClip;
    [Range(0.05f, 0.5f)][SerializeField] private float sharedPreviewCooldown = 0.15f;

    public enum Channel { Master, Music, SFX, AttackSFX, UISFX, PlayerSFX }

    [Serializable]
    public class VolumeUI
    {
        public Channel channel;

        [Header("UI")]
        public Slider slider;                 // min=0.0001, max=1, wholeNumbers=false
        public TMP_Text percentLabel;
        public AudioSource previewSource;     // will be auto-routed to the right group
        public AudioClip previewClip;
        [Range(0.05f, 0.5f)] public float previewCooldown = 0.15f;

        [Header("Defaults & Keys")]
        [Range(0.0001f, 1f)] public float defaultLinear = 0.8f;
        public string overridePlayerPrefKey;

        // runtime wiring state (so we can safely rebind in Update)
        [NonSerialized] public Slider boundSlider;
        [NonSerialized] public UnityAction<float> handler;
    }

    [SerializeField] private List<VolumeUI> controls = new();

    readonly Dictionary<Channel, string> paramNames = new()
    {
        { Channel.Master,    "Master" },
        { Channel.Music,     "Music" },
        { Channel.SFX,       "SFX" },
        { Channel.AttackSFX, "AttackSFX" },
        { Channel.UISFX,     "UISFX" },
        { Channel.PlayerSFX, "PlayerSFX" },
    };

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Apply saved values once so any UI that appears later reads correct state
        foreach (var c in controls)
        {
            float saved = PlayerPrefs.GetFloat(KeyFor(c), c.defaultLinear);
            ApplyLinear(c.channel, saved);
        }

        // Initial bind if sliders are already in the scene
        RebindMissing(now: true);
    }

    void Update()
    {
        // Rebind periodically if references were lost (e.g., switching to another options panel)
        if (Time.unscaledTime >= _nextRebindTime)
        {
            RebindMissing(now: false);
            _nextRebindTime = Time.unscaledTime + 0.25f; // throttle to 4x/sec
        }
    }

    void OnDestroy()
    {
        // Remove listeners from whatever we’ve bound
        foreach (var c in controls)
        {
            if (c.boundSlider != null && c.handler != null)
                c.boundSlider.onValueChanged.RemoveListener(c.handler);
        }
    }

    // ---------- Public ----------
    public void SetLinear(Channel ch, float linear)
    {
        linear = Mathf.Clamp(linear, 0.0001f, 1f);
        ApplyLinear(ch, linear);
        Save(ch, linear);
    }

    public float GetLinear(Channel ch)
    {
        string param = paramNames[ch];
        if (mixer.GetFloat(param, out float dB))
            return DbToLinear(dB);
        return 1f;
    }

    public void ResetToDefaults()
    {
        foreach (var c in controls)
        {
            SetLinear(c.channel, c.defaultLinear);

            // Update any currently bound slider + label
            if (c.boundSlider)
                c.boundSlider.SetValueWithoutNotify(c.defaultLinear);
            UpdateLabel(c, c.defaultLinear);
        }
        PlayerPrefs.Save();
    }

    // ---------- Internals ----------
    void RebindMissing(bool now)
    {
        foreach (var c in controls)
        {
            // If our serialized "slider" is missing or the bound one got destroyed, try to find a marker
            bool needRebind = c.boundSlider == null || c.boundSlider.gameObject == null;
            if (!needRebind && c.slider != null && c.boundSlider != c.slider) needRebind = true;
            if (!needRebind && c.percentLabel == null) needRebind = true;

            if (!needRebind) continue;

            var marker = OptionsSliderMarker.FindActive(c.channel);
            if (!marker) continue;

            // Resolve UI from marker
            var newSlider = marker.GetSlider();
            var newLabel = marker.GetPercentLabel();

            if (newSlider != null)
            {
                AttachSlider(c, newSlider);
                c.percentLabel = newLabel; // may be null if not present; that’s ok
                // Set display to current mixer value
                float current = GetLinear(c.channel);
                newSlider.SetValueWithoutNotify(current);
                UpdateLabel(c, current);
            }
        }
    }

    void AttachSlider(VolumeUI c, Slider newSlider)
    {
        // Detach old
        if (c.boundSlider != null && c.handler != null)
            c.boundSlider.onValueChanged.RemoveListener(c.handler);

        // Configure & attach new
        newSlider.minValue = 0.0001f;
        newSlider.maxValue = 1f;
        newSlider.wholeNumbers = false;

        c.boundSlider = newSlider;

        // Keep one stored handler so we can remove it later
        c.handler = v => OnSliderChanged(c, v);
        newSlider.onValueChanged.AddListener(c.handler);
    }

    void OnSliderChanged(VolumeUI c, float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        ApplyLinear(c.channel, v);
        UpdateLabel(c, v);
        Save(c.channel, v);

        // Ensure preview is routed + played (prefer per-control, fallback to shared)
        AudioSource src = c.previewSource ? c.previewSource : sharedPreviewSource;
        AudioClip clip = c.previewSource ? c.previewClip : sharedPreviewClip;
        float cd = c.previewSource ? c.previewCooldown : sharedPreviewCooldown;

        if (src)
        {
            var g = ResolveGroup(c.channel);
            if (g && src.outputAudioMixerGroup != g)
                src.outputAudioMixerGroup = g;
        }
        TryPreview(src, clip, cd);
    }

    void ApplyLinear(Channel ch, float linear)
    {
        string param = paramNames[ch];
        mixer.SetFloat(param, LinearToDb(linear));
    }

    void Save(Channel ch, float linear)
    {
        PlayerPrefs.SetFloat(KeyFor(ch), linear);
        PlayerPrefs.Save();
    }

    string KeyFor(VolumeUI c) => string.IsNullOrEmpty(c.overridePlayerPrefKey) ? KeyFor(c.channel) : c.overridePlayerPrefKey;
    string KeyFor(Channel ch) => $"vol_{ch}";

    static float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return -80f;          // mute floor; avoids -Infinity
        return Mathf.Log10(linear) * 20f;            // dB = 20 * log10(amplitude)
    }

    static float DbToLinear(float dB) => Mathf.Pow(10f, dB / 20f);

    void UpdateLabel(VolumeUI c, float linear)
    {
        if (c.percentLabel) c.percentLabel.text = Mathf.RoundToInt(linear * 100f) + "%";
    }

    void TryPreview(AudioSource src, AudioClip clip, float cooldown)
    {
        if (!src || !clip) return;
        if (Time.unscaledTime - _lastPreview < Mathf.Max(0.05f, cooldown)) return;

        src.PlayOneShot(clip);
        _lastPreview = Time.unscaledTime;
    }

    // Finds the first mixer group whose path ends with the channel name
    AudioMixerGroup ResolveGroup(Channel ch)
    {
        var groups = mixer.FindMatchingGroups(ch.ToString());
        return (groups != null && groups.Length > 0) ? groups[0] : null;
    }

    float _lastPreview;
    float _nextRebindTime;
}
