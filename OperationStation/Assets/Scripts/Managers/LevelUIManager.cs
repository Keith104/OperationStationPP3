using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUIManager : MonoBehaviour
{
    public static LevelUIManager instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    public RectTransform minimap;
    public GameObject menuWin;
    public GameObject menuLose;
    [SerializeField] GameObject buildButton;
    [SerializeField] GameObject buildModule;
    [SerializeField] GameObject buildDefence;

    private bool isPaused;
    private float timescaleOrig;
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

    public void SetActiveMenu(GameObject activeMenu)
    {
        menuActive.SetActive(false);
        menuActive = activeMenu;
        menuActive.SetActive(true);
    }
}
