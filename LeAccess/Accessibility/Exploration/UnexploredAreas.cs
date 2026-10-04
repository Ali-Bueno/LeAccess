using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// One persistence volume of the game's minimap fog; entering it keeps that part of the map revealed.
internal readonly record struct MinimapRoom(int Index, Vector3 Centre, float Radius, bool Revealed);

// Which rooms have explored ground inside. Cells are never removed and new ones appear only around
// the player, so after one full pass per map only rooms within reach are searched again.
internal sealed class RoomGlimpses
{
    private readonly HashSet<int> _known = new();
    private ExploredMap _map;

    internal bool Seen(MinimapRoom room, Vector3 player, ExploredMap map, bool fullPass)
    {
        if (map is null) return false;
        if (_known.Contains(room.Index)) return true;
        float reach = room.Radius + 2 * map.ExploreRadius;
        if (!fullPass && ExploredMap.Finite(player) && Vector3.DistanceSquared(room.Centre, player) > reach * reach) return false;
        if (!map.AnyExploredWithin(room.Centre, room.Radius)) return false;
        _known.Add(room.Index);
        return true;
    }

    // Starts a pass; true when the map changed, so every room must be searched once.
    internal bool Begin(ExploredMap map)
    {
        if (ReferenceEquals(map, _map)) return false;
        _map = map;
        _known.Clear();
        return true;
    }

    internal void Clear()
    {
        _known.Clear();
        _map = null;
    }
}

// The scanner's unexplored areas (D2R "unexplored room"): minimap rooms never entered whose ground has
// been seen, or, in scenes without rooms, edges where unseen walkable ground continues. Nothing inside
// is revealed. See reference/exploration-mode.md#unexplored-areas.
internal static class UnexploredAreas
{
    internal const string RoomKeyPrefix = "room:";
    internal const string FrontierKeyPrefix = "frontier:";

    internal static bool IsUnexplored(string key) => key != null &&
        (key.StartsWith(RoomKeyPrefix, StringComparison.Ordinal) || key.StartsWith(FrontierKeyPrefix, StringComparison.Ordinal));

    // Remembered per scene with the seen places; the layout guards against a changed volume list.
    internal static string VisitKey(string layout, int index) => $"room|{layout}|{index}";

    // Marks the rooms the player stands in as visited, then lists unvisited rooms with explored ground inside.
    internal static int AddRooms(IReadOnlyList<MinimapRoom> rooms, string layout, Vector3 player,
        ExplorationSeenMemory memory, ExploredMap map, string name, List<ExplorationEntry> entries, out int visited,
        RoomGlimpses glimpses = null)
    {
        visited = 0;
        int listed = 0;
        if (rooms is null) return 0;
        bool fullPass = glimpses?.Begin(map) != false;
        foreach (var room in rooms)
        {
            if (!ExploredMap.Finite(room.Centre) || !float.IsFinite(room.Radius) || room.Radius <= 0) continue;
            string key = VisitKey(layout, room.Index);
            bool inside = ExploredMap.Finite(player) &&
                Vector3.DistanceSquared(room.Centre, player) <= room.Radius * room.Radius;
            if (inside) memory?.Add(key);
            if (room.Revealed || inside || memory?.Contains(key) == true)
            {
                visited++;
                continue;
            }
            if (glimpses is null ? map is null || !map.AnyExploredWithin(room.Centre, room.Radius)
                : !glimpses.Seen(room, player, map, fullPass)) continue;
            entries.Add(new ExplorationEntry(RoomKeyPrefix + room.Index, name, ExplorationCategory.Unexplored,
                room.Centre, room.Radius));
            listed++;
        }
        return listed;
    }

    // Each frontier area is reached at its edge, where its unseen ground begins.
    internal static int AddFrontier(ExploredMap map, List<FrontierArea> areas, string name, List<ExplorationEntry> entries)
    {
        if (map is null || areas is null) return 0;
        map.CollectFrontier(areas);
        float arrival = map.CellSize * ExploredMap.FrontierArrivalCells;
        foreach (var area in areas)
            entries.Add(new ExplorationEntry($"{FrontierKeyPrefix}{area.X},{area.Z},{area.Floor}", name,
                ExplorationCategory.Unexplored, area.Anchor, arrival));
        return areas.Count;
    }
}
