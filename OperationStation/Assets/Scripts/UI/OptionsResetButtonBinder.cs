using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsResetButtonBinder : MonoBehaviour
{
    [Header("Find/Bind a Button and Label in children")]
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text statusLabel;

    [Header("Label Texts")]
    [SerializeField] private string defaultText = "Just Default";
    [SerializeField] private string feedbackText = "Reseted";
    [SerializeField] private float feedbackSeconds = 1.0f; // after this, switch back to default text

    float _rebindProbeAt;
    float _revertAt;
    bool _bound;

    void OnEnable()
    {
        Resolve();
        Bind();
        ShowDefault();          // label is visible and says "Just Default"
    }

    void OnDisable()
    {
        Unbind();
    }

    void Update()
    {
        // Re-resolve & rebind if UI objects were rebuilt (scene/panel swap)
        if (Time.unscaledTime >= _rebindProbeAt)
        {
            if (!resetButton || !resetButton.gameObject.activeInHierarchy || !statusLabel)
            {
                Resolve();
                Bind();
                if (statusLabel) ShowDefault();
            }
            _rebindProbeAt = Time.unscaledTime + 0.5f; // probe ~2x/sec
        }

        // Never disable the label; just switch text back after delay
        if (_revertAt > 0f && Time.unscaledTime >= _revertAt)
        {
            ShowDefault();
            _revertAt = 0f;
        }
    }

    void Resolve()
    {
        if (!resetButton) resetButton = GetComponentInChildren<Button>(true);

        if (!statusLabel)
        {
            // Prefer a clearly named child if you have one
            Transform t = transform.Find("ResetLabel");
            if (!t) t = transform.Find("Label");
            if (!t) t = transform.Find("Text");

            statusLabel = t ? t.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>(true);
        }
    }

    void Bind()
    {
        if (!resetButton) return;
        if (!_bound)
        {
            resetButton.onClick.AddListener(HandleResetClicked);
            _bound = true;
        }
    }

    void Unbind()
    {
        if (resetButton && _bound)
        {
            resetButton.onClick.RemoveListener(HandleResetClicked);
            _bound = false;
        }
    }

    void HandleResetClicked()
    {
        var mgr = OptionsManager.instance;
        if (!mgr) return;

        // 1) Reset mixer + prefs
        mgr.ResetToDefaults();

        // 2) Sync any visible sliders + % labels
        SyncVisibleUI(mgr);

        // 3) Feedback text (keep label visible; just change the text)
        ShowFeedback();
        _revertAt = Time.unscaledTime + Mathf.Max(0.1f, feedbackSeconds);
    }

    void SyncVisibleUI(OptionsManager mgr)
    {
        foreach (OptionsManager.Channel ch in System.Enum.GetValues(typeof(OptionsManager.Channel)))
        {
            var marker = OptionsSliderMarker.FindActive(ch);
            if (!marker) continue;

            var slider = marker.GetSlider();
            var label = marker.GetPercentLabel();

            float v = mgr.GetLinear(ch);

            if (slider) slider.SetValueWithoutNotify(v);
            if (label) label.text = Mathf.RoundToInt(v * 100f) + "%";
        }
    }

    void ShowDefault()
    {
        if (!statusLabel) return;
        statusLabel.text = defaultText;
        if (!statusLabel.gameObject.activeSelf) statusLabel.gameObject.SetActive(true); // keep visible
    }

    void ShowFeedback()
    {
        if (!statusLabel) return;
        statusLabel.text = feedbackText; // "Reseted"
        if (!statusLabel.gameObject.activeSelf) statusLabel.gameObject.SetActive(true);
    }
}
