using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] int speed;
    [SerializeField] Vector2 limit;

    [SerializeField] int scrollSpeed;
    [SerializeField] int min;
    [SerializeField] int max;

    [SerializeField] List<GameObject> selected = new List<GameObject>();

    [SerializeField] RectTransform UI;
    [SerializeField] RectTransform selectionBox;
    [SerializeField] Vector2 startMousePos;
    [SerializeField] LayerMask clickableLayers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Zoom();
        Select();
    }
    void Move()
    {
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(xInput, 0, zInput);
        transform.position += dir * speed * Time.deltaTime;
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
        // I forgot so it's a Trello thing for later
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
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, float.MaxValue, clickableLayers))
            {
                if (selected.Contains(hit.collider.gameObject) == false)
                {
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
