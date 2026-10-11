using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// Walkable-ground queries: Unity's NavMesh in the game, a tile grid in tests.
internal interface IWalkability
{
    bool TrySample(Vector3 point, out Vector3 onMesh);
    bool Clear(Vector3 from, Vector3 to);
}

// Ground the player has seen in walkable line of sight: XZ cells, one per floor in each column.
// The beacon routes only through it (D2R "helps you return, not discover"); see reference/exploration-mode.md.
internal sealed partial class ExploredMap
{
    // Navigation tuning, not game constants: a cell is about two body widths, the radius four cells.
    internal const float DefaultCellSize = 2f;
    internal const float DefaultLevelHeight = 2f;
    internal const float DefaultExploreRadius = 20f;
    internal const int DefaultQueriesPerObserve = 12;
    internal const int MaxCells = 100_000;
    private const int MaxCachedEdges = MaxCells * 8;

    private readonly List<Vector3> _points = new();
    private readonly List<(int X, int Z)> _cellColumns = new();
    private readonly Dictionary<(int X, int Z), List<int>> _columns = new();
    private readonly Dictionary<long, bool> _edges = new();
    private readonly List<(int X, int Z)> _pending = new();
    private int _pendingNext;
    private bool _planned;
    private (int X, int Z) _planColumn;
    private Vector3 _planOrigin;
    private float _exploreRadius = DefaultExploreRadius;

    internal ExploredMap(float cellSize = DefaultCellSize, float levelHeight = DefaultLevelHeight)
    {
        CellSize = float.IsFinite(cellSize) && cellSize > 0 ? cellSize : DefaultCellSize;
        LevelHeight = float.IsFinite(levelHeight) && levelHeight > 0 ? levelHeight : DefaultLevelHeight;
    }

    internal float CellSize { get; }
    // Two walkable floors over one point are at least an agent height apart.
    internal float LevelHeight { get; }
    internal int QueriesPerObserve { get; set; } = DefaultQueriesPerObserve;
    internal int CellCount => _points.Count;
    internal long EdgeChecks { get; private set; }

    internal float ExploreRadius
    {
        get => _exploreRadius;
        set { if (float.IsFinite(value) && value > 0) _exploreRadius = value; }
    }

    // Marks unexplored cells within the radius that the player sees in a straight walkable line.
    // Work per call is bounded: the cell list is planned once per cell entered, then drained a few at a time.
    internal int Observe(Vector3 player, IWalkability nav)
    {
        if (nav is null || !Finite(player) || !nav.TrySample(player, out var origin) || !Finite(origin)) return 0;
        int added = 0;
        var column = ColumnOf(origin);
        if (Find(column, origin.Y) < 0 && TryAdd(column, origin, nav)) added++;
        if (!_planned || column != _planColumn || MathF.Abs(origin.Y - _planOrigin.Y) >= LevelHeight)
            Plan(column, origin, nav);
        for (int queries = 0; _pendingNext < _pending.Count && queries < QueriesPerObserve; queries++)
        {
            var next = _pending[_pendingNext++];
            var center = new Vector3(CenterOf(next.X), _planOrigin.Y, CenterOf(next.Z));
            if (!nav.TrySample(center, out var point) || !Finite(point) || !Inside(next, point) ||
                Find(next, point.Y) >= 0 || !nav.Clear(_planOrigin, point)) continue;
            if (TryAdd(next, point, nav)) added++;
        }
        return added;
    }

    internal IReadOnlyList<Vector3> Points => _points;

    // Restores saved cells without NavMesh queries; their links are proven lazily when a route needs them.
    internal int Import(IEnumerable<Vector3> points)
    {
        int added = 0;
        if (points is null) return 0;
        foreach (var point in points)
        {
            if (_points.Count >= MaxCells) break;
            var column = ColumnOf(point);
            if (!Finite(point) || Find(column, point.Y) >= 0) continue;
            AddCell(column, point);
            added++;
        }
        return added;
    }

    internal bool Explored(Vector3 point) => Finite(point) && Find(ColumnOf(point), point.Y) >= 0;

    private void Plan((int X, int Z) column, Vector3 origin, IWalkability nav)
    {
        _planned = true;
        _planColumn = column;
        _planOrigin = origin;
        _pending.Clear();
        _pendingNext = 0;
        int reach = (int)MathF.Ceiling(_exploreRadius / CellSize);
        float limit = _exploreRadius * _exploreRadius;
        for (int dz = -reach; dz <= reach; dz++)
            for (int dx = -reach; dx <= reach; dx++)
            {
                var candidate = (column.X + dx, column.Z + dz);
                float x = CenterOf(candidate.Item1) - origin.X, z = CenterOf(candidate.Item2) - origin.Z;
                if (x * x + z * z <= limit && Find(candidate, origin.Y) < 0) _pending.Add(candidate);
            }
        _pending.Sort((a, b) => PlanDistance(a).CompareTo(PlanDistance(b)));
        // Doors open and close: the player's own cell re-proves its links whenever a cell is entered.
        int own = Find(column, origin.Y);
        if (own >= 0) Link(own, nav, refresh: true);
    }

    private float PlanDistance((int X, int Z) column)
    {
        float x = CenterOf(column.X) - _planOrigin.X, z = CenterOf(column.Z) - _planOrigin.Z;
        return x * x + z * z;
    }

    private bool TryAdd((int X, int Z) column, Vector3 point, IWalkability nav)
    {
        if (_points.Count >= MaxCells) return false;
        int index = AddCell(column, point);
        // Proving the new cell's links now keeps later route searches free of native queries.
        Link(index, nav, refresh: false);
        return true;
    }

    private int AddCell((int X, int Z) column, Vector3 point)
    {
        int index = _points.Count;
        _points.Add(point);
        _cellColumns.Add(column);
        if (!_columns.TryGetValue(column, out var cells)) _columns[column] = cells = new List<int>(1);
        cells.Add(index);
        TrackFrontier(index);
        return index;
    }

    private void Link(int cell, IWalkability nav, bool refresh)
    {
        var (x, z) = _cellColumns[cell];
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if ((dx != 0 || dz != 0) && _columns.TryGetValue((x + dx, z + dz), out var cells))
                    foreach (int other in cells) Edge(cell, other, nav, refresh);
            }
    }

    private bool Edge(int a, int b, IWalkability nav, bool refresh = false)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (!refresh && _edges.TryGetValue(key, out bool known)) return known;
        bool clear = nav.Clear(_points[a], _points[b]);
        EdgeChecks++;
        if (_edges.Count >= MaxCachedEdges && !_edges.ContainsKey(key)) _edges.Clear();
        _edges[key] = clear;
        return clear;
    }

    // The cell of this column on the same floor as height y, nearest first; -1 when none.
    private int Find((int X, int Z) column, float y)
    {
        if (!_columns.TryGetValue(column, out var cells)) return -1;
        int best = -1;
        float bestGap = LevelHeight;
        foreach (int cell in cells)
        {
            float gap = MathF.Abs(_points[cell].Y - y);
            if (gap < bestGap) { bestGap = gap; best = cell; }
        }
        return best;
    }

    private (int X, int Z) ColumnOf(Vector3 point) =>
        (IndexOf(point.X), IndexOf(point.Z));

    internal float CenterOf(int index) => (index + 0.5f) * CellSize;

    internal int IndexOf(float coordinate) => (int)MathF.Floor(coordinate / CellSize);

    private bool Inside((int X, int Z) column, Vector3 point) =>
        MathF.Abs(point.X - CenterOf(column.X)) <= CellSize / 2 && MathF.Abs(point.Z - CenterOf(column.Z)) <= CellSize / 2;

    internal static bool Finite(Vector3 point) =>
        float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z);

    internal static float Horizontal(Vector3 a, Vector3 b)
    {
        float x = a.X - b.X, z = a.Z - b.Z;
        return MathF.Sqrt(x * x + z * z);
    }
}
