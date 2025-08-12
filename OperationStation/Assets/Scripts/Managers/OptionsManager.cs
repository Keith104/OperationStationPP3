using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("Audio Options")]
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

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

    void Start()
    {
        foreach (var c in controls)
        {
            // Auto-route preview source to this channel's mixer group (if not already set)
            if (c.previewSource)
            {
                var g = ResolveGroup(c.channel);
                if (g && c.previewSource.outputAudioMixerGroup != g)
                    c.previewSource.outputAudioMixerGroup = g;
            }

            float saved = PlayerPrefs.GetFloat(KeyFor(c), c.defaultLinear);
            ApplyLinear(c.channel, saved);

            if (c.slider)
            {
                c.slider.minValue = 0.0001f;
                c.slider.maxValue = 1f;
                c.slider.wholeNumbers = false;

                c.slider.SetValueWithoutNotify(saved);
                UpdateLabel(c, saved);
                c.slider.onValueChanged.AddListener(v => OnSliderChanged(c, v));
            }
        }
    }

    void OnDestroy()
    {
        foreach (var c in controls)
            if (c.slider) c.slider.onValueChanged.RemoveAllListeners();
    }

    // Public
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
            if (c.slider) c.slider.SetValueWithoutNotify(c.defaultLinear);
            UpdateLabel(c, c.defaultLinear);
        }
        PlayerPrefs.Save();
    }

    // Internal
    void OnSliderChanged(VolumeUI c, float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        ApplyLinear(c.channel, v);
        UpdateLabel(c, v);
        Save(c.channel, v);

        // Ensure preview source is routed to the right group before playing
        if (c.previewSource)
        {
            var g = ResolveGroup(c.channel);
            if (g) c.previewSource.outputAudioMixerGroup = g;
        }

        TryPreview(c);
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

    void TryPreview(VolumeUI c)
    {
        if (!c.previewSource || !c.previewClip) return;
        if (Time.unscaledTime - lastPreview < c.previewCooldown) return;

        c.previewSource.PlayOneShot(c.previewClip);
        lastPreview = Time.unscaledTime;
    }

    // Finds the first mixer group whose path ends with the channel name
    AudioMixerGroup ResolveGroup(Channel ch)
    {
        var groups = mixer.FindMatchingGroups(ch.ToString());
        return (groups != null && groups.Length > 0) ? groups[0] : null;
    }

    float lastPreview;
}
