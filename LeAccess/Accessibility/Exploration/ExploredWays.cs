using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// An opening of the explored map (an unexplored room or a frontier edge), entered on reaching its radius.
internal readonly record struct ExploredOpening(Vector3 Position, float Radius);

// The explored route to an opening and the straight distance from it to the target; guided when the engine's path
// to the target leaves explored ground there.
internal readonly record struct ExploredWay(int Opening, IReadOnlyList<Vector3> Path, float Length, float Beyond, bool Guided = false)
{
    internal float Estimate => ExploredMap.WayEstimate(Length, Beyond);
}

// Ways toward a target beyond explored ground, ranked as a sighted player reads the minimap;
// reference/exploration-mode.md "Ways toward a target beyond explored ground".
internal sealed partial class ExploredMap
{
    private const int FromStart = -1;
    // Navigation tuning, not a game constant: unseen ground costs this many times its straight line, so a way may
    // walk up to three times what it gains toward the target before one nearer the player, even behind, wins.
    internal const float BeyondWeight = 3f;

    // How a way ranks: a sighted player heads for the edge of the fog nearest the marker, not the nearest edge.
    internal static float WayEstimate(float length, float beyond) => length + BeyondWeight * beyond;

    // Seconds a plan of the explore radius takes to drain at this observe cadence: the time to take in new ground.
    internal double PlanSeconds(double observeSeconds) =>
        MathF.PI * _exploreRadius * _exploreRadius / (CellSize * CellSize) / Math.Max(1, QueriesPerObserve) * observeSeconds;

    // One search for every opening: a cell that reaches an opening may finish there at its walk so far, the step
    // into the opening and the weighted straight line on to the target. Finishes leave the queue in order of that estimate.
    internal int FindWays(Vector3 from, Vector3 target, IReadOnlyList<ExploredOpening> openings, int limit,
        IWalkability nav, List<ExploredWay> ways)
    {
        ways.Clear();
        LastExpansions = 0;
        LastFailure = null;
        if (nav is null || openings is null || limit <= 0 || !Finite(from) || !Finite(target))
        {
            Fail("invalid");
            return 0;
        }
        if (!nav.TrySample(from, out var start) || !Finite(start))
        {
            Fail("player off NavMesh");
            return 0;
        }

        int count = openings.Count;
        var goals = new RouteGoal[count];
        var reach = new float[count];
        var estimate = new float[count];
        var beyond = new float[count];
        var via = new int[count];
        var done = new bool[count];
        for (int index = 0; index < count; index++)
        {
            estimate[index] = beyond[index] = float.PositiveInfinity;
            var opening = openings[index];
            if (!Finite(opening.Position) || !(opening.Radius > 0) || !float.IsFinite(opening.Radius))
            {
                done[index] = true;
                continue;
            }
            goals[index] = ResolveGoal(opening.Position, opening.Radius, true, nav);
            reach[index] = goals[index].OnMesh ? opening.Radius : Math.Max(opening.Radius, CellSize);
            beyond[index] = BeyondWeight * Horizontal(goals[index].Point, target);
        }

        Prepare();
        Finishes(FromStart, start, 0);
        var column = ColumnOf(start);
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (!_columns.TryGetValue((column.X + dx, column.Z + dz), out var cells)) continue;
                foreach (int cell in cells)
                    if (nav.Clear(start, _points[cell]))
                        Offer(cell, -1, Vector3.Distance(start, _points[cell]), Bound(cell));
            }
        if (_open.Count == 0)
        {
            Fail("player cell unexplored");
            return 0;
        }

        while (ways.Count < limit && _open.TryDequeue(out int node, out _))
        {
            if (node < 0)
            {
                int opening = ~node;
                if (done[opening]) continue;
                done[opening] = true;
                var goal = goals[opening].Point;
                var raw = via[opening] == FromStart ? new List<Vector3> { start, goal } : Trace(start, via[opening], goal);
                Finish(raw, goals[opening].OnMesh, nav, out var path, out float length);
                ways.Add(new ExploredWay(opening, path, length, Horizontal(goal, target)));
                continue;
            }
            if (_closed[node] == _search) continue;
            _closed[node] = _search;
            if (++LastExpansions > MaxExpansions)
            {
                Fail("expansion cap");
                break;
            }
            var point = _points[node];
            Finishes(node, point, _cost[node]);
            var (x, z) = _cellColumns[node];
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0 || !_columns.TryGetValue((x + dx, z + dz), out var cells)) continue;
                    foreach (int next in cells)
                        if (_closed[next] != _search && Edge(node, next, nav))
                            Offer(next, node, _cost[node] + Vector3.Distance(point, _points[next]), Bound(next));
                }
        }
        if (ways.Count < limit && LastFailure == null)
        {
            // Openings the grid's links did not reach, most promising first, by the engine's path over explored ground.
            var rest = new List<int>();
            for (int index = 0; index < count; index++)
                if (!done[index] && float.IsFinite(beyond[index])) rest.Add(index);
            rest.Sort((a, b) => Promise(a).CompareTo(Promise(b)));
            for (int tried = 0; tried < rest.Count && tried < limit; tried++)
            {
                int index = rest[tried];
                if (!TryMeshRoute(start, goals[index], reach[index], nav, out var raw)) continue;
                Finish(raw, goals[index].OnMesh, nav, out var path, out float length);
                ways.Add(new ExploredWay(index, path, length, Horizontal(goals[index].Point, target)));
            }
        }
        int guide = GuideOpening(start, target, openings, nav);
        if (guide >= 0)
        {
            int at = ways.FindIndex(way => way.Opening == guide);
            if (at < 0 && TryMeshRoute(start, goals[guide], reach[guide], nav, out var raw))
            {
                Finish(raw, goals[guide].OnMesh, nav, out var path, out float length);
                ways.Add(new ExploredWay(guide, path, length, Horizontal(goals[guide].Point, target)));
                at = ways.Count - 1;
            }
            if (at >= 0) ways[at] = ways[at] with { Guided = true };
        }
        if (ways.Count == 0 && LastFailure == null) Fail("no opening reached");
        // Straightening shortens each walk a little, so the spoken order follows the straightened estimates.
        ways.Sort((a, b) => a.Guided != b.Guided ? (a.Guided ? -1 : 1) : a.Estimate.CompareTo(b.Estimate));
        if (ways.Count > limit) ways.RemoveRange(limit, ways.Count - limit);
        return ways.Count;

        float Promise(int index) => Horizontal(start, goals[index].Point) + beyond[index];

        // No finish through a cell costs less than its straight line to an opening plus that opening's way on; the
        // bound changes by at most a step's length, so cells are expanded once and finishes leave in order.
        float Bound(int cell)
        {
            if (_seen[cell] == _search) return _bound[cell];
            float best = float.PositiveInfinity;
            for (int index = 0; index < count; index++)
                if (float.IsFinite(beyond[index])) best = Math.Min(best, Horizontal(_points[cell], goals[index].Point) + beyond[index]);
            return _bound[cell] = best;
        }

        void Finishes(int cell, Vector3 point, float cost)
        {
            for (int index = 0; index < count; index++)
            {
                if (done[index] || !Reaches(point, goals[index], reach[index], nav)) continue;
                var goal = goals[index].Point;
                float total = cost + Vector3.Distance(point, goal) + beyond[index];
                if (!(total < estimate[index])) continue;
                estimate[index] = total;
                via[index] = cell;
                _open.Enqueue(~index, total);
            }
        }
    }
}
