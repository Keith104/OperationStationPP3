using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{

    [Header("Buttons")]
    [SerializeField] Button exitButton;



    private void Start()
    {

#if UNITY_WEBGL
    exitButton.gameObject.SetActive(false);
#endif
    }

    public void PlayButton(int sceneIndexToLoad)
    {
        SceneManager.LoadScene(sceneIndexToLoad);
    }

    public void ExitButton()
    {
        #if UNITY_EDITOR
               if(EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }
        #else
                Application.Quit();
        #endif
    }
}
