using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DebugTool : MonoBehaviour
{
    [Header("Enable/Disable")]
    public bool consoleAllowed = true;

    [Header("UI")]
    public GameObject consoleRoot;
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("Controls")]
    public KeyCode togglekey = KeyCode.BackQuote;

    [Header("Output FX")]
    [Tooltip("Animate text output character-by-character.")]
    public bool useTypewriter = true;
    [Range(5, 400)] public float typewriterCharsPerSecond = 40f; // slower so it’s visible
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

    void Awake()
    {
        if (consoleRoot) consoleRoot.SetActive(false);
        if (inputField) inputField.onSubmit.AddListener(OnSubmit);

        if (outputText)
        {
            var m = outputText.margin; // left, top, right, bottom
            outputText.margin = new Vector4(m.x, outputTopMargin, m.z, m.w);
        }

        // Commands
        Register(new HelpCommand(() => commands.Values));
        Register(new AddResourceCommand());
        Register(new RemoveResourceCommand());
        Register(new ListResourceTypesCommand());
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
        // Always log full line
        if (!string.IsNullOrEmpty(textToShow))
            Debug.Log($"[Console] {textToShow}");

        // Stop any running animation and reset buffers
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
            outputText.text = "";        // CLEAR the label immediately
            typeQueue.Enqueue(textToShow);

            // Debounce skip so the submit-frame Enter/Escape can't instantly complete
            _skipSuppressUntil = Time.unscaledTime + 0.1f;

            typeCo = StartCoroutine(TypewriterCo());
        }
        else
        {
            outputBuffer.Append(textToShow);
            outputText.text = outputBuffer.ToString();
        }
    }

    IEnumerator TypewriterCo()
    {
        float cps = Mathf.Max(1f, typewriterCharsPerSecond);
        float interval = 1f / cps;
        float nextTick = Time.unscaledTime; // start immediately

        while (typeQueue.Count > 0)
        {
            string next = typeQueue.Dequeue();
            int i = 0;

            while (i < next.Length)
            {
                // Instant-complete if requested (after debounce window)
                if (IsSkipRequested())
                {
                    outputBuffer.Append(next, i, next.Length - i);
                    i = next.Length;
                    if (outputText) outputText.text = outputBuffer.ToString();
                    break;
                }

                // Emit exactly ONE character per tick
                if (Time.unscaledTime >= nextTick)
                {
                    outputBuffer.Append(next[i]);
                    i++;
                    nextTick += interval;

                    if (outputText) outputText.text = outputBuffer.ToString();
                }

                yield return null; // important: first yield happens after checks
            }
        }

        typeCo = null;
    }

    bool IsSkipRequested()
    {
        if (Time.unscaledTime < _skipSuppressUntil) return false; // ignore early frame(s)
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

    void Register(ICommand command, params string[] aliases)
    {
        commands[command.Name] = command;
        foreach (var a in aliases) commands[a] = command;
    }

    // ===== Command API =====
    public interface ICommand
    {
        string Name { get; }
        string Help { get; }
        string Run(string[] args);
    }

    class HelpCommand : ICommand
    {
        public string Name => "help";
        public string Help => "help -- List available commands.";
        readonly Func<IEnumerable<ICommand>> _getAll;
        public HelpCommand(Func<IEnumerable<ICommand>> getAll) => _getAll = getAll;

        public string Run(string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Commands:");
            foreach (var c in _getAll())
                sb.AppendLine(c.Help);
            return sb.ToString().TrimEnd();
        }
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
            };

        public static bool TryParse(string raw, out ResourceSO.ResourceType type)
            => map.TryGetValue(Normalize(raw), out type);

        public static string ValidList()
            => "tritium, silver, polonium, ingots, coins, crystals";

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
        public string Help => "add <type> <amount> -- Add resources. Types: tritium, silver, polonium, ingots, coins, crystals";

        public string Run(string[] args)
        {
            if (ResourceManager.instance == null)
                return "ResourceManager.instance is null. Ensure it exists in the scene.";

            if (args.Length < 2)
                return "Usage: add <type> <amount>\nTypes: " + ResourceParse.ValidList();

            if (!ResourceParse.TryParse(args[0], out var type))
                return $"Unknown resource '{args[0]}'. Types: {ResourceParse.ValidList()}";

            if (!int.TryParse(args[1], out var amount) || amount <= 0)
                return "Amount must be a positive integer.";

            ResourceManager.instance.AddResource(type, amount);
            return $"OK: Added {amount} {args[0]}.";
        }
    }

    class RemoveResourceCommand : ICommand
    {
        public string Name => "remove";
        public string Help => "remove <type> <amount> -- Remove resources. Types: tritium, silver, polonium, ingots, coins, crystals";

        public string Run(string[] args)
        {
            if (ResourceManager.instance == null)
                return "ResourceManager.instance is null. Ensure it exists in the scene.";

            if (args.Length < 2)
                return "Usage: remove <type> <amount>\nTypes: " + ResourceParse.ValidList();

            if (!ResourceParse.TryParse(args[0], out var type))
                return $"Unknown resource '{args[0]}'. Types: {ResourceParse.ValidList()}";

            if (!int.TryParse(args[1], out var amount) || amount <= 0)
                return "Amount must be a positive integer.";

            ResourceManager.instance.RemoveResource(type, amount);
            return $"OK: Removed {amount} {args[0]}.";
        }
    }

    class ListResourceTypesCommand : ICommand
    {
        public string Name => "listresources";
        public string Help => "listresources -- Show valid resource types.";

        public string Run(string[] args)
        {
            return "Valid resource types: " + ResourceParse.ValidList() +
                   "\nExamples:\n" +
                   "  add tritium 25\n" +
                   "  add silver 100\n" +
                   "  add ingots 5\n" +
                   "  add coins 20\n" +
                   "  add crystals 2\n" +
                   "  remove tritium 5\n" +
                   "  remove coins 10";
        }
    }
}
