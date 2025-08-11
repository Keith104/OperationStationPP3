using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class Minimap : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Camera minimapCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        minimapCamera = GameObject.FindWithTag("MinimapCamera").GetComponent<Camera>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Minimap Clicked");
        Vector3 worldPosition = GetClickWorldPosition(Input.mousePosition);
        Camera.main.transform.position = worldPosition;
    }
    Vector3 GetClickWorldPosition(Vector3 screenPosition)
    {
        float minimapX;
        float minimapY;
        float posX;
        float posY;


        Debug.DrawRay(new Vector3(
            screenPosition.x,
            minimapCamera.transform.position.y,
            screenPosition.z)
            , Vector3.down * 60, Color.white);

        minimapCamera.ScreenPointToRay(new Vector3(
            screenPosition.x,
            screenPosition.y,
            minimapCamera.nearClipPlane));

        // Default
        return Vector3.zero; 
    }
}
