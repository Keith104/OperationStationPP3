using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundSendUp : MonoBehaviour
{
    [SerializeField] AudioSource AudioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.SetParent(transform.parent.parent, true);
        StartCoroutine(DestroyWhenFinished(AudioSource));
    }
    IEnumerator DestroyWhenFinished(AudioSource playingSource)
    {
        yield return new WaitForSeconds(playingSource.clip.length);
        Destroy(gameObject);
    }
}
