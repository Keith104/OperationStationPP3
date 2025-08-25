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

    readonly Dictionary<GraphicRaycaster, bool> raycasterCache = new();
    readonly Dictionary<Selectable, bool> selectableCache = new();
    readonly List<SimpleMenuNavigator> disabledNavigators = new();

    bool paused;

    InputAction pauseAction;
    bool createdRuntimeAction;

    CursorLockMode prevLockState = CursorLockMode.None;
    bool prevCursorVisible = true;

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

        if (paused)
        {
            // If we’re unloading the scene, don’t try to restore destroyed objects
            if (gameObject.scene.isLoaded)
                UnlockOtherUI();
            else
            {
                raycasterCache.Clear();
                selectableCache.Clear();
                disabledNavigators.Clear();
            }
        }
    }

    void BuildPauseAction()
    {
        var ia = new InputAction(name: "Pause", type: InputActionType.Button);
        ia.AddBinding("<Gamepad>/start");
#if UNITY_WEBGL
        ia.AddBinding("<Keyboard>/p");
#else
        ia.AddBinding("<Keyboard>/escape");
#endif
        pauseAction = ia;
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
            prevLockState = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;

            if (pauseMenu != null)
            {
                pauseMenu.SetActive(true);
                AddToActiveMenus(pauseMenu);
                LockOtherUI(pauseMenu.transform);

                var firstSel = pauseMenu.GetComponentInChildren<Selectable>(true);
                EventSystem.current?.SetSelectedGameObject(firstSel ? firstSel.gameObject : pauseMenu);
            }
        }
        else
        {
            Time.timeScale = 1f;

            Cursor.lockState = prevLockState;
            Cursor.visible = prevCursorVisible;

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
        // Ensure we leave paused state before switching scenes
        if (paused)
        {
            paused = false;
            Time.timeScale = 1f;
            Cursor.lockState = prevLockState;
            Cursor.visible = prevCursorVisible;

            foreach (GameObject m in activeMenus) if (m) m.SetActive(false);
            activeMenus.Clear();
            UnlockOtherUI();
            EventSystem.current?.SetSelectedGameObject(null);
        }

        Debug.Log("Loading Main Menu");
        SceneTransition.RunNoHints(sceneName);
#else
        if (quitConfirmPopup != null)
        {
            quitConfirmPopup.SetActive(true);
            foreach (GameObject m in activeMenus) if (m) m.SetActive(false);
            activeMenus.Clear();
            AddToActiveMenus(quitConfirmPopup);
            LockOtherUI(quitConfirmPopup.transform);

            var firstSel = quitConfirmPopup.GetComponentInChildren<Selectable>(true);
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

    void LockOtherUI(Transform keepRoot)
    {
        raycasterCache.Clear();
        selectableCache.Clear();
        disabledNavigators.Clear();

        var allRaycasters = Object.FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var gr in allRaycasters)
        {
            if (!gr) continue;
            if (IsKeptForRaycaster(gr.transform, keepRoot)) continue;
            raycasterCache[gr] = gr.enabled;
            gr.enabled = false;
        }

        var allSelectables = Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sel in allSelectables)
        {
            if (!sel) continue;
            if (IsKeptForInteractable(sel.transform, keepRoot)) continue;
            selectableCache[sel] = sel.interactable;
            sel.interactable = false;
        }

        var allNavs = Object.FindObjectsByType<SimpleMenuNavigator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var nav in allNavs)
        {
            if (!nav) continue;
            if (IsKeptForInteractable(nav.transform, keepRoot)) continue;
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
        {
            var sel = kv.Key;
            if (sel == null) continue;
            try { sel.interactable = kv.Value; }
            catch { /* ignore destroyed objects */ }
        }
        selectableCache.Clear();

        foreach (var nav in disabledNavigators)
            if (nav) nav.enabled = true;
        disabledNavigators.Clear();
    }

    bool IsKeptForRaycaster(Transform t, Transform keepRoot)
    {
        if (!t || !keepRoot) return false;
        if (t == keepRoot || t.IsChildOf(keepRoot) || keepRoot.IsChildOf(t))
            return true;
        for (int i = 0; i < neverLockRoots.Count; i++)
        {
            var r = neverLockRoots[i];
            if (!r) continue;
            if (t == r || t.IsChildOf(r) || r.IsChildOf(t))
                return true;
        }
        return false;
    }

    bool IsKeptForInteractable(Transform t, Transform keepRoot)
    {
        if (!t) return false;
        if (keepRoot && (t == keepRoot || t.IsChildOf(keepRoot)))
            return true;
        for (int i = 0; i < neverLockRoots.Count; i++)
        {
            var r = neverLockRoots[i];
            if (r && (t == r || t.IsChildOf(r)))
                return true;
        }
        return false;
    }
}
