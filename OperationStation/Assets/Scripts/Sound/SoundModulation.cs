using UnityEngine;

public class SoundModulation : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ModulateSound(int newPitch)
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
