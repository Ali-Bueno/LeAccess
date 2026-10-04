using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Plans moves in a grid of groups (rows, columns or branches) that never wraps or leaves on its own.</summary>
internal static class GroupedGridOrder
{
    internal enum Step { NextInGroup, PreviousInGroup, NextGroup, PreviousGroup }
    internal enum MoveKind { Select, Edge }
    internal readonly record struct Move(MoveKind Kind, int Group = -1, int Index = -1);

    /// <summary>Steps inside the group, or to the neighbouring non-empty group at the same position (clamped to its size).</summary>
    internal static Move Plan(IReadOnlyList<int> sizes, int group, int index, Step step)
    {
        if (group < 0 || group >= sizes.Count || index < 0 || index >= sizes[group]) return new Move(MoveKind.Edge);
        switch (step)
        {
            case Step.NextInGroup:
                return index + 1 < sizes[group] ? new Move(MoveKind.Select, group, index + 1) : new Move(MoveKind.Edge);
            case Step.PreviousInGroup:
                return index > 0 ? new Move(MoveKind.Select, group, index - 1) : new Move(MoveKind.Edge);
            case Step.NextGroup:
                for (int g = group + 1; g < sizes.Count; g++)
                    if (sizes[g] > 0) return new Move(MoveKind.Select, g, Math.Min(index, sizes[g] - 1));
                return new Move(MoveKind.Edge);
            case Step.PreviousGroup:
                for (int g = group - 1; g >= 0; g--)
                    if (sizes[g] > 0) return new Move(MoveKind.Select, g, Math.Min(index, sizes[g] - 1));
                return new Move(MoveKind.Edge);
        }
        return new Move(MoveKind.Edge);
    }
}
