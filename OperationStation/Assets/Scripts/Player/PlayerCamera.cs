using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] int speed;
    [SerializeField] Vector2 limit;

    [SerializeField] int scrollSpeed;
    [SerializeField] int min;
    [SerializeField] int max;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Zoom();
    }
    void Move()
    {
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");
    }
    void Zoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0 && transform.position.y > min && transform.position.y < max)
            transform.position += transform.forward * scrollInput * scrollSpeed;
        if (transform.position.y < min)
            transform.position -= transform.forward * 1;
        if (transform.position.y > max)
            transform.position += transform.forward * 1;
    }
    void Focus()
    {

    }
}
