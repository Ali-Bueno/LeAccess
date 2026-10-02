using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LeAccess.Accessibility.MapSections;

internal static class SceneryFootprint
{
    // Authored model families verified in the installed Z32 bundle; see runtime-map-places.md.
    private static readonly Regex Tent = new(@"^Tent_0[123](?: \(\d+\))?$", RegexOptions.CultureInvariant);
    private static readonly Regex Wagon = new(@"^Wagon1B(?:\(Empty\))?(?: \(\d+\))?$", RegexOptions.CultureInvariant);
    private static readonly Regex Gate = new(@"^Bridge_Gate_00[12](?: \(\d+\))?$", RegexOptions.CultureInvariant);

    internal static string Classify(string name, string parent)
    {
        if (Tent.IsMatch(name ?? "")) return "map.place.tents";
        if (name == "WagonBody" && Wagon.IsMatch(parent ?? "")) return "map.place.wagon";
        if (name == "Eterra_Statue_Model" && parent == "Circle_Statue") return "map.place.eterra_statue";
        if (Gate.IsMatch(name ?? "")) return "map.place.gate";
        return null;
    }

    internal static bool TryClosest(Transform transform, Bounds bounds, Vector3 position, out Vector3 closest)
    {
        closest = default;
        var min = bounds.min;
        var max = bounds.max;
        var scale = transform.lossyScale;
        if (!Finite(position) || !Finite(min) || !Finite(max) || !Finite(scale) ||
            max.x <= min.x || max.y < min.y || max.z <= min.z || scale.x == 0 || scale.y <= 0 || scale.z == 0) return false;
        var local = transform.InverseTransformPoint(position);
        if (!Finite(local)) return false;
        closest = transform.TransformPoint(new Vector3(Math.Clamp(local.x, min.x, max.x),
            min.y, Math.Clamp(local.z, min.z, max.z)));
        return Finite(closest);
    }

    internal static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x, z = a.z - b.z;
        return MathF.Sqrt(x * x + z * z);
    }

    internal static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
}
