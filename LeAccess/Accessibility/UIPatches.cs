using ChatView;
using GraphicsBackend.Options;
using HarmonyLib;
using LE.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility;

/// <summary>
/// Harmony patches for UI accessibility.
/// </summary>
public static class UIPatches
{
    private static int _lastAnnouncedInstanceId;
    private static float _lastAnnounceTime;
    private const float AnnounceCooldown = 0.15f;

    // Track toggle changes to announce state updates
    private static readonly Dictionary<int, bool> _togglePreviousStates = new();

    // Track custom toggle components for state change detection
    private static ActiveWhileMoveToAttackEnabled _trackedMoveToAttack;
    private static bool _trackedMoveToAttackLastState;
    private static ActiveWhileFriendAddNotificationEnabled _trackedFriendNotif;
    private static bool _trackedFriendNotifLastState;

    // Known value patterns - only universal short values that appear in most games/languages
    private static readonly HashSet<string> KnownValuePatterns = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "on", "off", "yes", "no", "auto", "none", "default", "custom",
        "low", "medium", "high", "ultra", "max", "min", "normal"
    };


    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Open))]
    public static class UIPanelOpenPatch
    {
        public static void Postfix(UIPanel __instance)
        {
            try
            {
                if (__instance?.instance == null) return;
                var cleanName = CleanPanelName(__instance.instance.name);
                if (!string.IsNullOrEmpty(cleanName))
                    ScreenReader.Speak($"{cleanName} opened");
                DisableVirtualMouse();
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Close))]
    public static class UIPanelClosePatch
    {
        public static void Prefix(UIPanel __instance)
        {
            try
            {
                if (__instance?.isOpen != true) return;
                var cleanName = CleanPanelName(__instance?.instance?.name);
                if (!string.IsNullOrEmpty(cleanName))
                    ScreenReader.Speak($"{cleanName} closed");
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(ControllerFocusCursor), nameof(ControllerFocusCursor.Show), typeof(RectTransform), typeof(int))]
    public static class ControllerFocusCursorShowPatch
    {
        public static void Postfix(RectTransform rectTransform)
        {
            if (rectTransform != null)
                AnnounceElement(rectTransform.gameObject);
        }
    }

    [HarmonyPatch(typeof(ControllerFocusCursor), nameof(ControllerFocusCursor.Show),
        typeof(RectTransform), typeof(ControllerCursorPreset), typeof(bool), typeof(bool), typeof(float), typeof(int))]
    public static class ControllerFocusCursorShowPatch2
    {
        public static void Postfix(RectTransform element)
        {
            if (element != null)
                AnnounceElement(element.gameObject);
        }
    }

    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.SetActiveVirtualMouse))]
    public static class DisableVirtualMousePatch
    {
        public static bool Prefix(ref bool active) { active = false; return true; }
    }

    // For class selection carousel monitoring
    private static string _lastDetectedCenterClass;
    private static float _lastClassCheckTime;

    /// <summary>
    /// Called from MenuAccessibility.Update to check for class changes in the carousel.
    /// Uses ListNavigable "Select A Class Navigable" with _lastSelectedIndex to track selection.
    /// </summary>
    public static void CheckPendingClassAnnouncement()
    {
        // Periodically check for class changes when in class selection
        if (Time.time - _lastClassCheckTime < 0.25f) return;
        _lastClassCheckTime = Time.time;

        try
        {
            // Check if we're in class selection mode
            var charSelect = CharacterSelect.instance;
            if (charSelect == null) return;
            if (charSelect.currentState != CharacterSelect.CharacterSelectState.ClassSelection)
            {
                // Reset when leaving class selection
                if (_lastDetectedCenterClass != null)
                {
                    _lastDetectedCenterClass = null;
                }
                return;
            }

            // Find the "Select A Class Navigable" ListNavigable
            var allListNavs = UnityEngine.Object.FindObjectsOfType<ListNavigable>();
            string classToAnnounce = null;

            foreach (var nav in allListNavs)
            {
                if (nav == null) continue;
                var children = nav._selectableChildren;
                if (children == null || children.Count == 0) continue;

                // Check if this ListNavigable has CharacterCreationSelection children
                bool hasClassSelections = false;
                for (int i = 0; i < children.Count && i < 3; i++)
                {
                    var child = children[i];
                    if (child?.TryCast<CharacterCreationSelection>() != null)
                    {
                        hasClassSelections = true;
                        break;
                    }
                }

                if (hasClassSelections)
                {
                    var listNavIdx = nav._lastSelectedIndex;
                    if (listNavIdx >= 0 && listNavIdx < children.Count)
                    {
                        var selectedChild = children[listNavIdx];
                        var charCreation = selectedChild?.TryCast<CharacterCreationSelection>();
                        if (charCreation != null)
                        {
                            // Try to find localized text in the UI hierarchy
                            var textComponents = charCreation.GetComponentsInChildren<TMPro.TMP_Text>(true);
                            if (textComponents != null)
                            {
                                foreach (var txt in textComponents)
                                {
                                    if (txt == null) continue;
                                    var text = txt.text;
                                    if (!string.IsNullOrEmpty(text) && text.Length > 1 && text.Length < 30)
                                    {
                                        classToAnnounce = text;
                                        break;
                                    }
                                }
                            }

                            // Fallback to className if no localized text found
                            if (string.IsNullOrEmpty(classToAnnounce) && charCreation.characterClass != null)
                            {
                                classToAnnounce = charCreation.characterClass.className;
                            }
                        }
                    }
                    break;
                }
            }

            if (!string.IsNullOrEmpty(classToAnnounce) && classToAnnounce != _lastDetectedCenterClass)
            {
                _lastDetectedCenterClass = classToAnnounce;
                ScreenReader.Speak(classToAnnounce);
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Error checking class selection: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.VirtualMouseIsEnabled))]
    public static class VirtualMouseIsEnabledPatch
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
    }

    /// <summary>
    /// Patch Toggle.isOn setter to detect state changes and re-announce
    /// </summary>
    [HarmonyPatch(typeof(Toggle), nameof(Toggle.isOn), MethodType.Setter)]
    public static class ToggleIsOnSetterPatch
    {
        public static void Postfix(Toggle __instance, bool value)
        {
            HandleToggleValueChange(__instance, value, "setter");
        }
    }

    /// <summary>
    /// Patch Toggle.SetIsOnWithoutNotify to detect state changes (some UI uses this instead of setter)
    /// </summary>
    [HarmonyPatch(typeof(Toggle), nameof(Toggle.SetIsOnWithoutNotify))]
    public static class ToggleSetIsOnWithoutNotifyPatch
    {
        public static void Postfix(Toggle __instance, bool value)
        {
            HandleToggleValueChange(__instance, value, "SetIsOnWithoutNotify");
        }
    }

    /// <summary>
    /// Common handler for toggle value changes
    /// </summary>
    private static void HandleToggleValueChange(Toggle toggle, bool value, string source)
    {
        try
        {
            if (toggle == null || toggle.gameObject == null) return;

            var instanceId = toggle.GetInstanceID();

            // Check if value actually changed
            if (_togglePreviousStates.TryGetValue(instanceId, out var previousValue))
            {
                if (previousValue == value) return;
            }

            _togglePreviousStates[instanceId] = value;

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
    /// Check if a GameObject is related to the currently focused element.
    /// </summary>
    private static bool IsRelatedToFocusedElement(GameObject go)
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

    private static GameObject _cachedFocusedGo;
    private static int _cachedFocusedId;

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

    /// <summary>
    /// Patch ChatToggleField.IsActive setter for chat options
    /// </summary>
    [HarmonyPatch(typeof(ChatToggleField), nameof(ChatToggleField.IsActive), MethodType.Setter)]
    public static class ChatToggleFieldIsActivePatch
    {
        public static void Postfix(ChatToggleField __instance, bool value)
        {
            try
            {
                if (__instance == null || __instance.gameObject == null) return;

                var instanceId = __instance.GetInstanceID();

                if (_togglePreviousStates.TryGetValue(instanceId, out var previousValue))
                {
                    if (previousValue == value) return;
                }

                _togglePreviousStates[instanceId] = value;

                bool isCurrentlyFocused = IsRelatedToFocusedElement(__instance.gameObject);
                if (!isCurrentlyFocused) return;

                var stateText = value ? "On" : "Off";
                ScreenReader.Speak(stateText, true);
            }
            catch { }
        }
    }

    /// <summary>
    /// Patch PrioritizedNavigable.HandleSubmitInput to detect A/X button presses on custom toggles
    /// </summary>
    [HarmonyPatch(typeof(PrioritizedNavigable), nameof(PrioritizedNavigable.HandleSubmitInput))]
    public static class PrioritizedNavigableHandleSubmitInputPatch
    {
        public static void Postfix(PrioritizedNavigable __instance)
        {
            try
            {
                if (__instance == null) return;

                var activeSelectable = __instance.ActiveSelectable;
                if (activeSelectable == null) return;

                var go = activeSelectable.gameObject;
                if (go == null) return;

                // Check for custom toggle components
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
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"HandleSubmitInput error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch VerticalNavigable.HandleSubmitInput - settings panels often use this
    /// </summary>
    [HarmonyPatch(typeof(VerticalNavigable), nameof(VerticalNavigable.HandleSubmitInput))]
    public static class VerticalNavigableHandleSubmitInputPatch
    {
        public static void Postfix(VerticalNavigable __instance)
        {
            try
            {
                if (__instance == null) return;
                var selectables = __instance.selectables;
                var idx = __instance.currentlySelectedIdx;
                if (selectables != null && idx >= 0 && idx < selectables.Length)
                {
                    var selected = selectables[idx];
                    if (selected != null)
                        HandleSubmitForGameObject(selected.gameObject);
                }
            }
            catch { }
        }
    }

    private static void HandleSubmitForGameObject(GameObject go)
    {
        if (go == null) return;

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

    private static string BuildAnnouncementText(GameObject go)
    {
        string label = null;
        string value = null;
        string description = null;

        // Special case: Character creation selection
        // Check in parents too, but exclude generic buttons that happen to be in the hierarchy
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

        // Special case: Check if we're inside an expanded dropdown list
        var dropdownItemText = TryGetDropdownItemText(go);
        if (dropdownItemText != null)
            return dropdownItemText;

        // Check if this option is disabled by the game developers
        bool isDisabledOption = IsDisabledOption(go);

        // Layer 1: Try to get label from InfoForTooltip
        TryGetFromTooltipInfo(go, ref label, ref description);

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
            var placeholder = placeholderTmp != null ? CleanText(placeholderTmp.text) : null;

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
            var buttonText = GetTextFromHierarchy(button.gameObject);
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
                value = CleanText(dropdown.captionText.text);
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
            TryGetFromGraphicsOption(go, ref label);
        }

        // Layer 3: Intelligent text search
        if (string.IsNullOrEmpty(label))
        {
            var controlGo = dropdown?.gameObject ?? toggle?.gameObject ?? slider?.gameObject ?? go;
            FindLabelInImmediateContainer(controlGo, ref label, ref description, value);
        }

        // Layer 3.5: Aggressive search
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(description))
        {
            FindLabelAndDescriptionAggressive(go, value, ref label, ref description);
        }

        // Layer 4: Last resort
        if (string.IsNullOrEmpty(label))
        {
            label = FindAnyTextInParents(go, value);
        }

        // Layer 4.5: Try harder to find toggle
        if (!string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value) && !isColorPicker && !isInCharacterCreation)
        {
            var optionContainer = FindSingleOptionContainer(go);
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
            var goName = CleanObjectName(go.name);
            if (!IsGenericName(goName) && goName.Length > 3)
                label = goName;
        }

        if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(label))
        {
            var directText = GetTextFromHierarchy(go);
            if (!string.IsNullOrEmpty(directText))
                return directText;
        }

        return FormatAnnouncement(label, value, description, go, isDisabledOption);
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

    private static string TryGetDropdownItemText(GameObject go)
    {
        var toggle = go.GetComponent<Toggle>() ?? go.GetComponentInParent<Toggle>();
        if (toggle == null) return null;

        var current = go.transform;
        bool isInDropdownList = false;
        for (int i = 0; i < 6 && current != null; i++)
        {
            var name = current.name.ToLowerInvariant();
            if (name.Contains("dropdown list") || name.Contains("dropdownlist"))
            {
                isInDropdownList = true;
                break;
            }
            if (name.Contains("content") && current.parent?.name.ToLowerInvariant().Contains("viewport") == true)
            {
                isInDropdownList = true;
                break;
            }
            current = current.parent;
        }

        if (!isInDropdownList) return null;

        var itemText = GetTextFromHierarchy(go);
        if (!string.IsNullOrEmpty(itemText))
            return itemText;

        return null;
    }

    private static void TryGetFromTooltipInfo(GameObject go, ref string label, ref string description)
    {
        try
        {
            var tooltip = go.GetComponent<InfoForTooltip>() ?? go.GetComponentInParent<InfoForTooltip>();
            if (tooltip == null) return;

            tooltip.SetText();

            var info = tooltip.tooltipInfo;
            if (info != null)
            {
                var nameText = info.nameText;
                if (!string.IsNullOrWhiteSpace(nameText))
                    label = CleanText(nameText);

                var mainText = info.mainText;
                if (!string.IsNullOrWhiteSpace(mainText))
                    description = CleanText(mainText);
            }
        }
        catch { }
    }

    private static void TryGetFromGraphicsOption(GameObject go, ref string label)
    {
        try
        {
            var toggleOption = go.GetComponent<ToggleOption>() ?? go.GetComponentInParent<ToggleOption>();
            if (toggleOption != null && !string.IsNullOrWhiteSpace(toggleOption.displayName))
            {
                label = CleanText(toggleOption.displayName);
                return;
            }

            var sliderOption = go.GetComponent<SliderOption>() ?? go.GetComponentInParent<SliderOption>();
            if (sliderOption != null && !string.IsNullOrWhiteSpace(sliderOption.displayName))
            {
                label = CleanText(sliderOption.displayName);
            }
        }
        catch { }
    }

    private static void FindLabelAndDescriptionAggressive(GameObject go, string excludeValue, ref string label, ref string description)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);
        if (!string.IsNullOrEmpty(label))
            seen.Add(label);
        if (!string.IsNullOrEmpty(description))
            seen.Add(description);

        var optionContainer = FindSingleOptionContainer(go);
        if (optionContainer == null)
            return;

        var allTexts = optionContainer.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var tmp in allTexts)
        {
            if (tmp == null || string.IsNullOrWhiteSpace(tmp.text)) continue;

            var text = CleanText(tmp.text);
            if (string.IsNullOrEmpty(text) || seen.Contains(text)) continue;

            seen.Add(text);
            var textType = ClassifyText(text);

            if (label == null && textType == TextType.Label)
                label = text;
            else if (description == null && textType == TextType.Description)
                description = text;

            if (label != null && description != null)
                return;
        }
    }

    private static string FindAnyTextInParents(GameObject go, string excludeValue)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);

        var current = go.transform.parent;

        for (int level = 0; level < 5 && current != null; level++)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);

                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                {
                    var text = CleanText(tmp.text);
                    if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
                    {
                        var textType = ClassifyText(text);
                        if (textType == TextType.Label || textType == TextType.Unknown)
                            return text;
                    }
                }

                for (int j = 0; j < child.childCount; j++)
                {
                    var grandchild = child.GetChild(j);
                    tmp = grandchild.GetComponent<TextMeshProUGUI>();
                    if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                    {
                        var text = CleanText(tmp.text);
                        if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
                        {
                            var textType = ClassifyText(text);
                            if (textType == TextType.Label || textType == TextType.Unknown)
                                return text;
                        }
                    }
                }
            }

            if (IsScrollContent(current))
                break;

            current = current.parent;
        }

        return null;
    }

    private static Transform FindSingleOptionContainer(GameObject controlGo)
    {
        var current = controlGo.transform.parent;

        for (int level = 0; level < 5 && current != null; level++)
        {
            int controlCount = 0;
            var toggles = current.GetComponentsInChildren<Toggle>(true);
            var sliders = current.GetComponentsInChildren<Slider>(true);
            var dropdowns = current.GetComponentsInChildren<TMP_Dropdown>(true);

            controlCount = toggles.Length + sliders.Length + dropdowns.Length;

            if (controlCount == 1)
                return current;

            if (controlCount == 0)
            {
                current = current.parent;
                continue;
            }

            if (level == 0)
                return current;

            return null;
        }

        return null;
    }

    private static void FindLabelInImmediateContainer(GameObject controlGo, ref string label, ref string description, string excludeValue)
    {
        var parent = controlGo.transform.parent;

        for (int level = 0; level < 3 && parent != null; level++)
        {
            if (IsScrollContent(parent))
                break;

            FindTextsInSiblings(parent, controlGo, excludeValue, ref label, ref description, level);

            if (label != null)
                break;

            parent = parent.parent;
        }
    }

    private static bool IsScrollContent(Transform t)
    {
        var name = t.name.ToLowerInvariant();

        if (name == "content" || name == "viewport" || name.Contains("scroll"))
            return true;

        if (t.parent != null && t.parent.name.ToLowerInvariant().Contains("viewport"))
            return true;

        if (t.childCount > 5)
        {
            int childrenWithControls = 0;
            for (int i = 0; i < Mathf.Min(t.childCount, 8); i++)
            {
                var child = t.GetChild(i);
                if (child.GetComponentInChildren<Toggle>() != null ||
                    child.GetComponentInChildren<Slider>() != null ||
                    child.GetComponentInChildren<TMP_Dropdown>() != null)
                    childrenWithControls++;
            }
            if (childrenWithControls >= 3)
                return true;
        }

        return false;
    }

    private static void FindTextsInSiblings(Transform parent, GameObject controlGo, string excludeValue,
        ref string label, ref string description, int level)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);

        var allTexts = new List<(string text, TextType type, int siblingIndex)>();

        for (int i = 0; i < parent.childCount; i++)
        {
            var sibling = parent.GetChild(i);

            if (controlGo.transform.IsChildOf(sibling) && sibling.gameObject != controlGo.transform.parent?.gameObject)
                continue;

            CollectTextsFromTransform(sibling, controlGo, seen, allTexts, i, 0);
        }

        int controlSiblingIndex = controlGo.transform.parent == parent
            ? controlGo.transform.GetSiblingIndex()
            : controlGo.transform.parent?.GetSiblingIndex() ?? 0;

        foreach (var (text, textType, siblingIndex) in allTexts)
        {
            if (label == null && textType == TextType.Label && siblingIndex <= controlSiblingIndex)
            {
                label = text;
                break;
            }
        }

        if (label == null)
        {
            foreach (var (text, textType, siblingIndex) in allTexts)
            {
                if (textType == TextType.Label)
                {
                    label = text;
                    break;
                }
            }
        }

        foreach (var (text, textType, siblingIndex) in allTexts)
        {
            if (description == null && textType == TextType.Description && siblingIndex >= controlSiblingIndex)
            {
                description = text;
                break;
            }
        }

        if (description == null)
        {
            foreach (var (text, textType, siblingIndex) in allTexts)
            {
                if (textType == TextType.Description)
                {
                    description = text;
                    break;
                }
            }
        }
    }

    private static void CollectTextsFromTransform(Transform t, GameObject controlGo, HashSet<string> seen,
        List<(string text, TextType type, int siblingIndex)> results, int siblingIndex, int depth)
    {
        if (depth > 2) return;

        if (t.gameObject == controlGo) return;

        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
        {
            var text = CleanText(tmp.text);
            if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
            {
                seen.Add(text);
                var textType = ClassifyText(text);
                results.Add((text, textType, siblingIndex));
            }
        }

        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child.gameObject == controlGo || controlGo.transform.IsChildOf(child))
                continue;
            CollectTextsFromTransform(child, controlGo, seen, results, siblingIndex, depth + 1);
        }
    }

    private enum TextType
    {
        Label,
        Value,
        Description,
        Unknown
    }

    private static TextType ClassifyText(string text)
    {
        if (string.IsNullOrEmpty(text)) return TextType.Unknown;

        if (KnownValuePatterns.Contains(text))
            return TextType.Value;

        if (int.TryParse(text, out _) || float.TryParse(text, out _))
            return TextType.Value;

        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+%?$"))
            return TextType.Value;

        if (IsDescriptionText(text))
            return TextType.Description;

        if (!text.EndsWith(".") && !text.EndsWith("!") && !text.EndsWith("?"))
            return TextType.Label;

        return TextType.Unknown;
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
            var cleanName = CleanObjectName(go.name);
            if (!IsGenericName(cleanName))
            {
                if (isDisabled)
                    return cleanName + " (Disabled)";
                return cleanName;
            }
        }

        return result.ToString();
    }

    private static string GetTextFromHierarchy(GameObject go)
    {
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
            return CleanText(tmp.text);

        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
            return CleanText(tmp.text);

        var text = go.GetComponent<Text>();
        if (text != null && !string.IsNullOrWhiteSpace(text.text))
            return CleanText(text.text);

        text = go.GetComponentInChildren<Text>();
        if (text != null && !string.IsNullOrWhiteSpace(text.text))
            return CleanText(text.text);

        return null;
    }

    private static bool IsDescriptionText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        if (text.Length < 30) return false;

        if (text.Contains(". ") ||
            text.EndsWith(".") ||
            text.Contains(", ") ||
            text.Contains("; ") ||
            text.Contains("- "))
            return true;

        return false;
    }

    private static bool IsGenericName(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        var lower = name.ToLowerInvariant();
        return lower.Contains("button") || lower.Contains("toggle") ||
               lower.Contains("slider") || lower.Contains("dropdown") ||
               lower.Contains("background") || lower.Contains("image") ||
               lower.Contains("content") || lower.Contains("viewport") ||
               lower.Contains("scroll") || lower.Contains("mask") ||
               lower.Length <= 2;
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static string CleanObjectName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(\d+\)$", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(Clone\)$", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        return name.Replace("_", " ").Trim();
    }

    private static string CleanPanelName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace("Panel", "").Replace("(Clone)", "").Replace("UI", "").Trim();
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2").Trim();
    }

    private static void DisableVirtualMouse()
    {
        try { EpochInputManager.SetActiveVirtualMouse(false); } catch { }
    }
}
