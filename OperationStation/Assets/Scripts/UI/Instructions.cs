using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Instructions : MonoBehaviour
{

    [SerializeField] List<string> textBlocks = new List<string>();
    [SerializeField] TextMeshProUGUI textDisplay;
    [SerializeField] Button ContinueButton;
    [SerializeField] Button BackButton;

    private int currentBlock = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textDisplay.text = textBlocks[currentBlock];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CycleForward()
    {
        if (currentBlock < textBlocks.Count)
        {
            ContinueButton.enabled = true;
            BackButton.enabled = true;
            currentBlock++;
            textDisplay.text = textBlocks[currentBlock];
        }
        else
            ContinueButton.enabled = false;
    }
    public void CycleBack()
    {
        if (currentBlock > 0)
        {
            ContinueButton.enabled = true;
            BackButton.enabled = true;
            currentBlock--;
            textDisplay.text = textBlocks[currentBlock];
        }
        else
            BackButton.enabled = false;
    }

    public void StateUnpause()
    {
        gameObject.SetActive(false);
    }
}
