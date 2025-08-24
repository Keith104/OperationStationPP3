using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel; // for InputState.Change

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Speed")]
    [SerializeField] int moveSpeed = 30;
    [SerializeField] int rotateSpeed = 120;
    [SerializeField] int scrollSpeed = 50;
    [SerializeField] Vector2 limit = new Vector2(60, 80);

    [Header("Camera Limits")]
    [SerializeField] int moveMin = -500;
    [SerializeField] int moveMax = 500;
    [SerializeField] int zoomMin = 10;
    [SerializeField] int zoomMax = 200;

    [Header("Selection")]
    [SerializeField] RectTransform UI;
    [SerializeField] RectTransform selectionBox;
    [SerializeField] Vector2 startMousePos;
    [SerializeField] LayerMask clickableLayers;

    [Header("Virtual Cursor (Gamepad)")]
    [SerializeField] RectTransform cursorUI;           // <- assign your cursor graphic here
    [SerializeField] float cursorSpeed = 1100f;        // pixels/sec from right stick
    [SerializeField] float gamepadDeadzone = 0.15f;

    [Header("Misc.")]
    [SerializeField] List<GameObject> selected = new List<GameObject>();
    [SerializeField] SoundObject soundHovered;
    [SerializeField] AudioSource selectedSource;

    Vector3 focusPosition;
    bool isFocused;

    // NOTE: this "PlayerInput" is your generated input-actions C# class.
    PlayerInput controls;

    // virtual cursor state
    Vector2 virtualCursor;          // screen-space pixels
    bool useGamepadCursor;
    float lastMouseActivity;
    float lastGamepadActivity;

    void Awake()
    {
        UI = GameObject.FindWithTag("UI").GetComponent<RectTransform>();
        selectionBox = UI.Find("SelectionBox").GetComponent<RectTransform>();
        controls = new PlayerInput();
        clickableLayers = LayerMask.GetMask("Object", "Ship");

        virtualCursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    void OnEnable()
    {
        controls.Player.Focus.performed += OnFocus;
        controls.Player.Select.started += OnSelectStarted;
        controls.Player.Select.canceled += OnSelectCanceled;
        controls.Enable();

        // start with hardware mouse visible; virtual cursor hidden
        SetCursorMode(false, true);
    }

    void OnDisable()
    {
        controls.Player.Focus.performed -= OnFocus;
        controls.Player.Select.started -= OnSelectStarted;
        controls.Player.Select.canceled -= OnSelectCanceled;
        controls.Disable();

        SetCursorMode(false, true);
    }

    void Update()
    {
        UpdateInputMode();
        if (useGamepadCursor)
            UpdateVirtualCursor();

        SyncCursorGraphic();

        Move();
        RotateKeys();
        OrbitWhileHeld();
        Zoom();
        HandleFocus();
        HoverObject();
        HandleDragSelection();
    }

    // -------------------- Input Mode & Virtual Cursor --------------------

    void UpdateInputMode()
    {
        // detect mouse motion
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude > 0.0001f)
                lastMouseActivity = Time.unscaledTime;
        }

        // detect gamepad activity (right stick / triggers / buttons)
        if (Gamepad.current != null)
        {
            Vector2 rs = Gamepad.current.rightStick.ReadValue();
            Vector2 ls = Gamepad.current.leftStick.ReadValue();
            bool any =
                rs.sqrMagnitude > gamepadDeadzone * gamepadDeadzone ||
                ls.sqrMagnitude > gamepadDeadzone * gamepadDeadzone ||
                Gamepad.current.rightTrigger.ReadValue() > 0.01f ||
                Gamepad.current.leftTrigger.ReadValue() > 0.01f ||
                controls.Player.Select.IsPressed();
            if (any) lastGamepadActivity = Time.unscaledTime;
        }

        // prefer the device that moved most recently
        bool preferGamepad = Gamepad.current != null && lastGamepadActivity > lastMouseActivity;
        if (preferGamepad != useGamepadCursor)
        {
            useGamepadCursor = preferGamepad;
            SetCursorMode(useGamepadCursor, !useGamepadCursor);
        }
    }

    void SetCursorMode(bool useVirtual, bool showHardwareMouse)
    {
        Cursor.visible = showHardwareMouse;
        if (cursorUI) cursorUI.gameObject.SetActive(useVirtual);

        // keep UI & physics using the correct pointer position
        Vector2 pos = useVirtual ? virtualCursor : (Mouse.current?.position.ReadValue() ?? virtualCursor);
        if (Mouse.current != null)
        {
            Mouse.current.WarpCursorPosition(pos);
            InputState.Change(Mouse.current.position, pos);
        }
    }

    void UpdateVirtualCursor()
    {
        // Use your "Look" action (mapped to Right Stick) to move the cursor
        Vector2 input = controls.Player.Look.ReadValue<Vector2>();

        if (input.sqrMagnitude < gamepadDeadzone * gamepadDeadzone)
            input = Vector2.zero;

        if (input != Vector2.zero)
        {
            virtualCursor += input * cursorSpeed * Time.unscaledDeltaTime;

            virtualCursor.x = Mathf.Clamp(virtualCursor.x, 0f, Screen.width);
            virtualCursor.y = Mathf.Clamp(virtualCursor.y, 0f, Screen.height);

            if (Mouse.current != null)
            {
                Mouse.current.WarpCursorPosition(virtualCursor);
                InputState.Change(Mouse.current.position, virtualCursor);
            }
        }
    }

    void SyncCursorGraphic()
    {
        if (!cursorUI) return;

        // Canvas assumed Screen Space - Overlay
        cursorUI.anchoredPosition = virtualCursor;
    }

    // -------------------- Camera --------------------

    void Move()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        if (transform.position.x > moveMin && transform.position.x < moveMax
            && transform.position.z > moveMin && transform.position.z < moveMax)
        {
            Vector3 dir = new Vector3(move.x, 0f, move.y);
            float yOrg = transform.position.y;
            transform.Translate(moveSpeed * Time.deltaTime * dir, Space.Self);
            transform.position = new Vector3(transform.position.x, yOrg, transform.position.z);
        }
        if (transform.position.x < moveMin) transform.position += transform.right;
        if (transform.position.x > moveMax) transform.position -= transform.right;
        if (transform.position.z < moveMin) transform.position += transform.up;
        if (transform.position.z > moveMax) transform.position -= transform.up;
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
            Vector3 e = transform.eulerAngles;
            transform.eulerAngles = new Vector3(e.x, e.y, 0f);
        }
    }

    void Zoom()
    {
        float scroll = controls.Player.Zoom.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.0001f && transform.position.y > zoomMin && transform.position.y < zoomMax)
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

        if (transform.position.y < zoomMin) transform.position -= transform.forward;
        if (transform.position.y > zoomMax) transform.position += transform.forward;
    }

    // -------------------- Selection & Focus --------------------

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
        var ui = UnitUIManager.instance;
        var selectables = go.GetComponents<ISelectable>();
        ISelectable chosen =
            selectables.FirstOrDefault(s => s is Smelter)
            ?? selectables.FirstOrDefault(s => s.GetType().Name != "Module")
            ?? selectables.FirstOrDefault();

        if (ui) ui.currUnit = go;
        if (chosen != null) chosen.TakeControl();
    }

    Vector2 GetPointerPosOrCenter()
    {
        if (useGamepadCursor)
            return virtualCursor;

        Vector2 pos = controls.Player.Point.ReadValue<Vector2>();
        if (pos == Vector2.zero && Gamepad.current != null)
            return new Vector2(Screen.width / 2f, Screen.height / 2f);
        return pos;
    }
}
