using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterSheet;

/// <summary>Reads a character-sheet stat line as its displayed name and value.</summary>
internal static class CharacterStatLineReader
{
    internal static bool TryRead(GameObject go, out string text)
    {
        text = TryReadParts(go, out string name, out string value) ? ScreenContentTracker.Combine(new[] { name, value }) : null;
        return text is not null;
    }

    /// <summary>The line's displayed name and value, apart.</summary>
    internal static bool TryReadParts(GameObject go, out string name, out string value)
    {
        name = value = null;
        var line = go.GetComponentInParent<CharacterStatDisplayLineItem>();
        if (!IsAlive(line) || !UiVisibility.IsVisible(line.gameObject)) return false;
        var valueLabel = line._statValueDisplay;
        value = TextUtils.IsVisibleLabel(valueLabel) ? TextUtils.CleanText(valueLabel.text) : null;
        // The stat name is the line's other visible label; the value label is typed on the line.
        foreach (var label in line.GetComponentsInChildren<TMP_Text>())
        {
            if (!IsAlive(label) || IsAlive(valueLabel) && label.Pointer == valueLabel.Pointer ||
                !TextUtils.IsVisibleLabel(label)) continue;
            name = TextUtils.CleanText(label.text);
            if (!string.IsNullOrWhiteSpace(name)) break;
        }
        return !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value);
    }
}
