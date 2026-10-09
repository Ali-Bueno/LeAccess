using System.Collections.Generic;
using LeAccess.Accessibility.Utils;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Reads world-map icon tooltips (dungeons, factions, rifts) as the game's cursor opens them.</summary>
internal static class MapIconReader
{
    internal static (string Title, string Body) Read(GeneralTooltop icon)
    {
        if (!IsAlive(icon)) return default;
        icon.SetText();
        var info = icon.tooltipInfo;
        return info is null ? default : (TextUtils.CleanText(info.nameText), TextUtils.CleanText(info.mainText));
    }

    /// <summary>Titles of tooltips on a waypoint's own icon, such as the dungeon it leads to.</summary>
    internal static IEnumerable<string> AttachedTitles(UIWaypoint waypoint)
    {
        if (!IsAlive(waypoint)) yield break;
        foreach (var icon in waypoint.GetComponentsInChildren<GeneralTooltop>(false))
            if (Read(icon).Title is { Length: > 0 } title) yield return title;
    }

    /// <summary>Icons with a tooltip of their own; those on a waypoint are read with it.</summary>
    internal static bool IsStandalone(GeneralTooltop icon) =>
        IsAlive(icon) && !IsAlive(icon.GetComponentInParent<UIWaypoint>(true));
}
