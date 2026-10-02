using LeAccess.I18n;
using ChatView;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Skills;
using LeAccess.Accessibility.Passives;
using LeAccess.Accessibility.Quests;
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
    private static string _lastAnnouncementText;
    private static GameObject _pendingFocus;
    private const float AnnounceCooldown = 0.15f;

    // Track toggle changes to announce state updates
    internal static readonly Dictionary<int, bool> TogglePreviousStates = new();

    // Cache for focused element lookup
    private static GameObject _cachedFocusedGo;
    private static int _cachedFocusedId;

    /// <summary>
    /// Gets the last announced instance ID for toggle state tracking.
    /// </summary>
    public static int LastAnnouncedInstanceId => _lastAnnouncedInstanceId;

    internal static void ResetFocus()
    {
        _lastAnnouncedInstanceId = 0;
        _lastAnnouncementText = null;
        _pendingFocus = null;
        _cachedFocusedGo = null;
        _cachedFocusedId = 0;
    }

    /// <summary>
    /// Announces a UI element to the screen reader.
    /// </summary>
    public static void AnnounceElement(GameObject go, bool interrupt = true)
    {
        if (!Application.isFocused || !UiVisibility.IsVisible(go) || LE.UI.LoadingScreen.IsVisible) return;
        bool popupActive = ScreenContentAnnouncer.HasActiveContent;
        bool dialogueActive = !popupActive && DialogueAnnouncer.HasActiveContent;
        if (popupActive || dialogueActive)
        {
            bool owns = popupActive ? ScreenContentAnnouncer.OwnsFocus(go) : DialogueAnnouncer.OwnsFocus(go);
            if (!owns) return;
            bool pending = popupActive ? ScreenContentAnnouncer.HasPendingContent : DialogueAnnouncer.HasPendingContent;
            // Response focus can arrive on the same frame as its conversation body.
            if (pending || dialogueActive && interrupt && ScreenReader.SpokeThisFrame)
            { _pendingFocus = go; return; }
        }

        if (ControllerMenuAccessibility.TryHandleFocus(go) || InventoryAccessibility.TryHandleFocus(go) ||
            PassiveAccessibility.TryHandleFocus(go) || QuestAccessibility.TryHandleFocus(go) ||
            SkillAccessibility.TryHandleFocus(go) || ClassCarouselHandler.TryHandleFocus(go)) return;

        var instanceId = go.GetInstanceID();
        var now = Time.unscaledTime;

        if (instanceId == _lastAnnouncedInstanceId && (now - _lastAnnounceTime) < AnnounceCooldown)
            return;

        if (instanceId != _lastAnnouncedInstanceId) _lastAnnouncementText = null;
        _lastAnnouncedInstanceId = instanceId;
        _lastAnnounceTime = now;
        _cachedFocusedGo = go;
        _cachedFocusedId = instanceId;

        // DEBUG: Log all element names to find what class selection uses
        var debugParent = go.transform.parent?.name ?? "no-parent";
        var hasCharCreation = go.GetComponentInParent<CharacterCreationSelection>() != null;
        Plugin.Log.LogDebug($"[Element] name={go.name}, parent={debugParent}, hasCharCreationInParent={hasCharCreation}");

        var text = BuildAnnouncementText(go, popupActive || dialogueActive);
        if (!string.IsNullOrWhiteSpace(text) && text != _lastAnnouncementText)
        {
            _lastAnnouncementText = text;
            ScreenReader.Speak(text, interrupt);
        }
    }

    internal static void FlushPendingFocus()
    {
        if (_pendingFocus is null) return;
        bool popup = ScreenContentAnnouncer.HasActiveContent;
        bool dialogue = !popup && DialogueAnnouncer.HasActiveContent;
        if (popup && ScreenContentAnnouncer.HasPendingContent || dialogue && DialogueAnnouncer.HasPendingContent) return;
        var pending = _pendingFocus;
        _pendingFocus = null;
        if (!UiVisibility.IsVisible(pending) ||
            !(popup ? ScreenContentAnnouncer.OwnsFocus(pending) : dialogue && DialogueAnnouncer.OwnsFocus(pending))) return;
        // The initial selection follows the body; subsequent deliberate navigation may interrupt it.
        _lastAnnouncedInstanceId = 0;
        _lastAnnouncementText = null;
        AnnounceElement(pending, interrupt: false);
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
            var stateText = ModText.Get(value ? "state.on" : "state.off");
            ScreenReader.Speak(stateText, true);
        }
        catch { }
    }

    // Class content is read by the same settled selection path after submit.
    public static void HandleSubmitForGameObject(GameObject go)
    {
        if (UiVisibility.IsVisible(go) && MasteryReader.TryGetMasteryInfo(go, out _, out _))
            AnnounceElement(go);
    }
    /// <summary>
    /// Check if a GameObject is related to the currently focused element.
    /// </summary>
    public static bool IsRelatedToFocusedElement(GameObject go)
    {
        if (!UiVisibility.IsVisible(go) || _lastAnnouncedInstanceId == 0) return false;
        if (ScreenContentAnnouncer.HasActiveContent && !ScreenContentAnnouncer.OwnsFocus(go)) return false;
        if (!ScreenContentAnnouncer.HasActiveContent && DialogueAnnouncer.HasActiveContent &&
            !DialogueAnnouncer.OwnsFocus(go)) return false;

        var focused = _cachedFocusedGo;
        if (!UiVisibility.IsVisible(focused) || _cachedFocusedId != _lastAnnouncedInstanceId) return false;
        if (go.transform.IsChildOf(focused.transform) || focused.transform.IsChildOf(go.transform)) return true;
        var container = TextDetector.FindSingleOptionContainer(focused);
        return container != null && go.transform.IsChildOf(container);
    }

    internal static void ResetFocusIfOwned(GameObject root)
    {
        if (_cachedFocusedGo is not null && !_cachedFocusedGo.WasCollected && _cachedFocusedGo != null &&
            root is not null && !root.WasCollected && root != null &&
            _cachedFocusedGo.transform.IsChildOf(root.transform)) ResetFocus();
    }
    private static string BuildAnnouncementText(GameObject go, bool restrictToControl = false)
    {
        string label = null;
        string value = null;
        string description = null;

        if (ClassSelectionReader.TryReadControl(go, out var classSelection))
            return string.IsNullOrWhiteSpace(classSelection.Name) ? null :
                ScreenContentTracker.Combine(new[] { classSelection.Name, classSelection.Description });
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
        var colorPicker = go.GetComponent<UnityEngine.UI.Extensions.ColorPicker.ColorPickerControl>() ?? go.GetComponentInParent<UnityEngine.UI.Extensions.ColorPicker.ColorPickerControl>();

        bool isColorPicker = colorPicker != null;

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
                    return ModText.Format("control.field_value", placeholder, inputText);
                return ModText.Format("control.field_value", ModText.Get("control.text_field"), inputText);
            }
            else
            {
                if (!string.IsNullOrEmpty(placeholder))
                    return ModText.Format("control.empty_field", placeholder);
                return ModText.Format("control.empty_field", ModText.Get("control.text_field"));
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
                return ScreenContentTracker.Combine(new[] { buttonText, description });
        }

        // Get control value
        if (isColorPicker)
        {
            value = ModText.Get("control.color_selector");
        }
        else if (chatToggle != null && !isInCharacterCreation)
        {
            value = ModText.Get(chatToggle.IsActive ? "state.on" : "state.off");
        }
        else if (dropdown != null)
        {
            if (dropdown.captionText != null)
                value = TextUtils.CleanText(dropdown.captionText.text);
        }
        else if (toggle != null)
        {
            value = ModText.Get(toggle.isOn ? "state.on" : "state.off");
        }
        else if (slider != null)
        {
            value = Mathf.RoundToInt(slider.value).ToString();
        }

        if (restrictToControl)
        {
            // A popup's body belongs to its content reader, not to an icon-only button's ancestors.
            string controlText = ScreenContentReader.Read(go, null, null);
            if (string.IsNullOrWhiteSpace(controlText))
                controlText = go.GetComponent<InfoForTooltip>() != null && !string.IsNullOrWhiteSpace(label)
                    ? label : TextUtils.CleanObjectName(go.name);
            return ScreenContentTracker.Combine(new[] { controlText, value });
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
                        value = ModText.Get(toggle.isOn ? "state.on" : "state.off");
                    else
                        toggle = null;
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
            string disabledText = ModText.Format("control.disabled", result.ToString());
            result.Clear();
            result.Append(disabledText);
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
                    return ModText.Format("control.disabled", cleanName);
                return cleanName;
            }
        }

        return result.ToString();
    }
}
