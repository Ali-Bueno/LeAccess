using GraphicsBackend.Options;
using HarmonyLib;
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

    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.VirtualMouseIsEnabled))]
    public static class VirtualMouseIsEnabledPatch
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
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

        var text = BuildAnnouncementText(go);
        if (!string.IsNullOrWhiteSpace(text))
            ScreenReader.Speak(text);
    }

    private static string BuildAnnouncementText(GameObject go)
    {
        string label = null;
        string value = null;
        string description = null;

        // Special case: Check if we're inside an expanded dropdown list
        var dropdownItemText = TryGetDropdownItemText(go);
        if (dropdownItemText != null)
            return dropdownItemText;

        // Layer 1: Try to get label from InfoForTooltip
        TryGetFromTooltipInfo(go, ref label, ref description);

        // Detect control type and get value (search in go, parents, and children)
        var dropdown = go.GetComponent<TMP_Dropdown>() ?? go.GetComponentInParent<TMP_Dropdown>() ?? go.GetComponentInChildren<TMP_Dropdown>();
        var toggle = go.GetComponent<Toggle>() ?? go.GetComponentInParent<Toggle>() ?? go.GetComponentInChildren<Toggle>();
        var slider = go.GetComponent<Slider>() ?? go.GetComponentInParent<Slider>() ?? go.GetComponentInChildren<Slider>();
        var button = go.GetComponent<Button>() ?? go.GetComponentInParent<Button>();

        // Get control value
        if (dropdown != null)
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

        // For buttons without value controls, just get their text directly
        if (button != null && dropdown == null && toggle == null && slider == null)
        {
            var buttonText = GetTextFromHierarchy(button.gameObject);
            if (!string.IsNullOrEmpty(buttonText))
                return buttonText;
        }

        // Layer 2: Try to get label from GraphicsOption components
        if (string.IsNullOrEmpty(label))
        {
            TryGetFromGraphicsOption(go, ref label);
        }

        // Layer 3: Intelligent text search (limited to immediate option container)
        if (string.IsNullOrEmpty(label))
        {
            var controlGo = dropdown?.gameObject ?? toggle?.gameObject ?? slider?.gameObject ?? go;
            FindLabelInImmediateContainer(controlGo, ref label, ref description, value);
        }

        // Layer 3.5: If still no label, search more aggressively in parent hierarchy
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(description))
        {
            FindLabelAndDescriptionAggressive(go, value, ref label, ref description);
        }

        // Layer 4: Last resort - search ANY text in parent hierarchy
        if (string.IsNullOrEmpty(label))
        {
            label = FindAnyTextInParents(go, value);
        }

        // Layer 4.5: If we found a label but no value, try harder to find the control
        if (!string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value))
        {
            // Search in parents up to 4 levels for a toggle
            var current = go.transform.parent;
            for (int i = 0; i < 4 && current != null && toggle == null; i++)
            {
                toggle = current.GetComponentInChildren<Toggle>();
                if (toggle != null)
                {
                    value = toggle.isOn ? "On" : "Off";
                    break;
                }
                current = current.parent;
            }
        }

        // Layer 5: Use GameObject name if nothing else works
        if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value))
        {
            var goName = CleanObjectName(go.name);
            if (!IsGenericName(goName) && goName.Length > 3)
                label = goName;
        }

        // If no specific control found and no label, try to get text directly
        if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(label))
        {
            var directText = GetTextFromHierarchy(go);
            if (!string.IsNullOrEmpty(directText))
                return directText;
        }

        // Build result in order: label, value, description
        return FormatAnnouncement(label, value, description, go);
    }

    /// <summary>
    /// Check if we're navigating inside an expanded dropdown list and get the item text
    /// </summary>
    private static string TryGetDropdownItemText(GameObject go)
    {
        // Check if this is a dropdown item (usually has Toggle component inside a Dropdown List)
        var toggle = go.GetComponent<Toggle>() ?? go.GetComponentInParent<Toggle>();
        if (toggle == null) return null;

        // Check if we're inside a dropdown list by looking for parent names
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
            // Stop if we hit a scroll content - we're definitely in a list
            if (name.Contains("content") && current.parent?.name.ToLowerInvariant().Contains("viewport") == true)
            {
                isInDropdownList = true;
                break;
            }
            current = current.parent;
        }

        if (!isInDropdownList) return null;

        // Get the text from this item
        var itemText = GetTextFromHierarchy(go);
        if (!string.IsNullOrEmpty(itemText))
            return itemText;

        return null;
    }

    /// <summary>
    /// Layer 1: Try to get label and description from InfoForTooltip component
    /// </summary>
    private static void TryGetFromTooltipInfo(GameObject go, ref string label, ref string description)
    {
        try
        {
            var tooltip = go.GetComponent<InfoForTooltip>() ?? go.GetComponentInParent<InfoForTooltip>();
            if (tooltip == null) return;

            // Call SetText to populate tooltipInfo
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

    /// <summary>
    /// Layer 2: Try to get label from GraphicsOption components (ToggleOption, SliderOption)
    /// </summary>
    private static void TryGetFromGraphicsOption(GameObject go, ref string label)
    {
        try
        {
            // Try ToggleOption
            var toggleOption = go.GetComponent<ToggleOption>() ?? go.GetComponentInParent<ToggleOption>();
            if (toggleOption != null && !string.IsNullOrWhiteSpace(toggleOption.displayName))
            {
                label = CleanText(toggleOption.displayName);
                return;
            }

            // Try SliderOption
            var sliderOption = go.GetComponent<SliderOption>() ?? go.GetComponentInParent<SliderOption>();
            if (sliderOption != null && !string.IsNullOrWhiteSpace(sliderOption.displayName))
            {
                label = CleanText(sliderOption.displayName);
            }
        }
        catch { }
    }

    /// <summary>
    /// Layer 3.5: Find the single-option container and search for texts there
    /// Key insight: option container has exactly ONE control, section container has MANY
    /// </summary>
    private static void FindLabelAndDescriptionAggressive(GameObject go, string excludeValue, ref string label, ref string description)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);
        if (!string.IsNullOrEmpty(label))
            seen.Add(label);
        if (!string.IsNullOrEmpty(description))
            seen.Add(description);

        // Find the option container - the smallest container that has our control
        var optionContainer = FindSingleOptionContainer(go);
        if (optionContainer == null)
            return;

        // Search all texts within this container
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

    /// <summary>
    /// Layer 4: Last resort - find any text that looks like a label in parent hierarchy
    /// </summary>
    private static string FindAnyTextInParents(GameObject go, string excludeValue)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);

        var current = go.transform.parent;

        // Search up to 5 levels, looking for any text
        for (int level = 0; level < 5 && current != null; level++)
        {
            // Get all texts at this level (direct children only, not recursive)
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);

                // Check the child itself
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                {
                    var text = CleanText(tmp.text);
                    if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
                    {
                        var textType = ClassifyText(text);
                        // Accept Label or Unknown (but not Value or Description)
                        if (textType == TextType.Label || textType == TextType.Unknown)
                            return text;
                    }
                }

                // Also check grandchildren (one level deeper)
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

            // Stop at scroll content
            if (IsScrollContent(current))
                break;

            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// Find the container that holds exactly this one option (not a section with multiple options)
    /// </summary>
    private static Transform FindSingleOptionContainer(GameObject controlGo)
    {
        var current = controlGo.transform.parent;

        for (int level = 0; level < 5 && current != null; level++)
        {
            // Count how many controls are in this container
            int controlCount = 0;
            var toggles = current.GetComponentsInChildren<Toggle>(true);
            var sliders = current.GetComponentsInChildren<Slider>(true);
            var dropdowns = current.GetComponentsInChildren<TMP_Dropdown>(true);

            controlCount = toggles.Length + sliders.Length + dropdowns.Length;

            // If this container has exactly 1 control, this is our option container
            if (controlCount == 1)
                return current;

            // If it has 0 controls, keep going up
            if (controlCount == 0)
            {
                current = current.parent;
                continue;
            }

            // If it has multiple controls, we went too far - use previous level
            // But first check if we're at level 0, then we need this container
            if (level == 0)
                return current;

            // Otherwise return null to use other methods
            return null;
        }

        return null;
    }

    /// <summary>
    /// Layer 3: Find label and description in the option's container
    /// </summary>
    private static void FindLabelInImmediateContainer(GameObject controlGo, ref string label, ref string description, string excludeValue)
    {
        // Go up to find the option row container (usually 1-3 levels)
        var parent = controlGo.transform.parent;

        for (int level = 0; level < 3 && parent != null; level++)
        {
            var parentName = parent.name.ToLowerInvariant();

            // Stop if we hit a scroll content container (has many option children)
            if (IsScrollContent(parent))
                break;

            // Look for text in siblings at this level
            FindTextsInSiblings(parent, controlGo, excludeValue, ref label, ref description, level);

            // If we found a label, we can stop going up
            if (label != null)
                break;

            parent = parent.parent;
        }
    }

    /// <summary>
    /// Check if this is a scroll content container (has many similar children)
    /// </summary>
    private static bool IsScrollContent(Transform t)
    {
        var name = t.name.ToLowerInvariant();

        // Direct name matches for scroll containers
        if (name == "content" || name == "viewport" || name.Contains("scroll"))
            return true;

        // If parent is viewport, this is likely content
        if (t.parent != null && t.parent.name.ToLowerInvariant().Contains("viewport"))
            return true;

        // If it has many children with similar structures, it's likely a list container
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
            // If multiple children have controls, this is a list of options
            if (childrenWithControls >= 3)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Find label and description texts among siblings of the control
    /// </summary>
    private static void FindTextsInSiblings(Transform parent, GameObject controlGo, string excludeValue,
        ref string label, ref string description, int level)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);

        // Collect all texts first, then assign based on position and type
        var allTexts = new List<(string text, TextType type, int siblingIndex)>();

        for (int i = 0; i < parent.childCount; i++)
        {
            var sibling = parent.GetChild(i);

            // Skip if the control is inside this sibling (but allow collecting from control's parent)
            if (controlGo.transform.IsChildOf(sibling) && sibling.gameObject != controlGo.transform.parent?.gameObject)
                continue;

            CollectTextsFromTransform(sibling, controlGo, seen, allTexts, i, 0);
        }

        // Find the control's sibling index (or its parent's index)
        int controlSiblingIndex = controlGo.transform.parent == parent
            ? controlGo.transform.GetSiblingIndex()
            : controlGo.transform.parent?.GetSiblingIndex() ?? 0;

        // First pass: find label (prefer texts BEFORE the control, classified as Label)
        foreach (var (text, textType, siblingIndex) in allTexts)
        {
            if (label == null && textType == TextType.Label && siblingIndex <= controlSiblingIndex)
            {
                label = text;
                break;
            }
        }

        // If no label found before control, try any label
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

        // Find description (prefer texts AFTER the control)
        foreach (var (text, textType, siblingIndex) in allTexts)
        {
            if (description == null && textType == TextType.Description && siblingIndex >= controlSiblingIndex)
            {
                description = text;
                break;
            }
        }

        // If no description after control, try any description
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

    /// <summary>
    /// Collect and classify texts from a transform and its children
    /// </summary>
    private static void CollectTextsFromTransform(Transform t, GameObject controlGo, HashSet<string> seen,
        List<(string text, TextType type, int siblingIndex)> results, int siblingIndex, int depth)
    {
        if (depth > 2) return;

        // Skip the control itself
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

        // Recurse into children
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            // Don't recurse into the control
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

    /// <summary>
    /// Classify text as label, value, or description based on patterns
    /// </summary>
    private static TextType ClassifyText(string text)
    {
        if (string.IsNullOrEmpty(text)) return TextType.Unknown;

        // Check if it's a known value pattern (exact match)
        if (KnownValuePatterns.Contains(text))
            return TextType.Value;

        // Check if it's numeric (value)
        if (int.TryParse(text, out _) || float.TryParse(text, out _))
            return TextType.Value;

        // Check if it looks like a percentage or numeric value
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+%?$"))
            return TextType.Value;


        // Description: text with sentence-like patterns (punctuation, specific words)
        // Don't classify as description based on length alone
        if (IsDescriptionText(text))
            return TextType.Description;

        // Label: text without sentence endings (can be any length if no punctuation)
        if (!text.EndsWith(".") && !text.EndsWith("!") && !text.EndsWith("?"))
            return TextType.Label;

        return TextType.Unknown;
    }

    /// <summary>
    /// Format the announcement in the correct order: label, value, description
    /// </summary>
    private static string FormatAnnouncement(string label, string value, string description, GameObject go)
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

        if (!string.IsNullOrEmpty(description) && result.Length > 0)
        {
            result.Append(". ");
            result.Append(description);
        }

        // Layer 4: Fallback to GameObject name
        if (result.Length == 0)
        {
            var cleanName = CleanObjectName(go.name);
            if (!IsGenericName(cleanName))
                return cleanName;
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets text from element or its children
    /// </summary>
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

    /// <summary>
    /// Checks if text looks like a description using ONLY universal patterns (no language-specific words)
    /// </summary>
    private static bool IsDescriptionText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // Too short to be a description
        if (text.Length < 30) return false;

        // Descriptions have sentence punctuation - this is universal across all languages
        // Labels typically don't have periods, commas in the middle, or colons
        if (text.Contains(". ") ||      // Period followed by space (sentence)
            text.EndsWith(".") ||        // Ends with period
            text.Contains(", ") ||       // Comma in sentence
            text.Contains("; ") ||       // Semicolon
            text.Contains("- "))         // Dash in explanation
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
