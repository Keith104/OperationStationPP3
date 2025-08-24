using UnityEngine;

public class DefencePreview : MonoBehaviour
{
    public bool isDefenceBuildActive;
    private GameObject preview;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }
    // Update is called once per frame
    void Update()
    {
        if (isDefenceBuildActive) 
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit);
            GameObject hitObject = hit.collider.gameObject;
            Tile tile = hitObject.GetComponent<Tile>();
            Defence defence = hitObject.GetComponent<Defence>();
            Module module = hitObject.GetComponent<Module>();

            if(tile == null && defence == null && module == null)
            {
                preview.SetActive(true);
                preview.transform.position = hit.point + hit.normal * 0.5f;
            }
            else
            {
                preview.SetActive(false);
            }

            

        }

        if(preview != null && isDefenceBuildActive == false)
        {
            Destroy(preview);
        }
    }

    public void PreviewDefence(GameObject defence)
    {
        BoxCollider defenceCollider = defence.GetComponent<BoxCollider>();
        defenceCollider.enabled = false;
        isDefenceBuildActive = true;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Physics.Raycast(ray, out hit);
        Vector3 spawnLocation = hit.point + hit.normal * 0.5f;

        preview = Instantiate(defence, spawnLocation, Quaternion.identity);
    }
}
