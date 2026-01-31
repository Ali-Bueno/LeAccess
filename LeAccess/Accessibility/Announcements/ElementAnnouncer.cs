using ChatView;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility.Announcements;

/// <summary>
/// Handles announcing UI elements to the screen reader.
/// </summary>
public static class ElementAnnouncer
{
    private static int _lastAnnouncedInstanceId;
    private static float _lastAnnounceTime;
    private const float AnnounceCooldown = 0.15f;

    // Track toggle changes to announce state updates
    internal static readonly Dictionary<int, bool> TogglePreviousStates = new();

    // Track custom toggle components for state change detection
    private static ActiveWhileMoveToAttackEnabled _trackedMoveToAttack;
    private static bool _trackedMoveToAttackLastState;
    private static ActiveWhileFriendAddNotificationEnabled _trackedFriendNotif;
    private static bool _trackedFriendNotifLastState;

    // Cache for focused element lookup
    private static GameObject _cachedFocusedGo;
    private static int _cachedFocusedId;

    /// <summary>
    /// Gets the last announced instance ID for toggle state tracking.
    /// </summary>
    public static int LastAnnouncedInstanceId => _lastAnnouncedInstanceId;

    /// <summary>
    /// Announces a UI element to the screen reader.
    /// </summary>
    public static void AnnounceElement(GameObject go)
    {
        if (go == null) return;

        var instanceId = go.GetInstanceID();
        var now = Time.time;

        if (instanceId == _lastAnnouncedInstanceId && (now - _lastAnnounceTime) < AnnounceCooldown)
            return;

        _lastAnnouncedInstanceId = instanceId;
        _lastAnnounceTime = now;

        // DEBUG: Log all element names to find what class selection uses
        var debugParent = go.transform.parent?.name ?? "no-parent";
        var hasCharCreation = go.GetComponentInParent<CharacterCreationSelection>() != null;
        Plugin.Log.LogInfo($"[DEBUG Element] name={go.name}, parent={debugParent}, hasCharCreationInParent={hasCharCreation}");

        // Check for custom toggle state changes
        var parentName = go.transform.parent?.name ?? "no-parent";
        if (parentName.Contains("Popup") || parentName.Contains("Move") || parentName.Contains("Attack") ||
            parentName.Contains("social") || parentName.Contains("Social") || parentName.Contains("Friend"))
        {
            var moveToAttack = go.GetComponent<ActiveWhileMoveToAttackEnabled>()
                ?? go.GetComponentInParent<ActiveWhileMoveToAttackEnabled>()
                ?? go.GetComponentInChildren<ActiveWhileMoveToAttackEnabled>();
            var friendNotif = go.GetComponent<ActiveWhileFriendAddNotificationEnabled>()
                ?? go.GetComponentInParent<ActiveWhileFriendAddNotificationEnabled>()
                ?? go.GetComponentInChildren<ActiveWhileFriendAddNotificationEnabled>();

            bool stateChanged = false;
            if (moveToAttack != null)
            {
                bool currentState = moveToAttack.moveAttackEnabled;
                if (_trackedMoveToAttack != null && _trackedMoveToAttack.Pointer == moveToAttack.Pointer)
                {
                    if (currentState != _trackedMoveToAttackLastState)
                    {
                        ScreenReader.Speak(currentState ? "On" : "Off", true);
                        stateChanged = true;
                    }
                }
                _trackedMoveToAttack = moveToAttack;
                _trackedMoveToAttackLastState = currentState;
            }
            if (friendNotif != null)
            {
                bool currentState = friendNotif.hideUnknownSocialRequests;
                if (_trackedFriendNotif != null && _trackedFriendNotif.Pointer == friendNotif.Pointer)
                {
                    if (currentState != _trackedFriendNotifLastState)
                    {
                        ScreenReader.Speak(currentState ? "On" : "Off", true);
                        stateChanged = true;
                    }
                }
                _trackedFriendNotif = friendNotif;
                _trackedFriendNotifLastState = currentState;
            }
            if (stateChanged)
                return;
        }

        var text = BuildAnnouncementText(go);
        if (!string.IsNullOrWhiteSpace(text))
        {
            ScreenReader.Speak(text);
        }
    }

    /// <summary>
    /// Handles toggle value change announcements.
    /// </summary>
    public static void HandleToggleValueChange(Toggle toggle, bool value, string source)
    {
        try
        {
            if (toggle == null || toggle.gameObject == null) return;

            var instanceId = toggle.GetInstanceID();

            // Check if value actually changed
            if (TogglePreviousStates.TryGetValue(instanceId, out var previousValue))
            {
                if (previousValue == value) return;
            }

            TogglePreviousStates[instanceId] = value;

            // Only announce if this toggle is related to the currently focused element
            var toggleGo = toggle.gameObject;
            bool isCurrentlyFocused = IsRelatedToFocusedElement(toggleGo);

            if (!isCurrentlyFocused) return;

            // Re-announce the state change
            var stateText = value ? "On" : "Off";
            ScreenReader.Speak(stateText, true);
        }
        catch { }
    }

    /// <summary>
    /// Handles submit input for custom toggle components and ClassTabs.
    /// </summary>
    public static void HandleSubmitForGameObject(GameObject go)
    {
        if (go == null) return;

        // Check for ClassTab - read description on submit
        // Only check if NOT in class carousel (CharacterCreationSelection is for carousel)
        var charCreation = go.GetComponent<CharacterCreationSelection>()
            ?? go.GetComponentInParent<CharacterCreationSelection>();
        if (charCreation != null)
        {
            // This is class carousel, not ClassTab - don't handle here
            Plugin.Log.LogInfo($"[Submit] Skipping - this is CharacterCreationSelection (class carousel)");
            return;
        }

        // Check for ClassTab by name
        var goName = go.name;
        if (goName.StartsWith("ClassTab"))
        {
            HandleClassTabSubmit(go, goName);
            return;
        }

        // Check parents for ClassTab
        var current = go.transform.parent;
        for (int i = 0; i < 3 && current != null; i++)
        {
            if (current.name.StartsWith("ClassTab"))
            {
                HandleClassTabSubmit(current.gameObject, current.name);
                return;
            }
            current = current.parent;
        }

        var moveToAttack = go.GetComponent<ActiveWhileMoveToAttackEnabled>()
            ?? go.GetComponentInParent<ActiveWhileMoveToAttackEnabled>();
        if (moveToAttack != null)
        {
            var stateText = moveToAttack.moveAttackEnabled ? "On" : "Off";
            ScreenReader.Speak(stateText, true);
            return;
        }

        var friendNotification = go.GetComponent<ActiveWhileFriendAddNotificationEnabled>()
            ?? go.GetComponentInParent<ActiveWhileFriendAddNotificationEnabled>();
        if (friendNotification != null)
        {
            var stateText = friendNotification.hideUnknownSocialRequests ? "On" : "Off";
            ScreenReader.Speak(stateText, true);
            return;
        }
    }

    /// <summary>
    /// Handles submit on ClassTab elements - reads the class/mastery description.
    /// </summary>
    private static void HandleClassTabSubmit(GameObject go, string goName)
    {
        try
        {
            Plugin.Log.LogInfo($"[ClassTab Submit] Detected submit on {goName}");

            // Find the description in the parent hierarchy
            // ClassTabs are in LeftColClassMenu, descriptions are usually in a sibling or parent container
            var parent = go.transform.parent;
            if (parent == null) return;

            // Search in parent and grandparent for description text
            string description = null;
            for (int level = 0; level < 3 && parent != null; level++)
            {
                var allTexts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in allTexts)
                {
                    if (txt == null || !txt.gameObject.activeInHierarchy) continue;
                    var text = txt.text;
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    // Description is longer text with punctuation
                    if (text.Length > 50 && text.Contains("."))
                    {
                        description = TextUtils.CleanText(text);
                        Plugin.Log.LogInfo($"[ClassTab Submit] Found description at level {level}: {description.Substring(0, System.Math.Min(50, description.Length))}...");
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(description))
                    break;

                parent = parent.parent;
            }

            if (!string.IsNullOrEmpty(description))
            {
                ScreenReader.Speak(description, false);
            }
            else
            {
                Plugin.Log.LogInfo("[ClassTab Submit] No description found");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"HandleClassTabSubmit error: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a GameObject is related to the currently focused element.
    /// </summary>
    public static bool IsRelatedToFocusedElement(GameObject go)
    {
        if (go == null || _lastAnnouncedInstanceId == 0) return false;

        // Direct match
        if (go.GetInstanceID() == _lastAnnouncedInstanceId)
            return true;

        // Check if go is an ancestor of focused element
        var current = go.transform;
        for (int i = 0; i < 6 && current != null; i++)
        {
            if (current.gameObject.GetInstanceID() == _lastAnnouncedInstanceId)
                return true;
            current = current.parent;
        }

        // Check if go is a descendant of focused element
        var focusedGo = FindGameObjectByInstanceId(_lastAnnouncedInstanceId, go.transform.root);
        if (focusedGo != null && go.transform.IsChildOf(focusedGo.transform))
            return true;

        // Check if they share a common parent within 3 levels
        var goParent = go.transform.parent;
        for (int i = 0; i < 3 && goParent != null; i++)
        {
            if (focusedGo != null && focusedGo.transform.IsChildOf(goParent))
                return true;
            goParent = goParent.parent;
        }

        return false;
    }

    private static GameObject FindGameObjectByInstanceId(int instanceId, Transform searchRoot)
    {
        if (_cachedFocusedId == instanceId && _cachedFocusedGo != null)
            return _cachedFocusedGo;

        var allTransforms = searchRoot.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t.gameObject.GetInstanceID() == instanceId)
            {
                _cachedFocusedGo = t.gameObject;
                _cachedFocusedId = instanceId;
                return t.gameObject;
            }
        }

        return null;
    }

    private static string BuildAnnouncementText(GameObject go)
    {
        string label = null;
        string value = null;
        string description = null;

        // Special case: Character creation selection
        var elementName = go.name;
        bool isGenericButton = elementName == "LeftButton" || elementName == "RightButton" ||
                               elementName == "Button" || elementName == "BackButton" ||
                               elementName == "ConfirmButton" || elementName == "CancelButton";

        if (!isGenericButton)
        {
            var charCreation = go.GetComponent<CharacterCreationSelection>()
                ?? go.GetComponentInParent<CharacterCreationSelection>();
            if (charCreation != null && charCreation.gameObject.activeInHierarchy)
            {
                var charClass = charCreation.characterClass;
                if (charClass != null && !string.IsNullOrEmpty(charClass.className))
                {
                    Plugin.Log.LogInfo($"[ClassSelect] Announcing class from BuildAnnouncementText: {charClass.className}, go={elementName}");
                    return charClass.className;
                }
            }
        }

        // Special case: Class tabs in character creation (ClassTabVoidKnight, ClassTabPaladin, etc.)
        if (elementName.StartsWith("ClassTab"))
        {
            var classTabResult = TryGetClassTabInfo(go, elementName);
            if (!string.IsNullOrEmpty(classTabResult))
            {
                return classTabResult;
            }
        }

        // Special case: Mastery selection in skill tree
        if (MasteryReader.TryGetMasteryInfo(go, out var masteryName, out var masteryDescription))
        {
            if (!string.IsNullOrEmpty(masteryDescription))
            {
                return $"{masteryName}. {masteryDescription}";
            }
            return masteryName;
        }

        // Special case: Check if we're inside an expanded dropdown list
        var dropdownItemText = TextDetector.TryGetDropdownItemText(go);
        if (dropdownItemText != null)
            return dropdownItemText;

        // Check if this option is disabled by the game developers
        bool isDisabledOption = IsDisabledOption(go);

        // Layer 1: Try to get label from InfoForTooltip
        TextDetector.TryGetFromTooltipInfo(go, ref label, ref description);

        // Check if we're in character creation context
        bool isInCharacterCreation = go.GetComponentInParent<CharacterCreationSelection>() != null
            || go.GetComponentInParent<LocalCharacterSlots>() != null
            || go.GetComponentInParent<MasteryPanel>() != null
            || go.GetComponentInParent<CharacterSelect>() != null
            || IsInCharacterCreationByName(go);

        // Check for color picker
        var colorPicker = go.GetComponent<ColorPickerManager>() ?? go.GetComponentInParent<ColorPickerManager>();
        var ruleColorPicker = go.GetComponent<RuleColorPicker>() ?? go.GetComponentInParent<RuleColorPicker>();
        bool isColorPicker = colorPicker != null || ruleColorPicker != null;

        var chatToggle = go.GetComponent<ChatToggleField>() ?? go.GetComponentInParent<ChatToggleField>();

        var dropdown = go.GetComponent<TMP_Dropdown>() ?? go.GetComponentInParent<TMP_Dropdown>() ?? go.GetComponentInChildren<TMP_Dropdown>();
        var slider = go.GetComponent<Slider>() ?? go.GetComponentInParent<Slider>() ?? go.GetComponentInChildren<Slider>();
        var button = go.GetComponent<Button>() ?? go.GetComponentInParent<Button>();
        var inputField = go.GetComponent<TMP_InputField>() ?? go.GetComponentInParent<TMP_InputField>() ?? go.GetComponentInChildren<TMP_InputField>();

        // For input fields
        if (inputField != null)
        {
            var inputText = inputField.text;
            var placeholderTmp = inputField.placeholder?.TryCast<TextMeshProUGUI>();
            var placeholder = placeholderTmp != null ? TextUtils.CleanText(placeholderTmp.text) : null;

            if (!string.IsNullOrEmpty(inputText))
            {
                if (!string.IsNullOrEmpty(placeholder))
                    return $"{placeholder}: {inputText}";
                return $"Text field: {inputText}";
            }
            else
            {
                if (!string.IsNullOrEmpty(placeholder))
                    return $"{placeholder}: empty";
                return "Text field: empty";
            }
        }

        Toggle toggle = null;
        if (!isColorPicker && !isInCharacterCreation && button == null)
        {
            toggle = go.GetComponent<Toggle>() ?? go.GetComponentInParent<Toggle>() ?? go.GetComponentInChildren<Toggle>();
            if (toggle != null && toggle.group != null)
            {
                toggle = null;
            }
        }

        // For buttons, just get their text directly
        if (button != null)
        {
            var buttonText = TextUtils.GetTextFromHierarchy(button.gameObject);
            if (!string.IsNullOrEmpty(buttonText))
                return buttonText;
        }

        // Get control value
        if (isColorPicker)
        {
            value = "Color selector";
        }
        else if (chatToggle != null && !isInCharacterCreation)
        {
            value = chatToggle.IsActive ? "On" : "Off";
        }
        else if (dropdown != null)
        {
            if (dropdown.captionText != null)
                value = TextUtils.CleanText(dropdown.captionText.text);
        }
        else if (toggle != null)
        {
            value = toggle.isOn ? "On" : "Off";
        }
        else if (slider != null)
        {
            value = Mathf.RoundToInt(slider.value).ToString();
        }

        // Layer 2: Try to get label from GraphicsOption components
        if (string.IsNullOrEmpty(label))
        {
            TextDetector.TryGetFromGraphicsOption(go, ref label);
        }

        // Layer 3: Intelligent text search
        if (string.IsNullOrEmpty(label))
        {
            var controlGo = dropdown?.gameObject ?? toggle?.gameObject ?? slider?.gameObject ?? go;
            TextDetector.FindLabelInImmediateContainer(controlGo, ref label, ref description, value);
        }

        // Layer 3.5: Aggressive search
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(description))
        {
            TextDetector.FindLabelAndDescriptionAggressive(go, value, ref label, ref description);
        }

        // Layer 4: Last resort
        if (string.IsNullOrEmpty(label))
        {
            label = TextDetector.FindAnyTextInParents(go, value);
        }

        // Layer 4.5: Try harder to find toggle
        if (!string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value) && !isColorPicker && !isInCharacterCreation)
        {
            var optionContainer = TextDetector.FindSingleOptionContainer(go);
            if (optionContainer != null)
            {
                toggle = optionContainer.GetComponentInChildren<Toggle>();
                if (toggle != null)
                {
                    if (toggle.group == null)
                        value = toggle.isOn ? "On" : "Off";
                    else
                        toggle = null;
                }
            }

            if (toggle == null)
            {
                var current = go.transform.parent;
                for (int i = 0; i < 6 && current != null; i++)
                {
                    var toggles = current.GetComponentsInChildren<Toggle>(true);
                    foreach (var t in toggles)
                    {
                        if (t.GetComponentInParent<ColorPickerManager>() != null ||
                            t.GetComponentInParent<RuleColorPicker>() != null)
                            continue;

                        if (t.group != null)
                            continue;

                        toggle = t;
                        value = toggle.isOn ? "On" : "Off";
                        break;
                    }

                    if (toggle != null) break;
                    current = current.parent;
                }
            }
        }

        // Layer 5: Use GameObject name
        if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value))
        {
            var goName = TextUtils.CleanObjectName(go.name);
            if (!TextUtils.IsGenericName(goName) && goName.Length > 3)
                label = goName;
        }

        if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(label))
        {
            var directText = TextUtils.GetTextFromHierarchy(go);
            if (!string.IsNullOrEmpty(directText))
                return directText;
        }

        return FormatAnnouncement(label, value, description, go, isDisabledOption);
    }

    /// <summary>
    /// Extracts class/mastery info from ClassTab elements in character creation.
    /// </summary>
    private static string TryGetClassTabInfo(GameObject go, string elementName)
    {
        string className = null;

        // First, try to get visible text from the tab (localized name)
        var tmpText = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
        {
            className = tmpText.text.Trim();
        }

        // Fallback: extract from GameObject name (ClassTabVoidKnight -> Void Knight)
        if (string.IsNullOrEmpty(className))
        {
            var rawName = elementName.Substring(8); // "ClassTab".Length = 8

            if (rawName == "Base")
            {
                className = "Base Class";
            }
            else
            {
                // Insert spaces before capital letters (VoidKnight -> Void Knight)
                className = System.Text.RegularExpressions.Regex.Replace(rawName, "([a-z])([A-Z])", "$1 $2");
            }
        }

        if (string.IsNullOrEmpty(className))
            return null;

        // Log components for debugging
        var button = go.GetComponent<Button>();
        var selectable = go.GetComponent<Selectable>();
        Plugin.Log.LogInfo($"[ClassTab] name={className}, hasButton={button != null}, hasSelectable={selectable != null}");

        return className;
    }

    private static bool IsDisabledOption(GameObject go)
    {
        var current = go.transform.parent;
        for (int i = 0; i < 3 && current != null; i++)
        {
            var parentName = current.name;
            if (parentName.Contains("Disabled") || parentName.Contains("disabled"))
                return true;
            current = current.parent;
        }
        return false;
    }

    private static bool IsInCharacterCreationByName(GameObject go)
    {
        var current = go.transform;
        for (int i = 0; i < 15 && current != null; i++)
        {
            var name = current.name.ToLowerInvariant();
            if (name.Contains("character") ||
                name.Contains("class") ||
                name.Contains("mastery") ||
                name.Contains("newchar") ||
                name.Contains("slot") ||
                name.Contains("selection"))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    private static string FormatAnnouncement(string label, string value, string description, GameObject go, bool isDisabled = false)
    {
        var result = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(label))
        {
            result.Append(label);
            if (!string.IsNullOrEmpty(value))
            {
                result.Append(": ");
                result.Append(value);
            }
        }
        else if (!string.IsNullOrEmpty(value))
        {
            result.Append(value);
        }

        if (isDisabled && result.Length > 0)
        {
            result.Append(" (Disabled)");
        }

        if (!string.IsNullOrEmpty(description) && result.Length > 0)
        {
            result.Append(". ");
            result.Append(description);
        }

        if (result.Length == 0)
        {
            var cleanName = TextUtils.CleanObjectName(go.name);
            if (!TextUtils.IsGenericName(cleanName))
            {
                if (isDisabled)
                    return cleanName + " (Disabled)";
                return cleanName;
            }
        }

        return result.ToString();
    }
}
