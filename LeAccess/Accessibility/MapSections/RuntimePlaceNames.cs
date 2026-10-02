using System;
using System.Text.RegularExpressions;

namespace LeAccess.Accessibility.MapSections;

internal static class RuntimePlaceNames
{
    private static readonly Regex CamelBoundary = new("([a-z])([A-Z])", RegexOptions.CultureInvariant);
    private static readonly Regex Words = new("[a-z]+", RegexOptions.CultureInvariant);

    internal static bool IsContext(string key) => key is "map.section.room_orobyss" or "map.section.room_eterra" or
        "map.section.courtyard" or "map.section.council_chamber" or "map.section.torture_room" or "map.section.junction";

    internal static bool RejectBranch(string name, bool explicitFloor = false)
    {
        string normalized = Normalize(name);
        if (normalized.Contains("not walkable", StringComparison.Ordinal) ||
            normalized.Contains("non walkable", StringComparison.Ordinal)) return true;
        foreach (Match word in Words.Matches(normalized))
            if (word.Value is "background" or "backdrop" or "preview" or "broken" or "destroyed" or
                "collapsed" or "rubble" or "spawner" or "sfx" or "vfx" or "effect" or "effects" or
                "vegetation" or "coral" or "biomass" or "vase" or "crate" or "barrel" or "objective" or
                "notification" or "interactable" or "clicklistener" or "notwalkable" or "nonwalkable") return true;
            else if (!explicitFloor && word.Value is "railing" or "railings" or "rail" or "rails" or "pillar" or
                "pillars" or "column" or "columns" or "wall" or "walls" or "roof" or "roofs" or "shield" or "shields") return true;
        return false;
    }

    internal static bool IsFloor(string name)
    {
        if (RejectBranch(name)) return false;
        string normalized = Normalize(name);
        if (normalized.StartsWith("torture room center", StringComparison.Ordinal) ||
            normalized.StartsWith("toe_base_square", StringComparison.Ordinal)) return true;
        bool floor = false;
        foreach (Match word in Words.Matches(Normalize(name)))
        {
            if (word.Value is "folder" or "subfolder" or "group") return false;
            if (word.Value is "floor" or "ground" or "road" or "walkway" or "boardwalk" or "corridor" or "tunnel") floor = true;
        }
        return floor;
    }

    internal static string ClassifyContext(string name)
    {
        if (RejectBranch(name)) return null;
        string normalized = Normalize(name);
        if (normalized.StartsWith("orobyss room", StringComparison.Ordinal)) return "map.section.room_orobyss";
        if (normalized.StartsWith("eterra room", StringComparison.Ordinal)) return "map.section.room_eterra";
        if (normalized.StartsWith("torture room center", StringComparison.Ordinal)) return "map.section.torture_room";
        bool council = false, chamber = false;
        foreach (Match word in Words.Matches(normalized))
        {
            if (word.Value == "courtyard") return "map.section.courtyard";
            if (word.Value is "junction" or "intersection") return "map.section.junction";
            council |= word.Value == "council";
            chamber |= word.Value == "chamber";
        }
        if (council && chamber) return "map.section.council_chamber";
        return null;
    }

    internal static string Classify(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || RejectBranch(name)) return null;
        bool bridge = false, stairs = false, walkway = false, cave = false, entrance = false, dock = false;
        bool corridor = false, tunnel = false, floor = false;
        foreach (Match word in Words.Matches(Normalize(name)))
        {
            switch (word.Value)
            {
                case "folder": case "subfolder": case "group": case "groups": return null;
                case "bridge": case "bridges": case "woodbridge": bridge = true; break;
                case "stair": case "stairs": case "staircase": case "stairway": stairs = true; break;
                case "walkway": case "walkways": case "boardwalk": case "catwalk": walkway = true; break;
                case "cave": case "cavern": cave = true; break;
                case "entrance": entrance = true; break;
                case "dock": case "docks": case "pier": dock = true; break;
                case "corridor": case "hallway": corridor = true; break;
                case "tunnel": tunnel = true; break;
                case "floor": floor = true; break;
            }
        }
        // For example, TOE_Walkway_Stairs is the stair piece, not the whole walkway.
        string category = stairs ? "stairs" : cave && entrance ? "cave_entrance" : bridge ? "bridge" :
            walkway ? "walkway" : dock ? "dock" : corridor ? "corridor" : tunnel ? "tunnel" : cave && floor ? "cave" : null;
        return category is null ? null : "map.section." + category;
    }

    private static string Normalize(string name) => CamelBoundary.Replace(name ?? "", "$1 $2").ToLowerInvariant();
}
