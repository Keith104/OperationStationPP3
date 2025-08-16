using UnityEngine;
using UnityEngine.Audio;

public class AsteroidSoundTrigger : MonoBehaviour
{

    [Header("Sound")]
    [SerializeField] SoundBank soundBank;
    [SerializeField] AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Asteroid Collision");
        soundBank.SetSourceToRandomClip();
        audioSource.Play();
    }
}
