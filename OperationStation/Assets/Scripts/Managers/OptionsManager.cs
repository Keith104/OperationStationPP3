using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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

        // runtime wiring state (so we can safely rebind later)
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
            SceneManager.sceneLoaded += OnSceneLoaded; // update when scenes change
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Apply saved values so the mixer is correct regardless of UI state
        foreach (var c in controls)
        {
            float saved = PlayerPrefs.GetFloat(KeyFor(c), c.defaultLinear);
            ApplyLinear(c.channel, saved);
        }

        // Initial bind (works even if the menu/panel is inactive)
        RebindMissing(now: true);

        // Sync hidden sliders immediately; visible ones won't snap
        RefreshBoundSlidersAvoidingVisibleSnap();
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

        SceneManager.sceneLoaded -= OnSceneLoaded; // Clean up scene event
    }

    // ---------- Scene Events ----------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Rebind on scene load (finds inactive menus too)
        RebindMissing(now: true);

        // Sync only hidden sliders to avoid visible snap when menus are shown
        RefreshBoundSlidersAvoidingVisibleSnap();
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

            // Only push into hidden sliders to avoid a visible snap
            if (c.boundSlider && !c.boundSlider.gameObject.activeInHierarchy)
                c.boundSlider.SetValueWithoutNotify(c.defaultLinear);

            // Keep label in sync with whatever the slider is currently showing
            UpdateLabelFromSlider(c);
        }
        PlayerPrefs.Save();
    }

    // ---------- Internals ----------
    // Rebinds controls; finds sliders even if their parent menu/panel is inactive
    void RebindMissing(bool now)
    {
        foreach (var c in controls)
        {
            bool needRebind = c.boundSlider == null || c.boundSlider.gameObject == null;
            if (!needRebind && c.slider != null && c.boundSlider != c.slider) needRebind = true;
            if (!needRebind && c.percentLabel == null) needRebind = true;

            if (!needRebind) continue;

            var marker = FindMarkerEvenIfInactive(c.channel);
            if (!marker) continue;

            var newSlider = marker.GetSlider();
            var newLabel = marker.GetPercentLabel();

            if (newSlider != null)
            {
                AttachSlider(c, newSlider);
                c.percentLabel = newLabel; // may be null; that's ok

                float current = GetLinear(c.channel);

                // Sync value only if the slider is hidden to prevent a visible jump
                if (!newSlider.gameObject.activeInHierarchy)
                    newSlider.SetValueWithoutNotify(current);

                // Keep label in step with whatever the slider *currently* shows
                UpdateLabelFromSlider(c);
            }
        }
    }

    // Finds an OptionsSliderMarker regardless of active state
    OptionsSliderMarker FindMarkerEvenIfInactive(Channel ch)
    {
        var allMarkers = Resources.FindObjectsOfTypeAll<OptionsSliderMarker>();
        foreach (var m in allMarkers)
        {
            if (m && m.channel == ch) // assumes marker exposes 'channel' field
                return m;
        }
        return null;
    }

    // Sync hidden sliders to mixer values; don't move visible sliders
    void RefreshBoundSlidersAvoidingVisibleSnap()
    {
        foreach (var c in controls)
        {
            if (!c.boundSlider) continue;

            if (!c.boundSlider.gameObject.activeInHierarchy)
            {
                float current = GetLinear(c.channel);
                c.boundSlider.SetValueWithoutNotify(current);
            }

            // Always keep the label in sync with what the slider is visually showing
            UpdateLabelFromSlider(c);
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

    // Push a value into the mixer
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

    // Update label based on the slider's *current* visual value
    void UpdateLabelFromSlider(VolumeUI c)
    {
        if (!c.boundSlider) return;
        UpdateLabel(c, c.boundSlider.value);
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
