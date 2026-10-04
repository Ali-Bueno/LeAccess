using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;
using static LeAccess.Accessibility.Navigation.GroupedGridOrder;

namespace LeAccess.Accessibility.Passives;

/// <summary>Plans passive-grid moves as columns left to right, each column top to bottom.</summary>
internal static class PassiveNavigationOrder
{
    internal enum MoveKind { Select, Stay, Exit, Native }
    internal readonly record struct Move(MoveKind Kind, int Column = -1, int Row = -1);
    /// <summary>A cell's column ordinal among the non-empty columns, its index in that column and the column's size.</summary>
    internal readonly record struct Place(int Group, int Index, int Size);

    // Native rows grow upward, so within a column the higher row reads first.
    private static bool Before((int Column, int Row) cell, int column, int row) =>
        cell.Column < column || cell.Column == column && cell.Row > row;

    internal static int IndexOf(IReadOnlyList<(int Column, int Row)> order, int column, int row)
    {
        for (int i = 0; i < order.Count; i++)
            if (order[i].Column == column && order[i].Row == row) return i;
        return -1;
    }

    // The order lists a column's cells together, so each column is a run of equal Column values.
    private static List<int> ColumnSizes(IReadOnlyList<(int Column, int Row)> order)
    {
        var sizes = new List<int>();
        for (int i = 0; i < order.Count; i++)
            if (i > 0 && order[i].Column == order[i - 1].Column) sizes[sizes.Count - 1]++;
            else sizes.Add(1);
        return sizes;
    }

    internal static Place? PlaceOf(IReadOnlyList<(int Column, int Row)> order, int column, int row)
    {
        int index = IndexOf(order, column, row);
        if (index < 0) return null;
        var sizes = ColumnSizes(order);
        int start = 0;
        for (int group = 0; group < sizes.Count; start += sizes[group], group++)
            if (index < start + sizes[group]) return new Place(group, index - start, sizes[group]);
        return null;
    }

    /// <summary>Down/Up stay inside the column, Right/Left go to the same place in the next/previous column.</summary>
    internal static Move Plan(IReadOnlyList<(int Column, int Row)> order, int column, int row, Direction direction)
    {
        if (direction is not (Direction.DOWN or Direction.UP or Direction.RIGHT or Direction.LEFT)) return new Move(MoveKind.Native);
        return PlaceOf(order, column, row) is { } place
            ? PlanFrom(order, place, direction) : PlanFromEmptyCell(order, column, row, direction);
    }

    private static Move PlanFrom(IReadOnlyList<(int Column, int Row)> order, Place place, Direction direction)
    {
        var sizes = ColumnSizes(order);
        var step = direction switch
        {
            Direction.DOWN => Step.NextInGroup,
            Direction.UP => Step.PreviousInGroup,
            Direction.RIGHT => Step.NextGroup,
            _ => Step.PreviousGroup
        };
        var move = GroupedGridOrder.Plan(sizes, place.Group, place.Index, step);
        if (move.Kind == GroupedGridOrder.MoveKind.Edge) return new Move(direction == Direction.RIGHT ? MoveKind.Stay : MoveKind.Exit);
        int start = 0;
        for (int group = 0; group < move.Group; group++) start += sizes[group];
        return Select(order[start + move.Index]);
    }

    // A cursor left on an empty cell resumes from its place: next or previous node in reading order, top of a neighbouring column.
    private static Move PlanFromEmptyCell(IReadOnlyList<(int Column, int Row)> order, int column, int row, Direction direction)
    {
        int following = 0;
        while (following < order.Count && Before(order[following], column, row)) following++;
        int preceding = following - 1;
        switch (direction)
        {
            case Direction.DOWN:
                return following < order.Count ? Select(order[following]) : new Move(MoveKind.Exit);
            case Direction.UP:
                return preceding >= 0 ? Select(order[preceding]) : new Move(MoveKind.Exit);
            case Direction.RIGHT:
                foreach (var cell in order) if (cell.Column > column) return Select(cell);
                return new Move(MoveKind.Stay);
            default:
                int target = -1;
                foreach (var cell in order) if (cell.Column < column) target = cell.Column;
                if (target < 0) return new Move(MoveKind.Exit);
                foreach (var cell in order) if (cell.Column == target) return Select(cell);
                return new Move(MoveKind.Native);
        }
    }

    private static Move Select((int Column, int Row) cell) => new(MoveKind.Select, cell.Column, cell.Row);
}
