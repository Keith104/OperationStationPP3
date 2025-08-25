using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-10)]
public class GamepadCursorController : MonoBehaviour
{
    [Header("Gamepad")]
    [SerializeField] float gamepadSpeed = 1400f;   // px/sec when using stick
    [SerializeField] float deadzone = 0.05f;   // stick deadzone

    [Header("Mouse Priority")]
    [SerializeField] float mouseMoveEpsilon = 0.001f; // tiny threshold to detect real mouse movement
    [SerializeField] float mouseLeadTime = 0.25f;  // seconds mouse keeps control after moving

    PlayerInput controls;
    bool clickRequested;
    bool clickDownSent;

    float lastMouseMoveTime = -999f;

    void Awake()
    {
        controls = new PlayerInput();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        // Inputs
        bool orbitHeld = controls.Player.OrbitHold.IsPressed();
        Vector2 mDelta = mouse.delta.ReadValue();
        Vector2 stick = controls.Player.Look.ReadValue<Vector2>();

        // Detect real mouse motion and remember the time
        if (!orbitHeld && mDelta.sqrMagnitude > mouseMoveEpsilon * mouseMoveEpsilon)
            lastMouseMoveTime = Time.unscaledTime;

        // Mouse wins for a short grace window after movement
        bool mouseActive = !orbitHeld && (Time.unscaledTime - lastMouseMoveTime) <= mouseLeadTime;
        bool stickActive = !orbitHeld && stick.sqrMagnitude >= deadzone * deadzone;

        if (mouseActive)
        {
            // DO NOT ADJUST MOUSE POSITION.
            // Let the OS handle the cursor so it can leave the game window/monitor.
        }
        else if (stickActive)
        {
            // Drive cursor with gamepad (and clamp to game view)
            Vector2 pos = mouse.position.ReadValue() + stick * gamepadSpeed * Time.unscaledDeltaTime;
            pos.x = Mathf.Clamp(pos.x, 0, Screen.width - 1);
            pos.y = Mathf.Clamp(pos.y, 0, Screen.height - 1);
            mouse.WarpCursorPosition(pos);
        }

        // Handle a one-tap click (press then release over two frames)
        if (clickRequested)
        {
            Vector2 pos = mouse.position.ReadValue(); // current OS cursor position

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
