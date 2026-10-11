using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// Fills the explored map from the walkable NavMesh triangles, a bounded batch per step: the whole mesh (a town) or
// only the parts inside given spheres (visited minimap rooms).
internal sealed class NavMeshSeed
{
    // Work per beacon tick, like ExploredMap.DefaultQueriesPerObserve: triangles are cheap, native queries are not.
    internal const int TrianglesPerStep = 400;
    // With regions most triangles are rejected by a bounding-box test, so a step may look at this many times more.
    internal const int RegionScanFactor = 10;
    private const int VerticesPerTriangle = 3;
    private const int AreaBits = 32;
    private readonly List<Vector3> _batch = new();
    private readonly List<(Vector3 Centre, float Radius)> _regions = new();
    private Vector3[] _vertices = Array.Empty<Vector3>();
    private int[] _indices = Array.Empty<int>(), _areas = Array.Empty<int>();
    private int _areaMask, _next;

    internal bool Done => _next >= Triangles;
    internal int Triangles => _indices.Length / VerticesPerTriangle;
    internal bool HasMesh => Triangles > 0;

    internal void Begin(Vector3[] vertices, int[] indices, int[] areas, int areaMask)
    {
        _vertices = vertices ?? Array.Empty<Vector3>();
        _indices = indices ?? Array.Empty<int>();
        _areas = areas ?? Array.Empty<int>();
        _areaMask = areaMask;
        _regions.Clear();
        _next = 0;
    }

    /// <summary>Starts a new pass over the same mesh that keeps only ground inside these spheres.</summary>
    internal void Restart(IEnumerable<(Vector3 Centre, float Radius)> regions)
    {
        _regions.Clear();
        _regions.AddRange(regions);
        _next = 0;
    }

    // Returns the cells added.
    internal int Step(ExploredMap map)
    {
        _batch.Clear();
        int scan = _regions.Count == 0 ? TrianglesPerStep : TrianglesPerStep * RegionScanFactor;
        for (int examined = 0, rasterized = 0; _next < Triangles && examined < scan && rasterized < TrianglesPerStep;
            _next++, examined++)
        {
            if (!Walkable(_next) || !TryCorners(_next, out var a, out var b, out var c) || !Touches(a, b, c)) continue;
            Rasterize(map, a, b, c);
            rasterized++;
        }
        int added = map.Import(_batch);
        _batch.Clear();
        if (map.CellCount >= ExploredMap.MaxCells) _next = Triangles;
        return added;
    }

    private bool Walkable(int triangle)
    {
        if (triangle >= _areas.Length) return false;
        int area = _areas[triangle];
        return area >= 0 && area < AreaBits && (_areaMask >> area & 1) != 0;
    }

    private bool TryCorners(int triangle, out Vector3 a, out Vector3 b, out Vector3 c)
    {
        int first = triangle * VerticesPerTriangle;
        a = b = c = default;
        if (_indices[first] >= _vertices.Length || _indices[first + 1] >= _vertices.Length ||
            _indices[first + 2] >= _vertices.Length) return false;
        a = _vertices[_indices[first]];
        b = _vertices[_indices[first + 1]];
        c = _vertices[_indices[first + 2]];
        return true;
    }

    // The triangle's bounding box meets some region's bounding box.
    private bool Touches(Vector3 a, Vector3 b, Vector3 c)
    {
        if (_regions.Count == 0) return true;
        var low = Vector3.Min(a, Vector3.Min(b, c));
        var high = Vector3.Max(a, Vector3.Max(b, c));
        foreach (var (centre, radius) in _regions)
            if (low.X <= centre.X + radius && high.X >= centre.X - radius && low.Y <= centre.Y + radius &&
                high.Y >= centre.Y - radius && low.Z <= centre.Z + radius && high.Z >= centre.Z - radius) return true;
        return false;
    }

    private bool InRegion(Vector3 point)
    {
        if (_regions.Count == 0) return true;
        foreach (var (centre, radius) in _regions)
            if (Vector3.DistanceSquared(point, centre) <= radius * radius) return true;
        return false;
    }

    // One point per cell whose centre lies inside the triangle seen from above, at the triangle's height there.
    private void Rasterize(ExploredMap map, Vector3 a, Vector3 b, Vector3 c)
    {
        float denominator = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
        if (!float.IsFinite(denominator) || denominator == 0) return;
        int x0 = map.IndexOf(Math.Min(a.X, Math.Min(b.X, c.X))), x1 = map.IndexOf(Math.Max(a.X, Math.Max(b.X, c.X)));
        int z0 = map.IndexOf(Math.Min(a.Z, Math.Min(b.Z, c.Z))), z1 = map.IndexOf(Math.Max(a.Z, Math.Max(b.Z, c.Z)));
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                float px = map.CenterOf(x), pz = map.CenterOf(z);
                float u = ((b.Z - c.Z) * (px - c.X) + (c.X - b.X) * (pz - c.Z)) / denominator;
                float v = ((c.Z - a.Z) * (px - c.X) + (a.X - c.X) * (pz - c.Z)) / denominator;
                float w = 1 - u - v;
                if (u < 0 || v < 0 || w < 0) continue;
                var point = new Vector3(px, u * a.Y + v * b.Y + w * c.Y, pz);
                if (ExploredMap.Finite(point) && InRegion(point)) _batch.Add(point);
            }
    }
}
