using HarmonyLib;
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

        // Check for TMP_Dropdown
        var dropdown = go.GetComponent<TMP_Dropdown>() ?? go.GetComponentInParent<TMP_Dropdown>();
        if (dropdown != null)
        {
            // Get selected value from dropdown's caption
            if (dropdown.captionText != null)
                value = CleanText(dropdown.captionText.text);

            // Find label and description in parent hierarchy
            FindLabelAndDescription(dropdown.gameObject, out label, out description, value);
        }

        // Check for Toggle
        var toggle = go.GetComponent<Toggle>() ?? go.GetComponentInParent<Toggle>();
        if (toggle != null && dropdown == null)
        {
            value = toggle.isOn ? "activado" : "desactivado";
            FindLabelAndDescription(toggle.gameObject, out label, out description, value);
        }

        // Check for Slider
        var slider = go.GetComponent<Slider>() ?? go.GetComponentInParent<Slider>();
        if (slider != null && dropdown == null && toggle == null)
        {
            value = Mathf.RoundToInt(slider.value).ToString();
            FindLabelAndDescription(slider.gameObject, out label, out description, value);
        }

        // Check for Button (menu items)
        var button = go.GetComponent<Button>() ?? go.GetComponentInParent<Button>();
        if (button != null && dropdown == null && toggle == null && slider == null)
        {
            // For buttons, just get the text
            var buttonText = GetTextFromHierarchy(button.gameObject);
            if (!string.IsNullOrEmpty(buttonText))
                return buttonText;
        }

        // If no specific control found, try to get text directly
        if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(label))
        {
            var directText = GetTextFromHierarchy(go);
            if (!string.IsNullOrEmpty(directText))
                return directText;
        }

        // Build result
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

        if (result.Length == 0)
        {
            var cleanName = CleanObjectName(go.name);
            if (!IsGenericName(cleanName))
                return cleanName;
        }

        return result.ToString();
    }

    /// <summary>
    /// Finds label and description by going up the hierarchy to find the option container
    /// </summary>
    private static void FindLabelAndDescription(GameObject controlGo, out string label, out string description, string excludeValue)
    {
        label = null;
        description = null;

        // Go up to find the option container (usually 1-2 levels up)
        var current = controlGo.transform.parent;
        int controlIndex = controlGo.transform.GetSiblingIndex();

        for (int level = 0; level < 3 && current != null; level++)
        {
            // Collect all TextMeshProUGUI in this container's direct children
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);

                // Skip the control itself and its children
                if (child.gameObject == controlGo) continue;
                if (child.IsChildOf(controlGo.transform)) continue;
                if (controlGo.transform.IsChildOf(child)) continue;

                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                    tmp = child.GetComponentInChildren<TextMeshProUGUI>();

                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                {
                    var cleanedText = CleanText(tmp.text);
                    if (string.IsNullOrEmpty(cleanedText) || cleanedText == excludeValue)
                        continue;

                    // Label: comes BEFORE the control, is short, not a description
                    if (label == null && i < controlIndex && cleanedText.Length < 80 && !IsDescriptionText(cleanedText))
                    {
                        label = cleanedText;
                    }
                    // Description: comes AFTER the control, is longer, looks like description
                    // ONLY look for description at level 0 (immediate parent) to avoid mixing
                    else if (description == null && level == 0 && i > controlIndex &&
                             cleanedText.Length >= 20 && IsDescriptionText(cleanedText))
                    {
                        description = cleanedText;
                    }
                }
            }

            // If we found a label, we're done searching for label
            // But keep looking at higher levels if we haven't found one
            if (label != null)
                break;

            // Update control index for next level
            controlIndex = current.GetSiblingIndex();
            current = current.parent;
        }
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
    /// Checks if text looks like a description (longer, contains punctuation or keywords)
    /// </summary>
    private static bool IsDescriptionText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // Descriptions are usually longer
        if (text.Length < 20) return false;

        // Contains sentence-like patterns
        if (text.Contains(".") || text.Contains(",") ||
            text.Contains("will") || text.Contains("puede") ||
            text.Contains("affect") || text.Contains("afecta") ||
            text.Contains("adjust") || text.Contains("ajusta") ||
            text.Contains("change") || text.Contains("cambia") ||
            text.Contains("modifica") || text.Contains("controla"))
            return true;

        return text.Length > 50;
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
