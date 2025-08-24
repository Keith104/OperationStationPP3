using System.Collections.Generic;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject quitConfirmPopup;

    [Tooltip("Menus that should remain interactive even while paused (eg. debug HUD, streaming chat, watermark buttons). Add their root Transforms here.")]
    [SerializeField] List<Transform> neverLockRoots = new();

    [SerializeField] List<GameObject> activeMenus = new();

    [Header("Input (Optional)")]
    [Tooltip("Reference to an Input Actions asset 'Pause' action. Bind whatever you like; this script filters to allowed controls per platform. If empty, a runtime action is created.")]
    [SerializeField] InputActionReference pauseActionRef;

    readonly Dictionary<GraphicRaycaster, bool> raycasterCache = new();
    readonly Dictionary<Selectable, bool> selectableCache = new();
    readonly List<SimpleMenuNavigator> disabledNavigators = new();

    bool paused;

    // Input System
    InputAction pauseAction;
    bool createdRuntimeAction;

    void OnEnable()
    {
        BuildPauseAction();
        pauseAction.performed += OnPausePerformed;
        pauseAction.Enable();
    }

    void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
            if (createdRuntimeAction)
            {
                pauseAction.Dispose();
                pauseAction = null;
                createdRuntimeAction = false;
            }
        }
    }

    // Build an action that only contains the allowed bindings for the current platform.
    void BuildPauseAction()
    {
        bool AllowedPath(string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath)) return false;
            if (controlPath.Contains("<Gamepad>/start")) return true; // Always allow Start

#if UNITY_WEBGL
            // WebGL: allow P; block Escape
            if (controlPath.Contains("<Keyboard>/p")) return true;
            if (controlPath.Contains("<Keyboard>/escape")) return false;
#else
            // Desktop (non-WebGL): allow Escape; block P
            if (controlPath.Contains("<Keyboard>/escape")) return true;
            if (controlPath.Contains("<Keyboard>/p")) return false;
#endif
            return false;
        }

        if (pauseActionRef != null && pauseActionRef.action != null)
        {
            var src = pauseActionRef.action;
            var ia = new InputAction(name: "Pause (Filtered)", type: InputActionType.Button);

            bool addedAny = false;
            foreach (var b in src.bindings)
            {
                if (!b.isComposite && !b.isPartOfComposite && AllowedPath(b.effectivePath))
                {
                    ia.AddBinding(b.effectivePath);
                    addedAny = true;
                }
            }

            if (!addedAny)
            {
#if UNITY_WEBGL
                ia.AddBinding("<Gamepad>/start");
                ia.AddBinding("<Keyboard>/p");
#else
                ia.AddBinding("<Gamepad>/start");
                ia.AddBinding("<Keyboard>/escape");
#endif
            }

            pauseAction = ia;
            createdRuntimeAction = true;
            return;
        }

        var runtime = new InputAction(name: "Pause", type: InputActionType.Button);
        runtime.AddBinding("<Gamepad>/start");
#if UNITY_WEBGL
        runtime.AddBinding("<Keyboard>/p");
#else
        runtime.AddBinding("<Keyboard>/escape");
#endif
        pauseAction = runtime;
        createdRuntimeAction = true;
    }

    void OnPausePerformed(InputAction.CallbackContext _)
    {
        Pause();
    }

    void Pause()
    {
        paused = !paused;

        if (paused)
        {
            Time.timeScale = 0f;
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(true);
                AddToActiveMenus(pauseMenu);
                LockOtherUI(pauseMenu.transform);

                var firstSel = pauseMenu.GetComponentInChildren<Selectable>();
                EventSystem.current?.SetSelectedGameObject(firstSel ? firstSel.gameObject : pauseMenu);
            }
        }
        else
        {
            Time.timeScale = 1f;
            foreach (GameObject m in activeMenus) if (m) m.SetActive(false);
            activeMenus.Clear();
            UnlockOtherUI();
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    public void ResumeButton() => Pause();

    public void QuitButton(string sceneName)
    {
#if UNITY_WEBGL
        SceneTransition.RunNoHints(sceneName);
#else
        if (quitConfirmPopup != null)
        {
            quitConfirmPopup.SetActive(true);
            foreach (GameObject m in activeMenus) if (m) m.SetActive(false);
            activeMenus.Clear();
            AddToActiveMenus(quitConfirmPopup);
            LockOtherUI(quitConfirmPopup.transform);

            var firstSel = quitConfirmPopup.GetComponentInChildren<Selectable>();
            EventSystem.current?.SetSelectedGameObject(firstSel ? firstSel.gameObject : quitConfirmPopup);
        }
        else
        {
            SceneTransition.RunNoHints(sceneName);
        }
#endif
    }

    public void ToDesktopButton()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    public void ToMainMenuButton(string sceneName) =>
        SceneTransition.RunNoHints(sceneName);

    public void AddToActiveMenus(GameObject menuToAdd)
    {
        if (menuToAdd && !activeMenus.Contains(menuToAdd))
            activeMenus.Add(menuToAdd);
    }

    public void RemoveFromActiveMenus(GameObject menuToRemove)
    {
        if (menuToRemove)
            activeMenus.Remove(menuToRemove);
    }

    // ---------- Locking logic with exceptions ----------

    void LockOtherUI(Transform keepRoot)
    {
        raycasterCache.Clear();
        selectableCache.Clear();
        disabledNavigators.Clear();

        var allRaycasters = Object.FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var gr in allRaycasters)
        {
            if (!gr) continue;
            if (IsKept(gr.transform, keepRoot)) continue;
            raycasterCache[gr] = gr.enabled;
            gr.enabled = false;
        }

        var allSelectables = Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sel in allSelectables)
        {
            if (!sel) continue;
            if (IsKept(sel.transform, keepRoot)) continue;
            selectableCache[sel] = sel.interactable;
            sel.interactable = false;
        }

        var allNavs = Object.FindObjectsByType<SimpleMenuNavigator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var nav in allNavs)
        {
            if (!nav) continue;
            if (IsKept(nav.transform, keepRoot)) continue;
            if (nav.enabled)
            {
                disabledNavigators.Add(nav);
                nav.enabled = false;
            }
        }
    }

    void UnlockOtherUI()
    {
        foreach (var kv in raycasterCache)
            if (kv.Key) kv.Key.enabled = kv.Value;
        raycasterCache.Clear();

        foreach (var kv in selectableCache)
            if (kv.Key) kv.Key.interactable = kv.Value;
        selectableCache.Clear();

        foreach (var nav in disabledNavigators)
            if (nav) nav.enabled = true;
        disabledNavigators.Clear();
    }

    // Returns true if 't' is under the active menu OR under any 'neverLockRoots'
    bool IsKept(Transform t, Transform keepRoot)
    {
        if (!t) return false;

        if (keepRoot && t.IsChildOf(keepRoot))
            return true;

        // Skip anything under any "never lock" root
        for (int i = 0; i < neverLockRoots.Count; i++)
        {
            var r = neverLockRoots[i];
            if (r && t.IsChildOf(r))
                return true;
        }
        return false;
    }
}
