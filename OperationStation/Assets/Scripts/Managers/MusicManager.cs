using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioSource outgoingSource;
    [SerializeField] AudioSource incomingSource;
    [SerializeField] AudioClip[] musicClips;

    [SerializeField] bool fadeNewClipIn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        NearEnd();
        if (fadeNewClipIn == true)
        {
            CrossFade();
        }
    }

    void NearEnd()
    {
        float length = outgoingSource.clip.length;
        if (outgoingSource.time > length - 1)
            fadeNewClipIn = true;
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
}
