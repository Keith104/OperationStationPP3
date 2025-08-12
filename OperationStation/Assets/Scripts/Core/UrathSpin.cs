using UnityEngine;

public class UrathSpin : MonoBehaviour
{
    [SerializeField] Material material;

    public float scrollSpeedX;

    void Update()
    {
        Vector2 offset = material.mainTextureOffset;
        offset.x += scrollSpeedX * Time.deltaTime;
        material.mainTextureOffset = offset;
    }
}
