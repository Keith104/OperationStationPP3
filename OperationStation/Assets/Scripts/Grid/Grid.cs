using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [SerializeField] Module module;
    [SerializeField] Tile _tilePrefab;

    [SerializeField] Renderer modelOne;
    [SerializeField] Renderer modelTwo;

    public Material highlight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnAvailableSpaces();
    }

    // Update is called once per frame
    public void spawnAvailableSpaces()
    {
        RaycastHit hit;

        if (module.isRightAvailable == true && !Physics.Raycast(transform.position, transform.right, out hit, 10f))
        {
            var spawnTileRight = Instantiate(_tilePrefab, new Vector3(module.transform.position.x + 1,
                module.transform.position.y, module.transform.position.z), 
                Quaternion.identity, transform);
            spawnTileRight.name = $"Right";

        }
        else
        {
            module.isRightAvailable = false;
        }
        if (module.isLeftAvailable == true && !Physics.Raycast(transform.position, -transform.right, out hit, 10f))
        {
            var spawnTileLeft = Instantiate(_tilePrefab, new Vector3(module.transform.position.x - 1,
                module.transform.position.y, module.transform.position.z), 
                Quaternion.identity, transform);
            spawnTileLeft.name = $"Left";
        }
        else
        {
            module.isLeftAvailable = false;
        }
        if (module.isUpAvailable == true && !Physics.Raycast(transform.position, transform.forward, out hit, 10f))
        {
            var spawnTileUp = Instantiate(_tilePrefab, new Vector3(module.transform.position.x,
                module.transform.position.y, module.transform.position.z + 1),
                Quaternion.identity, transform);
            spawnTileUp.name = $"Up";
        }
        else
        {
            module.isUpAvailable = false;
        }
        if (module.isDownAvailable == true && !Physics.Raycast(transform.position, -transform.forward, out hit, 10f))
        {
            var spawnTileDown = Instantiate(_tilePrefab, new Vector3(module.transform.position.x,
                module.transform.position.y, module.transform.position.z - 1), 
                Quaternion.identity, transform);
            spawnTileDown.name = $"Down";
        }
        else
        {
            module.isDownAvailable = false;
        }
    }

    void OnMouseEnter()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        bool placementActive = ObjectSpawner.awaitingPlacement;
        bool isDefence = ObjectSpawner.isDefence;
        if (objectRenderer != null && placementActive == false && isDefence == false)
        {
            modelOne.material = highlight;
            modelTwo.material = highlight;

        }
    }

    void OnMouseExit()
    {
        Renderer objectRenderer = GetComponent<Renderer>();

        modelOne.material = objectRenderer.material;
        modelTwo.material = objectRenderer.material;
    }
}
