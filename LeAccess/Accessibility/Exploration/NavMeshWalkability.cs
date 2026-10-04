using System;
using LeAccess.Accessibility.MapSections;
using UnityEngine.AI;
using NVector3 = System.Numerics.Vector3;
using UVector3 = UnityEngine.Vector3;

namespace LeAccess.Accessibility.Exploration;

// The local player's NavMesh through the primitive filter queries only; the filter-object
// overloads crash in this Unity build (reference/runtime-feedback.md).
internal sealed class NavMeshWalkability : IWalkability
{
    private long _session;
    private int _agentTypeId, _areaMask;
    private float _agentRadius, _floorTolerance;
    private bool _configured;

    internal float SampleRadius { get; private set; }
    internal float LevelHeight { get; private set; } = ExploredMap.DefaultLevelHeight;
    internal long UnverifiedFloors { get; private set; }

    // Uses the player's owned query agent (MapPlayerFloor, as the guidance probe does).
    internal bool TryConfigure(MapSectionWorldSnapshot world, float cellSize)
    {
        if (!MapPlayerFloor.TryGetQueryAgent(world, out var agent)) return _configured = false;
        int type = agent.agentTypeID, mask = agent.areaMask;
        float radius = agent.radius;
        if (mask == 0 || !float.IsFinite(radius) || radius <= 0) return _configured = false;
        if (_configured && _session == world.Session && _agentTypeId == type && _areaMask == mask && _agentRadius == radius)
            return true;
        var baked = NavMesh.GetSettingsByID(type);
        float bakedRadius = float.IsFinite(baked.agentRadius) && baked.agentRadius > radius ? baked.agentRadius : radius;
        float climb = float.IsFinite(baked.agentClimb) && baked.agentClimb > 0 ? baked.agentClimb : 0;
        // Walkable floors over one point are at least the baked agent height apart.
        LevelHeight = float.IsFinite(baked.agentHeight) && baked.agentHeight > 0 ? baked.agentHeight : ExploredMap.DefaultLevelHeight;
        _floorTolerance = LevelHeight / 2;
        // Reaches any walkable point of a cell from its centre, and the mesh edge eroded inside walls.
        SampleRadius = Math.Max(cellSize * MathF.Sqrt(2) / 2, bakedRadius + climb);
        _session = world.Session;
        _agentTypeId = type;
        _areaMask = mask;
        _agentRadius = radius;
        return _configured = true;
    }

    public bool TrySample(NVector3 point, out NVector3 onMesh)
    {
        onMesh = default;
        if (!_configured || !ExploredMap.Finite(point) ||
            !NavMesh.SamplePositionFilter(Unity(point), out var hit, SampleRadius, _agentTypeId, _areaMask)) return false;
        onMesh = new NVector3(hit.position.x, hit.position.y, hit.position.z);
        return ExploredMap.Finite(onMesh);
    }

    public bool Clear(NVector3 from, NVector3 to)
    {
        if (!_configured || !ExploredMap.Finite(from) || !ExploredMap.Finite(to)) return false;
        var end = Unity(to);
        if (NavMesh.RaycastFilter(Unity(from), end, out var hit, _agentTypeId, _areaMask)) return false;
        // An unblocked ray ends on the last polygon it crossed: another height there is another floor.
        var reached = hit.position;
        float x = reached.x - end.x, z = reached.z - end.z;
        if (float.IsFinite(reached.y) && float.IsFinite(x * x + z * z) && x * x + z * z <= SampleRadius * SampleRadius)
            return Math.Abs(reached.y - end.y) <= _floorTolerance;
        UnverifiedFloors++;
        return true;
    }

    internal void Reset()
    {
        _configured = false;
        _session = 0;
        UnverifiedFloors = 0;
    }

    private static UVector3 Unity(NVector3 point) => new(point.X, point.Y, point.Z);
}
