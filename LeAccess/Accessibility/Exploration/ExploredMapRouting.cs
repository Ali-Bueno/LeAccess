using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// A* over explored cells along NavMesh-proved links, straightened near the player.
internal sealed partial class ExploredMap
{
    internal const int DefaultMaxExpansions = 6000;
    // Only the first stretch is straightened; it sets the bearing and, until the cadence saturates, the pace.
    private const int PullHorizon = 32;

    private readonly PriorityQueue<int, float> _open = new();
    private float[] _cost = Array.Empty<float>();
    private int[] _parent = Array.Empty<int>(), _seen = Array.Empty<int>(), _closed = Array.Empty<int>();
    private int _search;

    internal int MaxExpansions { get; set; } = DefaultMaxExpansions;
    internal int LastExpansions { get; private set; }
    internal string LastFailure { get; private set; }
    internal RouteGoal LastGoal { get; private set; }

    // Whether the straight line from the player to the target is walkable (the "straight" reach word).
    internal static bool StraightClear(Vector3 from, Vector3 target, IWalkability nav) =>
        nav is not null && nav.TrySample(from, out var start) && nav.TrySample(target, out var end) &&
        nav.Clear(start, end);

    // An area (a minimap room, a frontier edge) is reached on entering its radius; a point target where it is at hand.
    internal bool TryRoute(Vector3 from, Vector3 target, float arrivalRadius, IWalkability nav,
        out IReadOnlyList<Vector3> path, out float length, bool area = false)
    {
        path = Array.Empty<Vector3>();
        length = 0;
        LastExpansions = 0;
        LastFailure = null;
        LastGoal = default;
        if (nav is null || !Finite(from) || !Finite(target)) return Fail("invalid");
        if (!nav.TrySample(from, out var start) || !Finite(start)) return Fail("player off NavMesh");
        float reach = float.IsFinite(arrivalRadius) && arrivalRadius > 0 ? arrivalRadius : 0;
        var goal = LastGoal = ResolveGoal(target, reach, area, nav);
        // Behind a counter the nearest explored cell centre can sit up to a cell from the walkable edge.
        if (!goal.OnMesh) reach = Math.Max(reach, CellSize);
        if (Reaches(start, goal, reach, nav))
            return Finish(new List<Vector3> { start, goal.Point }, goal.OnMesh, nav, out path, out length);

        Prepare();
        float slack = Math.Max(reach, CellSize * HopCells);
        var column = ColumnOf(start);
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (!_columns.TryGetValue((column.X + dx, column.Z + dz), out var cells)) continue;
                foreach (int cell in cells)
                    if (nav.Clear(start, _points[cell]))
                        Offer(cell, -1, Vector3.Distance(start, _points[cell]), goal.Point, slack);
            }
        if (_open.Count == 0) return Fail("player cell unexplored");

        while (_open.TryDequeue(out int cell, out _))
        {
            if (_closed[cell] == _search) continue;
            _closed[cell] = _search;
            if (++LastExpansions > MaxExpansions) return Fail("expansion cap");
            var point = _points[cell];
            if (Reaches(point, goal, reach, nav))
                return Finish(Trace(start, cell, goal.Point), goal.OnMesh, nav, out path, out length);
            var (x, z) = _cellColumns[cell];
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0 || !_columns.TryGetValue((x + dx, z + dz), out var cells)) continue;
                    foreach (int next in cells)
                        if (_closed[next] != _search && Edge(cell, next, nav))
                            Offer(next, cell, _cost[cell] + Vector3.Distance(point, _points[next]), goal.Point, slack);
                }
        }
        return Fail("no explored route");
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
        var pulled = Pull(raw, Math.Min(raw.Count - 1, PullHorizon), onMesh, nav);
        length = 0;
        for (int index = 1; index < pulled.Count; index++) length += Vector3.Distance(pulled[index - 1], pulled[index]);
        path = pulled;
        return true;
    }

    /// <summary>A route's turning points: all of it straightened, a stretch shorter than a cell folded into the next.</summary>
    internal IReadOnlyList<Vector3> Corners(IReadOnlyList<Vector3> path, IWalkability nav)
    {
        if (path is null || path.Count < 3 || nav is null) return path;
        // The goal may lie off the mesh, so the last stretch is kept, as the route's own straightening does.
        var pulled = Pull(path, path.Count - 1, false, nav);
        var corners = new List<Vector3>(pulled.Count) { pulled[0] };
        for (int index = 1; index < pulled.Count - 1; index++)
            if (Horizontal(corners[^1], pulled[index]) >= CellSize) corners.Add(pulled[index]);
        corners.Add(pulled[^1]);
        return corners;
    }

    // Each point jumps to the farthest one ahead in a clear line, up to the horizon; past it the points stay.
    private static List<Vector3> Pull(IReadOnlyList<Vector3> raw, int horizon, bool onMesh, IWalkability nav)
    {
        var pulled = new List<Vector3>(raw.Count) { raw[0] };
        int last = raw.Count - 1, anchor = 0;
        while (anchor < horizon)
        {
            int next = anchor + 1;
            while (next < horizon && (next + 1 < last || onMesh) && nav.Clear(raw[anchor], raw[next + 1])) next++;
            pulled.Add(raw[next]);
            anchor = next;
        }
        for (int index = anchor + 1; index <= last; index++) pulled.Add(raw[index]);
        return pulled;
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
