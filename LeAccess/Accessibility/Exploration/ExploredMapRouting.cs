using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// A* over explored cells along NavMesh-proved links, straightened near the player.
internal sealed partial class ExploredMap
{
    internal const int DefaultMaxExpansions = 6000;
    // A target is reached from an explored cell within its arrival radius or a clear hop this many cells long.
    private const int HopCells = 2;
    // Only the first stretch is straightened; it sets the bearing and, until the cadence saturates, the pace.
    private const int PullHorizon = 32;

    private readonly PriorityQueue<int, float> _open = new();
    private float[] _cost = Array.Empty<float>();
    private int[] _parent = Array.Empty<int>(), _seen = Array.Empty<int>(), _closed = Array.Empty<int>();
    private int _search;

    internal int MaxExpansions { get; set; } = DefaultMaxExpansions;
    internal int LastExpansions { get; private set; }
    internal string LastFailure { get; private set; }

    // Whether the straight line from the player to the target is walkable (the "straight" reach word).
    internal static bool StraightClear(Vector3 from, Vector3 target, IWalkability nav) =>
        nav is not null && nav.TrySample(from, out var start) && nav.TrySample(target, out var end) &&
        nav.Clear(start, end);

    internal bool TryRoute(Vector3 from, Vector3 target, float arrivalRadius, IWalkability nav,
        out IReadOnlyList<Vector3> path, out float length)
    {
        path = Array.Empty<Vector3>();
        length = 0;
        LastExpansions = 0;
        LastFailure = null;
        if (nav is null || !Finite(from) || !Finite(target)) return Fail("invalid");
        if (!nav.TrySample(from, out var start) || !Finite(start)) return Fail("player off NavMesh");
        // Objects can stand off the walkable mesh; then their own position is the goal.
        bool onMesh = nav.TrySample(target, out var goal) && Finite(goal);
        if (!onMesh) goal = target;
        float reach = float.IsFinite(arrivalRadius) && arrivalRadius > 0 ? arrivalRadius : 0;
        // Behind a counter the nearest explored cell centre can sit up to a cell from the walkable edge.
        if (!onMesh) reach = Math.Max(reach, CellSize);
        if (Reaches(start, goal, onMesh, reach, nav))
            return Finish(new List<Vector3> { start, goal }, onMesh, nav, out path, out length);

        Prepare();
        float slack = Math.Max(reach, CellSize * HopCells);
        var column = ColumnOf(start);
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (!_columns.TryGetValue((column.X + dx, column.Z + dz), out var cells)) continue;
                foreach (int cell in cells)
                    if (nav.Clear(start, _points[cell]))
                        Offer(cell, -1, Vector3.Distance(start, _points[cell]), goal, slack);
            }
        if (_open.Count == 0) return Fail("player cell unexplored");

        while (_open.TryDequeue(out int cell, out _))
        {
            if (_closed[cell] == _search) continue;
            _closed[cell] = _search;
            if (++LastExpansions > MaxExpansions) return Fail("expansion cap");
            var point = _points[cell];
            if (Reaches(point, goal, onMesh, reach, nav))
                return Finish(Trace(start, cell, goal), onMesh, nav, out path, out length);
            var (x, z) = _cellColumns[cell];
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0 || !_columns.TryGetValue((x + dx, z + dz), out var cells)) continue;
                    foreach (int next in cells)
                        if (_closed[next] != _search && Edge(cell, next, nav))
                            Offer(next, cell, _cost[cell] + Vector3.Distance(point, _points[next]), goal, slack);
                }
        }
        return Fail("no explored route");
    }

    private bool Reaches(Vector3 point, Vector3 goal, bool onMesh, float reach, IWalkability nav)
    {
        float horizontal = Horizontal(point, goal);
        if (onMesh && MathF.Abs(point.Y - goal.Y) >= LevelHeight) return false;
        return horizontal <= reach || onMesh && horizontal <= CellSize * HopCells && nav.Clear(point, goal);
    }

    // Horizontal distance less the goal's slack never overestimates, so the search stays optimal.
    private void Offer(int cell, int parent, float cost, Vector3 goal, float slack)
    {
        if (_seen[cell] == _search && cost >= _cost[cell]) return;
        _seen[cell] = _search;
        _cost[cell] = cost;
        _parent[cell] = parent;
        _open.Enqueue(cell, cost + Math.Max(0, Horizontal(_points[cell], goal) - slack));
    }

    private List<Vector3> Trace(Vector3 start, int cell, Vector3 goal)
    {
        var route = new List<Vector3> { goal };
        for (int index = cell; index >= 0; index = _parent[index]) route.Add(_points[index]);
        route.Add(start);
        route.Reverse();
        return route;
    }

    private static bool Finish(List<Vector3> raw, bool onMesh, IWalkability nav,
        out IReadOnlyList<Vector3> path, out float length)
    {
        var pulled = new List<Vector3>(raw.Count) { raw[0] };
        int last = raw.Count - 1, horizon = Math.Min(last, PullHorizon), anchor = 0;
        while (anchor < horizon)
        {
            int next = anchor + 1;
            while (next < horizon && (next + 1 < last || onMesh) && nav.Clear(raw[anchor], raw[next + 1])) next++;
            pulled.Add(raw[next]);
            anchor = next;
        }
        for (int index = anchor + 1; index <= last; index++) pulled.Add(raw[index]);
        length = 0;
        for (int index = 1; index < pulled.Count; index++) length += Vector3.Distance(pulled[index - 1], pulled[index]);
        path = pulled;
        return true;
    }

    private void Prepare()
    {
        if (_cost.Length < _points.Count)
        {
            int size = Math.Max(_points.Count, _cost.Length * 2);
            _cost = new float[size];
            _parent = new int[size];
            _seen = new int[size];
            _closed = new int[size];
            _search = 0;
        }
        if (_search == int.MaxValue)
        {
            Array.Clear(_seen, 0, _seen.Length);
            Array.Clear(_closed, 0, _closed.Length);
            _search = 0;
        }
        _search++;
        _open.Clear();
    }

    private bool Fail(string reason)
    {
        LastFailure = reason;
        return false;
    }
}
