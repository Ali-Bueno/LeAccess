using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Labels the realm buttons through their owned title and explanation.</summary>
internal static class CreationRealmReader
{
    // Exact bindings in the installed CharacterSelection prefab; see reference/creation-menu-reading.md.
    private const string ContentsPath = "CycleModal/Contents";

    internal static bool TryRead(GameObject go, out ClassSelection selection)
    {
        selection = default;
        var button = go.GetComponentInParent<Button>();
        if (!IsAlive(button) || !button.isActiveAndEnabled || !UiVisibility.IsVisible(button.gameObject)) return false;
        bool season;
        switch (button.gameObject.name)
        {
            case "SeasonSelectionButton":
            case "chooseCycleInsteadButton": season = true; break;
            case "LegacySelectionButton":
            case "chooseLegacyInsteadButton": season = false; break;
            default: return false;
        }
        var character = CharacterSelect.instance;
        if (!IsAlive(character) || !UiVisibility.IsVisible(character.gameObject) ||
            !button.transform.IsChildOf(character.transform)) return false;
        var contents = character.transform.Find(ContentsPath);
        if (!IsAlive(contents) || !UiVisibility.IsVisible(contents.gameObject)) return false;
        var tile = contents.Find(season ? "CycleTile" : "CycleTile (2)");
        if (!IsAlive(tile) || !IsAlive(button.transform.parent) || button.transform.parent.Pointer != tile.Pointer) return false;

        string name = Read(tile, season ? "SeasonName" : "Name");
        string label = string.IsNullOrWhiteSpace(name) ? null : ScreenContentTracker.Combine(new[]
        {
            Read(contents, "EterraFrameHeader/Header"), name, Read(tile, "Label/Type")
        });
        string description = ScreenContentTracker.Combine(new[]
        {
            Read(tile, "Message"), Read(contents, "Footer/CycleEndExplanation")
        });
        selection = new($"creation-realm:{button.Pointer}", button.gameObject, label, description);
        return true;
    }

    private static string Read(Transform root, string path)
    {
        var child = root.Find(path);
        var label = IsAlive(child) ? child.GetComponent<TMP_Text>() : null;
        return TextUtils.IsVisibleLabel(label) ? TextUtils.CleanText(label.text) : null;
    }
}
