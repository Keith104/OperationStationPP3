using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Wipe Material")]
    public Material wipeMaterial;

    [Header("Timings")]
    public float coverDuration = 0.8f;
    public float revealDuration = 0.8f;
    public float waitAfterCoverSeconds = 0f;
    public float minLoadingDisplaySeconds = 1.5f;
    public float loadDelaySeconds = 0.0f;
    public AnimationCurve coverEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve revealEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Wipe Look")]
    [Range(0, 0.1f)] public float edgeSoftness = 0.02f;
    public bool clockwise = true;
    [Range(0, 1)] public float centerX = 0.5f, centerY = 0.5f;
    [Range(-180, 180)] public float startAngleDeg = 90f;

    [Header("Canvas")]
    public int sortingOrder = 32760;

    [Header("Fonts")]
    public TMP_FontAsset headerFont;
    public TMP_FontAsset bodyFont;

    [Header("Loading Text bottom left")]
    public bool showLoadingText = true;
    public string loadingBaseText = "Loading";
    public int loadingFontSize = 28;
    public Color loadingColor = Color.white;
    public Vector2 loadingPadding = new Vector2(32, 28);
    public float loadingEllipsisInterval = 0.35f;

    [Header("Hints bottom right")]
    public bool showHints = true;
    [TextArea(2, 6)]
    public string[] hints;
    public bool hintsAutoSize = true;
    public int hintsFontSizeMin = 16;
    public int hintsFontSizeMax = 26;
    public int hintsFontSizeDefault = 24;
    public Color hintsColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Vector2 hintsPadding = new Vector2(32, 28);
    public float hintsMaxWidth = 420f;
    public float hintsAutoCycleSeconds = 3.5f;

    [Header("Hint Controls input system")]
    public InputActionReference hintNextAction;
    public InputActionReference hintPrevAction;

    [Header("Continue Button")]
    public Button continueButtonPrefab;
    public Vector2 continueButtonAnchorPadding = new Vector2(32, 28);
    public Vector2 continueButtonTextPadding = new Vector2(16, 8);
    public Vector2 continueButtonMinSize = new Vector2(120, 36);
    public Color continueNormal = Color.white;
    public Color continueHighlighted = new Color(0.92f, 0.92f, 1f, 1f);
    public Color continuePressed = new Color(0.85f, 0.85f, 0.98f, 1f);
    public Color continueDisabled = new Color(0.6f, 0.6f, 0.6f, 1f);
    public float continueFadeDuration = 0.08f;

    Canvas canvas;
    RawImage overlay;
    Material runtimeMat;

    TMP_Text loadingText;
    TMP_Text hintsText;
    Button continueButton;

    Coroutine dotsRoutine;
    Coroutine hintsRoutine;
    int currentHintIndex;
    bool isRunning;

    float hintsNextCycleAt = 0f;

    struct SavedSelectableState { public Selectable sel; public bool wasInteractable; }
    readonly List<SavedSelectableState> _disabledNewSceneUI = new List<SavedSelectableState>();
    readonly List<SavedSelectableState> _disabledActiveSceneUI = new List<SavedSelectableState>();
    bool _armDisableNewSceneUI = false;
    bool _continueClicked = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureEventSystem();

        canvas = new GameObject("SceneTransitionCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(canvas.gameObject);

        overlay = new GameObject("SceneTransitionOverlay",
                 typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        overlay.transform.SetParent(canvas.transform, false);
        var ort = (RectTransform)overlay.transform;
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        overlay.raycastTarget = false;

        runtimeMat = new Material(wipeMaterial);
        overlay.material = runtimeMat;
        ApplyLook();
        runtimeMat.SetFloat("_Progress", 0f);
        overlay.enabled = false;

        BuildLoadingText();
        SetLoadingVisible(false);

        ShuffleHints();
        BuildHintsText();
        SetHintsVisible(false);
    }

    void EnsureEventSystem()
    {
        var es = EventSystem.current ?? FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            es = new GameObject("EventSystem").AddComponent<EventSystem>();
            es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
        else
        {
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy) Destroy(legacy);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    void ApplyLook()
    {
        if (!runtimeMat) return;
        runtimeMat.SetFloat("_Edge", edgeSoftness);
        runtimeMat.SetFloat("_Clockwise", clockwise ? 1f : 0f);
        runtimeMat.SetVector("_Center", new Vector4(centerX, centerY, 0, 0));
        runtimeMat.SetFloat("_StartAngleDeg", startAngleDeg);
    }

    void BuildLoadingText()
    {
        var go = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);
        loadingText = go.GetComponent<TextMeshProUGUI>();
        if (headerFont) loadingText.font = headerFont;
        loadingText.fontSize = loadingFontSize;
        loadingText.color = loadingColor;
        loadingText.alignment = TextAlignmentOptions.BottomLeft;
        loadingText.textWrappingMode = TextWrappingModes.NoWrap;
        loadingText.raycastTarget = false;
        loadingText.text = loadingBaseText;

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(0f, 0f);
        r.pivot = new Vector2(0f, 0f);
        r.anchoredPosition = loadingPadding;
        go.transform.SetAsLastSibling();
    }

    void BuildHintsText()
    {
        var go = new GameObject("HintsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);
        hintsText = go.GetComponent<TextMeshProUGUI>();
        if (bodyFont) hintsText.font = bodyFont;
        hintsText.color = hintsColor;
        hintsText.alignment = TextAlignmentOptions.BottomRight;
        hintsText.textWrappingMode = TextWrappingModes.Normal;
        hintsText.enableAutoSizing = hintsAutoSize;
        hintsText.fontSizeMin = hintsFontSizeMin;
        hintsText.fontSizeMax = hintsFontSizeMax;
        if (!hintsAutoSize) hintsText.fontSize = hintsFontSizeDefault;
        hintsText.raycastTarget = false;
        hintsText.text = hints != null && hints.Length > 0 ? hints[0] : "";

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1f, 0f);
        r.anchorMax = new Vector2(1f, 0f);
        r.pivot = new Vector2(1f, 0f);
        r.anchoredPosition = new Vector2(-hintsPadding.x, hintsPadding.y);
        r.sizeDelta = new Vector2(Mathf.Max(16f, hintsMaxWidth), r.sizeDelta.y);

        RefreshHintsRect();
        go.transform.SetAsLastSibling();
    }

    void RefreshHintsRect()
    {
        if (hintsText == null) return;
        float maxW = Mathf.Max(16f, hintsMaxWidth);
        Vector2 pref = hintsText.GetPreferredValues(hintsText.text, maxW, 0f);
        var r = hintsText.rectTransform;
        r.sizeDelta = new Vector2(maxW, pref.y);
    }

    Button BuildContinueButton()
    {
        if (continueButtonPrefab == null) return null;

        var btn = Instantiate(continueButtonPrefab, canvas.transform);
        btn.gameObject.name = "ContinueButton";
        btn.interactable = true;

        var r = btn.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(0f, 0f);
        r.pivot = new Vector2(0f, 0f);
        r.anchoredPosition = continueButtonAnchorPadding;

        var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt != null)
        {
            txt.text = "Continue";
            if (headerFont) txt.font = headerFont;
            txt.enableAutoSizing = false;

            Vector2 pref = txt.GetPreferredValues(txt.text);
            Vector2 size = new Vector2(
                Mathf.Max(continueButtonMinSize.x, pref.x + continueButtonTextPadding.x * 2f),
                Mathf.Max(continueButtonMinSize.y, pref.y + continueButtonTextPadding.y * 2f)
            );
            r.sizeDelta = size;
        }

        var img = btn.targetGraphic as Graphic;
        if (img == null)
        {
            var image = btn.GetComponent<Image>();
            if (image == null) image = btn.gameObject.AddComponent<Image>();
            img = image;
            btn.targetGraphic = img;
        }
        var colors = btn.colors;
        colors.normalColor = continueNormal;
        colors.highlightedColor = continueHighlighted;
        colors.pressedColor = continuePressed;
        colors.selectedColor = continueHighlighted;
        colors.disabledColor = continueDisabled;
        colors.fadeDuration = continueFadeDuration;
        btn.colors = colors;
        if (img != null) img.raycastTarget = true;

        var le = btn.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.minWidth = r.sizeDelta.x;
            le.minHeight = r.sizeDelta.y;
            le.preferredWidth = r.sizeDelta.x;
            le.preferredHeight = r.sizeDelta.y;
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnContinuePressed);

        btn.gameObject.SetActive(false);
        btn.transform.SetAsLastSibling();
        return btn;
    }

    void OnContinuePressed()
    {
        _continueClicked = true;
    }

    void SetLoadingVisible(bool v)
    {
        if (!showLoadingText || loadingText == null) return;
        loadingText.enabled = v;
        if (v)
        {
            if (dotsRoutine != null) StopCoroutine(dotsRoutine);
            dotsRoutine = StartCoroutine(AnimateDots());
        }
        else if (dotsRoutine != null)
        {
            StopCoroutine(dotsRoutine);
            dotsRoutine = null;
        }
    }

    void SetHintsVisible(bool v)
    {
        if (!showHints || hintsText == null) return;
        hintsText.enabled = v;
        if (v && hints != null && hints.Length > 0)
        {
            if (hintsRoutine != null) StopCoroutine(hintsRoutine);
            hintsNextCycleAt = Time.realtimeSinceStartup + hintsAutoCycleSeconds;
            hintsRoutine = StartCoroutine(AutoCycleHints());
            EnableHintControls(true);
        }
        else
        {
            if (hintsRoutine != null) { StopCoroutine(hintsRoutine); hintsRoutine = null; }
            EnableHintControls(false);
        }
    }

    IEnumerator AnimateDots()
    {
        int i = 0;
        var wait = new WaitForSecondsRealtime(loadingEllipsisInterval);
        while (true)
        {
            int dots = i % 4;
            loadingText.text = loadingBaseText + new string('.', dots);
            i++;
            yield return wait;
        }
    }

    IEnumerator AutoCycleHints()
    {
        if (hintsAutoCycleSeconds <= 0f) yield break;

        while (true)
        {
            if (Time.realtimeSinceStartup >= hintsNextCycleAt)
            {
                NextHint();
                hintsNextCycleAt = Time.realtimeSinceStartup + hintsAutoCycleSeconds;
            }
            yield return null;
        }
    }

    void NextHint()
    {
        if (hints == null || hints.Length == 0) return;
        currentHintIndex = (currentHintIndex + 1) % hints.Length;
        hintsText.text = hints[currentHintIndex];
        RefreshHintsRect();
        hintsNextCycleAt = Time.realtimeSinceStartup + hintsAutoCycleSeconds;
    }

    void PrevHint()
    {
        if (hints == null || hints.Length == 0) return;
        currentHintIndex = (currentHintIndex - 1 + hints.Length) % hints.Length;
        hintsText.text = hints[currentHintIndex];
        RefreshHintsRect();
        hintsNextCycleAt = Time.realtimeSinceStartup + hintsAutoCycleSeconds;
    }

    void EnableHintControls(bool enable)
    {
        if (hintNextAction != null && hintNextAction.action != null)
        {
            if (enable) { hintNextAction.action.performed += OnHintNext; hintNextAction.action.Enable(); }
            else { hintNextAction.action.performed -= OnHintNext; hintNextAction.action.Disable(); }
        }
        if (hintPrevAction != null && hintPrevAction.action != null)
        {
            if (enable) { hintPrevAction.action.performed += OnHintPrev; hintPrevAction.action.Enable(); }
            else { hintPrevAction.action.performed -= OnHintPrev; hintPrevAction.action.Disable(); }
        }
    }

    void OnHintNext(InputAction.CallbackContext ctx) { if (hintsText != null && hintsText.enabled) NextHint(); }
    void OnHintPrev(InputAction.CallbackContext ctx) { if (hintsText != null && hintsText.enabled) PrevHint(); }

    void ClearUISelectionAndArrows()
    {
        UIHoverArrow.HideAll();
        UIHoverArrow.KeyboardMode = false;
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
    }

    public static void Run(string sceneName)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.CoverLoadRevealOptions(sceneName, true, true));
    }

    public static void Run(int scene)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.CoverLoadRevealOptions(scene, true, true));
    }

    public static void RunNoHints(string sceneName)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.CoverLoadRevealOptions(sceneName, false, false));
    }

    public static void RunNoHints(int scene)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.CoverLoadRevealOptions(scene, false, false));
    }

    void HandleSceneLoadedDisableUI(Scene scene, LoadSceneMode mode)
    {
        if (!_armDisableNewSceneUI) return;
        _armDisableNewSceneUI = false;
        SceneManager.sceneLoaded -= HandleSceneLoadedDisableUI;

        EnsureSingleEventSystemFor(scene);

        _disabledNewSceneUI.Clear();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var sels = root.GetComponentsInChildren<Selectable>(true);
            foreach (var s in sels)
            {
                if (canvas != null && s && s.transform.IsChildOf(canvas.transform)) continue;
                _disabledNewSceneUI.Add(new SavedSelectableState { sel = s, wasInteractable = s.interactable });
                if (s) s.interactable = false;
            }
        }
    }

    void RestoreNewSceneUI()
    {
        for (int i = 0; i < _disabledNewSceneUI.Count; i++)
        {
            var entry = _disabledNewSceneUI[i];
            if (entry.sel) entry.sel.interactable = entry.wasInteractable;
        }
        _disabledNewSceneUI.Clear();
    }

    void DisableAllSelectablesExceptContinue()
    {
        _disabledActiveSceneUI.Clear();
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var sels = root.GetComponentsInChildren<Selectable>(true);
            foreach (var s in sels)
            {
                if (!s) continue;
                if (continueButton != null && s == continueButton) continue;
                _disabledActiveSceneUI.Add(new SavedSelectableState { sel = s, wasInteractable = s.interactable });
                s.interactable = false;
            }
        }
    }

    void RestoreActiveSceneUI()
    {
        for (int i = 0; i < _disabledActiveSceneUI.Count; i++)
        {
            var entry = _disabledActiveSceneUI[i];
            if (entry.sel) entry.sel.interactable = entry.wasInteractable;
        }
        _disabledActiveSceneUI.Clear();
    }

    IEnumerator CoverLoadRevealOptions(string sceneName, bool showHintsDuringLoad, bool showContinueButton)
    {
        if (isRunning) yield break;
        isRunning = true;

        ApplyLook();

        runtimeMat.SetFloat("_Progress", 0f);
        overlay.enabled = true;
        overlay.raycastTarget = true;

        SetLoadingVisible(false);
        SetHintsVisible(false);
        yield return Animate(0f, 1f, coverDuration, coverEase);

        SetLoadingVisible(true);
        if (showHintsDuringLoad) SetHintsVisible(true); else SetHintsVisible(false);

        if (waitAfterCoverSeconds > 0f)
            yield return new WaitForSecondsRealtime(waitAfterCoverSeconds);

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        float shownAt = Time.realtimeSinceStartup;
        while (op.progress < 0.9f) yield return null;

        float elapsed = Time.realtimeSinceStartup - shownAt;
        float hold = Mathf.Max(0f, minLoadingDisplaySeconds - elapsed) + Mathf.Max(0f, loadDelaySeconds);
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        if (showContinueButton)
        {
            SetLoadingVisible(false);
            if (overlay != null) overlay.raycastTarget = false;
            if (continueButton == null) continueButton = BuildContinueButton();
            continueButton.transform.SetAsLastSibling();
            continueButton.interactable = true;
            continueButton.gameObject.SetActive(true);
            DisableAllSelectablesExceptContinue();
            _continueClicked = false;
            var es = EventSystem.current;
            if (es != null)
            {
                UIHoverArrow.KeyboardMode = true;
                es.SetSelectedGameObject(continueButton.gameObject);
            }
            while (!_continueClicked) yield return null;
            ClearUISelectionAndArrows();
            continueButton.gameObject.SetActive(false);
        }
        else
        {
            SetLoadingVisible(true);
        }

        ClearUISelectionAndArrows();

        _armDisableNewSceneUI = true;
        SceneManager.sceneLoaded += HandleSceneLoadedDisableUI;

        RestoreActiveSceneUI();
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        SetHintsVisible(false);
        SetLoadingVisible(false);

        runtimeMat.SetFloat("_Progress", 1f);
        yield return Animate(1f, 0f, revealDuration, revealEase);

        RestoreNewSceneUI();
        overlay.raycastTarget = false;

        ForceUnpaused();

        overlay.enabled = false;
        ClearUISelectionAndArrows();

        isRunning = false;
    }

    IEnumerator CoverLoadRevealOptions(int scene, bool showHintsDuringLoad, bool showContinueButton)
    {
        if (isRunning) yield break;
        isRunning = true;

        ApplyLook();

        runtimeMat.SetFloat("_Progress", 0f);
        overlay.enabled = true;
        overlay.raycastTarget = true;

        SetLoadingVisible(false);
        SetHintsVisible(false);
        yield return Animate(0f, 1f, coverDuration, coverEase);

        SetLoadingVisible(true);
        if (showHintsDuringLoad) SetHintsVisible(true); else SetHintsVisible(false);

        if (waitAfterCoverSeconds > 0f)
            yield return new WaitForSecondsRealtime(waitAfterCoverSeconds);

        var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        float shownAt = Time.realtimeSinceStartup;
        while (op.progress < 0.9f) yield return null;

        float elapsed = Time.realtimeSinceStartup - shownAt;
        float hold = Mathf.Max(0f, minLoadingDisplaySeconds - elapsed) + Mathf.Max(0f, loadDelaySeconds);
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        if (showContinueButton)
        {
            SetLoadingVisible(false);
            if (overlay != null) overlay.raycastTarget = false;
            if (continueButton == null) continueButton = BuildContinueButton();
            continueButton.transform.SetAsLastSibling();
            continueButton.interactable = true;
            continueButton.gameObject.SetActive(true);
            DisableAllSelectablesExceptContinue();
            _continueClicked = false;
            var es = EventSystem.current;
            if (es != null)
            {
                UIHoverArrow.KeyboardMode = true;
                es.SetSelectedGameObject(continueButton.gameObject);
            }
            while (!_continueClicked) yield return null;
            ClearUISelectionAndArrows();
            continueButton.gameObject.SetActive(false);
        }
        else
        {
            SetLoadingVisible(true);
        }

        ClearUISelectionAndArrows();

        _armDisableNewSceneUI = true;
        SceneManager.sceneLoaded += HandleSceneLoadedDisableUI;

        RestoreActiveSceneUI();
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        SetHintsVisible(false);
        SetLoadingVisible(false);

        runtimeMat.SetFloat("_Progress", 1f);
        yield return Animate(1f, 0f, revealDuration, revealEase);

        RestoreNewSceneUI();
        overlay.raycastTarget = false;

        ForceUnpaused();

        overlay.enabled = false;
        ClearUISelectionAndArrows();

        isRunning = false;
    }

    IEnumerator Animate(float from, float to, float dur, AnimationCurve curve)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float p = Mathf.Lerp(from, to, curve.Evaluate(Mathf.Clamp01(t)));
            runtimeMat.SetFloat("_Progress", p);
            yield return null;
        }
        runtimeMat.SetFloat("_Progress", to);
    }

    void ShuffleHints()
    {
        if (hints == null || hints.Length <= 1) return;
        var rng = new System.Random(unchecked((int)System.DateTime.Now.Ticks));
        for (int i = hints.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = hints[i];
            hints[i] = hints[j];
            hints[j] = tmp;
        }
        currentHintIndex = 0;
    }

    void EnsureSingleEventSystemFor(Scene scene)
    {
        var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (all == null || all.Length == 0) return;

        EventSystem keep = null;
        foreach (var es in all)
            if (es && es.gameObject.scene == scene) { keep = es; break; }
        if (keep == null) keep = all[0];

        foreach (var es in all)
            if (es && es != keep) Destroy(es.gameObject);

        var legacy = keep.GetComponent<StandaloneInputModule>();
        if (legacy) Destroy(legacy);
        if (keep.GetComponent<InputSystemUIInputModule>() == null)
            keep.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    static void ForceUnpaused()
    {
        if (Time.timeScale != 1f) Time.timeScale = 1f;
        if (AudioListener.pause) AudioListener.pause = false;
    }
}
