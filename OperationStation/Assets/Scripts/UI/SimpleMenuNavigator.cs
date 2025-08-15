using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class SimpleMenuNavigator : MonoBehaviour
{
    public enum BuildMode { None, LinearVertical }

    [Header("Scope")]
    public Transform menuRoot;

    [Header("Navigation Build")]
    public BuildMode buildMode = BuildMode.LinearVertical;
    public bool wrap = true;

    [Header("Behavior")]
    public bool autoHealSelection = true;
    public bool rescanOnEnable = true;
    public int healDebounceMs = 50;

    [Header("Default Selection")]
    [Tooltip("Optional: Pre-select this UI control when menu appears (if valid).")]
    public Selectable firstSelected;

    readonly List<Selectable> _list = new List<Selectable>();
    EventSystem _es;
    float _nextHealAt;
    GameObject _lastSelectedKeyboard;
    GameObject _lastSelected;
    bool _mouseMode = false; // default to keyboard mode

    void Reset() { menuRoot = transform; }

    void Awake()
    {
        if (!menuRoot) menuRoot = transform;
        EnsureEventSystem();
    }

    void OnEnable()
    {
        if (rescanOnEnable)
        {
            Rescan();
            BuildNavigationIfRequested();
        }

        EnterKeyboardMode(initial: true);

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    void OnTransformChildrenChanged() => DebouncedReselect(); // catch runtime "Continue"

    void Update()
    {
        _es = EventSystem.current;

        if (MouseUsedThisFrame()) EnterMouseMode();
        else if (KeyboardOrGamepadUsedThisFrame()) EnterKeyboardMode();

        if (!_mouseMode && autoHealSelection && Time.unscaledTime >= _nextHealAt && !HasValidSelection())
            TrySelectBest();

        if (!_mouseMode && _es?.currentSelectedGameObject != _lastSelected)
        {
            _lastSelected = _es.currentSelectedGameObject;
            UIHoverArrow.PingSelectedArrow();
        }

        if (!_mouseMode && _es && _es.currentSelectedGameObject)
            _lastSelectedKeyboard = _es.currentSelectedGameObject;
    }

    public void RescanAndRebuild()
    {
        Rescan();
        BuildNavigationIfRequested();
        DebouncedReselect();
    }

    void OnSceneChanged(Scene a, Scene b)
    {
        Rescan();
        BuildNavigationIfRequested();
        if (!_mouseMode) SelectDefaultOrBest();
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            var esGO = new GameObject("EventSystem");
            var es = esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGO.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(esGO);
        }
        _es = EventSystem.current;
    }

    void Rescan()
    {
        _list.Clear();
        if (!menuRoot) return;
        menuRoot.GetComponentsInChildren(true, _list);
        _list.RemoveAll(s => s == null || !s.IsActive() || !s.interactable);
    }

    void BuildNavigationIfRequested()
    {
        if (buildMode == BuildMode.None || _list.Count == 0) return;

        for (int i = 0; i < _list.Count; i++)
        {
            var sel = _list[i];
            if (!sel) continue;

            var nav = sel.navigation;
            nav.mode = Navigation.Mode.Explicit;
            int prev = wrap ? (i - 1 + _list.Count) % _list.Count : Mathf.Max(0, i - 1);
            int next = wrap ? (i + 1) % _list.Count : Mathf.Min(_list.Count - 1, i + 1);

            nav.selectOnUp = (wrap || i > 0) ? _list[prev] : null;
            nav.selectOnDown = (wrap || i < _list.Count - 1) ? _list[next] : null;
            nav.selectOnLeft = nav.selectOnUp;
            nav.selectOnRight = nav.selectOnDown;

            sel.navigation = nav;
        }
    }

    bool HasValidSelection()
    {
        if (_es == null) return false;
        var go = _es.currentSelectedGameObject;
        var sel = go?.GetComponent<Selectable>();
        return sel && sel.IsActive() && sel.interactable;
    }

    void TrySelectBest()
    {
        if (_es == null) return;

        if (_lastSelectedKeyboard)
        {
            var sel = _lastSelectedKeyboard.GetComponent<Selectable>();
            if (sel && sel.IsActive() && sel.interactable)
            {
                _es.SetSelectedGameObject(_lastSelectedKeyboard);
                return;
            }
        }

        SelectDefaultOrBest();
    }

    void SelectDefaultOrBest()
    {
        if (_es == null) return;

        if (firstSelected)
        {
            if (firstSelected.IsActive() && firstSelected.interactable)
            {
                _es.SetSelectedGameObject(firstSelected.gameObject);
                return;
            }
        }

        Rescan();
        foreach (var s in _list)
        {
            if (s && s.IsActive() && s.interactable)
            {
                _es.SetSelectedGameObject(s.gameObject);
                return;
            }
        }

        _es.SetSelectedGameObject(null);
    }

    void DebouncedReselect() => _nextHealAt = Time.unscaledTime + (healDebounceMs / 1000f);

    void EnterMouseMode()
    {
        if (_mouseMode) return;
        _mouseMode = true;
        UIHoverArrow.KeyboardMode = false;
        if (menuRoot) UIHoverArrow.HideAllInParent(menuRoot);
        if (_es?.currentSelectedGameObject != null)
            _es.SetSelectedGameObject(null);
    }

    void EnterKeyboardMode(bool initial = false)
    {
        if (!initial && !_mouseMode) return;
        _mouseMode = false;
        UIHoverArrow.KeyboardMode = true;
        SelectDefaultOrBest();
        UIHoverArrow.PingSelectedArrow();
    }

    static bool KeyboardOrGamepadUsedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null && (k.anyKey.wasPressedThisFrame ||
                          k.upArrowKey.wasPressedThisFrame ||
                          k.downArrowKey.wasPressedThisFrame ||
                          k.leftArrowKey.wasPressedThisFrame ||
                          k.rightArrowKey.wasPressedThisFrame))
            return true;

        var g = Gamepad.current;
        if (g != null && (
            g.dpad.up.wasPressedThisFrame ||
            g.dpad.down.wasPressedThisFrame ||
            g.dpad.left.wasPressedThisFrame ||
            g.dpad.right.wasPressedThisFrame ||
            g.buttonSouth.wasPressedThisFrame ||
            g.startButton.wasPressedThisFrame ||
            g.selectButton.wasPressedThisFrame))
            return true;
#else
        if (Input.anyKeyDown) return true;
#endif
        return false;
    }

    static bool MouseUsedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m == null) return false;
        if (m.delta.ReadValue() != Vector2.zero) return true;
        if (m.scroll.ReadValue() != Vector2.zero) return true;
        if (m.leftButton.wasPressedThisFrame ||
            m.rightButton.wasPressedThisFrame ||
            m.middleButton.wasPressedThisFrame)
            return true;
#else
        if (Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.0f) return true;
        if (Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.0f) return true;
        if (Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1) ||
            Input.GetMouseButtonDown(2)) return true;
#endif
        return false;
    }
}
