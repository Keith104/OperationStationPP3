using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] int speed;
    [SerializeField] int fapSpeed;
    [SerializeField] Vector2 limit;

    [SerializeField] int scrollSpeed;
    [SerializeField] int min;
    [SerializeField] int max;

    [SerializeField] List<GameObject> selected = new List<GameObject>();
    [SerializeField] AudioSource selectedSource;

    [SerializeField] RectTransform UI;
    [SerializeField] RectTransform selectionBox;
    [SerializeField] Vector2 startMousePos;
    [SerializeField] LayerMask clickableLayers;

    private Vector3 focusPosition;
    private bool isFocused;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Rotate();
        FixedAtPoint();

        Zoom();

        Focus();
        Select();
    }
    void Move()
    {
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");

        Vector3 dir = new(xInput, 0, zInput);

        float yOrg = transform.position.y;
        transform.Translate(speed * Time.deltaTime * dir, Space.Self);
        transform.position = new(transform.position.x, yOrg, transform.position.z);
    }
    void Rotate()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, -1, 0, Space.World);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, 1, 0, Space.World);
        }
    }
    void FixedAtPoint()
    {
        if (Input.GetMouseButton(1))
        {
            // Get mouse movement
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Rotate around the Y-axis 
            transform.Rotate(Vector3.up, mouseX * fapSpeed * Time.deltaTime);

            // Rotate around the X-axis 
            transform.Rotate(Vector3.right, -mouseY * fapSpeed * Time.deltaTime);

            // Keep Z-axis zero 
            transform.eulerAngles = new(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }
    }
    void Zoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // zooms in or out if it's within the bounds
        if (scrollInput != 0 && transform.position.y > min && transform.position.y < max)
        {
            // zooms in or out
            transform.position += scrollInput * scrollSpeed * transform.forward;

            // moves mouse into objects
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (scrollInput > 0)
                if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, clickableLayers))
                    transform.position = Vector3.Lerp(transform.position, hit.transform.position, 0.5f);
        }

        // moves mouse back within the bounds
        if (transform.position.y < min)
            transform.position -= transform.forward * 1;
        if (transform.position.y > max)
            transform.position += transform.forward * 1;

    }
    void Focus()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFocused = !isFocused;
            if (isFocused == true)
            {
                focusPosition = Vector3.Lerp(transform.position, selected[0].transform.position, 0.5f);
                transform.position = focusPosition;
            }
            else
            {
                transform.eulerAngles = new(45, transform.eulerAngles.y, transform.eulerAngles.z);
            }
        }

        if(isFocused == true)
        {
            transform.LookAt(selected[0].transform);
        }
    }

    void Select()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetKey(KeyCode.LeftShift) == false && UnitUIManager.instance.unitMenu.activeSelf == false)
                selected.Clear();
            else if (EventSystem.current.IsPointerOverGameObject() == false)
            {
                selected.Clear();
                UnitUIManager.instance.unitMenu.SetActive(false);
            }


            startMousePos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, clickableLayers))
            {
                if (selected.Contains(hit.collider.gameObject) == false)
                {
                    selectedSource.Play();
                    selected.Add(hit.collider.gameObject);
                    TrySelect(hit.collider.gameObject);
                }
                else
                    selected.Remove(hit.collider.gameObject);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (selectionBox.gameObject.activeSelf == false)
                selectionBox.gameObject.SetActive(true);
            Drag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            selectionBox.gameObject.SetActive(false);
        }
    }

    void Drag()
    {
        float width = Input.mousePosition.x - startMousePos.x;
        float height = Input.mousePosition.y - startMousePos.y;
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBox.anchoredPosition = startMousePos + new Vector2(width / 2, height / 2);
        DragDetect();
    }

    void DragDetect()
    {
        Vector3 worldPointStart = Camera.main.ScreenToWorldPoint
            (new Vector3(startMousePos.x, startMousePos.y, Camera.main.nearClipPlane));
        Vector3 worldPointEnd = Camera.main.ScreenToWorldPoint
            (new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));

        Debug.DrawRay(worldPointStart, transform.forward * 100, Color.green);
        Debug.DrawRay(worldPointEnd, transform.forward * 100, Color.green);

        Vector3 center = Vector3.Lerp(
            Camera.main.ScreenPointToRay(startMousePos).origin,
            Camera.main.ScreenPointToRay(Input.mousePosition).origin, 
            0.5f);

        Vector3 size = new Vector3
            (Mathf.Abs(worldPointStart.x - worldPointEnd.x), 
            Mathf.Abs(worldPointStart.y - worldPointEnd.y),
            Mathf.Abs(worldPointStart.z - worldPointEnd.z));

        Vector2 dragCenter = Vector2.Lerp(startMousePos ,Input.mousePosition, 0.5f);
        Ray ray = Camera.main.ScreenPointToRay(dragCenter);
        RaycastHit centerhit;

        if (Physics.Raycast(ray, out centerhit))
        {
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.yellow);
        }

        Vector3 direction = ray.direction;

        RaycastHit[] raycastHits = Physics.BoxCastAll(center, size * 10, direction);

        foreach (RaycastHit hit in raycastHits)
        {
            if (selected.Contains(hit.collider.gameObject) == false)
            {
                selected.Add(hit.collider.gameObject);
                TrySelect(hit.collider.gameObject);
            }
        }
    }

    void TrySelect(GameObject selected)
    {
        Debug.Log("Check");
        ISelectable selectable = selected.GetComponent<ISelectable>();

        if (selectable != null)
        {
            selectable.TakeControl();
        }
    }
}
