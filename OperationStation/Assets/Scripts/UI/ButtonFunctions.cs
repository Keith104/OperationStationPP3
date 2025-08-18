using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    [SerializeField] AudioSource clickSource;
    [SerializeField] AudioSource hoverInSource;
    [SerializeField] AudioSource hoverOutSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlayClick()
    {
        clickSource.Play();
    }

    public void PlayHoverIn()
    {
        if(hoverInSource.isPlaying == false)
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
        yield return new WaitForSeconds(playingSource.clip.length);
        SceneTransition.RunNoHints(SceneManager.GetActiveScene().name);
        LevelUIManager.instance.StateUnpause();
    }
    IEnumerator LoadSceneWaitForSourceToFinish(AudioSource playingSource, int scene)
    {
        yield return new WaitForSeconds(playingSource.clip.length);
        SceneTransition.RunNoHints(scene);
        LevelUIManager.instance.StateUnpause();
    }
    IEnumerator QuitGameWaitForSourceToFinish(AudioSource playingSource)
    {
        yield return new WaitForSeconds(playingSource.clip.length);

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
}
