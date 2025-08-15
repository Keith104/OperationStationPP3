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
    public float horizontalPadding = 16f;
    public float topPadding = 0f;
    public float bottomPadding = 0f;

    [Header("Scroll")]
    public float pixelsPerSecondMin = 60f;
    public float pixelsPerSecondMax = 1300f;
    public float preRollDelay = 0.5f;
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

    // guards
    Coroutine runRoutine;
    bool isRolling;
    bool initialized;

    // Reference to main menu manager to re-enable buttons after credits
    MainMenuManager menuManager;

    void Awake()
    {
        pixelsPerSecond = pixelsPerSecondMin;

        scrollRect = GetComponent<ScrollRect>();
        if (viewport == null) viewport = scrollRect.viewport;
        if (content == null) content = scrollRect.content;

        if (!creditsText)
        {
            Debug.LogError("CreditsScroller: creditsText is not assigned.");
            enabled = false; return;
        }
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
            // Do not force alpha here; the opening menu may tween us in.
            creditsCanvasGroup.blocksRaycasts = true;
            creditsCanvasGroup.interactable = false;
        }

        // Input wrapper + action
        input = new PlayerInput();                  // OK to 'new' the GENERATED wrapper
        speedUpAction = input.Player.SpeedUpCredits;

        // Find the menu manager so we can toggle button interactability at the end
        menuManager = FindObjectOfType<MainMenuManager>();

        initialized = true;
    }

    void OnEnable()
    {
        if (!initialized) return;

        input.Player.Enable();                      // enable map each time we open
        pixelsPerSecond = pixelsPerSecondMin;

        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = StartCoroutine(SetupAndRun());
    }

    void OnDisable()
    {
        if (!initialized) return;

        input.Player.Disable();
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }
        isRolling = false;
    }

    void Update()
    {
        // Poll "speed up" while visible
        bool speedUpHeld = speedUpAction != null && speedUpAction.IsPressed();
        pixelsPerSecond = speedUpHeld ? pixelsPerSecondMax : pixelsPerSecondMin;

        // While we are in the roll phase, keep alpha pinned to 1
        if (isRolling && creditsCanvasGroup)
        {
            if (creditsCanvasGroup.alpha != 1f)
                creditsCanvasGroup.alpha = 1f;
        }
    }

    IEnumerator SetupAndRun()
    {
        if (disableUserScrollWhileRolling)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = false;   // we drive content directly
            scrollRect.inertia = false;
        }

        var originalMovement = scrollRect.movementType;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

        // --- Wait for viewport to size, then measure preferred height ---
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
            yield return null; // wait a frame
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

        // If an opening animation is running elsewhere, pin alpha to 1 before we roll
        if (creditsCanvasGroup) creditsCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(preRollDelay);

        // ----- Roll phase -----
        isRolling = true;

        float y = startY;
        while (y < endY)
        {
            y += pixelsPerSecond * Time.unscaledDeltaTime;
            if (y > endY) y = endY;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
            yield return null;
        }

        isRolling = false;

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

        // Re-enable main menu buttons now that credits are done
        if (menuManager != null)
            menuManager.SetMenuButtonsInteractable(true);

        // Restore original movement type
        scrollRect.movementType = originalMovement;

        runRoutine = null;
    }

    void SetTopAnchored(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
    }
}
