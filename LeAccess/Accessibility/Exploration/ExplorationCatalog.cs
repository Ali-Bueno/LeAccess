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
    Player = 9, LastDeath = 10, Treasure = 11, Shrine = 12, Interactable = 13, Item = 14
}

internal readonly record struct ExplorationEntry(string Key, string Name, ExplorationCategory Category,
    Vector3 Position, float ArrivalRadius, bool Boss = false);

internal delegate string ExplorationText(string key, params object[] arguments);

// Category/entry cursor of the right-stick scanner. Entries are nearest first and wrap; as in D2R a
// category step says the category's name, never a count, and readouts give the straight line.
internal sealed class ExplorationCatalog
{
    // D2R map_survey.cpp CategoryOrder: the ways out, where there is left to go, what the level is for,
    // who is there, then its scenery. Up/down on the right stick walks it, skipping empty categories.
    internal static readonly ExplorationCategory[] Order =
    {
        ExplorationCategory.Exit, ExplorationCategory.Portal, ExplorationCategory.Waypoint,
        ExplorationCategory.UnexploredRoom, ExplorationCategory.Room, ExplorationCategory.Quest,
        ExplorationCategory.Npc, ExplorationCategory.Player, ExplorationCategory.LastDeath, ExplorationCategory.Stash,
        ExplorationCategory.Enemy, ExplorationCategory.Treasure, ExplorationCategory.Shrine,
        ExplorationCategory.Interactable, ExplorationCategory.Item
    };
    private ExplorationCategory? _category;
    private string _selected;

    internal ExplorationCategory? Category => _category;
    internal string SelectedKey => _selected;

    internal void Reset()
    {
        _category = null;
        _selected = null;
    }

    internal ExplorationEntry? Selected(IReadOnlyList<ExplorationEntry> entries)
    {
        foreach (var entry in entries)
            if (entry.Key == _selected) return entry;
        return null;
    }

    // A category step names the category and its count, then reads and selects its nearest entry.
    internal string StepCategory(int delta, IReadOnlyList<ExplorationEntry> entries, Vector3 player,
        Vector3 forward, Vector3 right, float stepLength, ExplorationText text)
    {
        var present = Order.Where(category => entries.Any(entry => entry.Category == category &&
            GuidanceGeometry.Finite(entry.Position))).ToList();
        if (present.Count == 0)
        {
            Reset();
            return text("exploration.scanner.empty");
        }
        _category = present[NextCategory(present, Math.Sign(delta) >= 0 ? 1 : -1)];
        var sorted = Sorted(entries, _category.Value, player);
        _selected = sorted[0].Key;
        return text(CategoryKey(_category.Value)) + ". " + Describe(sorted[0], player, forward, right, stepLength, text, LevelHeight, Ground);
    }

    internal string StepEntry(int delta, IReadOnlyList<ExplorationEntry> entries, Vector3 player,
        Vector3 forward, Vector3 right, float stepLength, ExplorationText text)
    {
        if (!_category.HasValue) return StepCategory(1, entries, player, forward, right, stepLength, text);
        var sorted = Sorted(entries, _category.Value, player);
        if (sorted.Count == 0)
        {
            _selected = null;
            return text("exploration.scanner.category_empty", text(CategoryKey(_category.Value)));
        }
        int index = sorted.FindIndex(entry => entry.Key == _selected);
        index = index < 0 ? 0 : Wrap(index + (Math.Sign(delta) >= 0 ? 1 : -1), sorted.Count);
        _selected = sorted[index].Key;
        return Describe(sorted[index], player, forward, right, stepLength, text, LevelHeight, Ground);
    }

    /// <summary>Height of one walkable floor; a target farther above or below is said to be on another level.</summary>
    internal float LevelHeight { get; set; } = float.PositiveInfinity;

    /// <summary>Walkable ground height under a point, or null when unknown; without it no level is claimed.</summary>
    internal Func<Vector3, float?> Ground { get; set; }

    internal static string Describe(ExplorationEntry entry, Vector3 player, Vector3 forward, Vector3 right,
        float stepLength, ExplorationText text, float levelHeight = float.PositiveInfinity,
        Func<Vector3, float?> ground = null)
    {
        string name = entry.Boss ? text("exploration.boss", entry.Name) : entry.Name;
        var delta = entry.Position - player;
        float distance = new Vector2(delta.X, delta.Z).Length();
        int steps = stepLength > 0 && float.IsFinite(distance)
            ? (int)Math.Round(distance / stepLength, MidpointRounding.AwayFromZero) : 0;
        string direction = GuidanceGeometry.Direction(delta, forward, right);
        string place = steps < 1 || direction == null ? text("exploration.entry_here", name)
            : text("exploration.entry", name, steps == 1 ? text("exploration.steps_one") : text("exploration.steps_many", steps),
                text(direction));
        // Distance is horizontal. Markers float above their floor, so levels compare sampled ground, never raw heights.
        float? difference = GroundDifference(entry.Position, player, ground);
        bool otherLevel = difference is { } dy && float.IsFinite(levelHeight) && levelHeight > 0 && Math.Abs(dy) >= levelHeight;
        return otherLevel ? text("exploration.on_level", place, text(difference > 0 ? "exploration.level_higher" : "exploration.level_lower"))
            : place;
    }

    private static float? GroundDifference(Vector3 target, Vector3 player, Func<Vector3, float?> ground)
    {
        if (ground is null || ground(target) is not { } targetY || ground(player) is not { } playerY) return null;
        float difference = targetY - playerY;
        return float.IsFinite(difference) ? difference : null;
    }

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
        _ => "exploration.category.item"
    };

    private int NextCategory(List<ExplorationCategory> present, int direction)
    {
        if (!_category.HasValue) return direction > 0 ? 0 : present.Count - 1;
        int current = present.IndexOf(_category.Value);
        if (current >= 0) return Wrap(current + direction, present.Count);
        // The current category emptied: continue from its place in the fixed order.
        int order = Array.IndexOf(Order, _category.Value);
        if (direction > 0)
        {
            int next = present.FindIndex(category => Array.IndexOf(Order, category) > order);
            return next < 0 ? 0 : next;
        }
        int previous = present.FindLastIndex(category => Array.IndexOf(Order, category) < order);
        return previous < 0 ? present.Count - 1 : previous;
    }

    private static List<ExplorationEntry> Sorted(IReadOnlyList<ExplorationEntry> entries,
        ExplorationCategory category, Vector3 player) =>
        entries.Where(entry => entry.Category == category && GuidanceGeometry.Finite(entry.Position))
            .OrderBy(entry => HorizontalSquared(entry.Position - player))
            .ThenBy(entry => entry.Key, StringComparer.Ordinal).ToList();

    private static float HorizontalSquared(Vector3 delta) => delta.X * delta.X + delta.Z * delta.Z;
    private static int Wrap(int index, int count) => (index % count + count) % count;
}
