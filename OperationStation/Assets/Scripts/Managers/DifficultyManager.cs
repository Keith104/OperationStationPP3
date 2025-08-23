using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager instance { get; private set; }
    public List<DifficultySO> allDifficulties = new List<DifficultySO>();
    [SerializeField] List<Button> allButtons = new List<Button>();
    public DifficultySO currentDifficulty;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(allButtons.Count > 0)
        {
            //FindDiffButtons();
            SetLocks();
        }
    }
    void FindDiffButtons()
    {
        int diffDex = 0;
        foreach (Button button in GameObject.FindWithTag("DifficultyButtons").GetComponentsInChildren<Button>())
        {
            allButtons.Add(button);
            diffDex++;
        }
    }
    public void SetLocks()
    {
        int buttDex = 0;
        foreach (DifficultySO diff in allDifficulties)
        {
            if (diff.isLocked == false)
                allButtons[buttDex].interactable = true;
            else
                allButtons[buttDex].interactable = false;

            buttDex++;
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
