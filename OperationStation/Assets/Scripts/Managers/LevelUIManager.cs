using TMPro;
using UnityEngine;

public class LevelUIManager : MonoBehaviour
{
    public static LevelUIManager instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    public RectTransform minimap;
    public GameObject menuWin;
    public GameObject menuLose;

    private bool isPaused;
    private float timescaleOrig;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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

    }
    public void StatePause()
    {
        isPaused = !isPaused;
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StateUnpause()
    {
        isPaused = !isPaused;
        Time.timeScale = timescaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        menuActive.SetActive(false);
        menuActive = null;
    }
}
