using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ScrollRect))]
public class CreditsScroller : MonoBehaviour
{
    [Header("Wiring")]
    public TMP_Text creditsText;            // TMP Text (UI) inside Content
    public RectTransform viewport;          // ScrollRect.viewport
    public RectTransform content;           // ScrollRect.content
    public GameObject creditsMenuRoot;      // Parent overlay to disable at the end
    public CanvasGroup creditsCanvasGroup;  // If null, one is added to creditsMenuRoot

    [Header("Layout")]
    public float horizontalPadding = 16f;   // Extra width padding for preferred-size calc
    public float topPadding = 0f;           // Space above first line
    public float bottomPadding = 0f;        // Space below last line

    [Header("Scroll")]
    public float pixelsPerSecondMin = 60f;
    public float pixelsPerSecondMax = 1300f;
    public float preRollDelay = 0.5f;       // Hold before rolling
    public bool disableUserScrollWhileRolling = true;

    [Header("Fade Out")]
    public float fadeSeconds = 0.75f;       // Fade overlay OUT (1 → 0) at the end

    // runtime
    float pixelsPerSecond;
    ScrollRect scrollRect;
    RectTransform textRT;
    float textHeight;
    float viewportHeight;

    // Input (your GENERATED wrapper class name must match your .inputactions)
    PlayerInput input;                 // <-- generated C# class
    InputAction speedUpAction;

    void Awake()
    {
        pixelsPerSecond = pixelsPerSecondMin;

        scrollRect = GetComponent<ScrollRect>();
        if (viewport == null) viewport = scrollRect.viewport;
        if (content == null) content = scrollRect.content;

        textRT = creditsText.GetComponent<RectTransform>();

        // top-stretch anchors for clean "roll up" math
        SetTopAnchored(content);
        SetTopAnchored(textRT);

        // Ensure a CanvasGroup we can fade (let menu control fade-in)
        if (creditsCanvasGroup == null && creditsMenuRoot != null)
        {
            creditsCanvasGroup = creditsMenuRoot.GetComponent<CanvasGroup>();
            if (creditsCanvasGroup == null)
                creditsCanvasGroup = creditsMenuRoot.AddComponent<CanvasGroup>();
        }
        if (creditsCanvasGroup != null)
        {
            // don't force alpha here; main menu fades us in
            creditsCanvasGroup.blocksRaycasts = true;
            creditsCanvasGroup.interactable = false;
        }

        // Input wrapper + action
        input = new PlayerInput();                  // OK to 'new' the GENERATED wrapper
        speedUpAction = input.Player.SpeedUpCredits;
    }

    void OnEnable()
    {
        input.Player.Enable();                      // enable map each time we open
        pixelsPerSecond = pixelsPerSecondMin;
        StartCoroutine(SetupAndRun());
    }

    void OnDisable()
    {
        input.Player.Disable();                     // disable when hidden
        StopAllCoroutines();
    }

    void Update()
    {
        // Your original "if" style: just poll the action every frame.
        bool speedUpHeld = speedUpAction != null && speedUpAction.IsPressed();
        pixelsPerSecond = speedUpHeld ? pixelsPerSecondMax : pixelsPerSecondMin;
    }

    IEnumerator SetupAndRun()
    {
        if (disableUserScrollWhileRolling)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = false;   // we drive content directly
            scrollRect.inertia = false;
        }

        // Allow content to go outside viewport while we animate
        var originalMovement = scrollRect.movementType;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

        // --- Wait for viewport to have a real size, then measure preferred height ---
        float availableWidth = 0f;
        int safety = 0;
        while (safety++ < 8)
        {
            Canvas.ForceUpdateCanvases();
            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            availableWidth = (viewport != null ? viewport.rect.width : textRT.rect.width) - Mathf.Max(0, horizontalPadding);
            viewportHeight = viewport != null ? viewport.rect.height : 0f;

            if (availableWidth > 0f && viewportHeight > 0f) break;
            yield return null; // wait a frame and try again
        }

        availableWidth = Mathf.Max(1f, availableWidth);

        creditsText.ForceMeshUpdate();
        Vector2 preferred = creditsText.GetPreferredValues(creditsText.text, availableWidth, 0f);
        textHeight = Mathf.Ceil(preferred.y);

        // Apply sizes
        textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, availableWidth);
        textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        float contentHeight = textHeight + topPadding + bottomPadding;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

        // Place text with top padding (top-anchored)
        textRT.anchoredPosition = new Vector2(textRT.anchoredPosition.x, -topPadding);

        // Start BELOW the viewport; end when the bottom of text reaches the top edge
        float startY = -viewportHeight;
        float endY = textHeight + bottomPadding;

        // Always start at the bottom so we roll up, even if content is short
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, startY);

        yield return new WaitForSecondsRealtime(preRollDelay);

        // Constant-speed roll (no easing/slowdown), always run
        float y = startY;
        while (y < endY)
        {
            y += pixelsPerSecond * Time.unscaledDeltaTime;
            if (y > endY) y = endY;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
            yield return null;
        }

        // Fade OUT the overlay, then disable its root (back to menu underneath)
        if (creditsCanvasGroup != null)
        {
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float a = fadeSeconds <= 0f ? 0f : Mathf.Clamp01(1f - t / fadeSeconds);
                creditsCanvasGroup.alpha = a; // 1 -> 0
                yield return null;
            }
            creditsCanvasGroup.alpha = 0f;
        }

        if (creditsMenuRoot != null)
            creditsMenuRoot.SetActive(false);

        // Restore original movement type
        scrollRect.movementType = originalMovement;
    }

    void SetTopAnchored(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
    }
}
