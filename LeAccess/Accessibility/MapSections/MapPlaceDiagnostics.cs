using System.Collections.Generic;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class MapPlaceDiagnostics
{
    // Discovery stages plus a few actual floor samples, bounded for a whole player/scene session.
    private const int SessionMessageLimit = 16;
    private const int FloorSampleLimit = 3;
    private const int PathDepth = 6;
    private const int PathCharacterLimit = 240;
    private static readonly HashSet<string> Seen = new();
    private static long _session;
    private static string _scene;
    private static int _floorSamples;

    internal static void Begin(MapSectionWorldSnapshot world)
    {
        if (_session == world.Session) return;
        _session = world.Session;
        _scene = world.SceneName;
        _floorSamples = 0;
        Seen.Clear();
    }

    internal static void Report(string stage, string detail)
    {
        if (Seen.Count >= SessionMessageLimit || !Seen.Add(stage)) return;
        Plugin.Log?.LogInfo($"Map places [{_scene}] {stage}: {detail}");
    }

    internal static void UnknownFloor(Transform floor)
    {
        if (_floorSamples >= FloorSampleLimit) return;
        string path = Path(floor);
        string stage = "unnamed-floor " + path;
        if (Seen.Contains(stage)) return;
        _floorSamples++;
        Report(stage, "physical contact has no accepted place owner");
    }

    internal static string Path(Transform value)
    {
        var names = new List<string>();
        for (int depth = 0; depth < PathDepth && IsAlive(value); depth++, value = value.parent)
            names.Add((value.name ?? "").Replace('\r', ' ').Replace('\n', ' '));
        names.Reverse();
        string path = string.Join("/", names);
        return path.Length <= PathCharacterLimit ? path : path.Substring(path.Length - PathCharacterLimit);
    }
}
