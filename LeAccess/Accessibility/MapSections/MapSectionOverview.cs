using System.Collections.Generic;
using LeAccess.I18n;

namespace LeAccess.Accessibility.MapSections;

internal static class MapSectionOverview
{
    private static SectionMap _map;
    private static string _language, _text;
    private static int _scene;
    private static int? _variant;
    internal static string Describe(SectionMap map, MapSectionWorldSnapshot world)
    {
        if (ReferenceEquals(map, _map) && _language == ModText.Language && _scene == world.SceneHandle &&
            _variant == world.Variant) return _text;
        _map = map;
        _language = ModText.Language;
        _scene = world.SceneHandle;
        _variant = world.Variant;
        var keys = new HashSet<string>();
        foreach (var section in map.Sections)
        {
            string key = section.KindKey;
            if (!keys.Contains(key) && MapSectionSources.IsAvailable(section, world)) keys.Add(key);
        }
        var names = new List<string>();
        foreach (string key in new[] { "map.section.bridge", "map.section.stairs", "map.section.walkway", "map.section.cave_entrance" })
            if (keys.Contains(key)) names.Add(ModText.Get(key));
        return _text = names.Count == 0 ? null : ModText.Format("map.overview", string.Join(", ", names));
    }
    internal static void Reset() { _map = null; _language = _text = null; }
}
