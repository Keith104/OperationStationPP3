using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] Material available;
    [SerializeField] Material unAvailable;
<<<<<<< Updated upstream
    
=======

>>>>>>> Stashed changes

    void OnMouseEnter()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
<<<<<<< Updated upstream
        bool placementActive = ObjectSpawner.awaitingPlacement;
        if (objectRenderer != null && available != null && placementActive)
        {
            
=======
        if(objectRenderer != null && available != null)
        {
>>>>>>> Stashed changes
            objectRenderer.material = available;
        }
    }

    void OnMouseExit()
    {
        Renderer objectRenderer = GetComponent<Renderer>();

        objectRenderer.material = unAvailable;
    }
}
