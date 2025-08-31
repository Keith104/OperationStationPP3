using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] Material available;
    [SerializeField] Material unAvailable;
    [SerializeField] Material invisible;

    void OnMouseEnter()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        bool placementActive = ObjectSpawner.awaitingPlacement;
        bool isDefence = ObjectSpawner.isDefence;
        if (objectRenderer != null && available != null && placementActive == true && isDefence != true)
        {
            objectRenderer.material = available;
        }
        else if(isDefence == true)
        {
            objectRenderer.material = unAvailable;
        }
    }

    void OnMouseExit()
    {
        Renderer objectRenderer = GetComponent<Renderer>();

        objectRenderer.material = invisible;
    }
}
