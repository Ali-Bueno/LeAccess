using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class TentCanopy
{
    private const string Resource = "LeAccess.TentCanopies.json";
    private static readonly Dictionary<string, Profile> Profiles = new();

    internal static void Initialize(string buildHash)
    {
        Profiles.Clear();
        using var stream = typeof(TentCanopy).Assembly.GetManifestResourceStream(Resource);
        if (stream == null) return;
        using var reader = new StreamReader(stream);
        Load(reader.ReadToEnd(), buildHash);
    }

    internal static void Load(string json, string buildHash)
    {
        Profiles.Clear();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (buildHash == null || root.GetProperty("buildHash").GetString() != buildHash) return;
        foreach (var item in root.GetProperty("profiles").EnumerateArray())
        {
            var regions = new List<Region>();
            foreach (var component in item.GetProperty("components").EnumerateArray())
            {
                var holes = new List<Vector3[]>();
                foreach (var hole in component.GetProperty("holes").EnumerateArray()) holes.Add(Points(hole));
                regions.Add(new Region(Points(component.GetProperty("outer")), holes));
            }
            Profiles.Add(item.GetProperty("name").GetString(), new Profile(
                item.GetProperty("vertices").GetInt32(), item.GetProperty("submeshes").GetInt32(),
                Vector(item.GetProperty("min")), Vector(item.GetProperty("max")),
                item.GetProperty("roofFloor").GetSingle(), regions));
        }
    }

    internal static bool Contains(MeshRenderer renderer, Vector3 position, Vector3 supportedPosition, float radius)
    {
        if (!TryProfile(renderer, out var profile) || !float.IsFinite(radius) || radius <= 0) return false;
        var frame = new Frame(renderer.transform);
        var basePoint = frame.Apply(new Vector3(0, profile.Min.y, 0));
        var roofPoint = frame.Apply(new Vector3(0, profile.RoofFloor, 0));
        // These contours represent horizontal canopy planes, so tilted models fail closed.
        if (basePoint.x != roofPoint.x || basePoint.z != roofPoint.z ||
            !SceneryFootprint.Finite(roofPoint) || roofPoint.y <= position.y + radius) return false;
        foreach (var region in profile.Regions)
            if (Inside(region.Outer, frame, position, radius) && Inside(region.Outer, frame, supportedPosition, radius) &&
                !region.Holes.Exists(hole => Inside(hole, frame, position, 0) ||
                    Inside(hole, frame, supportedPosition, 0) || !ClearOfEdges(hole, frame, position, radius) ||
                    !ClearOfEdges(hole, frame, supportedPosition, radius))) return true;
        return false;
    }

    internal static bool IsAvailable(MeshRenderer renderer) => TryProfile(renderer, out _);

    private static bool TryProfile(MeshRenderer renderer, out Profile profile)
    {
        profile = null;
        if (!IsAlive(renderer) || !renderer.enabled || !renderer.gameObject.activeInHierarchy) return false;
        var filter = renderer.gameObject.GetComponent<MeshFilter>();
        var mesh = IsAlive(filter) ? filter.sharedMesh : null;
        if (!IsAlive(mesh) || !Profiles.TryGetValue(mesh.name, out profile) ||
            mesh.vertexCount != profile.Vertices || mesh.subMeshCount != profile.Submeshes ||
            !Same(renderer.localBounds.min, profile.Min) || !Same(renderer.localBounds.max, profile.Max)) return false;
        var scale = renderer.transform.lossyScale;
        var frame = new Frame(renderer.transform);
        return SceneryFootprint.Finite(scale) && scale.x != 0 && scale.y > 0 && scale.z != 0 &&
            SceneryFootprint.Finite(frame.Origin) && SceneryFootprint.Finite(frame.Y) &&
            frame.Y.x == 0 && frame.Y.z == 0 && frame.Y.y > 0;
    }

    private static bool Inside(Vector3[] points, Frame frame, Vector3 position, float radius)
    {
        bool inside = false;
        for (int i = 0, previous = points.Length - 1; i < points.Length; previous = i++)
        {
            var a = frame.Apply(points[previous]);
            var b = frame.Apply(points[i]);
            if ((a.z > position.z) != (b.z > position.z) &&
                position.x < (b.x - a.x) * (position.z - a.z) / (b.z - a.z) + a.x) inside = !inside;
        }
        return inside && ClearOfEdges(points, frame, position, radius);
    }

    private static bool ClearOfEdges(Vector3[] points, Frame frame, Vector3 position, float radius)
    {
        for (int i = 0, previous = points.Length - 1; i < points.Length; previous = i++)
        {
            var a = frame.Apply(points[previous]);
            var b = frame.Apply(points[i]);
            float x = b.x - a.x, z = b.z - a.z;
            float length = x * x + z * z;
            float fraction = length > 0 ? Math.Clamp(((position.x - a.x) * x + (position.z - a.z) * z) / length, 0, 1) : 0;
            float dx = position.x - (a.x + x * fraction), dz = position.z - (a.z + z * fraction);
            if (dx * dx + dz * dz <= radius * radius) return false;
        }
        return true;
    }

    private static bool Same(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
    private static Vector3 Vector(JsonElement array) => new(array[0].GetSingle(), array[1].GetSingle(), array[2].GetSingle());
    private static Vector3[] Points(JsonElement array)
    {
        var result = new List<Vector3>();
        foreach (var point in array.EnumerateArray()) result.Add(new Vector3(point[0].GetSingle(), 0, point[1].GetSingle()));
        return result.ToArray();
    }

    private sealed class Region
    {
        internal readonly Vector3[] Outer;
        internal readonly List<Vector3[]> Holes;
        internal Region(Vector3[] outer, List<Vector3[]> holes) { Outer = outer; Holes = holes; }
    }
    private sealed class Profile
    {
        internal readonly int Vertices, Submeshes;
        internal readonly Vector3 Min, Max;
        internal readonly float RoofFloor;
        internal readonly List<Region> Regions;
        internal Profile(int vertices, int submeshes, Vector3 min, Vector3 max, float roofFloor, List<Region> regions)
        { Vertices = vertices; Submeshes = submeshes; Min = min; Max = max; RoofFloor = roofFloor; Regions = regions; }
    }
    private readonly struct Frame
    {
        internal readonly Vector3 Origin, X, Y, Z;
        internal Frame(Transform transform)
        {
            Origin = transform.TransformPoint(default);
            X = transform.TransformPoint(new Vector3(1, 0, 0)) - Origin;
            Y = transform.TransformPoint(new Vector3(0, 1, 0)) - Origin;
            Z = transform.TransformPoint(new Vector3(0, 0, 1)) - Origin;
        }
        internal Vector3 Apply(Vector3 p) => new(Origin.x + X.x*p.x + Y.x*p.y + Z.x*p.z,
            Origin.y + X.y*p.x + Y.y*p.y + Z.y*p.z, Origin.z + X.z*p.x + Y.z*p.y + Z.z*p.z);
    }
}
