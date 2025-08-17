using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] Material available;
    [SerializeField] Material unAvailable;
    

    void OnMouseEnter()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        bool placementActive = ObjectSpawner.awaitingPlacement;
        if (objectRenderer != null && available != null && placementActive)
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
