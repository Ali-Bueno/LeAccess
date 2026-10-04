using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Exploration;

// Scanner order: up/down on the right stick walks this list, skipping empty categories.
internal enum ExplorationCategory { Objectives, Exits, Travel, Characters, Services, Enemies }

internal readonly record struct ExplorationEntry(string Key, string Name, ExplorationCategory Category,
    Vector3 Position, float ArrivalRadius, bool Boss = false);

internal delegate string ExplorationText(string key, params object[] arguments);

// Category/entry cursor of the right-stick scanner. Entries are nearest first and wrap;
// like D2R, readouts never give a position in the list or walking steps.
internal sealed class ExplorationCatalog
{
    private static readonly ExplorationCategory[] Order = Enum.GetValues<ExplorationCategory>();
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
        return text("exploration.scanner.category", text(CategoryKey(_category.Value)), sorted.Count) + ". " +
            Describe(sorted[0], player, forward, right, stepLength, text);
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
        return Describe(sorted[index], player, forward, right, stepLength, text);
    }

    internal static string Describe(ExplorationEntry entry, Vector3 player, Vector3 forward, Vector3 right,
        float stepLength, ExplorationText text)
    {
        string name = entry.Boss ? text("exploration.boss", entry.Name) : entry.Name;
        var delta = entry.Position - player;
        float distance = new Vector2(delta.X, delta.Z).Length();
        int steps = stepLength > 0 && float.IsFinite(distance)
            ? (int)Math.Round(distance / stepLength, MidpointRounding.AwayFromZero) : 0;
        string direction = GuidanceGeometry.Direction(delta, forward, right);
        if (steps < 1 || direction == null) return text("exploration.entry_here", name);
        string length = steps == 1 ? text("exploration.steps_one") : text("exploration.steps_many", steps);
        return text("exploration.entry", name, length, text(direction));
    }

    internal static string CategoryKey(ExplorationCategory category) => category switch
    {
        ExplorationCategory.Objectives => "exploration.category.objectives",
        ExplorationCategory.Exits => "exploration.category.exits",
        ExplorationCategory.Travel => "exploration.category.travel",
        ExplorationCategory.Characters => "exploration.category.characters",
        ExplorationCategory.Services => "exploration.category.services",
        _ => "exploration.category.enemies"
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
