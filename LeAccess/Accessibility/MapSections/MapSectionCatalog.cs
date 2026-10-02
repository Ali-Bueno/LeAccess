using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LeAccess.Accessibility.MapSections;

internal sealed class MapSectionCatalog
{
    private readonly Dictionary<(string Scene, string BuildHash), SectionMap> _maps = new();

    internal static MapSectionCatalog Parse(string json) => Parse(new[] { json });

    internal static MapSectionCatalog Parse(IEnumerable<string> catalogs)
    {
        if (catalogs == null) throw new FormatException("Map section catalogs must be supplied.");
        try
        {
            var result = new MapSectionCatalog();
            foreach (string json in catalogs)
            {
                using var document = JsonDocument.Parse(json);
                var maps = document.RootElement.GetProperty("maps");
                foreach (var map in maps.EnumerateArray())
                {
                    string scene = Nonblank(map.GetProperty("scene")), hash = Nonblank(map.GetProperty("buildHash"));
                    string scenePath = map.TryGetProperty("scenePath", out var path) && path.ValueKind != JsonValueKind.Null
                        ? Nonblank(path) : null;
                    var anchors = new List<MapAnchor>();
                    var anchorPaths = new HashSet<string>(StringComparer.Ordinal);
                    if (map.TryGetProperty("anchors", out var anchorArray) && anchorArray.ValueKind != JsonValueKind.Null)
                        foreach (var anchor in anchorArray.EnumerateArray())
                        {
                            string anchorPath = Nonblank(anchor.GetProperty("path"));
                            double tolerance = Number(anchor.GetProperty("tolerance"));
                            if (tolerance <= 0 || !anchorPaths.Add(anchorPath))
                                throw new FormatException("Anchor paths must be unique and tolerances positive.");
                            anchors.Add(new MapAnchor(anchorPath, Number(anchor.GetProperty("x")),
                                Number(anchor.GetProperty("y")), Number(anchor.GetProperty("z")), tolerance));
                        }
                    var sections = new List<MapSection>();
                    var ids = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var section in map.GetProperty("sections").EnumerateArray())
                    {
                        string id = Nonblank(section.GetProperty("id")), key = Nonblank(section.GetProperty("textKey"));
                        string speechGroup = section.TryGetProperty("speechGroup", out var group) ? Nonblank(group) : id;
                        MapSectionSource source = null;
                        if (section.TryGetProperty("source", out var sourceElement))
                        {
                            var colliders = new List<MapSectionCollider>();
                            if (sourceElement.TryGetProperty("colliders", out var colliderArray))
                                foreach (var collider in colliderArray.EnumerateArray())
                                {
                                    if (collider.TryGetProperty("colliders", out _))
                                        throw new FormatException("Collider transforms cannot contain nested colliders.");
                                    colliders.Add(new MapSectionCollider(ReadSource(collider),
                                        Numbers(collider.GetProperty("center")), Numbers(collider.GetProperty("size"))));
                                }
                            source = ReadSource(sourceElement, colliders.ToArray());
                        }
                        if (!ids.Add(id)) throw new FormatException("Duplicate map section id: " + id);
                        int priority = section.GetProperty("priority").GetInt32();
                        var components = new List<MapSectionComponent>();
                        if (section.TryGetProperty("components", out var componentArray))
                        {
                            if (section.TryGetProperty("polygon", out _) || section.TryGetProperty("minY", out _) ||
                                section.TryGetProperty("maxY", out _))
                                throw new FormatException("Do not mix single-polygon fields with components.");
                            foreach (var component in componentArray.EnumerateArray()) components.Add(ReadComponent(component));
                        }
                        else components.Add(ReadComponent(section));
                        sections.Add(new MapSection(id, key, priority, components.ToArray(), speechGroup, source));
                    }
                    if (!result._maps.TryAdd((scene, hash), new SectionMap(scene, scenePath, hash, anchors.ToArray(), sections.ToArray())))
                        throw new FormatException("Duplicate scene/build map: " + scene);
                }
            }
            return result;
        }
        catch (Exception ex) when (ex is JsonException || ex is ArgumentException ||
            ex is InvalidOperationException || ex is KeyNotFoundException || ex is OverflowException)
        { throw new FormatException("Invalid map section catalog: " + ex.Message, ex); }
    }

    internal SectionMap Find(string scene, string buildHash) => scene != null && buildHash != null &&
        _maps.TryGetValue((scene, buildHash), out var map) ? map : null;

    private static string Nonblank(JsonElement element)
    {
        string value = element.GetString();
        if (string.IsNullOrWhiteSpace(value)) throw new FormatException("Map names, ids and text keys must be nonblank.");
        return value;
    }

    private static double Number(JsonElement element)
    {
        double value = element.GetDouble();
        if (!double.IsFinite(value)) throw new FormatException("Map coordinates must be finite.");
        return value;
    }

    private static double[] Numbers(JsonElement element)
    {
        var result = new List<double>();
        foreach (var value in element.EnumerateArray()) result.Add(Number(value));
        return result.ToArray();
    }

    private static MapSectionSource ReadSource(JsonElement source, MapSectionCollider[] colliders = null)
        => new(Nonblank(source.GetProperty("path")), Numbers(source.GetProperty("matrix")),
            Number(source.GetProperty("tolerance")), colliders);

    private static MapSectionComponent ReadComponent(JsonElement component)
    {
        double minY = Number(component.GetProperty("minY")), maxY = Number(component.GetProperty("maxY"));
        var points = new List<MapPoint>();
        foreach (var point in component.GetProperty("polygon").EnumerateArray())
            points.Add(new MapPoint(Number(point.GetProperty("x")), Number(point.GetProperty("z"))));
        return new MapSectionComponent(minY, maxY, points.ToArray());
    }
}

internal readonly record struct MapAnchor(string Path, double X, double Y, double Z, double Tolerance);

internal sealed class SectionMap
{
    internal string Scene { get; }
    internal string ScenePath { get; }
    internal string BuildHash { get; }
    internal MapAnchor[] Anchors { get; }
    internal IReadOnlyList<MapSection> Sections { get; }
    internal SectionMap(string scene, string scenePath, string buildHash, MapAnchor[] anchors, MapSection[] sections)
    {
        Scene = scene; ScenePath = scenePath; BuildHash = buildHash;
        Anchors = (MapAnchor[])anchors.Clone(); Sections = Array.AsReadOnly(sections);
    }

    internal MapSection FindSection(double x, double y, double z, Func<MapSection, bool> available = null)
    {
        MapSection best = null;
        double bestArea = double.PositiveInfinity;
        foreach (var section in Sections)
        {
            if (!section.TryGetContainingArea(x, y, z, out double area)) continue;
            if (available != null && !available(section)) continue;
            if (best == null || section.Priority > best.Priority || section.Priority == best.Priority &&
                (area < bestArea || area == bestArea && string.CompareOrdinal(section.Id, best.Id) < 0))
            {
                best = section;
                bestArea = area;
            }
        }
        return best;
    }
}
