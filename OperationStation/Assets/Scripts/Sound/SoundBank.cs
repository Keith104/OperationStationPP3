using UnityEngine;

public class SoundBank : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip[] audioClips;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSourceToRandomClip()
    {
        audioSource.clip = audioClips[Random.Range(0, audioClips.Length)];
    }
}
