using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class Instructions : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] TextMeshProUGUI textDisplay;
    [SerializeField] Button ContinueButton;
    [SerializeField] Button BackButton;

    private int currentBlock = 0;
    private string lastUsedDevice = "Keyboard/Mouse";

    void Start()
    {
        Time.timeScale = 0;
        InputSystem.onAnyButtonPress.CallOnce(ctrl =>
        {
            if (ctrl.device is Keyboard || ctrl.device is Mouse)
                lastUsedDevice = "Keyboard/Mouse";
            else if (ctrl.device is Gamepad)
                lastUsedDevice = "Gamepad";

            Debug.Log("Last used device: " + lastUsedDevice);
        });

        TextBlock();
    }

    public void CycleForward()
    {
        if (currentBlock < 11)
        {
            ContinueButton.enabled = true;
            BackButton.enabled = true;
            currentBlock++;
            TextBlock();
        }
        else if (currentBlock == 11)
        {
            StateUnpause();
        }
    }

    public void CycleBack()
    {
        if (currentBlock > 0)
        {
            ContinueButton.enabled = true;
            BackButton.enabled = true;
            currentBlock--;
            TextBlock();
        }
        else
            BackButton.enabled = false;
    }

    void TextBlock()
    {
        string device = lastUsedDevice;
        string bindString;
        switch (currentBlock)
        {
            case 0:
                bindString = GetBindingDisplayName("Move", device);
                textDisplay.text = $"Use {bindString} to move the camera horizontally";
                break;

            case 1:
                bindString = GetBindingDisplayName("Zoom", device);
                textDisplay.text = $"Use {bindString} to zoom the camera in and out";
                break;

            case 2:
                bindString = GetBindingDisplayName("Rotate", device);
                textDisplay.text = $"Press {bindString} to rotate the camera left and right";
                break;

            case 3:
                bindString = GetBindingDisplayName("Select", device);
                textDisplay.text = $"Click on the minimap with {bindString} to move there";
                break;

            case 4:
                bindString = GetBindingDisplayName("Select", device);
                textDisplay.text = $"Click or drag with {bindString} to select units";
                break;

            case 5:
                bindString = GetBindingDisplayName("Focus", device);
                textDisplay.text = $"Press {bindString} to focus on a selected unit";
                break;

            case 6:
                textDisplay.text = "Selected mining ships can mine asteroids if you click on the ship and then the asteroid";
                break;

            case 7:
                textDisplay.text = "To build mining ships, build the Null Space Fabricator";
                break;

            case 8:
                textDisplay.text = "Mining these asteroids will give you resources";
                break;

            case 9:
                textDisplay.text = "You can also gain resources by building the Solar Panel Array, Polonium Reactor, and Macro Particle Smelter";
                break;

            case 10:
                textDisplay.text = "Build the Death Cat module with the resources you gain to win";
                break;

            case 11:
                textDisplay.text = "Make sure to build defenses to protect the Death Cat";
                break;
        }
    }

    string GetBindingDisplayName(string actionName, string deviceFilter)
    {
        InputAction action = inputActions.FindAction(actionName);
        if (action == null)
            return "ActionIsNull";

        var deviceBindings = new Dictionary<string, List<string>>();
        var visitedComposites = new HashSet<string>();

        for (int bindDex = 0; bindDex < action.bindings.Count; bindDex++)
        {
            var binding = action.bindings[bindDex];

            if (binding.isComposite)
            {
                string compositeId = $"{binding.name}_{binding.groups}";
                if (visitedComposites.Contains(compositeId))
                    continue;
                visitedComposites.Add(compositeId);

                string deviceType = null;

                for (int comDex = bindDex + 1; comDex < action.bindings.Count; comDex++)
                {
                    var part = action.bindings[comDex];
                    if (!part.isPartOfComposite) break;

                    string partDevice = GetDeviceType(part.effectivePath);
                    if (partDevice == "Other") partDevice = "Keyboard/Mouse";

                    if (deviceType == null) deviceType = partDevice;
                    else if (deviceType != partDevice)
                    {
                        deviceType = "Mixed";
                        break;
                    }
                }

                var parts = new Dictionary<string, string>();

                for (int comDex = bindDex + 1; comDex < action.bindings.Count; comDex++)
                {
                    var part = action.bindings[comDex];
                    if (!part.isPartOfComposite)
                        break;

                    string readable = InputControlPath.ToHumanReadableString(part.effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice);

                    parts[part.name] = readable;
                }

                string formatted = FormatComposite(binding.name, parts);
                if (!deviceBindings.ContainsKey(deviceType))
                    deviceBindings[deviceType] = new List<string>();
                deviceBindings[deviceType].Add(formatted);
            }
            else if (!binding.isPartOfComposite)
            {
                string deviceType = GetDeviceType(binding.effectivePath);
                if (deviceType == "Other")
                    deviceType = "Keyboard/Mouse";

                string readable = InputControlPath.ToHumanReadableString(binding.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);

                if (!deviceBindings.ContainsKey(deviceType))
                    deviceBindings[deviceType] = new List<string>();
                deviceBindings[deviceType].Add(readable);
            }
        }

        List<string> output = new List<string>();
        foreach (var bind in deviceBindings)
        {
            if (deviceFilter != null && bind.Key != deviceFilter)
                continue;

            string bindings = string.Join(" / ", bind.Value);
            output.Add($"{bindings}");
        }

        return output.Count > 0 ? string.Join("\n", output) : "Unbound";
    }

    string FormatComposite(string compositeName, Dictionary<string, string> parts)
    {
        if (compositeName == "2DVector")
        {
            string[] order = { "Up", "Left", "Down", "Right" };
            return string.Join(" / ", order.Where(parts.ContainsKey).Select(k => parts[k]));
        }
        else if (parts.ContainsKey("negative") || parts.ContainsKey("positive"))
            return $"{parts.GetValueOrDefault("negative")} / {parts.GetValueOrDefault("positive")}";
        else
            return string.Join(" / ", parts.Values);
    }

    string GetDeviceType(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Unknown";
        if (path.Contains("Keyboard") || path.Contains("Mouse"))
            return "Keyboard/Mouse";
        if (path.Contains("Gamepad"))
            return "Gamepad";

        return "Other";
    }

    public void StateUnpause()
    {
        Time.timeScale = 1;
        gameObject.SetActive(false);
    }
}
