using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-10)]
public class GamepadCursorController : MonoBehaviour
{
    enum Owner { Mouse, Gamepad }

    [Header("Gamepad")]
    [SerializeField] float gamepadSpeed = 1400f; // px/sec driven by stick
    [SerializeField] float deadzone = 0.05f; // stick deadzone

    [Header("Ownership (Mouse first)")]
    [SerializeField] float mouseLeadTime = 0.25f; // mouse keeps control this long after any movement
    [SerializeField] float mouseMoveEpsilon = 0.75f; // pixels; raise if tiny jitter steals ownership

    PlayerInput controls;
    bool clickRequested;
    bool clickDownSent;

    Owner owner = Owner.Mouse;
    float lastMouseMoveTime = -999f;

    void Awake()
    {
        controls = new PlayerInput();
        Cursor.lockState = CursorLockMode.None; // system cursor
        Cursor.visible = true;                // system cursor
    }

    void OnEnable()
    {
        controls.Enable();
        controls.Player.Select.performed += OnSelectPerformed;
    }

    void OnDisable()
    {
        controls.Player.Select.performed -= OnSelectPerformed;
        controls.Disable();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Always keep the real OS cursor (no virtual visuals)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Inputs
        bool orbitHeld = controls.Player.OrbitHold.IsPressed();
        Vector2 mDelta = mouse.delta.ReadValue();
        Vector2 stick = controls.Player.Look.ReadValue<Vector2>();

        bool mouseMoved = !orbitHeld && (mDelta.sqrMagnitude > mouseMoveEpsilon * mouseMoveEpsilon);
        bool stickMoved = !orbitHeld && (stick.sqrMagnitude >= deadzone * deadzone);

        // Mouse gets priority; after mouseLeadTime with no mouse, gamepad can take over
        if (mouseMoved)
        {
            lastMouseMoveTime = Time.unscaledTime;
            owner = Owner.Mouse;
        }
        else if (stickMoved && (Time.unscaledTime - lastMouseMoveTime) > mouseLeadTime)
        {
            owner = Owner.Gamepad;
        }

        // Move the OS cursor only when the gamepad owns it
        if (owner == Owner.Gamepad && stickMoved)
        {
            Vector2 pos = mouse.position.ReadValue() + stick * gamepadSpeed * Time.unscaledDeltaTime;
            pos.x = Mathf.Clamp(pos.x, 0, Screen.width - 1);
            pos.y = Mathf.Clamp(pos.y, 0, Screen.height - 1);
            mouse.WarpCursorPosition(pos); // <-- OS/system cursor; never used when Mouse owns it
        }
        // If owner == Mouse: DO NOT warp or modify position at all (user can leave the window, hover UI, etc.)

        // Synthetic click mapped from gamepad "Select"
        if (clickRequested)
        {
            Vector2 pos = mouse.position.ReadValue(); // use the real OS cursor position
            if (!clickDownSent)
            {
                var press = new MouseState { position = pos, buttons = 1 };
                InputSystem.QueueStateEvent(mouse, press);
                clickDownSent = true;
            }
            else
            {
                var release = new MouseState { position = pos, buttons = 0 };
                InputSystem.QueueStateEvent(mouse, release);
                clickRequested = false;
                clickDownSent = false;
            }
        }
    }

    void OnSelectPerformed(InputAction.CallbackContext _)
    {
        clickRequested = true;
        clickDownSent = false;
    }
}
