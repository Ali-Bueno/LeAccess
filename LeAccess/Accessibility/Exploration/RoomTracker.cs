using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// One persistence volume of the game's minimap fog; entering it keeps that part of the map revealed.
internal readonly record struct MinimapRoom(int Index, Vector3 Centre, float Radius, bool Revealed);

// The D2R scanner's rooms (docs/entity-scan.md): "sala N" for rooms stood in and "sala inexplorada N" for
// unvisited rooms whose ground has been seen; explored-ground edges in scenes without minimap rooms and,
// as unexplored areas, where no room covers the ground.
// Nothing inside an unexplored room is revealed. See reference/exploration-mode.md#rooms-and-unexplored-rooms.
internal sealed class RoomTracker
{
    internal const string RoomKeyPrefix = "room:";
    internal const string UnexploredKeyPrefix = "unexplored:";
    internal const string FrontierKeyPrefix = "frontier:";

    // Rooms known to hold explored ground; cells are never removed and new ones appear only around the
    // player, so after one full pass per map only rooms within reach are searched again.
    private readonly HashSet<int> _glimpsed = new();
    private readonly HashSet<int> _unvisited = new();
    private readonly Dictionary<(int X, int Z, int Floor), int> _frontierNumbers = new();
    private ExploredMap _map;
    private string _layout;
    private bool _fullPass, _baseline;

    internal static bool IsUnexplored(string key) => key != null &&
        (key.StartsWith(UnexploredKeyPrefix, StringComparison.Ordinal) || key.StartsWith(FrontierKeyPrefix, StringComparison.Ordinal));

    // Remembered per scene with the seen places; the layout guards against a changed volume list.
    internal static string VisitKey(string layout, int index) => $"room|{layout}|{index}";

    // Lists visited and unexplored rooms; entered receives the rooms first stepped into since the last pass
    // (never on the first pass of a map, which only takes the baseline).
    internal int AddRooms(IReadOnlyList<MinimapRoom> rooms, string layout, Vector3 player, ExplorationSeenMemory memory,
        ExploredMap map, Func<int, string> roomName, Func<int, string> unexploredName, List<ExplorationEntry> entries,
        List<MinimapRoom> entered, out int visited)
    {
        visited = 0;
        int unexplored = 0;
        if (rooms is null) return 0;
        Begin(map, layout);
        bool baseline = _baseline;
        _baseline = true;
        bool fullPass = _fullPass;
        _fullPass = false;
        foreach (var room in rooms)
        {
            if (!ExploredMap.Finite(room.Centre) || !float.IsFinite(room.Radius) || room.Radius <= 0) continue;
            string key = VisitKey(layout, room.Index);
            bool inside = ExploredMap.Finite(player) &&
                Vector3.DistanceSquared(room.Centre, player) <= room.Radius * room.Radius;
            if (inside) memory?.Add(key);
            int number = room.Index + 1;
            if (room.Revealed || inside || memory?.Contains(key) == true)
            {
                visited++;
                if (_unvisited.Remove(room.Index) && inside && baseline) entered?.Add(room);
                entries.Add(new ExplorationEntry(RoomKeyPrefix + room.Index, roomName(number), ExplorationCategory.Room,
                    room.Centre, room.Radius));
                continue;
            }
            _unvisited.Add(room.Index);
            if (!Glimpsed(room, player, map, fullPass)) continue;
            entries.Add(new ExplorationEntry(UnexploredKeyPrefix + room.Index, unexploredName(number),
                ExplorationCategory.UnexploredRoom, room.Centre, room.Radius));
            unexplored++;
        }
        return unexplored;
    }

    // Each frontier area keeps the number it was first given on this map and is reached at its edge. With rooms
    // (read by AddRooms first), only edges outside every visited or listed room count.
    internal int AddFrontier(ExploredMap map, List<FrontierArea> areas, Func<int, string> unexploredName,
        List<ExplorationEntry> entries, IReadOnlyList<MinimapRoom> rooms = null)
    {
        if (map is null || areas is null) return 0;
        Begin(map, null);
        map.CollectFrontier(areas, rooms is null ? null : point => Covered(rooms, point));
        float arrival = map.CellSize * ExploredMap.FrontierArrivalCells;
        foreach (var area in areas)
        {
            var key = (area.X, area.Z, area.Floor);
            if (!_frontierNumbers.TryGetValue(key, out int number)) _frontierNumbers[key] = number = _frontierNumbers.Count + 1;
            entries.Add(new ExplorationEntry($"{FrontierKeyPrefix}{area.X},{area.Z},{area.Floor}", unexploredName(number),
                ExplorationCategory.UnexploredRoom, area.Anchor, arrival));
        }
        return areas.Count;
    }

    // The nearest room by its nearest point (D2R room loops sound from the closest point inside the room):
    // distance from the player, camera-relative pan and metres ahead. The room the player is in never counts.
    internal static bool TryNearest(IReadOnlyList<MinimapRoom> rooms, Vector3 player, Vector3 forward, Vector3 right,
        out float distance, out float pan, out float front)
    {
        distance = float.PositiveInfinity;
        pan = front = 0;
        if (rooms is null || !ExploredMap.Finite(player)) return false;
        foreach (var room in rooms)
        {
            var offset = room.Centre - player;
            float gap = offset.Length() - room.Radius;
            if (!float.IsFinite(gap) || gap <= 0 || gap >= distance) continue;
            var toward = Vector3.Normalize(offset) * gap;
            float flat = MathF.Sqrt(toward.X * toward.X + toward.Z * toward.Z);
            distance = gap;
            pan = flat > 0 ? Math.Clamp(Vector3.Dot(toward, right) / flat, -1, 1) : 0;
            front = Vector3.Dot(toward, forward);
        }
        return float.IsFinite(distance);
    }

    internal void Clear()
    {
        _glimpsed.Clear();
        _unvisited.Clear();
        _frontierNumbers.Clear();
        _map = null;
        _layout = null;
        _fullPass = _baseline = false;
    }

    private void Begin(ExploredMap map, string layout)
    {
        if (ReferenceEquals(map, _map) && (layout is null || layout == _layout)) return;
        Clear();
        _map = map;
        _layout = layout;
        _fullPass = true;
    }

    // A visited room is explored ground and a glimpsed one is listed itself; an unseen room offers nothing yet.
    private bool Covered(IReadOnlyList<MinimapRoom> rooms, Vector3 point)
    {
        foreach (var room in rooms)
            if ((!_unvisited.Contains(room.Index) || _glimpsed.Contains(room.Index)) &&
                Vector3.DistanceSquared(room.Centre, point) <= room.Radius * room.Radius) return true;
        return false;
    }

    private bool Glimpsed(MinimapRoom room, Vector3 player, ExploredMap map, bool fullPass)
    {
        if (map is null) return false;
        if (_glimpsed.Contains(room.Index)) return true;
        float reach = room.Radius + 2 * map.ExploreRadius;
        if (!fullPass && ExploredMap.Finite(player) && Vector3.DistanceSquared(room.Centre, player) > reach * reach) return false;
        if (!map.AnyExploredWithin(room.Centre, room.Radius)) return false;
        _glimpsed.Add(room.Index);
        return true;
    }
}
