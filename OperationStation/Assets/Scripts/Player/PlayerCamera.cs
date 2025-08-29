using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Speed")]
    [SerializeField] int moveSpeed;
    [SerializeField] int rotateSpeed;
    [SerializeField] int scrollSpeed;
    [SerializeField] Vector2 limit;

    [Header("Camera Limits")]
    [SerializeField] int moveMin;
    [SerializeField] int moveMax;
    [SerializeField] int zoomMin;
    [SerializeField] int zoomMax;

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
        clickableLayers = LayerMask.GetMask("Object", "Ship");
    }

    void OnEnable()
    {
        if (controls == null) controls = new PlayerInput();
        controls.Player.Focus.performed += OnFocus;
        controls.Player.Select.started += OnSelectStarted;
        controls.Player.Select.canceled += OnSelectCanceled;
        controls.Enable();
    }

    void OnDisable()
    {
        if (controls == null) return;
        controls.Player.Focus.performed -= OnFocus;
        controls.Player.Select.started -= OnSelectStarted;
        controls.Player.Select.canceled -= OnSelectCanceled;
        controls.Disable();
    }

    void OnDestroy()
    {
        controls?.Dispose();
        controls = null;
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
        float yOrg = transform.position.y;
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        Vector3 dir = new Vector3(move.x, 0f, move.y);
        transform.Translate(moveSpeed * Time.deltaTime * dir, Space.Self);
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, moveMin, moveMax);
        clampedPos.z = Mathf.Clamp(clampedPos.z, moveMin, moveMax);
        clampedPos.y = yOrg;
        transform.position = clampedPos;
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
            transform.Rotate(Vector3.up, look.x * rotateSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, -look.y * rotateSpeed * Time.deltaTime, Space.Self);
            Vector3 rot = transform.eulerAngles;
            transform.eulerAngles = new Vector3(rot.x, rot.y, 0f);
            float currentRotX = transform.eulerAngles.x;
            if (currentRotX > 180f) currentRotX -= 360f;
            Vector3 clampedRot = transform.eulerAngles;
            clampedRot.x = Mathf.Clamp(currentRotX, -45f, 45f);
            transform.eulerAngles = clampedRot;
        }
    }

    void Zoom()
    {
        float scroll = controls.Player.Zoom.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.0001f)
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
        Vector3 clampedPos = transform.position;
        clampedPos.y = Mathf.Clamp(clampedPos.y, zoomMin, zoomMax);
        transform.position = clampedPos;
    }

    void OnFocus(InputAction.CallbackContext _)
    {
        isFocused = !isFocused;
    }

    void HandleFocus()
    {
        if (isFocused && selected.Count > 0 && selected[0])
        {
            Vector3 direction = selected[0].transform.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }
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
            if (go.layer == LayerMask.NameToLayer("Ship"))
            {
                if (!selected.Contains(go))
                {
                    selectedSource?.Play();
                    selected.Add(go);
                    TrySelect(go);
                }
                else
                {
                    selected.Remove(go);
                    if (selected.Count == 0)
                        UnitUIManager.instance.unitMenu.SetActive(false);
                }
            }
            else
            {
                selected.Clear();
                UnitUIManager.instance.unitMenu.SetActive(false);
            }
        }
        else
        {
            selected.Clear();
            UnitUIManager.instance.unitMenu.SetActive(false);
        }
    }

    void TrySelect(GameObject go)
    {
        var ui = UnitUIManager.instance;
        var selectables = go.GetComponents<ISelectable>();
        ISelectable chosen =
            selectables.FirstOrDefault(s => s is Smelter)
            ?? selectables.FirstOrDefault(s => s.GetType().Name != "Module")
            ?? selectables.FirstOrDefault();
        if (ui) ui.currUnit = go;
        if (chosen != null) chosen.TakeControl();
    }

    void OnSelectCanceled(InputAction.CallbackContext _)
    {
        if (selectionBox) selectionBox.gameObject.SetActive(false);
    }

    void HandleDragSelection()
    {
        if (!controls.Player.Select.IsPressed()) return;
        if (!selectionBox.gameObject.activeSelf)
            selectionBox.gameObject.SetActive(true);
        Vector2 cur = GetPointerPosOrCenter();
        Canvas canvas = selectionBox.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startMousePos,
            canvas.worldCamera, out Vector2 localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, cur,
            canvas.worldCamera, out Vector2 localCur);
        Vector2 diff = localCur - localStart;
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
        selectionBox.anchoredPosition = localStart + new Vector2(diff.x / 2f, diff.y / 2f);
        DragDetect(cur);
    }

    Vector2 GetPointerPosOrCenter()
    {
        if (controls == null) return Vector2.zero;
        Vector2 pos = controls.Player.Point.ReadValue<Vector2>();
        if (pos == Vector2.zero && Gamepad.current != null)
            return new Vector2(Screen.width / 2f, Screen.height / 2f);
        return pos;
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
}
