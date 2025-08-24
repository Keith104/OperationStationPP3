using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject quitConfirmPopup;

    [SerializeField] List<GameObject> activeMenus;

    readonly Dictionary<GraphicRaycaster, bool> raycasterCache = new();
    readonly Dictionary<Selectable, bool> selectableCache = new();
    readonly List<SimpleMenuNavigator> disabledNavigators = new();

    bool paused;

    private void Update()
    {
#if UNITY_WEBGL
        if (Input.GetKeyDown(KeyCode.P)) Pause();
#else
        if (Input.GetKeyDown(KeyCode.Escape)) Pause();
#endif
    }

    void Pause()
    {
        paused = !paused;

        if (paused)
        {
            Time.timeScale = 0f;
            pauseMenu.SetActive(true);
            AddToActiveMenus(pauseMenu);
            LockOtherUI(pauseMenu.transform);
        }
        else
        {
            Time.timeScale = 1f;
            foreach (GameObject m in activeMenus) m.SetActive(false);
            activeMenus.Clear();
            UnlockOtherUI();
        }

        EventSystem.current?.SetSelectedGameObject(paused ? pauseMenu : null);
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
            foreach (GameObject m in activeMenus) m.SetActive(false);
            activeMenus.Clear();
            AddToActiveMenus(quitConfirmPopup);
            LockOtherUI(quitConfirmPopup.transform);
            EventSystem.current?.SetSelectedGameObject(quitConfirmPopup);
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

    public void AddToActiveMenus(GameObject menuToAdd) => activeMenus.Add(menuToAdd);
    public void RemoveFromActiveMenus(GameObject menuToRemove) => activeMenus.Remove(menuToRemove);

    void LockOtherUI(Transform keepRoot)
    {
        raycasterCache.Clear();
        selectableCache.Clear();
        disabledNavigators.Clear();

        var allRaycasters = Object.FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var gr in allRaycasters)
        {
            if (gr == null) continue;
            if (keepRoot != null && gr.transform.IsChildOf(keepRoot)) continue;
            raycasterCache[gr] = gr.enabled;
            gr.enabled = false;
        }

        var allSelectables = Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sel in allSelectables)
        {
            if (sel == null) continue;
            if (keepRoot != null && sel.transform.IsChildOf(keepRoot)) continue;
            selectableCache[sel] = sel.interactable;
            sel.interactable = false;
        }

        var allNavs = Object.FindObjectsByType<SimpleMenuNavigator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var nav in allNavs)
        {
            if (nav == null) continue;
            if (keepRoot != null && nav.transform.IsChildOf(keepRoot)) continue;
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
}
