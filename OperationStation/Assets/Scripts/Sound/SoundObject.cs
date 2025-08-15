using UnityEngine;

public class SoundObject : MonoBehaviour
{
    [SerializeField] AudioSource objectInSource;
    [SerializeField] AudioSource objectOutSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void PlayObjectIn()
    {
        if (objectInSource.isPlaying == false)
            objectInSource.Play();
    }

    public void PlayObjectOut()
    {
        if (objectOutSource.isPlaying == false)
            objectOutSource.Play();
    }
}
