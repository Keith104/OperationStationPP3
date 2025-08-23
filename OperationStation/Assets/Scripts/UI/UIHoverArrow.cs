using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIHoverArrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    static readonly HashSet<UIHoverArrow> Instances = new HashSet<UIHoverArrow>();
    public static bool KeyboardMode = false;

    [SerializeField] RectTransform arrow;
    [SerializeField, Range(0.01f, 0.4f)] float fadeTime = 0.12f;
    [SerializeField, Range(0f, 24f)] float slideInPixels = 8f;

    Vector2 restPos;
    CanvasGroup cg;
    Coroutine anim;
    bool pointerInside;
    bool isSelected;
    bool inited;
    Selectable sel;

    void Awake()
    {
        sel = GetComponent<Selectable>();
        if (arrow)
        {
            EnsureInit();
            restPos = arrow.anchoredPosition;
            cg.alpha = 0f;
            arrow.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        Instances.Add(this);
        StartCoroutine(EnsureArrowMatchesSelectionNextFrame());
    }

    void OnDisable()
    {
        Instances.Remove(this);
        HideImmediate();
    }

    void OnDestroy() { Instances.Remove(this); }

    bool EnsureInit()
    {
        if (inited) return true;
        if (!arrow) return false;
        cg = arrow.GetComponent<CanvasGroup>() ?? arrow.gameObject.AddComponent<CanvasGroup>();
        restPos = arrow.anchoredPosition;
        inited = true;
        return true;
    }

    bool CanShow()
    {
        if (!sel) sel = GetComponent<Selectable>();
        return sel && sel.IsActive() && sel.interactable;
    }

    IEnumerator EnsureArrowMatchesSelectionNextFrame()
    {
        yield return null;
        var es = EventSystem.current;
        bool thisSelected = es && es.currentSelectedGameObject == gameObject;
        isSelected = thisSelected;
        if (thisSelected && (KeyboardMode || pointerInside) && CanShow()) ShowArrow();
        else if (!pointerInside) HideImmediate();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        KeyboardMode = false;
        pointerInside = true;
        if (CanShow()) ShowArrow();
    }

    public void OnPointerExit(PointerEventData e)
    {
        pointerInside = false;
        if (!isSelected || KeyboardMode == false) HideArrow();
    }

    public void OnSelect(BaseEventData e)
    {
        isSelected = true;
        if (KeyboardMode && CanShow()) ShowArrow();
    }

    public void OnDeselect(BaseEventData e)
    {
        isSelected = false;
        if (!pointerInside) HideArrow();
    }

    void LateUpdate()
    {
        if (arrow && arrow.gameObject.activeSelf)
        {
            var es = EventSystem.current;
            bool actuallySelected = es && es.currentSelectedGameObject == gameObject;
            if (!CanShow() || (!pointerInside && (!KeyboardMode || !actuallySelected)))
                HideImmediate();
        }
    }

    public static void HideAll()
    {
        foreach (var inst in Instances) if (inst) inst.HideImmediate();
    }

    public static void HideAllInParent(Transform parent)
    {
        foreach (var inst in Instances)
            if (inst && inst.transform.IsChildOf(parent))
                inst.HideImmediate();
    }

    public static void PingSelectedArrow()
    {
        var es = EventSystem.current;
        if (!es) return;
        var go = es.currentSelectedGameObject;
        if (!go) return;
        var a = go.GetComponent<UIHoverArrow>();
        if (!a) return;
        a.ShowImmediate();
    }

    void ShowArrow()
    {
        if (!EnsureInit() || !CanShow()) return;
        arrow.gameObject.SetActive(true);
        StartAnim(true);
    }

    void ShowImmediate()
    {
        if (!EnsureInit() || !CanShow()) return;
        if (anim != null) StopCoroutine(anim);
        anim = null;
        cg.alpha = 1f;
        arrow.anchoredPosition = restPos;
        arrow.gameObject.SetActive(true);
    }

    void HideArrow()
    {
        if (!EnsureInit()) return;
        StartAnim(false);
    }

    void HideImmediate()
    {
        if (!EnsureInit()) return;
        if (anim != null) StopCoroutine(anim);
        anim = null;
        cg.alpha = 0f;
        arrow.anchoredPosition = restPos;
        if (arrow) arrow.gameObject.SetActive(false);
    }

    void StartAnim(bool show)
    {
        if (!EnsureInit()) return;
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate(show));
    }

    IEnumerator Animate(bool show)
    {
        float t = 0f;
        float startA = cg.alpha;
        float endA = show ? 1f : 0f;
        Vector2 from = show ? (restPos + new Vector2(-slideInPixels, 0f)) : restPos;
        Vector2 to = restPos;
        if (show) arrow.anchoredPosition = from;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fadeTime);
            cg.alpha = Mathf.Lerp(startA, endA, u);
            arrow.anchoredPosition = Vector2.Lerp(show ? from : to, to, u);
            yield return null;
        }
        cg.alpha = endA;
        arrow.anchoredPosition = to;
        if (!show && arrow) arrow.gameObject.SetActive(false);
        anim = null;
    }
}
