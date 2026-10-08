using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Items;

/// <summary>Reads a focused stash tab or folder: its name, contents and state, from the stash's own components.</summary>
internal static class StashTabReader
{
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        var tab = element.GetComponentInParent<StashTabUI>();
        if (IsAlive(tab))
        {
            string name = Name(tab);
            if (name == null) return false;
            text = StashTabText.Tab(name, tab._itemCount, tab.IsActive, tab.HasPriority);
            return true;
        }
        var folder = element.GetComponentInParent<StashFolderUI>();
        if (!IsAlive(folder) || !IsAlive(folder._collapsibleFolder)) return false;
        string folderName = Name(folder);
        if (folderName == null) return false;
        var tabs = folder._activeTabs;
        text = StashTabText.Folder(folderName, folder._collapsibleFolder.IsExpanded,
            tabs is null || tabs.WasCollected ? 0 : tabs.Count);
        return true;
    }

    // The label the game shows, which is already localized; the data name is the fallback.
    private static string Name(StashVisualElement element)
    {
        string shown = IsAlive(element._tabName) ? TextUtils.CleanText(element._tabName.text) : null;
        return string.IsNullOrWhiteSpace(shown) ? TextUtils.CleanText(element.Name) is { Length: > 0 } data ? data : null : shown;
    }
}
