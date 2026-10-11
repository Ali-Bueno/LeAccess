using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// The walkable ground of every minimap room the player has visited (or the game shows revealed) counts as explored,
// as the minimap shows it, so routes can take detours never seen from close up.
// See reference/exploration-mode.md#visited-minimap-rooms-count-as-explored-2026-10-11.
internal sealed class VisitedRoomSeed
{
    // A failed mesh read is retried at this pace, never per tick.
    internal const double RetrySeconds = 5;
    private readonly NavMeshSeed _seed = new();
    private readonly HashSet<int> _seeded = new();
    private readonly List<int> _running = new();
    private ExploredMap _map;
    private long _session;
    private double _nextRead;
    private int _added;

    internal int SeededRooms => _seeded.Count;
    internal bool Running => _running.Count > 0;

    /// <summary>One bounded step; returns the rooms finished by it (their cells are on the map), else 0.</summary>
    internal int Update(long session, ExploredMap map, IReadOnlyList<MinimapRoom> visited, double now,
        Func<NavMeshSeed, bool> readMesh, out int cells)
    {
        cells = 0;
        if (map is null) return 0;
        if (session != _session || !ReferenceEquals(map, _map)) Reset(session, map);
        if (_running.Count == 0 && !Start(visited, now, readMesh)) return 0;
        _added += _seed.Step(map);
        if (!_seed.Done) return 0;
        int finished = _running.Count;
        _seeded.UnionWith(_running);
        _running.Clear();
        cells = _added;
        _added = 0;
        return finished;
    }

    private bool Start(IReadOnlyList<MinimapRoom> visited, double now, Func<NavMeshSeed, bool> readMesh)
    {
        if (visited is null) return false;
        var regions = new List<(Vector3, float)>();
        foreach (var room in visited)
        {
            if (_seeded.Contains(room.Index) || _running.Contains(room.Index) || !ExploredMap.Finite(room.Centre) ||
                !float.IsFinite(room.Radius) || room.Radius <= 0) continue;
            _running.Add(room.Index);
            regions.Add((room.Centre, room.Radius));
        }
        if (_running.Count == 0) return false;
        if (!_seed.HasMesh)
        {
            bool read = now >= _nextRead && readMesh(_seed);
            if (!read)
            {
                if (now >= _nextRead) _nextRead = now + RetrySeconds;
                _running.Clear();
                return false;
            }
        }
        _seed.Restart(regions);
        return true;
    }

    private void Reset(long session, ExploredMap map)
    {
        _session = session;
        _map = map;
        _seeded.Clear();
        _running.Clear();
        _nextRead = 0;
        _added = 0;
        _seed.Begin(null, null, null, 0);
    }
}
