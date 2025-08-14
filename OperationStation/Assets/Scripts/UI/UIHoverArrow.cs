using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIHoverArrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] RectTransform arrow; // Arrow object with CanvasGroup
    [SerializeField, Range(0.01f, 0.4f)] float fadeTime = 0.12f;
    [SerializeField, Range(0f, 24f)] float slideInPixels = 8f;

    Vector2 arrowRestPos;
    CanvasGroup cg;
    Coroutine anim;

    void Awake()
    {
        if (!arrow)
        {
            Debug.LogWarning($"UIHoverArrow: No arrow assigned on {name}.");
            enabled = false;
            return;
        }

        cg = arrow.GetComponent<CanvasGroup>();
        if (!cg) cg = arrow.gameObject.AddComponent<CanvasGroup>();

        arrowRestPos = arrow.anchoredPosition;
        cg.alpha = 0f;
        arrow.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowArrow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideArrow();
    }

    void ShowArrow()
    {
        arrow.gameObject.SetActive(true);
        StartAnim(true);
    }

    void HideArrow()
    {
        StartAnim(false);
    }

    void StartAnim(bool show)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate(show));
    }

    IEnumerator Animate(bool show)
    {
        float t = 0f;
        float startA = cg.alpha;
        float endA = show ? 1f : 0f;
        Vector2 from = show ? (arrowRestPos + new Vector2(-slideInPixels, 0f)) : arrowRestPos;
        Vector2 to = arrowRestPos;

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

        if (!show) arrow.gameObject.SetActive(false);
        anim = null;
    }
}
