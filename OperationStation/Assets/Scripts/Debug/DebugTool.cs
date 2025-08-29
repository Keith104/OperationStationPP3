using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugTool : MonoBehaviour
{
    [Header("Enable/Disable")]
    public bool consoleAllowed = true;

    [Header("UI")]
    public GameObject consoleRoot;
    public TMP_InputField inputField;
    public TMP_Text outputText;

    public ScrollRect outputScroll;
    public RectTransform contentRoot;

    [Header("Controls")]
    public KeyCode togglekey = KeyCode.BackQuote;

    [Header("Output FX")]
    [Tooltip("Animate text output character-by-character.")]
    public bool useTypewriter = true;
    [Range(5, 400)] public float typewriterCharsPerSecond = 40f;
    [Tooltip("Extra space above the first line of output (pixels).")]
    public float outputTopMargin = 8f;
    [Tooltip("Hold or tap to instantly finish the current typewriter chunk.")]
    public KeyCode skipTypewriterKey = KeyCode.Tab;

    string lastCommand = "";

    readonly StringBuilder outputBuffer = new();
    readonly Queue<string> typeQueue = new();
    Coroutine typeCo;

    float _skipSuppressUntil;

    readonly Dictionary<string, ICommand> commands =
        new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

    const int MaxPerOperation = 1_000_000;

    void Awake()
    {
        if (consoleRoot) consoleRoot.SetActive(false);
        if (inputField) inputField.onSubmit.AddListener(OnSubmit);

        if (outputText)
        {
            var m = outputText.margin;
            outputText.margin = new Vector4(m.x, outputTopMargin, m.z, m.w);
        }

        ConfigureScrollContent();

        Register(new HelpCommand(() => commands));
        Register(new AddResourceCommand());
        Register(new RemoveResourceCommand());
        Register(new ListResourceTypesCommand());
        Register(new InvincibilityCommand(), "god", "godmode");
        Register(new UIChildrenCommand(transform), "uichildren", "uioff", "uion");
    }

    void Update()
    {
        if (!consoleAllowed) return;

        if (Input.GetKeyDown(togglekey) && consoleRoot)
        {
            bool show = !consoleRoot.activeSelf;
            consoleRoot.SetActive(show);
            if (show && inputField)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }
        }

        if (consoleRoot && consoleRoot.activeSelf && inputField && inputField.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && !string.IsNullOrEmpty(lastCommand))
            {
                inputField.text = lastCommand;
                MoveCaretToEnd();
            }
        }
    }

    void ConfigureScrollContent()
    {
        if (!contentRoot) return;

        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = new Vector2(0f, 0f);

        var vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (!vlg) vlg = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (outputText)
        {
            var childFitter = outputText.GetComponent<ContentSizeFitter>();
            if (childFitter) Destroy(childFitter);
            var rt = outputText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
        }
    }

    void OnSubmit(string text)
    {
        if (!consoleAllowed) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        lastCommand = text;
        var (cmd, args) = Parse(text);
        string result = Execute(cmd, args);

        SetOutput(result ?? "");

        inputField.text = "";
        inputField.ActivateInputField();
    }

    (string cmd, string[] args) Parse(string input)
    {
        var tokens = new List<string>();
        var cur = new StringBuilder();
        bool inQuotes = false;

        foreach (char ch in input)
        {
            if (ch == '"') { inQuotes = !inQuotes; continue; }
            if (!inQuotes && char.IsWhiteSpace(ch))
            {
                if (cur.Length > 0) { tokens.Add(cur.ToString()); cur.Clear(); }
            }
            else cur.Append(ch);
        }

        if (cur.Length > 0) tokens.Add(cur.ToString());
        if (tokens.Count == 0) return ("", Array.Empty<string>());

        string cmd = tokens[0];
        tokens.RemoveAt(0);
        return (cmd, tokens.ToArray());
    }

    string Execute(string cmd, string[] args)
    {
        if (string.IsNullOrEmpty(cmd)) return "";
        if (!commands.TryGetValue(cmd, out var c))
            return $"Unknown command '{cmd}'. Type 'help'.";

        try { return c.Run(args) ?? ""; }
        catch (Exception e) { return $"Error: {e.Message}"; }
    }

    void SetOutput(string textToShow)
    {
        if (!string.IsNullOrEmpty(textToShow))
            Debug.Log($"[Console] {textToShow}");

        if (typeCo != null) { StopCoroutine(typeCo); typeCo = null; }
        typeQueue.Clear();
        outputBuffer.Clear();

        if (!outputText)
        {
            outputBuffer.Append(textToShow);
            return;
        }

        if (useTypewriter && textToShow.Length > 0)
        {
            outputText.text = "";
            typeQueue.Enqueue(textToShow);

            _skipSuppressUntil = Time.unscaledTime + 0.1f;
            typeCo = StartCoroutine(TypewriterCo());
        }
        else
        {
            outputBuffer.Append(textToShow);
            outputText.text = outputBuffer.ToString();
            AutoScrollIfAtBottom();
        }
    }

    IEnumerator TypewriterCo()
    {
        float cps = Mathf.Max(1f, typewriterCharsPerSecond);
        float interval = 1f / cps;
        float nextTick = Time.unscaledTime;

        while (typeQueue.Count > 0)
        {
            string next = typeQueue.Dequeue();
            int i = 0;

            while (i < next.Length)
            {
                if (IsSkipRequested())
                {
                    outputBuffer.Append(next, i, next.Length - i);
                    i = next.Length;
                    if (outputText) outputText.text = outputBuffer.ToString();
                    AutoScrollIfAtBottom();
                    break;
                }

                if (Time.unscaledTime >= nextTick)
                {
                    outputBuffer.Append(next[i]);
                    i++;
                    nextTick += interval;

                    if (outputText) outputText.text = outputBuffer.ToString();
                    AutoScrollIfAtBottom();
                }

                yield return null;
            }
        }

        typeCo = null;
    }

    bool IsSkipRequested()
    {
        if (Time.unscaledTime < _skipSuppressUntil) return false;
        return Input.GetKey(skipTypewriterKey)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.Escape);
    }

    void MoveCaretToEnd()
    {
        inputField.caretPosition = inputField.text.Length;
        inputField.stringPosition = inputField.text.Length;
        inputField.ForceLabelUpdate();
    }

    void AutoScrollIfAtBottom()
    {
        if (!outputScroll) return;
        bool userAtBottom = outputScroll.verticalNormalizedPosition <= 0.02f;
        StartCoroutine(CoForceScroll(userAtBottom));
    }

    IEnumerator CoForceScroll(bool forceToBottom)
    {
        yield return null;
        if (forceToBottom && outputScroll)
            outputScroll.verticalNormalizedPosition = 0f;
    }

    void Register(ICommand command, params string[] aliases)
    {
        commands[command.Name] = command;
        foreach (var a in aliases) commands[a] = command;
    }

    static DeathCat FindAnyDeathCat()
    {
        var all = GameObject.FindObjectsByType<DeathCat>(FindObjectsSortMode.None);
        return all.FirstOrDefault();
    }

    static class ResourceAccess
    {
        static readonly Dictionary<ResourceSO.ResourceType, string> FieldByType =
            new Dictionary<ResourceSO.ResourceType, string>
            {
                { ResourceSO.ResourceType.Tritium,          "tritium" },
                { ResourceSO.ResourceType.Silver,           "silver" },
                { ResourceSO.ResourceType.Polonium,         "polonium" },
                { ResourceSO.ResourceType.TritiumIngot,     "tritiumIngot" },
                { ResourceSO.ResourceType.SilverCoin,       "silverCoins" },
                { ResourceSO.ResourceType.PoloniumCrystal,  "poloniumCrystal" },
                { ResourceSO.ResourceType.Energy,           "energy" },
            };

        static BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        public static bool TryGetAmount(ResourceSO.ResourceType type, out int amount)
        {
            amount = 0;
            var rm = ResourceManager.instance;
            if (rm == null) return false;

            if (!FieldByType.TryGetValue(type, out var fieldName) || string.IsNullOrEmpty(fieldName))
                return false;

            var fi = typeof(ResourceManager).GetField(fieldName, Flags);
            if (fi == null) return false;

            try
            {
                object val = fi.GetValue(rm);
                amount = Convert.ToInt32(val);
                return true;
            }
            catch { return false; }
        }
    }

    static class AmountInput
    {
        public static bool TryParsePositive(string raw, out int amount, out string message)
        {
            amount = 0;
            message = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                message = "Amount is required.";
                return false;
            }

            string trimmed = raw.Trim();

            if (trimmed.StartsWith("-"))
            {
                message = "Amount must be a positive integer.";
                return false;
            }

            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (!(char.IsDigit(c) || c == ',' || c == '_' || c == ' '))
                {
                    message = "Invalid number format. Use digits only (commas/underscores/spaces optional).";
                    return false;
                }
            }

            var digitsOnly = new StringBuilder(trimmed.Length);
            foreach (char c in trimmed)
                if (char.IsDigit(c)) digitsOnly.Append(c);

            if (digitsOnly.Length == 0)
            {
                message = "Amount must contain digits.";
                return false;
            }

            if (!long.TryParse(digitsOnly.ToString(), out long big))
            {
                message = "That number is too large.";
                return false;
            }

            if (big <= 0)
            {
                message = "Amount must be a positive integer.";
                return false;
            }

            if (big > int.MaxValue)
            {
                message = $"Amount exceeds this console's limit ({int.MaxValue:n0}).";
                return false;
            }

            amount = (int)big;
            return true;
        }
    }

    public interface ICommand
    {
        string Name { get; }
        string Usage { get; }
        string Help { get; }
        string Run(string[] args);
    }

    class HelpCommand : ICommand
    {
        public string Name => "help";
        public string Usage => "";
        public string Help => "List available commands.";

        readonly Func<IEnumerable<KeyValuePair<string, ICommand>>> _getAllPairs;
        public HelpCommand(Func<IEnumerable<KeyValuePair<string, ICommand>>> getAllPairs)
            => _getAllPairs = getAllPairs;

        const string CmdColor = "#FFFFFF";
        const string UsageColor = "#B9D4FF";
        const string DescColor = "#D1D5DB";
        const string AliasColor = "#9AA4B2";
        const int DescPct = 92;

        public string Run(string[] args)
        {
            var byCmd = new Dictionary<ICommand, HashSet<string>>(RefEq<ICommand>.Instance);
            foreach (var kv in _getAllPairs())
            {
                if (!byCmd.TryGetValue(kv.Value, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    byCmd[kv.Value] = set;
                }
                set.Add(kv.Key);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Commands:");

            foreach (var kv in byCmd)
            {
                var cmd = kv.Key;

                var names = new List<string>(kv.Value);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                int primaryIndex = names.FindIndex(n => string.Equals(n, cmd.Name, StringComparison.OrdinalIgnoreCase));
                if (primaryIndex > 0) (names[0], names[primaryIndex]) = (names[primaryIndex], names[0]);

                string primary = names[0];
                string aliasTail = names.Count > 1 ? string.Join("/", names.GetRange(1, names.Count - 1)) : null;

                string usage = string.IsNullOrEmpty(cmd.Usage) ? "" :
                    $" <color={UsageColor}><mspace=0.6em><noparse>{cmd.Usage}</noparse></mspace></color>";
                sb.AppendLine($"<b><color={CmdColor}>{primary}</color></b>{usage}");

                if (!string.IsNullOrEmpty(cmd.Help))
                    sb.AppendLine($"<indent=6%><size={DescPct}%><color={DescColor}>{cmd.Help}</color></size></indent>");

                if (!string.IsNullOrEmpty(aliasTail))
                    sb.AppendLine($"<indent=6%><color={AliasColor}>(aliases: {aliasTail})</color></indent>");

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }

    sealed class RefEq<T> : IEqualityComparer<T> where T : class
    {
        public static readonly RefEq<T> Instance = new();
        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    static class ResourceParse
    {
        static readonly Dictionary<string, ResourceSO.ResourceType> map =
            new Dictionary<string, ResourceSO.ResourceType>(StringComparer.OrdinalIgnoreCase)
            {
                ["tritium"] = ResourceSO.ResourceType.Tritium,
                ["silver"] = ResourceSO.ResourceType.Silver,
                ["polonium"] = ResourceSO.ResourceType.Polonium,
                ["ingot"] = ResourceSO.ResourceType.TritiumIngot,
                ["ingots"] = ResourceSO.ResourceType.TritiumIngot,
                ["coin"] = ResourceSO.ResourceType.SilverCoin,
                ["coins"] = ResourceSO.ResourceType.SilverCoin,
                ["crystal"] = ResourceSO.ResourceType.PoloniumCrystal,
                ["crystals"] = ResourceSO.ResourceType.PoloniumCrystal,
                ["energy"] = ResourceSO.ResourceType.Energy,
            };

        public static bool TryParse(string raw, out ResourceSO.ResourceType type)
            => map.TryGetValue(Normalize(raw), out type);

        public static string ValidList()
            => "tritium, silver, polonium, ingots, coins, crystals, energy";

        static string Normalize(string s)
        {
            s = s?.Trim().ToLowerInvariant() ?? "";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == ' ' || c == '_' || c == '-') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }
    }

    class AddResourceCommand : ICommand
    {
        public string Name => "add";
        public string Usage => "<type> <amount>";
        public string Help => $"Add resources (per-command cap: {MaxPerOperation:n0}). Types: {ResourceParse.ValidList()}";

        public string Run(string[] args)
        {
            if (ResourceManager.instance == null)
                return "ResourceManager.instance is null. Ensure it exists in the scene.";

            if (args.Length < 2)
                return "Usage: add <type> <amount>\nTypes: " + ResourceParse.ValidList();

            if (!ResourceParse.TryParse(args[0], out var type))
                return $"Unknown resource '{args[0]}'. Types: {ResourceParse.ValidList()}";

            if (!AmountInput.TryParsePositive(args[1], out var requested, out var parseMsg))
                return parseMsg;

            int toAdd = requested;
            bool capped = false;
            if (toAdd > MaxPerOperation)
            {
                toAdd = MaxPerOperation;
                capped = true;
            }

            ResourceManager.instance.AddResource(type, toAdd);

            var sb = new StringBuilder();
            if (capped)
                sb.Append($"Requested {requested:n0}, but the per-command cap is {MaxPerOperation:n0}. Added {toAdd:n0} instead.");
            else
                sb.Append($"Added {toAdd:n0} {args[0]}.");

            if (ResourceAccess.TryGetAmount(type, out var now))
                sb.Append($" New balance: {now:n0}.");

            return sb.ToString();
        }
    }

    class RemoveResourceCommand : ICommand
    {
        public string Name => "remove";
        public string Usage => "<type> <amount>";
        public string Help => "Remove resources (won't go below zero). Types: " + ResourceParse.ValidList();

        public string Run(string[] args)
        {
            if (ResourceManager.instance == null)
                return "ResourceManager.instance is null. Ensure it exists in the scene.";

            if (args.Length < 2)
                return "Usage: remove <type> <amount>\nTypes: " + ResourceParse.ValidList();

            if (!ResourceParse.TryParse(args[0], out var type))
                return $"Unknown resource '{args[0]}'. Types: {ResourceParse.ValidList()}";

            if (!AmountInput.TryParsePositive(args[1], out var requested, out var parseMsg))
                return parseMsg;

            if (requested > MaxPerOperation) requested = MaxPerOperation;

            if (!ResourceAccess.TryGetAmount(type, out var current))
                return "Couldn't read current balance; aborting remove to avoid going negative.";

            if (current <= 0)
                return $"You have 0 {args[0]}; nothing to remove.";

            int toRemove = requested;
            bool clampedToAvailable = false;

            if (toRemove > current)
            {
                toRemove = current;
                clampedToAvailable = true;
            }

            if (toRemove > 0)
                ResourceManager.instance.RemoveResource(type, toRemove);

            var sb = new StringBuilder();
            if (clampedToAvailable)
                sb.Append($"Requested to remove {requested:n0}, but you only have {current:n0}. Removed {toRemove:n0}.");
            else
                sb.Append($"Removed {toRemove:n0} {args[0]}.");

            if (ResourceAccess.TryGetAmount(type, out var now))
                sb.Append($" New balance: {now:n0}.");

            return sb.ToString();
        }
    }

    class ListResourceTypesCommand : ICommand
    {
        public string Name => "listresources";
        public string Usage => "";
        public string Help => $"Show valid resource types. Per-command cap: {MaxPerOperation:n0}.";

        public string Run(string[] args)
        {
            return "Valid resource types: " + ResourceParse.ValidList() +
                   $"\nPer-command cap: {MaxPerOperation:n0}\nExamples:\n" +
                   "  add tritium 25\n" +
                   "  add silver 100\n" +
                   "  add ingots 5\n" +
                   "  add coins 20\n" +
                   "  add crystals 2\n" +
                   "  remove tritium 5\n" +
                   "  remove coins 10";
        }
    }

    class InvincibilityCommand : ICommand
    {
        public string Name => "invincibility";
        public string Usage => "[true|false|on|off|1|0]";
        public string Help => "Enable/disable damage for the DeathCat in the scene.";

        public string Run(string[] args)
        {
            if (args.Length < 1)
                return "Usage: invincibility <true|false>";

            if (!TryParseBool(args[0], out var value))
                return "Invalid value. Use true/false (also accepts on/off/1/0).";

            var dc = FindAnyDeathCat();
            if (!dc) return "No DeathCat found in the scene.";

            dc.invincible = value;
            return $"DeathCat invincibility: {(value ? "ON" : "OFF")}.";
        }

        static bool TryParseBool(string raw, out bool value)
        {
            if (bool.TryParse(raw, out value)) return true;

            switch (raw.Trim().ToLowerInvariant())
            {
                case "1":
                case "on":
                case "enable":
                case "enabled":
                    value = true; return true;
                case "0":
                case "off":
                case "disable":
                case "disabled":
                    value = false; return true;
                default:
                    value = default; return false;
            }
        }
    }

    class UIChildrenCommand : ICommand
    {
        public string Name => "uichildren";
        public string Usage => "<on|off>";
        public string Help => "Enable/disable all UI (uGUI) children under this DebugTool's GameObject.";

        readonly Transform _root;
        readonly HashSet<GameObject> _disabled = new HashSet<GameObject>();

        public UIChildrenCommand(Transform root) => _root = root;

        public string Run(string[] args)
        {
            if (args.Length < 1) return "Usage: uichildren <on|off>";

            if (!TryParseBool(args[0], out bool turnOn))
                return "Invalid value. Use on/off (also accepts true/false/1/0).";

            if (turnOn)
            {
                int count = 0;
                foreach (var go in _disabled)
                {
                    if (go) { go.SetActive(true); count++; }
                }
                _disabled.Clear();
                return $"Re-enabled {count:n0} UI object(s) under '{_root.name}'.";
            }
            else
            {
                var crs = _root.GetComponentsInChildren<CanvasRenderer>(includeInactive: true);
                int count = 0;

                for (int i = 0; i < crs.Length; i++)
                {
                    var go = crs[i].gameObject;
                    if (go == _root.gameObject) continue;
                    if (go.activeSelf)
                    {
                        go.SetActive(false);
                        _disabled.Add(go);
                        count++;
                    }
                }

                return $"Disabled {count:n0} UI object(s) under '{_root.name}'. Use 'uichildren on' to undo.";
            }
        }

        static bool TryParseBool(string raw, out bool value)
        {
            if (bool.TryParse(raw, out value)) return true;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "1":
                case "on":
                case "enable":
                case "enabled": value = true; return true;
                case "0":
                case "off":
                case "disable":
                case "disabled": value = false; return true;
                default: value = default; return false;
            }
        }
    }
}
