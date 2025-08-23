using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DifficultyButtonUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    public DifficultySO difficulty;

    [SerializeField] private Image buttonBorder;
    [SerializeField] private Color idleBorderColor = Color.white;

    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image descriptionBorder;
    [SerializeField] private Color descriptionIdleColor = Color.white;

    static readonly List<DifficultyButtonUI> instances = new List<DifficultyButtonUI>();
    static int hoverCount = 0;
    static GameObject lastSelectedGO;
    bool isHovered;

    void OnEnable()
    {
        if (!instances.Contains(this)) instances.Add(this);
        SetBorder(idleBorderColor);
        ApplyGlobal();
    }

    void OnDisable()
    {
        instances.Remove(this);
        ApplyGlobal();
    }

    void Update()
    {
        var cur = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (cur != lastSelectedGO)
        {
            lastSelectedGO = cur;
            ApplyGlobal();
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!isHovered) { isHovered = true; hoverCount++; }
        ApplyGlobal();
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (isHovered) { isHovered = false; hoverCount = Mathf.Max(0, hoverCount - 1); }
        ApplyGlobal();
    }

    public void OnPointerClick(PointerEventData e)
    {
        EventSystem.current?.SetSelectedGameObject(gameObject);
        DifficultyManager.instance.SetDifficulty(difficulty);
        ApplyGlobal();
    }

    public void OnSelect(BaseEventData e)
    {
        DifficultyManager.instance.SetDifficulty(difficulty);
        ApplyGlobal();
    }

    public void OnDeselect(BaseEventData e)
    {
        ApplyGlobal();
    }

    void ApplyGlobal()
    {
        DifficultySO active = null;
        for (int i = 0; i < instances.Count; i++)
            if (instances[i].isHovered) { active = instances[i].difficulty; break; }
        if (active == null)
        {
            var go = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            var sel = go ? go.GetComponent<DifficultyButtonUI>() : null;
            if (sel) active = sel.difficulty;
        }

        var borders = new HashSet<Image>();
        var descBorders = new HashSet<Image>();
        var descTexts = new HashSet<TMP_Text>();
        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i].buttonBorder) borders.Add(instances[i].buttonBorder);
            if (instances[i].descriptionBorder) descBorders.Add(instances[i].descriptionBorder);
            if (instances[i].descriptionText) descTexts.Add(instances[i].descriptionText);
        }

        Color bcol = active ? active.uiColor : (instances.Count > 0 ? instances[0].idleBorderColor : Color.white);
        foreach (var b in borders) b.color = bcol;

        foreach (var t in descTexts) t.text = active ? active.difficultyDescription : string.Empty;
        Color dcol = active ? active.uiColor : (instances.Count > 0 ? instances[0].descriptionIdleColor : Color.white);
        foreach (var ib in descBorders) ib.color = dcol;
    }

    void SetBorder(Color c)
    {
        if (buttonBorder) buttonBorder.color = c;
    }
}
