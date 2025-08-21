using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DifficultyButtons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] int index;
    [SerializeField] DifficultySO difficulty;
    [SerializeField] Image borderImage;
    [SerializeField] Image descriptionPanelImage;
    [SerializeField] TextMeshProUGUI descriptionTMP;
    [SerializeField] Color normalBorderColor = Color.white;
    [SerializeField] Color normalPanelColor = new Color(0, 0, 0, 0.35f);
    [SerializeField] Color[] difficultyColors;

    void Start() { }
    void Update() { }

    public void SetDiffFromHere(int i)
    {
        var d = difficulty != null ? difficulty : DifficultyManager.instance != null && DifficultyManager.instance.allDifficulties != null && i >= 0 && i < DifficultyManager.instance.allDifficulties.Count ? DifficultyManager.instance.allDifficulties[i] : null;
        if (d != null && DifficultyManager.instance != null) DifficultyManager.instance.SetDifficulty(d);
    }

    public void OnPointerEnter(PointerEventData eventData) { ApplyHover(); }
    public void OnPointerExit(PointerEventData eventData) { ClearHover(); }
    public void OnSelect(BaseEventData eventData) { ApplyHover(); }
    public void OnDeselect(BaseEventData eventData) { ClearHover(); }

    DifficultySO Diff
    {
        get
        {
            if (difficulty != null) return difficulty;
            var mgr = DifficultyManager.instance;
            if (mgr == null || mgr.allDifficulties == null) return null;
            if (index < 0) return null;
            if (mgr.allDifficulties is System.Collections.Generic.List<DifficultySO> list)
                return index < list.Count ? list[index] : null;
            return null;
        }
    }

    void ApplyHover()
    {
        var d = Diff;
        if (d == null) { ClearHover(); return; }

        var c = GetColorFor(d);
        if (borderImage) borderImage.color = c;
        if (descriptionPanelImage) descriptionPanelImage.color = new Color(c.r, c.g, c.b, descriptionPanelImage ? descriptionPanelImage.color.a : 0.35f);
        if (descriptionTMP) descriptionTMP.text = !string.IsNullOrEmpty(d.difficultyDescription)
            ? d.difficultyDescription
            : $"{d.difficultyName}\n• Health ×{d.health}\n• Damage ×{d.damage}\n• Attack Cooldown ×{d.attackCooldown}";
    }

    void ClearHover()
    {
        if (borderImage) borderImage.color = normalBorderColor;
        if (descriptionPanelImage) descriptionPanelImage.color = normalPanelColor;
        if (descriptionTMP) descriptionTMP.text = "";
    }

    Color GetColorFor(DifficultySO d)
    {
        if (difficultyColors != null && index >= 0 && index < difficultyColors.Length) return difficultyColors[index];
        return new Color(0.96f, 0.75f, 0.2f);
    }
}
