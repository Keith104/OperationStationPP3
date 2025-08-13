using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance { get; private set; }
    [Header("CrossFade Sources")]
    [SerializeField] AudioSource outgoingSource;
    [SerializeField] AudioSource incomingSource;

    [Header("AudioClips")]
    [SerializeField] AudioClip[] neutralMusicClips;
    [SerializeField] AudioClip[] aggressiveMusicClips;

    [Header("Tracker")]
    public int enemiesSeen;

    [Header("Dynamic Sight")]
    [SerializeField] int seenLimit;
    private bool aggressiveSightSet = false;
    private bool neutralSightSet = true;

    [Header("Misc.")]
    [SerializeField] bool fadeNewClipIn;
    public bool isWatchingFight;
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
        PlayNewNeutral();
    }

    // Update is called once per frame
    void Update()
    {
        if(outgoingSource.clip != null)
            NearEnd();
        if (fadeNewClipIn == true)
        {
            CrossFade();
        }

        CombatWatcher();
    }

    void NearEnd()
    {
        float length = outgoingSource.clip.length;
        if (outgoingSource.time > length - 1)
        {
            if (isWatchingFight == false)
                PlayNewNeutral();
            else
                PlayNewAggressive();
        }
    }

    void CrossFade()
    {

        if (incomingSource.volume == 0)
            incomingSource.Play();

        if (incomingSource.volume < 1)
            incomingSource.volume += 0.001f;
        if (outgoingSource.volume > 0)
            outgoingSource.volume -= 0.001f;

        if(incomingSource.volume == 1 && outgoingSource.volume == 0)
        {
            outgoingSource.volume = 1;
            incomingSource.volume = 0;

            outgoingSource.clip = incomingSource.clip;
            outgoingSource.Play();
            outgoingSource.time = incomingSource.time;
            fadeNewClipIn = false;
        }
    }

    void CombatWatcher()
    {
        if (enemiesSeen > seenLimit)
        {
            if (aggressiveSightSet == false)
            {
                MusicManager.instance.isWatchingFight = true;
                MusicManager.instance.PlayNewAggressive();
                aggressiveSightSet = true;
            }
            neutralSightSet = false;
        }
        else
        {
            if (neutralSightSet == false)
            {
                MusicManager.instance.isWatchingFight = false;
                MusicManager.instance.PlayNewNeutral();
                neutralSightSet = true;
            }
            aggressiveSightSet = false;
        }
    }

    public void PlayNewNeutral()
    {
        incomingSource.clip = neutralMusicClips[Random.Range(0, neutralMusicClips.Length)];
        incomingSource.Play();
        fadeNewClipIn = true;
    }
    public void PlayNewAggressive()
    {
        incomingSource.clip = aggressiveMusicClips[Random.Range(0, aggressiveMusicClips.Length)];
        incomingSource.Play();
        fadeNewClipIn = true;
    }
}
