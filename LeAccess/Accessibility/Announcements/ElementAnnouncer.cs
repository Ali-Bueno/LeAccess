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
        ClearLastFocus();
        _pendingFocus = null;
    }

    private static void ClearLastFocus()
    {
        _lastAnnouncedInstanceId = 0;
        _lastAnnouncementText = null;
        _cachedFocusedGo = null;
        _cachedFocusedId = 0;
    }

    internal static void ResetFocusIfUnowned()
    {
        if (_cachedFocusedGo is not null && !UiFocusOwnership.Allows(_cachedFocusedGo)) ClearLastFocus();
    }

    /// <summary>
    /// Announces a UI element to the screen reader.
    /// </summary>
    public static void AnnounceElement(GameObject go, bool interrupt = true, string context = null)
    {
        ResetFocusIfUnowned();
        if (!UiFocusOwnership.HasForegroundAccess(go)) return;
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

        if (!UiFocusOwnership.Allows(go)) return;

        var instanceId = go.GetInstanceID();
        var now = Time.unscaledTime;

        bool hasContext = !string.IsNullOrWhiteSpace(context);
        if (!hasContext && instanceId == _lastAnnouncedInstanceId && (now - _lastAnnounceTime) < AnnounceCooldown)
            return;

        if (instanceId != _lastAnnouncedInstanceId) _lastAnnouncementText = null;
        _lastAnnouncedInstanceId = instanceId;
        _lastAnnounceTime = now;
        _cachedFocusedGo = go;
        _cachedFocusedId = instanceId;

        var text = BuildAnnouncementText(go, popupActive || dialogueActive);
        if (hasContext || !string.IsNullOrWhiteSpace(text) && text != _lastAnnouncementText)
        {
            var debugParent = go.transform.parent?.name ?? "no-parent";
            Plugin.Log.LogDebug($"[Element] name={go.name}, parent={debugParent}");
            _lastAnnouncementText = text;
            ScreenReader.Speak(ScreenContentTracker.Combine(new[] { context, text }), interrupt);
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

            if (Settings.SettingsFocusReader.TryRead(out var setting) && UiVisibility.IsVisible(setting.Selected) &&
                (toggleGo.transform.IsChildOf(setting.Selected.transform) || setting.Selected.transform.IsChildOf(toggleGo.transform)))
                return;

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
        if (!UiFocusOwnership.HasForegroundAccess(go) || _lastAnnouncedInstanceId == 0) return false;
        if (ScreenContentAnnouncer.HasActiveContent && !ScreenContentAnnouncer.OwnsFocus(go)) return false;
        if (!ScreenContentAnnouncer.HasActiveContent && DialogueAnnouncer.HasActiveContent &&
            !DialogueAnnouncer.OwnsFocus(go)) return false;

        var focused = _cachedFocusedGo;
        if (!UiFocusOwnership.Allows(focused) || _cachedFocusedId != _lastAnnouncedInstanceId) return false;
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

    internal static bool TryReadDetails(GameObject go, out string text)
    {
        text = null;
        if (!UiFocusOwnership.Allows(go)) return false;
        if (go.GetComponent<TMP_InputField>() != null || go.GetComponentInParent<TMP_InputField>() != null) return false;
        bool restricted = ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent;
        string brief = BuildAnnouncementText(go, restricted);
        text = BuildAnnouncementText(go, restricted, includeDetails: true);
        if (string.IsNullOrWhiteSpace(text) || text == brief) { text = null; return false; }
        _lastAnnouncedInstanceId = go.GetInstanceID();
        _lastAnnounceTime = Time.unscaledTime;
        _lastAnnouncementText = brief;
        _cachedFocusedGo = go;
        _cachedFocusedId = _lastAnnouncedInstanceId;
        _pendingFocus = null;
        return true;
    }

    private static string BuildAnnouncementText(GameObject go, bool restrictToControl = false, bool includeDetails = false)
    {
        string label = null;
        string value = null;
        string description = null;

        if (!restrictToControl && Items.MerchantMenuReader.TryReadTab(go, out string merchantTab))
            return merchantTab;
        if (ClassSelectionReader.TryReadControl(go, out var classSelection))
            return string.IsNullOrWhiteSpace(classSelection.Name) ? null :
                ScreenContentTracker.Combine(new[] { classSelection.Name, includeDetails ? classSelection.Description : null });
        // Special case: Check if we're inside an expanded dropdown list
        var dropdownItemText = TextDetector.TryGetDropdownItemText(go);
        if (dropdownItemText != null)
            return dropdownItemText;

        // Check if this option is disabled by the game developers
        bool isDisabledOption = IsDisabledOption(go);

        if (CraftingMaterialReader.TryRead(go, out string materialText, out bool materialUnavailable))
            return FormatSpecializedAnnouncement(go, materialText, includeDetails, isDisabledOption || materialUnavailable);
        if (CraftingControlReader.TryRead(go, includeDetails, out string craftingText, out bool craftingUnavailable))
            return FormatSpecializedAnnouncement(go, craftingText, includeDetails, isDisabledOption || craftingUnavailable);
        if (FilterRangeControlReader.TryRead(go, out string filterRangeText, out bool filterRangeUnavailable))
            return FormatSpecializedAnnouncement(go, filterRangeText, includeDetails, isDisabledOption || filterRangeUnavailable);

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
        var inputField = go.GetComponent<TMP_InputField>() ?? go.GetComponentInParent<TMP_InputField>() ??
            (slider == null ? go.GetComponentInChildren<TMP_InputField>() : null);

        // For input fields
        if (inputField != null)
        {
            var inputText = inputField.text;
            var placeholderTmp = inputField.placeholder?.TryCast<TextMeshProUGUI>();
            var placeholder = placeholderTmp != null ? TextUtils.CleanText(placeholderTmp.text) : null;

            if (!restrictToControl && inputField.GetComponentInParent<LE.Settings.NumericSettingUI>() != null)
            {
                TextDetector.FindLabelInImmediateContainer(inputField.gameObject, ref label, ref description, inputText);
                if (!string.IsNullOrWhiteSpace(label))
                    return FormatAnnouncement(label, inputText, includeDetails ? description : null, go, isDisabledOption);
            }

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
                return ScreenContentTracker.Combine(new[] { buttonText, includeDetails ? description : null });
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
            if (TextUtils.IsVisibleLabel(dropdown.captionText))
                value = TextUtils.CleanText(dropdown.captionText.text);
        }
        else if (toggle != null)
        {
            value = ModText.Get(toggle.isOn ? "state.on" : "state.off");
        }
        else if (slider != null)
        {
            value = Settings.SettingsValueReader.ReadSlider(slider) ?? Mathf.RoundToInt(slider.value).ToString();
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

        return FormatAnnouncement(label, value, includeDetails ? description : null, go, isDisabledOption);
    }

    private static string FormatSpecializedAnnouncement(GameObject go, string text, bool includeDetails, bool unavailable)
    {
        string label = null;
        string description = null;
        if (includeDetails) TextDetector.TryGetFromTooltipInfo(go, ref label, ref description);
        return FormatAnnouncement(text, null, description, go, unavailable);
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
