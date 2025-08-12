using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField] GameObject module;
    [SerializeField] Tile _tilePrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnAvailableSpaces();
    }

    // Update is called once per frame
    void spawnAvailableSpaces()
    {
        var spawnTileRight = Instantiate(_tilePrefab, new Vector3(module.transform.localScale.x + 5, 
            module.transform.localScale.y, module.transform.localScale.z), Quaternion.identity);

        var spawnTileLeft = Instantiate(_tilePrefab, new Vector3(module.transform.localScale.x - 5,
            module.transform.localScale.y, module.transform.localScale.z), Quaternion.identity);

        var spawnTileUp = Instantiate(_tilePrefab, new Vector3(module.transform.localScale.x,
            module.transform.localScale.y, module.transform.localScale.z + 5), Quaternion.identity);

        var spawnTileDown = Instantiate(_tilePrefab, new Vector3(module.transform.localScale.x - 5,
            module.transform.localScale.y, module.transform.localScale.z - 5), Quaternion.identity);
    }
}
