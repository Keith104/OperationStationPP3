using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Speed")]
    [SerializeField] int speed;
    [SerializeField] int fapSpeed;
    [SerializeField] int scrollSpeed;
    [SerializeField] Vector2 limit;

    [Header("Camera Limits")]
    [SerializeField] int min;
    [SerializeField] int max;

    [Header("Selection")]
    [SerializeField] RectTransform UI;
    [SerializeField] RectTransform selectionBox;
    [SerializeField] Vector2 startMousePos;
    [SerializeField] LayerMask clickableLayers;

    [Header("Misc.")]
    [SerializeField] List<GameObject> selected = new List<GameObject>();
    [SerializeField] SoundObject soundHovered;
    [SerializeField] AudioSource selectedSource;

    Vector3 focusPosition;
    bool isFocused;

    PlayerInput controls;

    void Awake()
    {
        UI = GameObject.FindWithTag("UI").GetComponent<RectTransform>();
        selectionBox = UI.Find("SelectionBox").GetComponent<RectTransform>();
        controls = new PlayerInput();
    }

    void OnEnable()
    {
        controls.Player.Focus.performed += OnFocus;
        controls.Player.Select.started += OnSelectStarted;
        controls.Player.Select.canceled += OnSelectCanceled;
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Player.Focus.performed -= OnFocus;
        controls.Player.Select.started -= OnSelectStarted;
        controls.Player.Select.canceled -= OnSelectCanceled;
        controls.Disable();
    }

    void Update()
    {
        Move();
        RotateKeys();
        OrbitWhileHeld();
        Zoom();
        HandleFocus();
        HoverObject();
        HandleDragSelection();
    }

    void Move()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        Vector3 dir = new Vector3(move.x, 0f, move.y);
        float yOrg = transform.position.y;
        transform.Translate(speed * Time.deltaTime * dir, Space.Self);
        transform.position = new Vector3(transform.position.x, yOrg, transform.position.z);
    }

    void RotateKeys()
    {
        float axis = controls.Player.Rotate.ReadValue<float>();
        if (Mathf.Abs(axis) > 0.0001f)
            transform.Rotate(0f, axis, 0f, Space.World);
    }

    void OrbitWhileHeld()
    {
        if (controls.Player.OrbitHold.IsPressed())
        {
            Vector2 look = controls.Player.Look.ReadValue<Vector2>();
            transform.Rotate(Vector3.up, look.x * fapSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, -look.y * fapSpeed * Time.deltaTime, Space.Self);
            Vector3 e = transform.eulerAngles;
            transform.eulerAngles = new Vector3(e.x, e.y, 0f);
        }
    }

    void Zoom()
    {
        float scroll = controls.Player.Zoom.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.0001f && transform.position.y > min && transform.position.y < max)
        {
            transform.position += scroll * scrollSpeed * transform.forward;

            if (scroll > 0f)
            {
                Vector2 pos = GetPointerPosOrCenter();
                Ray ray = Camera.main.ScreenPointToRay(pos);
                if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, clickableLayers))
                    transform.position = Vector3.Lerp(transform.position, hit.transform.position, 0.5f);
            }
        }

        if (transform.position.y < min) transform.position -= transform.forward;
        if (transform.position.y > max) transform.position += transform.forward;
    }

    void OnFocus(InputAction.CallbackContext _)
    {
        isFocused = !isFocused;
        if (isFocused && selected.Count > 0 && selected[0])
        {
            focusPosition = Vector3.Lerp(transform.position, selected[0].transform.position, 0.5f);
            transform.position = focusPosition;
        }
        else
        {
            Vector3 e = transform.eulerAngles;
            transform.eulerAngles = new Vector3(45f, e.y, e.z);
        }
    }

    void HandleFocus()
    {
        if (isFocused && selected.Count > 0 && selected[0])
            transform.LookAt(selected[0].transform);
    }

    void HoverObject()
    {
        Vector2 pos = GetPointerPosOrCenter();
        Ray ray = Camera.main.ScreenPointToRay(pos);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, clickableLayers))
        {
            if (soundHovered == null)
            {
                soundHovered = hit.transform.Find("SoundObject")?.GetComponent<SoundObject>();
                if (soundHovered) soundHovered.PlayObjectIn();
            }
        }
        else if (soundHovered != null)
        {
            soundHovered.PlayObjectOut();
            soundHovered = null;
        }
    }

    void OnSelectStarted(InputAction.CallbackContext _)
    {
        bool shift = controls.Player.MultiSelectModifer.IsPressed();
        if (!shift && UnitUIManager.instance.unitMenu.activeSelf == false)
            selected.Clear();
        else if (!EventSystem.current.IsPointerOverGameObject())
        {
            selected.Clear();
            UnitUIManager.instance.unitMenu.SetActive(false);
        }

        startMousePos = GetPointerPosOrCenter();
        Ray ray = Camera.main.ScreenPointToRay(startMousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, clickableLayers))
        {
            GameObject go = hit.collider.gameObject;
            if (!selected.Contains(go))
            {
                selectedSource?.Play();
                selected.Add(go);
                TrySelect(go);
            }
            else selected.Remove(go);
        }
    }

    void HandleDragSelection()
    {
        if (!controls.Player.Select.IsPressed()) return;

        if (!selectionBox.gameObject.activeSelf)
            selectionBox.gameObject.SetActive(true);

        Vector2 cur = GetPointerPosOrCenter();
        float w = cur.x - startMousePos.x;
        float h = cur.y - startMousePos.y;

        selectionBox.sizeDelta = new Vector2(Mathf.Abs(w), Mathf.Abs(h));
        selectionBox.anchoredPosition = startMousePos + new Vector2(w / 2f, h / 2f);

        DragDetect(cur);
    }

    void OnSelectCanceled(InputAction.CallbackContext _)
    {
        if (selectionBox) selectionBox.gameObject.SetActive(false);
    }

    void DragDetect(Vector2 cur)
    {
        Vector3 ws = Camera.main.ScreenToWorldPoint(new Vector3(startMousePos.x, startMousePos.y, Camera.main.nearClipPlane));
        Vector3 we = Camera.main.ScreenToWorldPoint(new Vector3(cur.x, cur.y, Camera.main.nearClipPlane));

        Debug.DrawRay(ws, transform.forward * 100, Color.green);
        Debug.DrawRay(we, transform.forward * 100, Color.green);

        Vector3 center = Vector3.Lerp(Camera.main.ScreenPointToRay(startMousePos).origin,
                                      Camera.main.ScreenPointToRay(cur).origin, 0.5f);
        Vector3 size = new Vector3(Mathf.Abs(ws.x - we.x), Mathf.Abs(ws.y - we.y), Mathf.Abs(ws.z - we.z));
        Vector2 dc = Vector2.Lerp(startMousePos, cur, 0.5f);
        Ray ray = Camera.main.ScreenPointToRay(dc);
        if (Physics.Raycast(ray, out _)) Debug.DrawRay(ray.origin, ray.direction * 100, Color.yellow);

        Vector3 dir = ray.direction;
        foreach (var hit in Physics.BoxCastAll(center, size * 10f, dir))
        {
            GameObject go = hit.collider.gameObject;
            if (!selected.Contains(go))
            {
                selected.Add(go);
                TrySelect(go);
            }
        }
    }

    void TrySelect(GameObject go)
    {
        var selectables = new List<ISelectable>(go.GetComponents<ISelectable>());

        // Prefer Smelter if present
        foreach (var sel in selectables)
        {
            if (sel is Smelter)
            {
                sel.TakeControl();
                return;
            }
        }

        // Otherwise call the first one
        if (selectables.Count > 0)
        {
            selectables[0].TakeControl();
        }
    }

    Vector2 GetPointerPosOrCenter()
    {
        Vector2 pos = controls.Player.Point.ReadValue<Vector2>();
        if (pos == Vector2.zero && Gamepad.current != null)
            return new Vector2(Screen.width / 2f, Screen.height / 2f);
        return pos;
    }
}
