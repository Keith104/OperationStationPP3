using UnityEngine;

public class SoundModulation : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    [Header("Modulation On Start")]
    [SerializeField] bool onStart;
    [SerializeField] float randomMin;
    [SerializeField] float randomMax;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (onStart == true)
            ModulateSound(Random.Range(randomMin, randomMax));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ModulateSound(float newPitch)
    {
        if (newPitch < -3 || newPitch > 3)
        {
            Debug.Log("ModulateSound is out of range");
        }
        else
        {
            audioSource.pitch = newPitch;
        }
    }
}
