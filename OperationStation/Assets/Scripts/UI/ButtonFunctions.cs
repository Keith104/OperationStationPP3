using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    [SerializeField] AudioSource uiSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlaySource()
    {
        uiSource.Play();
    }

    public void Resume()
    {
        PlaySource();
        LevelUIManager.instance.StateUnpause();
    }

    public void Quit()
    {
        PlaySource();
        StartCoroutine(QuitGameWaitForSourceToFinish(uiSource));
    }

    public void Restart()
    {
        PlaySource();
        StartCoroutine(RestartWaitForSourceToFinish(uiSource));
    }

    public void LoadScene(int scene)
    {
        PlaySource();
        StartCoroutine(LoadSceneWaitForSourceToFinish(uiSource, scene));
    }

    IEnumerator RestartWaitForSourceToFinish(AudioSource playingSource)
    {
        yield return new WaitForSeconds(playingSource.clip.length);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        LevelUIManager.instance.StateUnpause();
    }
    IEnumerator LoadSceneWaitForSourceToFinish(AudioSource playingSource, int scene)
    {
        yield return new WaitForSeconds(playingSource.clip.length);
        SceneManager.LoadScene(scene);
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
}
