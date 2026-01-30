using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility.Utils;

/// <summary>
/// Utility methods for text cleaning and extraction.
/// </summary>
public static class TextUtils
{
    /// <summary>
    /// Cleans text by removing rich text tags and normalizing whitespace.
    /// </summary>
    public static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    /// <summary>
    /// Cleans GameObject names by removing clone suffixes and adding spaces to camelCase.
    /// </summary>
    public static string CleanObjectName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(\d+\)$", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(Clone\)$", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        return name.Replace("_", " ").Trim();
    }

    /// <summary>
    /// Cleans panel names for announcement.
    /// </summary>
    public static string CleanPanelName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace("Panel", "").Replace("(Clone)", "").Replace("UI", "").Trim();
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2").Trim();
    }

    /// <summary>
    /// Checks if a name is a generic UI element name.
    /// </summary>
    public static bool IsGenericName(string name)
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

    /// <summary>
    /// Gets text from a GameObject's hierarchy (TMP_Text or legacy Text).
    /// </summary>
    public static string GetTextFromHierarchy(GameObject go)
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
    /// Disables the virtual mouse cursor.
    /// </summary>
    public static void DisableVirtualMouse()
    {
        try { EpochInputManager.SetActiveVirtualMouse(false); } catch { }
    }
}
