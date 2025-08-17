#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button exitButton;
    [SerializeField] Button[] menuButtons;

    [Header("Menu UI")]
    [SerializeField] CanvasGroup menuGroup;

    [Header("References")]
    [SerializeField] GameObject creditsMenu;
    [SerializeField] CanvasGroup creditsCanvasGroup;

    [SerializeField] float fadeSeconds = 0.75f;

    void Start()
    {
#if UNITY_WEBGL
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
#endif
        if (menuGroup != null)
        {
            menuGroup.alpha = 1f;
            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
        }

        // Ensure buttons start interactable
        SetMenuButtonsInteractable(true);
    }

    public void PlayButton(string sceneName)
    {
        SetMenuButtonsInteractable(false);
        SceneTransition.Run(sceneName);
    }

    public void CreditsButton()
    {
        if (creditsMenu == null || creditsCanvasGroup == null) return;

        SetMenuButtonsInteractable(false);

        creditsCanvasGroup.alpha = 0f;
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
            creditsCanvasGroup.alpha = a;
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

    // Called by this manager and by CreditsScroller when credits finish.
    public void SetMenuButtonsInteractable(bool interactable)
    {
        if (menuButtons == null) return;
        foreach (var btn in menuButtons)
        {
            if (btn != null) btn.interactable = interactable;
        }
    }
}
