using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.WorldCues;

// Pure rules on the game's object names (prefab ids) for interactables that carry no readable component or hover text.
internal static class InteractableNames
{
    private const string CloneSuffix = " (Clone)";
    private const string ShrineSuffix = " Shrine";
    private const string ShrineBuffPrefix = "Shrine";
    private const string ChestPrefix = "Chest";

    internal static string Clean(string objectName)
    {
        string name = objectName?.Trim() ?? "";
        return name.EndsWith(CloneSuffix, StringComparison.Ordinal) ? name[..^CloneSuffix.Length].TrimEnd() : name;
    }

    /// <summary>Whether the object is a placed shrine prefab ("Warpath Shrine (Clone)").</summary>
    internal static bool IsShrineObject(string objectName) =>
        Clean(objectName).EndsWith(ShrineSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the object is a chest prefab ("Chest1 - suitcase (Clone)").</summary>
    internal static bool IsChestObject(string objectName) =>
        Clean(objectName).StartsWith(ChestPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>The shrine's id word without spaces ("Timeslow Aura Shrine (Clone)" gives "TimeslowAura"); null for other objects.</summary>
    internal static string ShrineToken(string objectName)
    {
        if (!IsShrineObject(objectName)) return null;
        string name = Clean(objectName);
        return name[..^ShrineSuffix.Length].Replace(" ", "");
    }

    /// <summary>The shrine buff id for a shrine token ("ShrinePhoenixFlamethrower" for "Phoenix"): the exact word, else the shortest id that starts with it.</summary>
    internal static string MatchShrineBuff(string token, IEnumerable<string> buffIds)
    {
        if (string.IsNullOrEmpty(token)) return null;
        string best = null;
        foreach (string id in buffIds)
        {
            if (id is null || !id.StartsWith(ShrineBuffPrefix, StringComparison.Ordinal)) continue;
            string rest = id[ShrineBuffPrefix.Length..];
            if (rest.Equals(token, StringComparison.OrdinalIgnoreCase)) return id;
            if (rest.StartsWith(token, StringComparison.OrdinalIgnoreCase) && (best is null || id.Length < best.Length)) best = id;
        }
        return best;
    }
}
