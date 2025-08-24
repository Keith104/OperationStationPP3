using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-10)]
public class GamepadCursorBootstrap : MonoBehaviour
{
    [SerializeField] float cursorSpeed = 1400f;
    [SerializeField] float deadzone = 0.05f;

    PlayerInput controls;

    bool clickRequested;
    bool clickDownSent;

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
        if (Mouse.current == null) return;

        if (!controls.Player.OrbitHold.IsPressed())
        {
            Vector2 stick = controls.Player.Look.ReadValue<Vector2>();
            if (stick.sqrMagnitude >= deadzone * deadzone)
            {
                Vector2 delta = stick * cursorSpeed * Time.unscaledDeltaTime;
                Vector2 pos = Mouse.current.position.ReadValue() + delta;
                pos.x = Mathf.Clamp(pos.x, 0, Screen.width - 1);
                pos.y = Mathf.Clamp(pos.y, 0, Screen.height - 1);
                Mouse.current.WarpCursorPosition(pos);
            }
        }

        // One-press -> full click (press this frame, release next frame)
        if (clickRequested)
        {
            var pos = Mouse.current.position.ReadValue();

            if (!clickDownSent)
            {
                // Press: buttons bit 1 (left) = down. Preserve position so it won't jump.
                var press = new MouseState { position = pos, buttons = 1 };
                InputSystem.QueueStateEvent(Mouse.current, press);
                clickDownSent = true;
            }
            else
            {
                // Release on the next Update tick.
                var release = new MouseState { position = pos, buttons = 0 };
                InputSystem.QueueStateEvent(Mouse.current, release);
                clickRequested = false;
                clickDownSent = false;
            }
        }
    }

    void OnSelectPerformed(InputAction.CallbackContext _)
    {
        // Request a one-shot click; do not call InputSystem.Update() here
        clickRequested = true;
        clickDownSent = false;
    }
}
