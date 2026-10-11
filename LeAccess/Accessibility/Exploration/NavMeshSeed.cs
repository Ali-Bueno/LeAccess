using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// Fills the explored map from the walkable NavMesh triangles, a bounded batch per step (a town counts as explored).
internal sealed class NavMeshSeed
{
    // Work per beacon tick, like ExploredMap.DefaultQueriesPerObserve: triangles are cheap, native queries are not.
    internal const int TrianglesPerStep = 400;
    private const int VerticesPerTriangle = 3;
    private const int AreaBits = 32;
    private readonly List<Vector3> _batch = new();
    private Vector3[] _vertices = Array.Empty<Vector3>();
    private int[] _indices = Array.Empty<int>(), _areas = Array.Empty<int>();
    private int _areaMask, _next;

    internal bool Done => _next >= Triangles;
    internal int Triangles => _indices.Length / VerticesPerTriangle;

    internal void Begin(Vector3[] vertices, int[] indices, int[] areas, int areaMask)
    {
        _vertices = vertices ?? Array.Empty<Vector3>();
        _indices = indices ?? Array.Empty<int>();
        _areas = areas ?? Array.Empty<int>();
        _areaMask = areaMask;
        _next = 0;
    }

    // Returns the cells added.
    internal int Step(ExploredMap map)
    {
        _batch.Clear();
        int end = Math.Min(Triangles, _next + TrianglesPerStep);
        for (; _next < end; _next++)
            if (Walkable(_next)) Rasterize(map, _next);
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

    // One point per cell whose centre lies inside the triangle seen from above, at the triangle's height there.
    private void Rasterize(ExploredMap map, int triangle)
    {
        int first = triangle * VerticesPerTriangle;
        if (_indices[first] >= _vertices.Length || _indices[first + 1] >= _vertices.Length ||
            _indices[first + 2] >= _vertices.Length) return;
        var a = _vertices[_indices[first]];
        var b = _vertices[_indices[first + 1]];
        var c = _vertices[_indices[first + 2]];
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
                if (ExploredMap.Finite(point)) _batch.Add(point);
            }
    }
}
