using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StatsMenuManager : MonoBehaviour
{
    public static StatsMenuManager instance { get; private set; }

    [Header("Entries in the stats menu")]
    public List<EnemiesSO> entries = new List<EnemiesSO>();

    [Header("Texts in the menu (will find them if I forget to set them)")]
    public TMP_Text unitNameText;
    public TMP_Text unitHealthText;
    public TMP_Text unitDamageAmountText;
    public TMP_Text attackCooldownText;
    public TMP_Text bulletTypeText;

    [Header("UI buttons for prev/next")]
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("Roots (auto-found if empty)")]
    public GameObject unitStatsRoot;          // expects "UnitStats"
    public GameObject noUnitsDiscoveredText;  // expects "NoUnitsDiscoveredText"

    PlayerInput controls;
    int _index;

    // cache so Update only refreshes when something actually changed
    bool _lastAnyDiscovered;
    bool _lastCurrentFound;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        controls = new PlayerInput();

        SceneManager.sceneLoaded += (s, m) =>
        {
            EnsureRefs();
            WireButtons();
            WireInput();
            RefreshVisibility();
            Show(_index);
            CacheStates();
        };
    }

    void OnEnable()
    {
        EnsureRefs();
        WireButtons();
        WireInput();
        RefreshVisibility();
        Show(_index);
        CacheStates();
    }

    void OnDisable() => UnwireInput();
    void OnDestroy() { UnwireInput(); controls?.Dispose(); }

    // keep UI in sync if EnemiesSO.found flips at runtime
    void Update()
    {
        bool any = AnyDiscovered();
        if (any != _lastAnyDiscovered)
        {
            _lastAnyDiscovered = any;
            RefreshVisibility();
            Show(_index);
        }
        else if (entries.Count > 0)
        {
            var so = entries[Mathf.Clamp(_index, 0, entries.Count - 1)];
            bool curFound = so && so.found;
            if (curFound != _lastCurrentFound)
            {
                _lastCurrentFound = curFound;
                Show(_index);
            }
        }
    }

    // navigation
    public void Next()
    {
        if (entries.Count == 0 || !AnyDiscovered()) return;
        _index = (_index + 1) % entries.Count;
        Show(_index);
        CacheStates();
    }

    public void Prev()
    {
        if (entries.Count == 0 || !AnyDiscovered()) return;
        _index = (_index - 1 + entries.Count) % entries.Count;
        Show(_index);
        CacheStates();
    }

    public void Show(int idx)
    {
        EnsureRefs();
        if (!AnyDiscovered() || entries.Count == 0) { ClearStatsTexts(); return; }

        idx = Mathf.Clamp(idx, 0, entries.Count - 1);
        var so = entries[idx];

        if (so == null || !so.found)
        {
            WriteLines("Not Discovered", "Not Discovered", "Not Discovered", "Not Discovered", "Not Discovered");
            _lastCurrentFound = false;
            return;
        }

        string rawName = so.enemyObject ? so.enemyObject.name : so.name;
        WriteLines(
            AddSpacesToCamelCase(rawName),
            so.health.ToString(),
            so.damageAmount.ToString(),
            so.attackCooldown.ToString(),
            so.bullet ? so.bullet.name : "Not Discovered"
        );
        _lastCurrentFound = true;
    }

    // visibility / refs
    bool AnyDiscovered() => entries.Any(e => e && e.found);

    void RefreshVisibility()
    {
        bool discovered = AnyDiscovered();

        if (!unitStatsRoot) unitStatsRoot = FindGO("UnitStats");
        if (!noUnitsDiscoveredText) noUnitsDiscoveredText = FindGO("NoUnitsDiscoveredText");

        if (noUnitsDiscoveredText) noUnitsDiscoveredText.SetActive(!discovered);
        if (unitStatsRoot) unitStatsRoot.SetActive(discovered);

        if (leftArrowButton) leftArrowButton.gameObject.SetActive(discovered);
        if (rightArrowButton) rightArrowButton.gameObject.SetActive(discovered);

        if (!discovered) ClearStatsTexts();
    }

    void ClearStatsTexts() => WriteLines("", "", "", "", "");

    void EnsureRefs()
    {
        if (!unitNameText) unitNameText = FindTMP("UnitNameText");
        if (!unitHealthText) unitHealthText = FindTMP("UnitHealthText");
        if (!unitDamageAmountText) unitDamageAmountText = FindTMP("UnitDamageAmountText");
        if (!attackCooldownText) attackCooldownText = FindTMP("AttackCooldownText");
        if (!bulletTypeText) bulletTypeText = FindTMP("BulletTypeText");

        if (!leftArrowButton) leftArrowButton = FindButton("LeftArrow");
        if (!rightArrowButton) rightArrowButton = FindButton("RightArrow");

        if (!unitStatsRoot) unitStatsRoot = FindGO("UnitStats");
        if (!noUnitsDiscoveredText) noUnitsDiscoveredText = FindGO("NoUnitsDiscoveredText");
    }

    // inactive-safe finders (filter out prefabs/assets using scene.IsValid)
    TMP_Text FindTMP(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            if (t && t.name == name && t.gameObject.scene.IsValid()) return t;
        return null;
    }

    Button FindButton(string name)
    {
        foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
            if (b && b.name == name && b.gameObject.scene.IsValid()) return b;
        return null;
    }

    GameObject FindGO(string name)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go && go.name == name && go.scene.IsValid()) return go; // active or inactive scene instance
        return null;
    }

    void WireButtons()
    {
        if (leftArrowButton)
        {
            leftArrowButton.onClick.RemoveListener(Prev);
            leftArrowButton.onClick.AddListener(Prev);
        }
        if (rightArrowButton)
        {
            rightArrowButton.onClick.RemoveListener(Next);
            rightArrowButton.onClick.AddListener(Next);
        }
    }

    void WriteLines(string name, string hp, string dmg, string cd, string bullet)
    {
        if (unitNameText) unitNameText.text = name;
        if (unitHealthText) unitHealthText.text = $"HP: {hp}";
        if (unitDamageAmountText) unitDamageAmountText.text = $"Damage Amount: {dmg}";
        if (attackCooldownText) attackCooldownText.text = $"Attack Cooldown : {cd}";
        if (bulletTypeText) bulletTypeText.text = $"Bullet Type: {bullet}";
    }

    string AddSpacesToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder(input.Length * 2);
        sb.Append(input[0]);
        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i], p = input[i - 1];
            bool boundary = (char.IsUpper(c) && char.IsLower(p)) ||
                            (char.IsUpper(c) && char.IsUpper(p) && i + 1 < input.Length && char.IsLower(input[i + 1]));
            if (boundary) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }

    // input (generated PlayerInput C# class)
    void WireInput()
    {
        UnwireInput();
        controls.Player.LeftNav.performed += OnLeftPerformed;
        controls.Player.RightNav.performed += OnRightPerformed;
        controls.Enable();
    }

    void UnwireInput()
    {
        if (controls == null) return;
        controls.Player.LeftNav.performed -= OnLeftPerformed;
        controls.Player.RightNav.performed -= OnRightPerformed;
        controls.Disable();
    }

    void OnLeftPerformed(InputAction.CallbackContext _) => Prev();
    void OnRightPerformed(InputAction.CallbackContext _) => Next();

    void CacheStates()
    {
        _lastAnyDiscovered = AnyDiscovered();
        _lastCurrentFound = (entries.Count > 0 && entries[Mathf.Clamp(_index, 0, entries.Count - 1)] && entries[_index].found);
    }
}
