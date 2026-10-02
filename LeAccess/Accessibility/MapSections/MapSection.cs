using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.MapSections;

internal readonly record struct MapPoint(double X, double Z);

internal sealed class MapSectionSource
{
    internal string Path { get; }
    internal IReadOnlyList<double> Matrix { get; }
    internal double Tolerance { get; }
    internal IReadOnlyList<MapSectionCollider> Colliders { get; }
    internal MapSectionSource(string path, double[] matrix, double tolerance, MapSectionCollider[] colliders = null)
    {
        if (string.IsNullOrWhiteSpace(path) || Array.Exists(path.Split('/'), string.IsNullOrWhiteSpace))
            throw new FormatException("Section source path must have nonempty segments.");
        if (matrix == null || matrix.Length != 16 || Array.Exists(matrix, value => !double.IsFinite(value)))
            throw new FormatException("Section source matrix needs sixteen finite row-major values.");
        if (!double.IsFinite(tolerance) || tolerance <= 0)
            throw new FormatException("Section source tolerance must be finite and positive.");
        Path = path; Matrix = Array.AsReadOnly((double[])matrix.Clone()); Tolerance = tolerance;
        var colliderSnapshot = (MapSectionCollider[])(colliders ?? Array.Empty<MapSectionCollider>()).Clone();
        var colliderPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var collider in colliderSnapshot)
            if (collider == null || !colliderPaths.Add(collider.Transform.Path))
                throw new FormatException("Source colliders must have unique transform paths.");
        Colliders = Array.AsReadOnly(colliderSnapshot);
    }
}

internal sealed class MapSectionCollider
{
    internal MapSectionSource Transform { get; }
    internal IReadOnlyList<double> Center { get; }
    internal IReadOnlyList<double> Size { get; }
    internal MapSectionCollider(MapSectionSource transform, double[] center, double[] size)
    {
        if (transform == null || transform.Colliders.Count != 0)
            throw new FormatException("Collider transform must not contain nested colliders.");
        if (center == null || size == null || center.Length != 3 || size.Length != 3 ||
            Array.Exists(center, value => !double.IsFinite(value)) ||
            Array.Exists(size, value => !double.IsFinite(value) || value == 0))
            throw new FormatException("Collider center/size need three finite values and nonzero size.");
        Transform = transform; Center = Array.AsReadOnly((double[])center.Clone()); Size = Array.AsReadOnly((double[])size.Clone());
    }
}

internal sealed class MapSection
{
    private readonly MapSectionComponent[] _components;
    internal string Id { get; }
    internal string TextKey { get; }
    internal string KindKey => TextKey.StartsWith("map.section.oldroad_bridge_", StringComparison.Ordinal)
        ? "map.section.bridge" : TextKey;
    internal string SpeechGroup { get; }
    internal MapSectionSource Source { get; }
    internal int Priority { get; }
    internal double Area { get; }

    internal MapSection(string id, string textKey, int priority, double minY, double maxY, MapPoint[] polygon, string speechGroup = null, MapSectionSource source = null)
        : this(id, textKey, priority, new[] { new MapSectionComponent(minY, maxY, polygon) }, speechGroup, source) { }

    internal MapSection(string id, string textKey, int priority, MapSectionComponent[] components, string speechGroup = null, MapSectionSource source = null)
    {
        if (components.Length == 0) throw new FormatException("A section needs at least one polygon component.");
        Id = id; TextKey = textKey; Priority = priority;
        SpeechGroup = speechGroup ?? id;
        Source = source;
        if (string.IsNullOrWhiteSpace(SpeechGroup)) throw new FormatException("Speech group must be nonblank.");
        _components = (MapSectionComponent[])components.Clone();
        foreach (var component in _components) Area += component.Area;
    }

    internal bool Contains(double x, double y, double z, double margin = 0)
    {
        if (!double.IsFinite(margin) || margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));
        foreach (var component in _components)
            if (component.Contains(x, y, z, margin)) return true;
        return false;
    }

    internal bool TryGetContainingArea(double x, double y, double z, out double area)
    {
        area = double.PositiveInfinity;
        foreach (var component in _components)
            if (component.Contains(x, y, z) && component.Area < area) area = component.Area;
        return double.IsFinite(area);
    }
}

internal sealed class MapSectionComponent
{
    private readonly MapPoint[] _polygon;
    private readonly double _minY, _maxY, _minX, _maxX, _minZ, _maxZ;
    internal double Area { get; }

    internal MapSectionComponent(double minY, double maxY, MapPoint[] polygon)
    {
        if (minY > maxY) throw new FormatException("Map section minY exceeds maxY.");
        if (polygon.Length < 3 || new HashSet<MapPoint>(polygon).Count != polygon.Length)
            throw new FormatException("A polygon needs at least three distinct points, without duplicate vertices.");
        double area = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Length];
            area += a.X * b.Z - b.X * a.Z;
            var previous = polygon[(i + polygon.Length - 1) % polygon.Length];
            if (Cross(previous, a, b) == 0 &&
                (previous.X - a.X) * (b.X - a.X) + (previous.Z - a.Z) * (b.Z - a.Z) > 0)
                throw new FormatException("Adjacent polygon edges must not overlap.");
            for (int j = i + 1; j < polygon.Length; j++)
            {
                if (j == i + 1 || i == 0 && j == polygon.Length - 1) continue;
                if (Intersects(a, b, polygon[j], polygon[(j + 1) % polygon.Length]))
                    throw new FormatException("Polygon edges must not intersect.");
            }
        }
        Area = Math.Abs(area) / 2;
        if (!double.IsFinite(Area) || Area == 0) throw new FormatException("Polygon area must be finite and nonzero.");
        _minX = _maxX = polygon[0].X; _minZ = _maxZ = polygon[0].Z;
        foreach (var point in polygon)
        {
            _minX = Math.Min(_minX, point.X); _maxX = Math.Max(_maxX, point.X);
            _minZ = Math.Min(_minZ, point.Z); _maxZ = Math.Max(_maxZ, point.Z);
        }
        _minY = minY; _maxY = maxY; _polygon = (MapPoint[])polygon.Clone();
    }

    internal bool Contains(double x, double y, double z, double margin = 0)
    {
        if (!double.IsFinite(margin) || margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z) ||
            y < _minY - margin || y > _maxY + margin || x < _minX - margin || x > _maxX + margin ||
            z < _minZ - margin || z > _maxZ + margin) return false;
        var point = new MapPoint(x, z);
        bool inside = false;
        for (int i = 0; i < _polygon.Length; i++)
        {
            var a = _polygon[i]; var b = _polygon[(i + 1) % _polygon.Length];
            if (OnSegment(a, b, point) || margin > 0 && EdgeDistanceSquared(a, b, point) <= margin * margin) return true;
            if ((a.Z > z) != (b.Z > z) && x < (b.X - a.X) * (z - a.Z) / (b.Z - a.Z) + a.X)
                inside = !inside;
        }
        return inside;
    }

    private static double Cross(MapPoint a, MapPoint b, MapPoint c) =>
        (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);

    private static bool OnSegment(MapPoint a, MapPoint b, MapPoint p) => Cross(a, b, p) == 0 &&
        p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) &&
        p.Z >= Math.Min(a.Z, b.Z) && p.Z <= Math.Max(a.Z, b.Z);

    private static bool Intersects(MapPoint a, MapPoint b, MapPoint c, MapPoint d)
    {
        double ac = Cross(a, b, c), ad = Cross(a, b, d), ca = Cross(c, d, a), cb = Cross(c, d, b);
        return ac == 0 && OnSegment(a, b, c) || ad == 0 && OnSegment(a, b, d) ||
            ca == 0 && OnSegment(c, d, a) || cb == 0 && OnSegment(c, d, b) ||
            (ac < 0 && ad > 0 || ac > 0 && ad < 0) && (ca < 0 && cb > 0 || ca > 0 && cb < 0);
    }

    private static double EdgeDistanceSquared(MapPoint a, MapPoint b, MapPoint p)
    {
        double dx = b.X - a.X, dz = b.Z - a.Z;
        double t = Math.Clamp(((p.X - a.X) * dx + (p.Z - a.Z) * dz) / (dx * dx + dz * dz), 0, 1);
        double px = p.X - (a.X + t * dx), pz = p.Z - (a.Z + t * dz);
        return px * px + pz * pz;
    }
}
