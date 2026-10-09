using LeAccess.Accessibility.Utils;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>A world-map region's localized name, as the game's own region label shows it.</summary>
internal static class MapRegionName
{
    // UIWaypointRegion.UpdateLabel (build 25764382, 0x181E980) shows GetText("Region_Name_" + regioniName); the label
    // keeps its English prefab text until the region icon is first enabled (reference/waypoint-map.md).
    private const string KeyPrefix = "Region_Name_";

    internal static string Get(UIWaypointRegion region)
    {
        if (!IsAlive(region)) return null;
        string key = KeyPrefix + region.regioniName;
        string text = string.IsNullOrEmpty(region.regioniName) ? null : TextUtils.CleanText(Localization.GetText(key, ""));
        if (!string.IsNullOrWhiteSpace(text) && text != key) return text;
        return IsAlive(region.regionLabel) ? TextUtils.CleanText(region.regionLabel.text) : null;
    }
}
