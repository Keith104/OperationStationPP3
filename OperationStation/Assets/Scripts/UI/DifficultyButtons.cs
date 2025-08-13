using UnityEngine;

public class DifficultyButtons : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SetDiffFromHere(int index)
    {
        DifficultyManager.instance.SetDifficulty(DifficultyManager.instance.allDifficulties[index]);
    }
}
