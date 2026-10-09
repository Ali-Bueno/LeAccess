using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>What an End of Time island shows on the map: name, timeline banners, corruption, harbinger and blessing.</summary>
internal static class MapIslandReader
{
    internal static IEnumerable<string> Read(MonolithIslandInfo island)
    {
        if (!IsAlive(island)) yield break;
        yield return WaypointReader.ReadLabel(island.IslandName);
        if (Shown(island.alternateTimelineLabel)) yield return WaypointReader.ReadLabel(island.alternateTimelineLabelText);
        if (Shown(island.conqueredTimelineLabel)) yield return TextUtils.GetTextFromHierarchy(island.conqueredTimelineLabel);
        if (Shown(island.empoweredTimelineLabel)) yield return WaypointReader.ReadLabel(island.empoweredTimelineLabelText);
        if (Shown(island.corruptionDisplay) && WaypointReader.ReadLabel(island.corruptionText) is { Length: > 0 } corruption)
            yield return ModText.Format("map.island.corruption", corruption);
        if (Shown(island.harbinger)) yield return ModText.Get("map.island.harbinger");
        var blessing = island.GetComponentInChildren<BlessingContainerUI>(false);
        if (IsAlive(blessing) && UiVisibility.IsVisible(blessing.gameObject))
            yield return MapItemName.Of(blessing) is { } name ? ModText.Format("map.island.blessing", name) : ModText.Get("map.island.no_blessing");
    }

    private static bool Shown(GameObject element) => IsAlive(element) && UiVisibility.IsVisible(element);
}
