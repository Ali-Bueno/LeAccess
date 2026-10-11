using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.MapSections;

namespace LeAccess.Accessibility.Exploration;

// In a town the whole walkable area counts as explored, so the beacon can route to any NPC or stash.
internal static class TownExplored
{
    private static readonly NavMeshSeed Seed = new();
    private static ConfigEntry<bool> _enabled;
    private static long _session;
    private static bool _seeding;

    internal static void Initialize(ConfigFile config, string section) =>
        _enabled = config.Bind(section, "TownFullyExplored", true,
            "In town scenes the whole walkable area counts as explored, so the beacon routes to any NPC or stash without walking there first.");

    // Called from each beacon tick with the current map; a bounded batch of the NavMesh is imported per call.
    internal static void Update(MapSectionWorldSnapshot world, ExploredMap map, NavMeshWalkability walkability)
    {
        if (_enabled is not { Value: true } || map is null) return;
        if (_session != world.Session)
        {
            _session = world.Session;
            _seeding = IsTown(world.SceneName) && walkability.TryReadMesh(Seed);
            if (_seeding) Plugin.Log.LogInfo($"[Exploration beacon] town scene={world.SceneName} seeding explored ground from {Seed.Triangles} NavMesh triangles");
        }
        if (!_seeding) return;
        Seed.Step(map);
        if (!Seed.Done) return;
        _seeding = false;
        Plugin.Log.LogInfo($"[Exploration beacon] town scene={world.SceneName} seeded cells={map.CellCount}");
    }

    private static bool IsTown(string sceneName)
    {
        try { return !string.IsNullOrEmpty(sceneName) && SceneList.IsTownZone(sceneName); }
        catch (Exception) { return false; }
    }

    internal static void Cleanup()
    {
        _enabled = null;
        _session = 0;
        _seeding = false;
    }
}
