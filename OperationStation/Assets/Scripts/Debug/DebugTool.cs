using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // ScrollRect, Layout

public class DebugTool : MonoBehaviour
{
    [Header("Enable/Disable")]
    public bool consoleAllowed = true;

    [Header("UI")]
    public GameObject consoleRoot;
    public TMP_InputField inputField;
    public TMP_Text outputText;

    // Scroll View bits
    public ScrollRect outputScroll;           // assign your Scroll View's ScrollRect
    public RectTransform contentRoot;         // assign ScrollRect.content (the parent of outputText)

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

    // Previous-command recall (Up Arrow)
    string lastCommand = "";

    // Output buffers / typewriter state
    readonly StringBuilder outputBuffer = new();
    readonly Queue<string> typeQueue = new();
    Coroutine typeCo;

    // Prevent same-frame submit (Enter/Escape) from skipping animation
    float _skipSuppressUntil;

    // Command registry
    readonly Dictionary<string, ICommand> commands =
        new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

    // --- Config ---
    const int MaxPerOperation = 1_000_000; // hard cap per add/remove command

    void Awake()
    {
        if (consoleRoot) consoleRoot.SetActive(false);
        if (inputField) inputField.onSubmit.AddListener(OnSubmit);

        // TMP label cosmetic margin
        if (outputText)
        {
            var m = outputText.margin; // left, top, right, bottom
            outputText.margin = new Vector4(m.x, outputTopMargin, m.z, m.w);
        }

        // Auto-configure the scroll content so it expands DOWN as the text grows
        ConfigureScrollContent();

        // Commands
        Register(new HelpCommand(() => commands)); // pass key/value pairs so Help sees aliases
        Register(new AddResourceCommand());
        Register(new RemoveResourceCommand());
        Register(new ListResourceTypesCommand());
        Register(new InvincibilityCommand(), "god", "godmode");
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

        // Re-input previous command when focused
        if (consoleRoot && consoleRoot.activeSelf && inputField && inputField.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && !string.IsNullOrEmpty(lastCommand))
            {
                inputField.text = lastCommand;
                MoveCaretToEnd();
            }
        }
    }

    // ===== Auto-layout config so parent expands DOWN =====
    void ConfigureScrollContent()
    {
        if (!contentRoot) return;

        // Ensure content grows downward: top-stretch anchors, top pivot
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = new Vector2(0f, 0f); // sit at top of viewport

        // Add/Configure VerticalLayoutGroup on the CONTENT parent.
        var vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (!vlg) vlg = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Fit to children
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

    // ===== Console plumbing =====
    void OnSubmit(string text)
    {
        if (!consoleAllowed) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        lastCommand = text;
        var (cmd, args) = Parse(text);
        string result = Execute(cmd, args);

        // CLEAR on every command and type this result only
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

    // ===== Output control (CLEAR + typewriter) =====
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

            _skipSuppressUntil = Time.unscaledTime + 0.1f; // debounce skip
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

    // ===== Scroll helpers =====
    void AutoScrollIfAtBottom()
    {
        if (!outputScroll) return;
        bool userAtBottom = outputScroll.verticalNormalizedPosition <= 0.02f;
        StartCoroutine(CoForceScroll(userAtBottom));
    }

    IEnumerator CoForceScroll(bool forceToBottom)
    {
        // Let layout rebuild first so sizes are up-to-date
        yield return null;
        if (forceToBottom && outputScroll)
            outputScroll.verticalNormalizedPosition = 0f; // 0 == bottom
    }

    void Register(ICommand command, params string[] aliases)
    {
        commands[command.Name] = command;                // primary
        foreach (var a in aliases) commands[a] = command; // aliases
    }

    // ===== Helpers visible to commands =====
    static DeathCat FindAnyDeathCat()
    {
        // includeInactive = true
        var all = GameObject.FindObjectsByType<DeathCat>(FindObjectsSortMode.None);
        return all.FirstOrDefault();
    }

    // --- FIX: Read ResourceManager's *private serialized fields* by name ---
    static class ResourceAccess
    {
        // Must match your private field names in ResourceManager exactly.
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

    // Normalizes and validates numeric amounts like "1000000", "1,000,000", "1_000_000"
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

            // Reject negatives right away
            if (trimmed.StartsWith("-"))
            {
                message = "Amount must be a positive integer.";
                return false;
            }

            // Allow digits, commas, underscores, and spaces only
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (!(char.IsDigit(c) || c == ',' || c == '_' || c == ' '))
                {
                    message = "Invalid number format. Use digits only (commas/underscores/spaces optional).";
                    return false;
                }
            }

            // Strip separators
            var digitsOnly = new StringBuilder(trimmed.Length);
            foreach (char c in trimmed)
                if (char.IsDigit(c)) digitsOnly.Append(c);

            if (digitsOnly.Length == 0)
            {
                message = "Amount must contain digits.";
                return false;
            }

            // Try parsing as long first to detect overflow cleanly
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

    // ===== Command API =====
    public interface ICommand
    {
        string Name { get; }
        string Usage { get; }
        string Help { get; }
        string Run(string[] args);
    }

    // Help shows aliases and prints:
    // <b>name</b> <usage>
    //   description
    //   (aliases: a/b)
    class HelpCommand : ICommand
    {
        public string Name => "help";
        public string Usage => "";
        public string Help => "List available commands.";

        readonly Func<IEnumerable<KeyValuePair<string, ICommand>>> _getAllPairs;
        public HelpCommand(Func<IEnumerable<KeyValuePair<string, ICommand>>> getAllPairs)
            => _getAllPairs = getAllPairs;

        // Styling
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

    // Reference-equality comparer for dictionary keyed by ICommand
    sealed class RefEq<T> : IEqualityComparer<T> where T : class
    {
        public static readonly RefEq<T> Instance = new();
        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    // ===== Resource helpers =====
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

    // ===== Resource commands =====
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
                return parseMsg; // specific feedback (format/overflow/negative/etc.)

            // Enforce cap explicitly with clear feedback
            int toAdd = requested;
            bool capped = false;
            if (toAdd > MaxPerOperation)
            {
                toAdd = MaxPerOperation;
                capped = true;
            }

            ResourceManager.instance.AddResource(type, toAdd);

            // Build a friendly response
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
                return parseMsg; // specific feedback (format/overflow/negative/etc.)

            // Mirror the add cap for removes too (optional, keeps UX consistent)
            if (requested > MaxPerOperation) requested = MaxPerOperation;

            // Read current via private-field reflection
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

            // Build friendly message
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

    // ===== Other Commands =====
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
}
