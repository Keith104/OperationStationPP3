using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DifficultyButtonUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    public DifficultySO difficulty;

    [SerializeField] private Image buttonBorder;
    [SerializeField] private Color idleBorderColor = Color.white;

    [SerializeField] private TMP_Text descriptionText;   // shared
    [SerializeField] private Image descriptionBorder;    // shared
    [SerializeField] private Color descriptionIdleColor = Color.white;

    private bool isHovered;
    private static int hoverCount = 0; // how many difficulty buttons are currently hovered

    void Awake()
    {
        SetBorder(idleBorderColor);
        SetDescription(null);
        SyncWithSelection();
    }

    // --------- POINTER ----------
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHovered) { isHovered = true; hoverCount++; }
        ShowActive();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHovered) { isHovered = false; hoverCount = Mathf.Max(0, hoverCount - 1); }

        if (IsSelected())
        {
            ShowActive();
        }
        else
        {
            SetBorder(idleBorderColor);
            EnsureDefaultsIfNoHoverOrSelection();
        }
    }

    // --------- SELECT / DESELECT ----------
    public void OnSelect(BaseEventData eventData)
    {
        DifficultyManager.instance.SetDifficulty(difficulty);
        ShowActive();
        RefreshAllBorders();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!isHovered)
        {
            SetBorder(idleBorderColor);
            EnsureDefaultsIfNoHoverOrSelection();
        }
    }

    // --------- CLICK makes this the selected UI element too ----------
    public void OnPointerClick(PointerEventData eventData)
    {
        EventSystem.current?.SetSelectedGameObject(gameObject);
        OnSelect(null);
    }

    // --------- Helpers ----------
    private bool IsSelected()
    {
        return DifficultyManager.instance.GetDifficulty() == difficulty;
    }

    private void ShowActive()
    {
        SetBorder(difficulty.uiColor);
        SetDescription(difficulty);
    }

    // If NO difficulty button is selected and NONE are hovered -> reset to defaults.
    private void EnsureDefaultsIfNoHoverOrSelection()
    {
        if (hoverCount > 0) return;

        var selObj = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        var selBtn = selObj ? selObj.GetComponent<DifficultyButtonUI>() : null;

        if (selBtn == null)
        {
            DifficultyManager.instance.SetDifficulty(null); // clear last selection
            SetDescription(null);

            var all = FindObjectsByType<DifficultyButtonUI>(FindObjectsSortMode.None);
            foreach (var b in all) b.SetBorder(b.idleBorderColor);
        }
        else
        {
            selBtn.ShowActive(); // another difficulty button is selected
        }
    }

    private void RefreshAllBorders()
    {
        var all = FindObjectsByType<DifficultyButtonUI>(FindObjectsSortMode.None);
        foreach (var b in all) b.SyncWithSelection();
    }

    public void SyncWithSelection()
    {
        if (isHovered || IsSelected()) SetBorder(difficulty.uiColor);
        else SetBorder(idleBorderColor);
        // Description is controlled by hover/select so we don't flicker it here.
    }

    private void SetBorder(Color c)
    {
        if (buttonBorder) buttonBorder.color = c;
    }

    private void SetDescription(DifficultySO d)
    {
        if (descriptionText) descriptionText.text = d ? d.difficultyDescription : string.Empty;
        if (descriptionBorder) descriptionBorder.color = d ? d.uiColor : descriptionIdleColor;
    }
}
