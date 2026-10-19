using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// The engine's own path, taken where it runs over explored ground: the grid's straight NavMesh checks cannot cross
// some links the player walks; reference/exploration-mode.md "Explored ground the grid could not link".
internal sealed partial class ExploredMap
{
    private readonly List<Vector3> _meshCorners = new();

    internal long MeshRoutes { get; private set; }
    internal bool LastByMesh { get; private set; }
    // How the last ways search was guided: an opening's index, or none, explored or unlisted.
    internal string LastGuide { get; private set; } = "none";

    // The opening nearest where the engine's path to the target first leaves explored ground (user decision
    // 2026-10-19): the beacon still leads only there, past no dead end. -1 when there is none.
    private int GuideOpening(Vector3 start, Vector3 target, IReadOnlyList<ExploredOpening> openings, IWalkability nav)
    {
        LastGuide = "none";
        // A marker off the walkable ground is approached from the nearest point a player could see it from.
        if (!nav.TrySampleWithin(target, ExploreRadius, out var end) || !nav.TryPath(start, end, _meshCorners)) return -1;
        if (!TryLeaving(out var leaving))
        {
            LastGuide = "explored";
            return -1;
        }
        int best = -1;
        float nearest = float.PositiveInfinity;
        for (int index = 0; index < openings.Count; index++)
        {
            float distance = Horizontal(openings[index].Position, leaving);
            if (distance <= Math.Max(openings[index].Radius, FrontierAreaCells * CellSize) && distance < nearest)
            {
                best = index;
                nearest = distance;
            }
        }
        LastGuide = best >= 0 ? best.ToString(System.Globalization.CultureInfo.InvariantCulture) : "unlisted";
        return best;
    }

    private bool TryLeaving(out Vector3 leaving)
    {
        for (int index = 1; index < _meshCorners.Count; index++)
        {
            Vector3 from = _meshCorners[index - 1], to = _meshCorners[index];
            int samples = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(from, to) / (CellSize / 2)));
            for (int sample = 1; sample <= samples; sample++)
            {
                leaving = Vector3.Lerp(from, to, (float)sample / samples);
                if (!OnExplored(leaving)) return true;
            }
        }
        leaving = default;
        return false;
    }

    private bool RouteByMesh(Vector3 start, RouteGoal goal, float reach, IWalkability nav,
        out IReadOnlyList<Vector3> path, out float length)
    {
        path = Array.Empty<Vector3>();
        length = 0;
        if (!TryMeshRoute(start, goal, reach, nav, out var route)) return false;
        LastByMesh = true;
        LastFailure = null;
        return Finish(route, goal.OnMesh, nav, out path, out length);
    }

    // The engine's path to the goal while every step of it lies on explored ground, cut where it enters an area;
    // the last stretch to a point target is free within the reach the grid's own routes end at.
    private bool TryMeshRoute(Vector3 start, RouteGoal goal, float reach, IWalkability nav, out List<Vector3> route)
    {
        route = null;
        var end = goal.Point;
        if (!goal.OnMesh && !TryNearestExplored(goal.Point, reach, out end)) return false;
        if (!nav.TryPath(start, end, _meshCorners) || _meshCorners.Count < 2 || !OnExplored(_meshCorners[0])) return false;
        bool area = goal.Kind == RouteGoalKind.Area;
        float free = area ? reach : Math.Max(reach, CellSize * HopCells);
        var steps = new List<Vector3>(_meshCorners.Count + 1) { _meshCorners[0] };
        for (int index = 1; index < _meshCorners.Count; index++)
        {
            Vector3 from = _meshCorners[index - 1], to = _meshCorners[index];
            int samples = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(from, to) / (CellSize / 2)));
            for (int sample = 1; sample <= samples; sample++)
            {
                var point = Vector3.Lerp(from, to, (float)sample / samples);
                if (Horizontal(point, goal.Point) <= free && (!goal.OnMesh || MathF.Abs(point.Y - goal.Point.Y) < LevelHeight))
                {
                    if (area) steps.Add(point);
                    else steps.AddRange(_meshCorners.GetRange(index, _meshCorners.Count - index));
                    return Accept(steps, goal, out route);
                }
                if (!OnExplored(point)) return false;
            }
            steps.Add(to);
        }
        return Accept(steps, goal, out route);
    }

    private bool Accept(List<Vector3> steps, RouteGoal goal, out List<Vector3> route)
    {
        if (!goal.OnMesh) steps.Add(goal.Point);
        route = steps;
        MeshRoutes++;
        return true;
    }

    // A cell on this floor in the point's column or a neighbouring one: seeded maps leave the columns along walls empty.
    private bool OnExplored(Vector3 point)
    {
        var (x, z) = ColumnOf(point);
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
                if (Find((x + dx, z + dz), point.Y) >= 0) return true;
        return false;
    }

    private bool TryNearestExplored(Vector3 centre, float radius, out Vector3 nearest)
    {
        nearest = default;
        float best = radius;
        bool found = false;
        int reach = (int)MathF.Ceiling(radius / CellSize);
        var middle = ColumnOf(centre);
        for (int dz = -reach; dz <= reach; dz++)
            for (int dx = -reach; dx <= reach; dx++)
            {
                if (!_columns.TryGetValue((middle.X + dx, middle.Z + dz), out var cells)) continue;
                foreach (int cell in cells)
                {
                    float distance = Horizontal(_points[cell], centre);
                    if (distance > best) continue;
                    best = distance;
                    nearest = _points[cell];
                    found = true;
                }
            }
        return found;
    }
}
