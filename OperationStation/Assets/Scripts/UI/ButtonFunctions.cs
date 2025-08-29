using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    [SerializeField] AudioSource clickSource;
    [SerializeField] AudioSource hoverInSource;
    [SerializeField] AudioSource hoverOutSource;

    void Start() { }
    void Update() { }

    public void PlayClick()
    {
        clickSource.Play();
    }

    public void PlayHoverIn()
    {
        if (hoverInSource.isPlaying == false)
            hoverInSource.Play();
    }

    public void PlayHoverOut()
    {
        if (hoverOutSource.isPlaying == false)
            hoverOutSource.Play();
    }

    public void Resume()
    {
        PlayClick();
        LevelUIManager.instance.StateUnpause();
    }

    public void Quit()
    {
        PlayClick();
        StartCoroutine(QuitGameWaitForSourceToFinish(clickSource));
    }

    public void Restart()
    {
        PlayClick();
        StartCoroutine(RestartWaitForSourceToFinish(clickSource));
    }

    public void LoadScene(int scene)
    {
        PlayClick();
        StartCoroutine(LoadSceneWaitForSourceToFinish(clickSource, scene));
    }

    IEnumerator RestartWaitForSourceToFinish(AudioSource playingSource)
    {
        yield return new WaitForSecondsRealtime(playingSource.clip.length);
        SceneTransition.RunNoHints(SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;
    }

    IEnumerator LoadSceneWaitForSourceToFinish(AudioSource playingSource, int scene)
    {
        yield return new WaitForSecondsRealtime(playingSource.clip.length);
        LevelUIManager.instance.StateUnpause();
        Time.timeScale = 1f;  // ensure not paused
        SceneTransition.RunNoHints(scene);
    }

    IEnumerator QuitGameWaitForSourceToFinish(AudioSource playingSource)
    {
        yield return new WaitForSecondsRealtime(playingSource.clip.length);

#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    public void SetActiveMenu(GameObject menuActive)
    {
        LevelUIManager.instance.SetActiveMenu(menuActive);
    }

    public void RemoveActiveMenu()
    {
        LevelUIManager.instance.RemoveActiveMenu();
    }
}
