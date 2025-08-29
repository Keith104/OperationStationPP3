using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Canvas canvas;

    RectTransform rect;
    RectTransform canvasRect;
    CanvasGroup group;

    public static bool IsDragging { get; private set; }
    Vector2 dragOffset;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasRect = canvas.GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        var g = GetComponent<UnityEngine.UI.Graphic>();
        if (g == null) gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
    }

    public void OnPointerDown(PointerEventData e)
    {
        transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        IsDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, canvas.worldCamera, out var localMouse);
        dragOffset = rect.anchoredPosition - localMouse;
        group.blocksRaycasts = true;
    }

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, canvas.worldCamera, out var localMouse);
        rect.anchoredPosition = localMouse + dragOffset;
        ClampInsideCanvas();
    }

    public void OnEndDrag(PointerEventData e)
    {
        IsDragging = false;
        group.blocksRaycasts = true;
    }

    void ClampInsideCanvas()
    {
        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;

        Vector3[] wc = new Vector3[4];
        rect.GetWorldCorners(wc);

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < 4; i++)
        {
            Vector2 p = (Vector2)canvasRect.InverseTransformPoint(wc[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        float dx = 0f;
        float dy = 0f;

        if (min.x < -halfW) dx = -halfW - min.x;
        else if (max.x > halfW) dx = halfW - max.x;

        if (min.y < -halfH) dy = -halfH - min.y;
        else if (max.y > halfH) dy = halfH - max.y;

        rect.anchoredPosition += new Vector2(dx, dy);
    }
}
