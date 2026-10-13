using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Exploration;

// The D2R scanner's categories (d2r-access src/entity_radar.h). Values are saved in seen-place keys,
// so earlier numbers are kept (waypoints had 2, the stash 4) and new ones appended.
internal enum ExplorationCategory
{
    Quest = 0, Exit = 1, Waypoint = 2, Npc = 3, Stash = 4, Enemy = 5, UnexploredRoom = 6, Portal = 7, Room = 8,
    Player = 9, LastDeath = 10, Treasure = 11, Shrine = 12, Interactable = 13, Item = 14, Breakable = 15, Way = 16
}

internal readonly record struct ExplorationEntry(string Key, string Name, ExplorationCategory Category,
    Vector3 Position, float ArrivalRadius, bool Boss = false);

internal delegate string ExplorationText(string key, params object[] arguments);

// A walking route through explored ground: its length, the point the beacon pans to, whether it is one straight leg,
// and its corners from the player to the goal when they were asked for.
internal readonly record struct ExplorationRoute(float Length, Vector3 Aim, bool Direct,
    IReadOnlyList<Vector3> Corners = null);

// Category/entry cursor of the right-stick scanner. Entries are nearest first and wrap; as in D2R a
// category step says the category's name, never a count. Readouts follow the explored route when one is known.
internal sealed partial class ExplorationCatalog
{
    // D2R map_survey.cpp CategoryOrder: the ways out, where there is left to go, what the level is for,
    // who is there, then its scenery. Up/down on the right stick walks it, skipping empty categories.
    internal static readonly ExplorationCategory[] Order =
    {
        ExplorationCategory.Exit, ExplorationCategory.Portal, ExplorationCategory.Waypoint,
        ExplorationCategory.UnexploredRoom, ExplorationCategory.Room, ExplorationCategory.Quest,
        ExplorationCategory.Npc, ExplorationCategory.Player, ExplorationCategory.LastDeath, ExplorationCategory.Stash,
        ExplorationCategory.Enemy, ExplorationCategory.Treasure, ExplorationCategory.Shrine,
        ExplorationCategory.Interactable, ExplorationCategory.Breakable, ExplorationCategory.Item
    };
    private ExplorationCategory? _category;
    private string _selected;

    internal ExplorationCategory? Category => _category;
    internal string SelectedKey => _selected;

    internal void Reset()
    {
        _category = null;
        _selected = null;
        ClearWays();
    }

    internal ExplorationEntry? Selected(IReadOnlyList<ExplorationEntry> entries) =>
        _selected == null ? null : Find(WithWays(entries), _selected);

    // A category step names the category and its count, then reads and selects its nearest entry.
    internal string StepCategory(int delta, IReadOnlyList<ExplorationEntry> entries, Vector3 player,
        Vector3 forward, Vector3 right, float stepLength, ExplorationText text)
    {
        entries = WithWays(entries);
        var order = CategoryOrder();
        var present = order.Where(category => entries.Any(entry => entry.Category == category &&
            GuidanceGeometry.Finite(entry.Position))).ToList();
        if (present.Count == 0)
        {
            Reset();
            return text("exploration.scanner.empty");
        }
        _category = present[NextCategory(present, order, Math.Sign(delta) >= 0 ? 1 : -1)];
        var sorted = Sorted(entries, _category.Value, player);
        _selected = sorted[0].Key;
        string name = CategoryName(_category.Value, text);
        return name + ". " + DescribeEntry(sorted[0], player, forward, right, stepLength, text);
    }

    internal string StepEntry(int delta, IReadOnlyList<ExplorationEntry> entries, Vector3 player,
        Vector3 forward, Vector3 right, float stepLength, ExplorationText text)
    {
        if (!_category.HasValue) return StepCategory(1, entries, player, forward, right, stepLength, text);
        var sorted = Sorted(WithWays(entries), _category.Value, player);
        if (sorted.Count == 0)
        {
            _selected = null;
            return text("exploration.scanner.category_empty", CategoryName(_category.Value, text));
        }
        int index = sorted.FindIndex(entry => entry.Key == _selected);
        index = index < 0 ? 0 : Wrap(index + (Math.Sign(delta) >= 0 ? 1 : -1), sorted.Count);
        _selected = sorted[index].Key;
        return DescribeEntry(sorted[index], player, forward, right, stepLength, text);
    }

    // Create+Circle: the selected entry again with the current distance and direction, or the empty-scanner line.
    internal string RepeatSelected(IReadOnlyList<ExplorationEntry> entries, Vector3 player,
        Vector3 forward, Vector3 right, float stepLength, ExplorationText text) =>
        Selected(entries) is { } entry && GuidanceGeometry.Finite(entry.Position)
            ? DescribeEntry(entry, player, forward, right, stepLength, text)
            : text("exploration.scanner.empty");

    /// <summary>The scanner's one readout of an entry, with this scanner's level height, ground, routes and ways.</summary>
    internal string DescribeEntry(ExplorationEntry entry, Vector3 player, Vector3 forward, Vector3 right,
        float stepLength, ExplorationText text)
    {
        if (entry.Category == ExplorationCategory.Way) return DescribeWay(entry, player, forward, right, stepLength, text);
        string readout = Describe(entry, player, forward, right, stepLength, text, LevelHeight, Ground, Route, LegsSpoken,
            out bool unrouted);
        if (unrouted) return WithBestWay(entry, readout, player, forward, right, stepLength, text);
        // A route found since the ways were listed makes them moot.
        if (Route is not null && _waysFor?.Key == entry.Key) ClearWays();
        return readout;
    }

    /// <summary>Height of one walkable floor; a target farther above or below is said to be on another level.</summary>
    internal float LevelHeight { get; set; } = float.PositiveInfinity;

    /// <summary>Walkable ground height under a point, or null when unknown; without it no level is claimed.</summary>
    internal Func<Vector3, float?> Ground { get; set; }

    /// <summary>The walking route from the player to an entry, or null when none is known; unset, routes are not spoken.</summary>
    internal Func<ExplorationEntry, Vector3, ExplorationRoute?> Route { get; set; }

    /// <summary>How many stretches of a detour are read; 0 reads only the way to head now.</summary>
    internal int LegsSpoken { get; set; }

    internal static string Describe(ExplorationEntry entry, Vector3 player, Vector3 forward, Vector3 right,
        float stepLength, ExplorationText text, float levelHeight = float.PositiveInfinity,
        Func<Vector3, float?> ground = null, Func<ExplorationEntry, Vector3, ExplorationRoute?> route = null, int legs = 0) =>
        Describe(entry, player, forward, right, stepLength, text, levelHeight, ground, route, legs, out _);

    internal static string Describe(ExplorationEntry entry, Vector3 player, Vector3 forward, Vector3 right,
        float stepLength, ExplorationText text, float levelHeight, Func<Vector3, float?> ground,
        Func<ExplorationEntry, Vector3, ExplorationRoute?> route, int legs, out bool unrouted)
    {
        unrouted = false;
        string name = entry.Boss ? text("exploration.boss", entry.Name) : entry.Name;
        var delta = entry.Position - player;
        int steps = Steps(new Vector2(delta.X, delta.Z).Length(), stepLength);
        string direction = GuidanceGeometry.Direction(delta, forward, right);
        string place = steps < 1 || direction == null ? text("exploration.entry_here", name)
            : text("exploration.entry", name, steps == 1 ? text("exploration.steps_one") : text("exploration.steps_many", steps),
                text(direction));
        if (route is not null && route(entry, player) is { } known)
            place = Routed(known, name, steps, direction, player, forward, right, stepLength, levelHeight, legs, text) ?? place;
        else if (route is not null)
        {
            unrouted = true;
            place = text("exploration.entry_unrouted", place);
        }
        // Distance is horizontal. Markers float above their floor, so levels compare sampled ground, never raw heights.
        float? difference = GroundDifference(entry.Position, player, ground);
        bool otherLevel = difference is { } dy && float.IsFinite(levelHeight) && levelHeight > 0 && Math.Abs(dy) >= levelHeight;
        return otherLevel ? text("exploration.on_level", place, text(difference > 0 ? "exploration.level_higher" : "exploration.level_lower"))
            : place;
    }

    // A detour speaks its walking length and the way to head now, or its stretches when they are asked for;
    // a straight route keeps the plain readout.
    private static string Routed(ExplorationRoute route, string name, int straightSteps, string straightDirection,
        Vector3 player, Vector3 forward, Vector3 right, float stepLength, float levelHeight, int legs, ExplorationText text)
    {
        int steps = Steps(route.Length, stepLength);
        string heading = GuidanceGeometry.Direction(route.Aim - player, forward, right);
        if (route.Direct || steps < 1 || heading == null || steps == straightSteps && heading == straightDirection) return null;
        string stretches = legs > 0 && route.Corners is { Count: > 2 } corners
            ? RouteLegs.Describe(RouteLegs.From(corners, forward, right), legs, stepLength, levelHeight, text)
            : null;
        return text("exploration.entry", name,
            steps == 1 ? text("exploration.route_steps_one") : text("exploration.route_steps_many", steps),
            stretches ?? text("exploration.route_heading", text(heading)));
    }

    internal static int Steps(float distance, float stepLength) => stepLength > 0 && float.IsFinite(distance)
        ? (int)Math.Round(distance / stepLength, MidpointRounding.AwayFromZero) : 0;

    private static float? GroundDifference(Vector3 target, Vector3 player, Func<Vector3, float?> ground)
    {
        if (ground is null || ground(target) is not { } targetY || ground(player) is not { } playerY) return null;
        float difference = targetY - playerY;
        return float.IsFinite(difference) ? difference : null;
    }

    // The ways' category names the entry they lead toward.
    private string CategoryName(ExplorationCategory category, ExplorationText text) =>
        category == ExplorationCategory.Way ? text(CategoryKey(category), _waysFor?.Name ?? "") : text(CategoryKey(category));

    internal static string CategoryKey(ExplorationCategory category) => category switch
    {
        ExplorationCategory.Quest => "exploration.category.quest",
        ExplorationCategory.Exit => "exploration.category.exit",
        ExplorationCategory.Waypoint => "exploration.category.waypoint",
        ExplorationCategory.Npc => "exploration.category.npc",
        ExplorationCategory.Stash => "exploration.category.stash",
        ExplorationCategory.Enemy => "exploration.category.enemy",
        ExplorationCategory.UnexploredRoom => "exploration.category.unexplored_room",
        ExplorationCategory.Portal => "exploration.category.portal",
        ExplorationCategory.Room => "exploration.category.room",
        ExplorationCategory.Player => "exploration.category.player",
        ExplorationCategory.LastDeath => "exploration.category.last_death",
        ExplorationCategory.Treasure => "exploration.category.treasure",
        ExplorationCategory.Shrine => "exploration.category.shrine",
        ExplorationCategory.Interactable => "exploration.category.interactable",
        ExplorationCategory.Breakable => "exploration.category.breakable",
        ExplorationCategory.Way => "exploration.category.way",
        _ => "exploration.category.item"
    };

    private int NextCategory(List<ExplorationCategory> present, IReadOnlyList<ExplorationCategory> order, int direction)
    {
        if (!_category.HasValue) return direction > 0 ? 0 : present.Count - 1;
        int current = present.IndexOf(_category.Value);
        if (current >= 0) return Wrap(current + direction, present.Count);
        // The current category emptied: continue from its place in the order.
        int place = IndexOf(order, _category.Value);
        if (direction > 0)
        {
            int next = present.FindIndex(category => IndexOf(order, category) > place);
            return next < 0 ? 0 : next;
        }
        int previous = present.FindLastIndex(category => IndexOf(order, category) < place);
        return previous < 0 ? present.Count - 1 : previous;
    }

    private static int IndexOf(IReadOnlyList<ExplorationCategory> order, ExplorationCategory category)
    {
        for (int index = 0; index < order.Count; index++)
            if (order[index] == category) return index;
        return -1;
    }

    // Ways keep their order, best first; every other category is nearest first.
    private static List<ExplorationEntry> Sorted(IReadOnlyList<ExplorationEntry> entries,
        ExplorationCategory category, Vector3 player)
    {
        var listed = entries.Where(entry => entry.Category == category && GuidanceGeometry.Finite(entry.Position));
        return category == ExplorationCategory.Way ? listed.ToList()
            : listed.OrderBy(entry => HorizontalSquared(entry.Position - player))
                .ThenBy(entry => entry.Key, StringComparer.Ordinal).ToList();
    }

    private static float HorizontalSquared(Vector3 delta) => delta.X * delta.X + delta.Z * delta.Z;
    private static int Wrap(int index, int count) => (index % count + count) % count;
}
