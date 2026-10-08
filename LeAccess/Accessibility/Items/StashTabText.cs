using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Items;

/// <summary>Builds what is spoken for a stash tab or folder.</summary>
internal static class StashTabText
{
    internal static string Tab(string name, int items, bool selected, bool priority) =>
        ScreenContentTracker.Combine(new[] { name, Count("stash.items", items),
            selected ? ModText.Get("stash.tab_selected") : null, priority ? ModText.Get("stash.priority") : null });

    internal static string Folder(string name, bool expanded, int tabs) =>
        ScreenContentTracker.Combine(new[] { name, ModText.Get("stash.folder"),
            ModText.Get(expanded ? "stash.folder_expanded" : "stash.folder_collapsed"), Count("stash.tabs", tabs) });

    private static string Count(string key, int count) =>
        ModText.Format(count == 1 ? key + "_one" : key + "_many", count);
}
