using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] Material available;
    [SerializeField] Material unAvailable;


    void OnMouseEnter()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        if(objectRenderer != null && available != null)
        {
            objectRenderer.material = available;
        }
    }

    void OnMouseExit()
    {
        Renderer objectRenderer = GetComponent<Renderer>();

        objectRenderer.material = unAvailable;
    }
}
