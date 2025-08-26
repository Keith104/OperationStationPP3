using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MinimapRect : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Camera minimapCamera;
    [SerializeField] AudioSource minimapSource;
    [SerializeField] RectTransform minimapRect;
    [SerializeField] RawImage minimapImage;
    [SerializeField] Vector3 worldPosition;

    [SerializeField] float lerpSpeed;
    private bool isLerping;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        minimapCamera = GameObject.FindWithTag("MinimapCamera").GetComponent<Camera>();
    }
    void Update()
    {
        if (isLerping == true)
        {
            Vector3 camPosition = Camera.main.transform.position;
            Camera.main.transform.position = Vector3.Lerp(camPosition, worldPosition, lerpSpeed * Time.deltaTime);

            if (Vector3.Distance(camPosition, worldPosition) < 0.1f)
            {
                Camera.main.transform.position = worldPosition;
                isLerping = false;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Minimap Clicked");
        minimapSource.Play();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minimapRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        GetClickWorldPosition(localPoint);

        isLerping = true;
    }
    void GetClickWorldPosition(Vector2 screenPosition)
    {
        // Normalized
        Rect rect = minimapRect.rect;
        float normalizedX = (screenPosition.x - rect.x) / rect.width;
        float normalizedY = (screenPosition.y - rect.y) / rect.height;

        // Convert to pixel
        Texture texture = minimapImage.texture;
        int posX = Mathf.RoundToInt(normalizedX * texture.width);
        int posY = Mathf.RoundToInt(normalizedY * texture.height);

        // Debug
        Debug.Log(posX + ", " + posY);
        Debug.DrawRay(new Vector3(
            posX,
            minimapCamera.transform.position.y,
            posY)
            , Vector3.down * 60, Color.white);

        // set new World Position
        worldPosition.x = posX;
        worldPosition.y = Camera.main.transform.position.y;
        worldPosition.z = posY;
    }
}
