using System;
using System.Collections.Generic;
using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Exploration;

// A way toward an entry with no explored route: an opening of the explored map and the route there.
internal readonly record struct ExplorationWay(ExplorationEntry Opening, ExplorationRoute Route);

// Ways toward the last entry read without an explored route: the best is read with it, and all of them form a
// category right after the entry's own; reference/exploration-mode.md "Ways toward a target beyond explored ground".
internal sealed partial class ExplorationCatalog
{
    internal const string WayKeyPrefix = "way:";
    private readonly List<ExplorationEntry> _wayOpenings = new();
    private ExplorationEntry? _waysFor;

    /// <summary>Ways toward an entry with no explored route, best first; unset, none are offered.</summary>
    internal Func<ExplorationEntry, Vector3, IReadOnlyList<ExplorationWay>> Ways { get; set; }

    /// <summary>The entry the listed ways lead toward, or null when none are listed.</summary>
    internal ExplorationEntry? WaysTarget => _waysFor;

    /// <summary>The opening behind a way entry, or null for any other entry.</summary>
    internal static string OpeningKey(ExplorationEntry entry) =>
        entry.Category == ExplorationCategory.Way ? entry.Key[WayKeyPrefix.Length..] : null;

    private void ClearWays()
    {
        _waysFor = null;
        _wayOpenings.Clear();
    }

    // The best way follows the readout of an entry with no explored route, and its ways become the listed ones.
    private string WithBestWay(ExplorationEntry entry, string readout, Vector3 player, Vector3 forward, Vector3 right,
        float stepLength, ExplorationText text)
    {
        if (Ways is null || entry.Category is ExplorationCategory.UnexploredRoom or ExplorationCategory.Way) return readout;
        var ways = Ways(entry, player);
        ClearWays();
        if (ways is not { Count: > 0 }) return readout;
        _waysFor = entry;
        foreach (var way in ways) _wayOpenings.Add(way.Opening);
        var best = ways[0];
        string first = WayReadout(best.Opening, (_, _) => best.Route, entry.Position, player, forward, right, stepLength, text);
        return ways.Count == 1 ? text("exploration.way_only", readout, first)
            : text("exploration.way_best", readout, ways.Count, first);
    }

    private string DescribeWay(ExplorationEntry way, Vector3 player, Vector3 forward, Vector3 right, float stepLength,
        ExplorationText text)
    {
        int index = _wayOpenings.FindIndex(opening => WayKeyPrefix + opening.Key == way.Key);
        return index < 0 || _waysFor is not { } target ? way.Name
            : WayReadout(_wayOpenings[index], Route, target.Position, player, forward, right, stepLength, text);
    }

    // An opening read as any entry, then the straight way on from it to the target.
    private string WayReadout(ExplorationEntry opening, Func<ExplorationEntry, Vector3, ExplorationRoute?> route,
        Vector3 target, Vector3 player, Vector3 forward, Vector3 right, float stepLength, ExplorationText text)
    {
        string readout = Describe(opening, player, forward, right, stepLength, text, LevelHeight, Ground, route, LegsSpoken, out _);
        var beyond = target - opening.Position;
        int steps = Steps(new Vector2(beyond.X, beyond.Z).Length(), stepLength);
        string direction = GuidanceGeometry.Direction(beyond, forward, right);
        return steps < 1 || direction == null ? readout
            : text("exploration.way_beyond", readout,
                steps == 1 ? text("exploration.steps_one") : text("exploration.steps_many", steps), text(direction));
    }

    // The listed entries plus the ways that still stand: target and openings still listed, positions refreshed.
    private IReadOnlyList<ExplorationEntry> WithWays(IReadOnlyList<ExplorationEntry> entries)
    {
        if (_waysFor is not { } target) return entries;
        if (Find(entries, target.Key) is not { } latest)
        {
            ClearWays();
            return entries;
        }
        _waysFor = latest;
        for (int index = _wayOpenings.Count - 1; index >= 0; index--)
            if (Find(entries, _wayOpenings[index].Key) is { } opening) _wayOpenings[index] = opening;
            else _wayOpenings.RemoveAt(index);
        if (_wayOpenings.Count == 0)
        {
            ClearWays();
            return entries;
        }
        var all = new List<ExplorationEntry>(entries.Count + _wayOpenings.Count);
        all.AddRange(entries);
        foreach (var opening in _wayOpenings)
            all.Add(new ExplorationEntry(WayKeyPrefix + opening.Key, opening.Name, ExplorationCategory.Way,
                opening.Position, opening.ArrivalRadius));
        return all;
    }

    // The fixed order with the ways right after their target's category.
    private IReadOnlyList<ExplorationCategory> CategoryOrder()
    {
        if (_waysFor is not { } target) return Order;
        var order = new List<ExplorationCategory>(Order);
        order.Insert(order.IndexOf(target.Category) + 1, ExplorationCategory.Way);
        return order;
    }

    private static ExplorationEntry? Find(IReadOnlyList<ExplorationEntry> entries, string key)
    {
        foreach (var entry in entries)
            if (entry.Key == key) return entry;
        return null;
    }
}
