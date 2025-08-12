using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    [Header("Camera References")]
    public GameObject playerCamera;
    public GameObject minimapCamera;

    [Header("Enemy Stats")]
    public float health;
    public float damage;
    public float attackCooldown;
    public GameObject Bullet;
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
        playerCamera = Camera.main.gameObject;
        minimapCamera = GameObject.FindWithTag("MinimapCamera");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
