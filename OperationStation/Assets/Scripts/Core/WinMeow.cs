using UnityEngine;
using UnityEngine.Timeline;

public class WinMeow : MonoBehaviour
{
    [SerializeField] Collider meowCollider;

    void Start()
    {
        meowCollider.enabled = false;
    }

    public void EnableCollider()
    {
        if (meowCollider != null)
            meowCollider.enabled = true;
    }
}
