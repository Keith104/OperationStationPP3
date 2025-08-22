using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class DifficultyButtonUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public DifficultySO difficulty;

    [SerializeField] private Image buttonBorder;
    [SerializeField] private Color idleBorderColor = Color.white;

    [Header("Shared Description UI")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image descriptionBorder;
    [SerializeField] private Color descriptionIdleColor = Color.white;

    [Header("Optional: Selected Values UI")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text attackCooldownText;

    [System.Serializable] public class DifficultySelectedEvent : UnityEvent<DifficultySO> { }
    [Header("Optional: Event when selected")]
    [SerializeField] private DifficultySelectedEvent onSelected;

    private bool isHovered;

    void Awake()
    {
        SetBorder(idleBorderColor);
        ClearDescription();
        RefreshVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        RefreshVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        DifficultyManager.instance.SetDifficulty(difficulty);
        ApplySelectedValues();
        if (onSelected != null) onSelected.Invoke(difficulty);
        RefreshAllButtons();
    }

    public void RefreshVisual()
    {
        bool isSelected = DifficultyManager.instance.GetDifficulty() == difficulty;

        if (isHovered)
        {
            SetBorder(difficulty.uiColor);
            if (descriptionText) descriptionText.text = difficulty.difficultyDescription;
            if (descriptionBorder) descriptionBorder.color = difficulty.uiColor;
        }
        else if (isSelected)
        {
            SetBorder(difficulty.uiColor);
            if (descriptionText) descriptionText.text = difficulty.difficultyDescription;
            if (descriptionBorder) descriptionBorder.color = difficulty.uiColor;
        }
        else
        {
            SetBorder(idleBorderColor);
            ClearDescription();
        }
    }

    private void ApplySelectedValues()
    {
        if (healthText) healthText.text = difficulty.health.ToString("0.##");
        if (damageText) damageText.text = difficulty.damage.ToString("0.##");
        if (attackCooldownText) attackCooldownText.text = difficulty.attackCooldown.ToString();
    }

    private void RefreshAllButtons()
    {
        var all = FindObjectsByType<DifficultyButtonUI>(FindObjectsSortMode.None);
        foreach (var b in all) b.RefreshVisual();
    }

    private void SetBorder(Color c)
    {
        if (buttonBorder) buttonBorder.color = c;
    }

    private void ClearDescription()
    {
        if (descriptionText) descriptionText.text = string.Empty;
        if (descriptionBorder) descriptionBorder.color = descriptionIdleColor;
    }
}
