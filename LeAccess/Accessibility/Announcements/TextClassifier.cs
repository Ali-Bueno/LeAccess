using System.Collections.Generic;

namespace LeAccess.Accessibility.Announcements;

/// <summary>
/// Classification types for UI text elements.
/// </summary>
public enum TextType
{
    Label,
    Value,
    Description,
    Unknown
}

/// <summary>
/// Classifies UI text elements by their semantic role.
/// </summary>
public static class TextClassifier
{
    /// <summary>
    /// Known value patterns - universal short values that appear in most games/languages.
    /// </summary>
    public static readonly HashSet<string> KnownValuePatterns = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "on", "off", "yes", "no", "auto", "none", "default", "custom",
        "low", "medium", "high", "ultra", "max", "min", "normal"
    };

    /// <summary>
    /// Classifies text as Label, Value, Description, or Unknown.
    /// </summary>
    public static TextType ClassifyText(string text)
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

    /// <summary>
    /// Checks if text appears to be a description (long, contains punctuation).
    /// </summary>
    public static bool IsDescriptionText(string text)
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
}
