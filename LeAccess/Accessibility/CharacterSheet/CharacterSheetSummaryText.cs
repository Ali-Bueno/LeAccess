using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.ScreenContent;

namespace LeAccess.Accessibility.CharacterSheet;

/// <summary>Pure text assembly for the character-sheet summary; the labels and values are the game's own.</summary>
internal static class CharacterSheetSummaryText
{
    internal static string Entry(string label, string value) =>
        string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value) ? null : label.Trim() + ": " + value.Trim();

    /// <summary>An attribute's displayed texts: the first text without digits names it, the first with digits is its value.</summary>
    internal static string Attribute(IEnumerable<string> texts)
    {
        var shown = texts.Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
        return Entry(shown.FirstOrDefault(text => !text.Any(char.IsDigit)), shown.FirstOrDefault(text => text.Any(char.IsDigit)));
    }

    internal static string Compose(IEnumerable<string> entries) => ScreenContentTracker.Combine(entries);
}
