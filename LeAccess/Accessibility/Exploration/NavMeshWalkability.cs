using System;
using System.Collections.Generic;
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
    private NavMeshPath _path;

    internal float SampleRadius { get; private set; }
    internal float LevelHeight { get; private set; } = ExploredMap.DefaultLevelHeight;
    internal long UnverifiedFloors { get; private set; }
    private long _pathsComplete, _pathsPartial, _pathsOffMesh, _pathsFailed;

    // Engine path queries this scene: complete, partial, refused for a corner off the player's mesh, failed.
    internal string PathCounts => string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"complete:{_pathsComplete},partial:{_pathsPartial},off_mesh:{_pathsOffMesh},failed:{_pathsFailed}");

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

    // Hands the scene's walkable triangles to a seed; the whole mesh is copied once.
    internal bool TryReadMesh(NavMeshSeed seed)
    {
        if (!_configured) return false;
        var mesh = NavMesh.CalculateTriangulation();
        var sourceVertices = mesh.vertices;
        var sourceIndices = mesh.indices;
        var sourceAreas = mesh.areas;
        if (sourceVertices is null || sourceVertices.WasCollected || sourceIndices is null || sourceIndices.WasCollected ||
            sourceAreas is null || sourceAreas.WasCollected) return false;
        var vertices = new NVector3[sourceVertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new NVector3(sourceVertices[i].x, sourceVertices[i].y, sourceVertices[i].z);
        var indices = new int[sourceIndices.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = sourceIndices[i];
        var areas = new int[sourceAreas.Length];
        for (int i = 0; i < areas.Length; i++) areas[i] = sourceAreas[i];
        seed.Begin(vertices, indices, areas, _areaMask);
        return true;
    }

    public bool TrySample(NVector3 point, out NVector3 onMesh) => TrySampleWithin(point, SampleRadius, out onMesh);

    public bool TrySampleWithin(NVector3 point, float radius, out NVector3 onMesh)
    {
        onMesh = default;
        if (!_configured || !ExploredMap.Finite(point) || !float.IsFinite(radius) || radius <= 0 ||
            !NavMesh.SamplePositionFilter(Unity(point), out var hit, radius, _agentTypeId, _areaMask)) return false;
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

    // The mask overload of CalculatePath names no agent type and the game registers two (Humanoid, Flying), so a path
    // counts only when every corner lies on the player's mesh.
    public PathReach FindPath(NVector3 from, NVector3 to, List<NVector3> corners)
    {
        corners.Clear();
        if (!_configured || !ExploredMap.Finite(from) || !ExploredMap.Finite(to)) return PathReach.None;
        _path ??= new NavMeshPath();
        _path.ClearCorners();
        var status = NavMesh.CalculatePath(Unity(from), Unity(to), _areaMask, _path) ? _path.status : NavMeshPathStatus.PathInvalid;
        var points = status == NavMeshPathStatus.PathInvalid ? null : _path.corners;
        if (points is not null && !points.WasCollected)
            foreach (var point in points) corners.Add(new NVector3(point.x, point.y, point.z));
        if (corners.Count < 2 || !corners.TrueForAll(ExploredMap.Finite))
        {
            _pathsFailed++;
            corners.Clear();
            return PathReach.None;
        }
        if (!corners.TrueForAll(corner => TrySample(corner, out _)))
        {
            _pathsOffMesh++;
            corners.Clear();
            return PathReach.None;
        }
        if (status == NavMeshPathStatus.PathComplete)
        {
            _pathsComplete++;
            return PathReach.Complete;
        }
        _pathsPartial++;
        return PathReach.Partial;
    }

    internal void Reset()
    {
        _configured = false;
        _session = 0;
        UnverifiedFloors = 0;
        _pathsComplete = _pathsPartial = _pathsOffMesh = _pathsFailed = 0;
    }

    private static UVector3 Unity(NVector3 point) => new(point.X, point.Y, point.Z);
}
