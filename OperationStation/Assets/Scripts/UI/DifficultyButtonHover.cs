using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DifficultyButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Button button;
    public DifficultySO difficulty;
    public Image borderImage;
    public Image descriptionPanelImage;
    public TextMeshProUGUI descriptionTMP;

    public Color normalBorderColor = Color.white;
    public Color normalPanelColor = new Color(0, 0, 0, 0.35f);

    bool _isHovered;
    bool _isSelected;

    void Reset()
    {
        if (!button) button = GetComponent<Button>();
    }

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
    }

    void Start()
    {
        _isHovered = false;
        _isSelected = IsThisObjectCurrentlySelected();
        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        UpdateVisuals();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        UpdateVisuals();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (difficulty == null)
        {
            ApplyNormal();
            return;
        }

        if (_isHovered)
        {
            ApplyDifficultyUI();
            return;
        }

        if (_isSelected)
        {
            ApplyDifficultyUI();
            return;
        }

        ApplyNormal();
    }

    void ApplyDifficultyUI()
    {
        if (borderImage) borderImage.color = difficulty.uiColor;

        if (descriptionPanelImage)
        {
            var a = descriptionPanelImage.color.a;
            var c = difficulty.uiColor;
            descriptionPanelImage.color = new Color(c.r, c.g, c.b, a);
        }

        if (descriptionTMP)
        {
            if (!string.IsNullOrEmpty(difficulty.difficultyDescription))
            {
                descriptionTMP.text = difficulty.difficultyDescription;
            }
            else
            {
                descriptionTMP.text =
                    $"{difficulty.difficultyName}\n" +
                    $"• Health ×{difficulty.health}\n" +
                    $"• Damage ×{difficulty.damage}\n" +
                    $"• Attack Cooldown ×{difficulty.attackCooldown}";
            }
        }
    }

    void ApplyNormal()
    {
        if (borderImage) borderImage.color = normalBorderColor;
        if (descriptionPanelImage) descriptionPanelImage.color = normalPanelColor;
        if (descriptionTMP) descriptionTMP.text = string.Empty;
    }

    bool IsThisObjectCurrentlySelected()
    {
        var es = EventSystem.current;
        if (!es) return false;
        var selected = es.currentSelectedGameObject;
        return selected != null && (button ? selected == button.gameObject : selected == gameObject);
    }
}
