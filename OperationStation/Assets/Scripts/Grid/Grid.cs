using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [SerializeField] Module module;
    [SerializeField] Tile tilePrefab;

    [SerializeField] Renderer modelOne;
    [SerializeField] Renderer modelTwo;

    public Material highlight;

    [SerializeField] float cellSize = 1f;
    [SerializeField] float checkRadius = 0.4f;
    [SerializeField] float refreshInterval = 0.15f;

    Tile rightTile, leftTile, upTile, downTile;
    float nextRefreshAt;

    void Start()
    {
        ForceRefreshImmediate();
        nextRefreshAt = Time.unscaledTime + refreshInterval;
    }

    void Update()
    {
        if (Time.unscaledTime >= nextRefreshAt)
        {
            RefreshTiles();
            nextRefreshAt = Time.unscaledTime + refreshInterval;
        }

        bool shouldShow = ObjectSpawner.awaitingPlacement && !ObjectSpawner.isDefence;
        SetTilesActive(shouldShow);
    }

    void RefreshTiles()
    {
        if (!module || !tilePrefab) return;

        Vector3 c = module.transform.position;

        ManageOne(ref rightTile, c + new Vector3(+cellSize, 0f, 0f));
        ManageOne(ref leftTile, c + new Vector3(-cellSize, 0f, 0f));
        ManageOne(ref upTile, c + new Vector3(0f, 0f, +cellSize));
        ManageOne(ref downTile, c + new Vector3(0f, 0f, -cellSize));
    }

    void ManageOne(ref Tile slot, Vector3 pos)
    {
        bool occupied = HasModuleAt(pos);
        if (!occupied)
        {
            if (slot == null)
            {
                slot = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                slot.name = $"Tile_{Mathf.RoundToInt(pos.x)}_{Mathf.RoundToInt(pos.z)}";
            }
        }
        else
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
                slot = null;
            }
        }
    }

    bool HasModuleAt(Vector3 pos)
    {
        var hits = Physics.OverlapSphere(pos, checkRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i]) continue;
            var m = hits[i].GetComponentInParent<Module>();
            if (m && m != module) return true;
        }
        return false;
    }

    void SetTilesActive(bool v)
    {
        if (rightTile) rightTile.gameObject.SetActive(v);
        if (leftTile) leftTile.gameObject.SetActive(v);
        if (upTile) upTile.gameObject.SetActive(v);
        if (downTile) downTile.gameObject.SetActive(v);
    }

    public void ForceRefreshImmediate()
    {
        RefreshTiles();
        SetTilesActive(ObjectSpawner.awaitingPlacement && !ObjectSpawner.isDefence);
    }

    void OnMouseEnter()
    {
        bool placementActive = ObjectSpawner.awaitingPlacement;
        bool isDef = ObjectSpawner.isDefence;
        if (!placementActive && !isDef && modelOne && modelTwo)
        {
            modelOne.material = highlight;
            modelTwo.material = highlight;
        }
    }

    void OnMouseExit()
    {
        var r = GetComponent<Renderer>();
        if (r && modelOne && modelTwo)
        {
            modelOne.material = r.material;
            modelTwo.material = r.material;
        }
    }
}
