using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;


public class OptionsManager : MonoBehaviour
{
    // Audio Options
    [Header("Mixer")]
    [SerializeField] AudioMixer mixer;

    // Channels to control
    public enum Channel { Master, Music, SFX, AttackSFX, UISFX, PlayerSFX}

    [Serializable]
    public class VolumeUI
    {
        public Channel channel;

        [Header("UI")]
        public Slider slider;
        public TMP_Text percentLabel;
        public AudioSource previewSource;
        public AudioClip previewClip;
        [Range(0.05f, 0.5f)] public float previewCooldown = 0.15f;

        [Header("Defaults & Keys")]
        [Range(0.0001f, 1f)] public float defaultLinear = 0.8f;
        public string overridePlayerPrefKey;
    }

    [SerializeField] List<VolumeUI> controls = new();

    // Map enum -> exposed parem name
    readonly Dictionary<Channel, string> paramNames = new()
    {
        {Channel.Master, "Master" },
        {Channel.Music, "Music" },
        {Channel.SFX, "SFX" },
        {Channel.AttackSFX, "AttackSFX" },
        {Channel.UISFX, "UISFX" },
        {Channel.PlayerSFX, "PlayerSFX" },
    };

    private void Start()
    {
        foreach(var c in controls)
        {
            // Load saved value (or default), apply to mixer, and initialize UI
            float saved = PlayerPrefs.GetFloat(KeyFor(c), c.defaultLinear);
            ApplyLinear(c.channel, saved);

            if(c.slider)
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

    private void OnDestroy()
    {
        foreach(var c in controls)
        {
            if (c.slider) c.slider.onValueChanged.RemoveAllListeners();
        }
    }


    // Public Methods
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
        foreach(var c in controls)
        {
            SetLinear(c.channel, c.defaultLinear);
            if (c.slider) c.slider.SetValueWithoutNotify(c.defaultLinear);
            UpdateLabel(c, c.defaultLinear);
        }

        PlayerPrefs.Save();
    }
    
    // Internal Methods
    void OnSliderChanged(VolumeUI c, float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        ApplyLinear(c.channel, v);
        UpdateLabel(c, v);
        Save(c.channel, v);
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
        if (linear <= 0.0001f) return -80f;
        return Mathf.Log10(linear) * 20f;
    }

    static float DbToLinear(float dB)
    {
        return Mathf.Pow(10f, dB / 20f);
    }

    void UpdateLabel(VolumeUI c, float linear)
    {
        if (c.percentLabel) c.percentLabel.text = Mathf.RoundToInt(linear * 100f) + "%";
    }

    void TryPreview(VolumeUI c)
    {
        if (!c.previewSource || !c.previewClip) return;

        // throttle to avoid spam while dragging
        if (Time.unscaledTime - lastPreview < c.previewCooldown) return;
        c.previewSource.PlayOneShot(c.previewClip);
        lastPreview = Time.unscaledTime;
    }

    float lastPreview;
}
