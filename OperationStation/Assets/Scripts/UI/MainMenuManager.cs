using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button exitButton;

    [Header("References")]
    [SerializeField] GameObject creditsMenu;
    [SerializeField] CanvasGroup creditsCanvasGroup;

    [SerializeField] float fadeSeconds = 0.75f;

    private void Start()
    {
#if UNITY_WEBGL
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
#endif
    }

    public void PlayButton(int sceneIndexToLoad)
    {
        SceneManager.LoadScene(sceneIndexToLoad);
    }

    public void CreditsButton()
    {
        if (creditsMenu == null || creditsCanvasGroup == null) return;

        creditsCanvasGroup.alpha = 0f;      // start invisible
        creditsMenu.SetActive(true);
        StartCoroutine(CreditsFadeIn());
    }

    IEnumerator CreditsFadeIn()
    {
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = (fadeSeconds <= 0f) ? 1f : Mathf.Clamp01(t / fadeSeconds);
            creditsCanvasGroup.alpha = a;   // 0 -> 1
            yield return null;
        }
        creditsCanvasGroup.alpha = 1f;
    }

    public void ExitButton()
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
}
