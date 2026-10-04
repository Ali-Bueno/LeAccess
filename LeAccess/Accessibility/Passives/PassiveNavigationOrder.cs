using System.Collections.Generic;

namespace LeAccess.Accessibility.Passives;

/// <summary>Plans passive-grid moves in reading order: columns left to right, each column top to bottom.</summary>
internal static class PassiveNavigationOrder
{
    internal enum MoveKind { Select, Stay, Exit, Native }
    internal readonly record struct Move(MoveKind Kind, int Column = -1, int Row = -1);

    // Native rows grow upward, so within a column the higher row reads first.
    private static bool Before((int Column, int Row) cell, int column, int row) =>
        cell.Column < column || cell.Column == column && cell.Row > row;

    internal static int IndexOf(IReadOnlyList<(int Column, int Row)> order, int column, int row)
    {
        for (int i = 0; i < order.Count; i++)
            if (order[i].Column == column && order[i].Row == row) return i;
        return -1;
    }

    /// <summary>Down/Up step through the whole list, Right/Left jump to the first node of the next/previous column.</summary>
    internal static Move Plan(IReadOnlyList<(int Column, int Row)> order, int column, int row, Direction direction)
    {
        int index = IndexOf(order, column, row);
        int following = index + 1, preceding = index - 1;
        if (index < 0)
        {
            following = 0;
            while (following < order.Count && Before(order[following], column, row)) following++;
            preceding = following - 1;
        }
        switch (direction)
        {
            case Direction.DOWN:
                return following < order.Count ? Select(order[following]) : new Move(MoveKind.Exit);
            case Direction.UP:
                return preceding >= 0 ? Select(order[preceding]) : new Move(MoveKind.Exit);
            case Direction.RIGHT:
                foreach (var cell in order) if (cell.Column > column) return Select(cell);
                return new Move(MoveKind.Stay);
            case Direction.LEFT:
                int target = -1;
                foreach (var cell in order) if (cell.Column < column) target = cell.Column;
                if (target < 0) return new Move(MoveKind.Exit);
                foreach (var cell in order) if (cell.Column == target) return Select(cell);
                break;
        }
        return new Move(MoveKind.Native);
    }

    private static Move Select((int Column, int Row) cell) => new(MoveKind.Select, cell.Column, cell.Row);
}
