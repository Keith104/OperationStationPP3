using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField] Module module;
    [SerializeField] Tile _tilePrefab;

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
}
