using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject quitConfirmPopup;

    [SerializeField] List<GameObject> activeMenus;


    bool paused;

    private void Update()
    {
#if UNITY_WEBGL
        if(Input.GetKeyDown(KeyCode.P))
        {
            Pause();
        }
#else
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
#endif
    }

    void Pause()
    {
        paused = !paused;

        if(paused)
        {
            Time.timeScale = 0f;
            pauseMenu.SetActive(true);
            AddToActiveMenus(pauseMenu);
        }
        else
        {
            Time.timeScale = 1f;
            foreach(GameObject menu in activeMenus)
            {
                menu.SetActive(false);
                
            }

            activeMenus.Clear();

        }
    }

    // Public methods
    public void ResumeButton()
    {
        Pause();
    }

    public void QuitButton(string sceneName)
    {
#if UNITY_WEBGL
        SceneTransition.RunNoHints(sceneName);
#else
        if (quitConfirmPopup != null)
        {
            quitConfirmPopup.SetActive(true);
            foreach(GameObject menu in activeMenus)
            {
                menu.SetActive(false);
            }
            activeMenus.Clear();
            AddToActiveMenus(quitConfirmPopup);

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
        if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
#else
        Application.Quit();
#endif
    }
    public void ToMainMenuButton(string sceneName)
    {
        SceneTransition.RunNoHints(sceneName);
    }

    public void AddToActiveMenus(GameObject menuToAdd)
    {
        activeMenus.Add(menuToAdd);
    }

    public void RemoveFromActiveMenus(GameObject menuToRemove)
    {
        activeMenus.Remove(menuToRemove);
    }
}
