using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Material (UI/WipeURP)")]
    public Material wipeMaterial;

    [Header("Timings")]
    public float coverDuration = 0.8f;
    public float revealDuration = 0.8f;
    public AnimationCurve coverEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve revealEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Look")]
    [Range(0, 0.1f)] public float edgeSoftness = 0.02f;
    public bool clockwise = true;
    [Range(0, 1)] public float centerX = 0.5f, centerY = 0.5f;
    [Range(-180, 180)] public float startAngleDeg = 90f; // top

    [Header("UI")]
    public int sortingOrder = 32760;

    Canvas canvas;
    RawImage overlay;
    Material runtimeMat;
    bool isRunning;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);                                        // persist overlay across loads :contentReference[oaicite:4]{index=4}

        canvas = new GameObject("SceneTransitionCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;                    // UI above scene :contentReference[oaicite:5]{index=5}
        canvas.sortingOrder = sortingOrder;
        DontDestroyOnLoad(canvas.gameObject);

        overlay = new GameObject("SceneTransitionOverlay",
                 typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        overlay.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)overlay.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        overlay.raycastTarget = false;

        runtimeMat = new Material(wipeMaterial);
        overlay.material = runtimeMat;
        ApplyLook();
        runtimeMat.SetFloat("_Progress", 0f); // start open
        overlay.enabled = false;
    }

    void ApplyLook()
    {
        runtimeMat.SetFloat("_Edge", edgeSoftness);
        runtimeMat.SetFloat("_Clockwise", clockwise ? 1f : 0f);
        runtimeMat.SetVector("_Center", new Vector4(centerX, centerY, 0, 0));
        runtimeMat.SetFloat("_StartAngleDeg", startAngleDeg);
    }

    public static void Run(string sceneName)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.CoverLoadReveal(sceneName));
    }

    IEnumerator CoverLoadReveal(string sceneName)
    {
        if (isRunning) yield break;  // prevent double triggers
        isRunning = true;
        ApplyLook();

        // --- COVER: open(0) -> black(1)
        runtimeMat.SetFloat("_Progress", 0f);  // force known start state
        overlay.enabled = true;
        yield return Animate(0f, 1f, coverDuration, coverEase);

        // --- LOAD new scene (Single)
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // async load :contentReference[oaicite:6]{index=6}
        while (!op.isDone) yield return null;

        // --- REVEAL: black(1) -> open(0)
        runtimeMat.SetFloat("_Progress", 1f);  // avoid any flash
        yield return Animate(1f, 0f, revealDuration, revealEase);
        overlay.enabled = false;

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
}
