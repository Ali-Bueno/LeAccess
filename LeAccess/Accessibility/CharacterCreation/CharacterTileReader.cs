using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Reads an existing character tile from its own displayed fields.</summary>
internal static class CharacterTileReader
{
    internal static bool TryRead(GameObject go, out string text)
    {
        text = null;
        var tile = go.GetComponentInParent<CharacterTile>();
        if (!IsAlive(tile) || !UiVisibility.IsVisible(tile.gameObject)) return false;
        text = ScreenContentTracker.Combine(new[]
        {
            ReadLabel(tile.charName), ReadLabel(tile.charLvlAndMastery), ReadLabel(tile.charCycle), ReadTag(tile.hardcoreTag)
        });
        return !string.IsNullOrWhiteSpace(text);
    }

    private static string ReadLabel(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) ? TextUtils.CleanText(label.text) : null;

    private static string ReadTag(GameObject tag) => IsAlive(tag) ? TextUtils.GetTextFromHierarchy(tag) : null;
}
