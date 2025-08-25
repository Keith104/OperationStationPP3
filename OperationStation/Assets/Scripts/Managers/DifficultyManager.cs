using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager instance { get; private set; }

    public List<DifficultySO> allDifficulties = new List<DifficultySO>();
    [SerializeField] List<Button> allButtons = new List<Button>();
    public DifficultySO currentDifficulty;

    bool buttonsInitialized = false;
    GameObject difficultyRoot;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        buttonsInitialized = false;
        if (scene.name == "MainMenu")
        {
            FindDiffButtons();
            ApplyLocks();
            WireButtonClicks();
            buttonsInitialized = true;
        }
    }

    void Update()
    {
        if (!buttonsInitialized && SceneManager.GetActiveScene().name == "MainMenu")
        {
            FindDiffButtons();
            ApplyLocks();
            WireButtonClicks();
            buttonsInitialized = true;
        }
    }

    GameObject FindInactiveByTag(string tag)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            var go = allObjects[i];
            if (go != null && go.CompareTag(tag))
                return go;
        }
        return null;
    }

    void FindDiffButtons()
    {
        if (!difficultyRoot)
            difficultyRoot = FindInactiveByTag("DifficultyButtons");

        allButtons.Clear();
        if (!difficultyRoot) return;

        foreach (Transform child in difficultyRoot.transform)
        {
            var btn = child.GetComponent<Button>();
            if (btn) allButtons.Add(btn);
        }
    }

    void ApplyLocks()
    {
        int count = Mathf.Min(allDifficulties.Count, allButtons.Count);
        for (int i = 0; i < count; i++)
        {
            var diff = allDifficulties[i];
            var btn = allButtons[i];
            if (!btn || !diff) continue;
            bool locked = diff.isLocked;
            btn.interactable = !locked;
        }
    }

    void WireButtonClicks()
    {
        int count = Mathf.Min(allDifficulties.Count, allButtons.Count);
        for (int i = 0; i < count; i++)
        {
            var diff = allDifficulties[i];
            var btn = allButtons[i];
            if (!btn || !diff) continue;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (diff.isLocked) return;
                SetDifficulty(diff);
            });
        }
    }

    public void SetDifficulty(DifficultySO newDifficulty)
    {
        currentDifficulty = newDifficulty;
    }

    public DifficultySO GetDifficulty()
    {
        return currentDifficulty;
    }
}
