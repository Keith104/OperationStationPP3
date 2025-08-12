using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioSource outgoingSource;
    [SerializeField] AudioSource incomingSource;
    [SerializeField] AudioClip[] musicClips;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FadeIn()
    {
        if(incomingSource.volume < 1)
            incomingSource.volume += 0.1f;
    }

    void FadeOut()
    {

    }
}
