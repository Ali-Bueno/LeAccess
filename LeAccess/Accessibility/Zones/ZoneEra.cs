using LeAccess.Accessibility.Utils;

namespace LeAccess.Accessibility.Zones;

/// <summary>The game's own name for a scene's era; the End of Time belongs to none.</summary>
internal static class ZoneEra
{
    // The game's Common string table names eras "Era_Name_" + SceneList.Era (survey in reference/zone-reading.md).
    private const string KeyPrefix = "Era_Name_";

    internal static string Name(SceneList.Era era)
    {
        if (era == SceneList.Era.EndOfTime) return null;
        string key = KeyPrefix + era;
        string text = TextUtils.CleanText(Localization.GetText(key, ""));
        return string.IsNullOrWhiteSpace(text) || text == key ? null : text;
    }
}
