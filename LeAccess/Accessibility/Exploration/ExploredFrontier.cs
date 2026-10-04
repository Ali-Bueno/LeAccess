using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

internal readonly record struct FrontierArea(int X, int Z, int Floor, Vector3 Anchor, int Links);

// Edges of explored ground where walkable ground continues unseen: the unexplored areas of scenes
// without minimap rooms (reference/exploration-mode.md#unexplored-areas).
internal sealed partial class ExploredMap
{
    // Navigation tuning, not game constants: an area groups about one room's worth of edge; unseen
    // ground past the edge must span this many cells, so a pocket behind a crate is not listed; an
    // area is reached this many cells from its edge, which is in plain sight by then.
    internal const int FrontierAreaCells = 8;
    internal const int FrontierRegionCells = 6;
    internal const int FrontierArrivalCells = 2;
    internal const int DefaultFrontierBudget = 24;
    private static readonly (int X, int Z)[] Sides = { (1, 0), (0, 1), (-1, 0), (0, -1) };
    private const int AllDirections = 0xFF;
    private static readonly (int X, int Z)[] Neighbours =
        { (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1) };

    private readonly List<byte> _frontierChecked = new();
    private readonly List<int> _frontierPending = new();
    private readonly HashSet<long> _frontierLinks = new();
    private readonly Dictionary<(int X, int Z, int Floor), List<Vector3>> _frontierGroups = new();
    private readonly List<long> _staleLinks = new();
    private readonly Dictionary<(int X, int Z, int Floor), bool> _regions = new();
    private readonly Queue<((int X, int Z) Column, Vector3 Point)> _regionQueue = new();
    private readonly HashSet<(int X, int Z)> _regionSeen = new();

    internal int FrontierPending => _frontierPending.Count;
    internal int FrontierLinks => _frontierLinks.Count;

    private void TrackFrontier(int cell)
    {
        _frontierChecked.Add(0);
        _frontierPending.Add(cell);
    }

    // Checks the newest cells first (the player's surroundings), within a budget of native queries.
    internal int UpdateFrontier(IWalkability nav, int budget = DefaultFrontierBudget)
    {
        if (nav is null) return 0;
        int queries = 0;
        while (queries < budget && _frontierPending.Count > 0)
        {
            int cell = _frontierPending[^1];
            int mask = _frontierChecked[cell];
            for (int direction = 0; direction < Neighbours.Length && queries < budget; direction++)
            {
                int bit = 1 << direction;
                if ((mask & bit) != 0) continue;
                if (OpensUnexplored(cell, direction, nav, ref queries)) _frontierLinks.Add(Link(cell, direction));
                mask |= bit;
            }
            _frontierChecked[cell] = (byte)mask;
            if (mask == AllDirections) _frontierPending.RemoveAt(_frontierPending.Count - 1);
        }
        return queries;
    }

    // Unexplored edges grouped by area, each anchored at its edge nearest the group's middle.
    internal void CollectFrontier(List<FrontierArea> areas)
    {
        areas.Clear();
        foreach (var group in _frontierGroups.Values) group.Clear();
        _staleLinks.Clear();
        foreach (long link in _frontierLinks)
        {
            int cell = (int)(link >> 3), direction = (int)(link & 7);
            var point = _points[cell];
            var (x, z) = _cellColumns[cell];
            var column = (x + Neighbours[direction].X, z + Neighbours[direction].Z);
            if (Find(column, point.Y) >= 0)
            {
                _staleLinks.Add(link);
                continue;
            }
            var key = (FloorDiv(column.Item1, FrontierAreaCells), FloorDiv(column.Item2, FrontierAreaCells), FloorOf(point.Y));
            if (!_frontierGroups.TryGetValue(key, out var edges)) _frontierGroups[key] = edges = new List<Vector3>();
            edges.Add(new Vector3(CenterOf(column.Item1), point.Y, CenterOf(column.Item2)));
        }
        foreach (long stale in _staleLinks) _frontierLinks.Remove(stale);
        foreach (var (key, edges) in _frontierGroups)
        {
            if (edges.Count == 0) continue;
            var middle = Vector3.Zero;
            foreach (var edge in edges) middle += edge;
            middle /= edges.Count;
            var anchor = edges[0];
            foreach (var edge in edges)
                if (Vector3.DistanceSquared(edge, middle) < Vector3.DistanceSquared(anchor, middle)) anchor = edge;
            areas.Add(new FrontierArea(key.Item1, key.Item2, key.Item3, anchor, edges.Count));
        }
    }

    // Whether any explored cell lies within the sphere; bounded by the sphere's columns.
    internal bool AnyExploredWithin(Vector3 centre, float radius)
    {
        if (!Finite(centre) || !float.IsFinite(radius) || radius <= 0 || _points.Count == 0) return false;
        int reach = (int)MathF.Ceiling(radius / CellSize);
        var middle = ColumnOf(centre);
        float limit = radius * radius;
        for (int dz = -reach; dz <= reach; dz++)
            for (int dx = -reach; dx <= reach; dx++)
            {
                if (!_columns.TryGetValue((middle.X + dx, middle.Z + dz), out var cells)) continue;
                foreach (int cell in cells)
                    if (Vector3.DistanceSquared(_points[cell], centre) <= limit) return true;
            }
        return false;
    }

    // The neighbour column is walkable on this floor, unexplored, reachable in a straight step, and opens
    // onto unseen ground rather than a pocket closed by explored ground or walls.
    private bool OpensUnexplored(int cell, int direction, IWalkability nav, ref int queries)
    {
        var point = _points[cell];
        var (x, z) = _cellColumns[cell];
        var (dx, dz) = Neighbours[direction];
        var column = (x + dx, z + dz);
        if (Find(column, point.Y) >= 0) return false;
        queries++;
        var centre = new Vector3(CenterOf(column.Item1), point.Y, CenterOf(column.Item2));
        if (!nav.TrySample(centre, out var next) || !Finite(next) || !Inside(column, next) ||
            MathF.Abs(next.Y - point.Y) >= LevelHeight || Find(column, next.Y) >= 0) return false;
        queries++;
        return nav.Clear(point, next) && UnseenRegion(column, next, nav, ref queries);
    }

    // Breadth-first over unexplored walkable columns until the region is large enough; pockets cannot
    // grow as exploration proceeds, so each verdict is kept.
    private bool UnseenRegion((int X, int Z) start, Vector3 point, IWalkability nav, ref int queries)
    {
        var key = (start.X, start.Z, FloorOf(point.Y));
        if (_regions.TryGetValue(key, out bool known)) return known;
        _regionQueue.Clear();
        _regionSeen.Clear();
        _regionQueue.Enqueue((start, point));
        _regionSeen.Add(start);
        int found = 0;
        while (_regionQueue.Count > 0 && found < FrontierRegionCells)
        {
            var (column, at) = _regionQueue.Dequeue();
            found++;
            foreach (var (dx, dz) in Sides)
            {
                var side = (column.X + dx, column.Z + dz);
                if (!_regionSeen.Add(side) || Find(side, at.Y) >= 0) continue;
                queries++;
                var centre = new Vector3(CenterOf(side.Item1), at.Y, CenterOf(side.Item2));
                if (!nav.TrySample(centre, out var next) || !Finite(next) || !Inside(side, next) ||
                    MathF.Abs(next.Y - at.Y) >= LevelHeight || Find(side, next.Y) >= 0) continue;
                queries++;
                if (nav.Clear(at, next)) _regionQueue.Enqueue((side, next));
            }
        }
        bool open = found >= FrontierRegionCells;
        _regions[key] = open;
        return open;
    }

    private int FloorOf(float height) => (int)MathF.Floor(height / LevelHeight);

    private static long Link(int cell, int direction) => ((long)cell << 3) | (uint)direction;

    private static int FloorDiv(int value, int divisor) => (int)MathF.Floor((float)value / divisor);
}
