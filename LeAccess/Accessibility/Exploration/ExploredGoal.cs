using System;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// Where a route ends and when a target is at hand; reference/exploration-mode.md
// "Route-aware directions and reachable goals (2026-10-11, tester report)".
internal enum RouteGoalKind
{
    // A minimap room or frontier edge: reached on entering its radius.
    Area,
    // A target standing on the walkable mesh.
    OnMesh,
    // A target off the mesh (a door, a counter): its nearest walkable point within its range.
    Approach,
    // Nothing walkable within its range: the range alone decides.
    Unsampled
}

internal readonly record struct RouteGoal(Vector3 Point, RouteGoalKind Kind, bool OnMesh);

internal sealed partial class ExploredMap
{
    // A target is reached from an explored cell within its arrival radius or a clear hop this many cells long.
    private const int HopCells = 2;

    internal RouteGoal ResolveGoal(Vector3 target, float reach, bool area, IWalkability nav)
    {
        if (nav.TrySample(target, out var sampled) && Finite(sampled))
            return new RouteGoal(sampled, area ? RouteGoalKind.Area : RouteGoalKind.OnMesh, true);
        // Nearest in 3D, not on the map: a door sunk below its pocket is approached from that pocket, not from a
        // plateau a level above that is nearer on the map. Within the range, so standing on it is arrival.
        if (!area && nav.TrySampleWithin(target, reach, out sampled) && Finite(sampled))
            return new RouteGoal(sampled, RouteGoalKind.Approach, true);
        return new RouteGoal(target, area ? RouteGoalKind.Area : RouteGoalKind.Unsampled, false);
    }

    /// <summary>Where a quest marker stands on walkable ground: its own spot at the ground's height, else the nearest walkable point within sight.</summary>
    // reference/exploration-mode.md "Quest markers off the walkable ground (2026-10-21, tester log)".
    internal static Vector3 MarkerGround(Vector3 marker, float sight, IWalkability nav)
    {
        if (nav is null || !Finite(marker)) return marker;
        if (nav.TrySample(marker, out var ground) && Finite(ground)) return new Vector3(marker.X, ground.Y, marker.Z);
        return float.IsFinite(sight) && sight > 0 && nav.TrySampleWithin(marker, sight, out ground) && Finite(ground)
            ? ground : marker;
    }

    // A point target needs its goal's level and a clear walkable line; an area or an unsampled target its radius.
    private bool Reaches(Vector3 point, RouteGoal goal, float reach, IWalkability nav)
    {
        float horizontal = Horizontal(point, goal.Point);
        if (goal.OnMesh && MathF.Abs(point.Y - goal.Point.Y) >= LevelHeight) return false;
        if (goal.Kind is RouteGoalKind.Area or RouteGoalKind.Unsampled)
            return horizontal <= reach || goal.OnMesh && horizontal <= CellSize * HopCells && nav.Clear(point, goal.Point);
        return horizontal <= Math.Max(reach, CellSize * HopCells) && nav.Clear(point, goal.Point);
    }

    /// <summary>Whether a player within a target's range has it at hand: never across a wall or from another level.</summary>
    internal bool AtHand(Vector3 player, Vector3 target, float arrivalRadius, bool area, IWalkability nav, out string blocked)
    {
        blocked = null;
        if (!(Horizontal(player, target) <= arrivalRadius))
        {
            blocked = "range";
            return false;
        }
        if (area || nav is null) return true;
        var goal = ResolveGoal(target, arrivalRadius, false, nav);
        if (goal.Kind == RouteGoalKind.Unsampled) return true;
        if (!nav.TrySample(player, out var standing) || !Finite(standing)) blocked = "player off NavMesh";
        else if (MathF.Abs(standing.Y - goal.Point.Y) >= LevelHeight) blocked = "other level";
        else if (!nav.Clear(standing, goal.Point)) blocked = "wall";
        return blocked == null;
    }
}
