using System.Collections.Generic;

namespace LeAccess.Accessibility.Inventory;

/// <summary>Plans item-grid moves over items in reading order: rows top to bottom, each row left to right.</summary>
internal static class InventoryNavigationOrder
{
    internal enum MoveKind { Select, Stay, Exit, Native }
    /// <summary>Grid sides where the game's own step leaves the grid instead of wrapping.</summary>
    [System.Flags] internal enum Edges { None = 0, Left = 1, Up = 2, Right = 4, Down = 8 }
    internal readonly record struct Move(MoveKind Kind, int Index = -1);
    /// <summary>An item by its top-left cell; native rows grow upward, so a higher Top reads first.</summary>
    internal readonly record struct Anchor(int Column, int Top);

    internal static int Compare(Anchor a, Anchor b) =>
        a.Top != b.Top ? b.Top.CompareTo(a.Top) : a.Column.CompareTo(b.Column);

    /// <summary>Right/Left step through the items, Down/Up jump to the first item of the next/previous row.</summary>
    /// <param name="current">Index of the focused item in the order, or -1 when the focused cell is empty.</param>
    /// <param name="exits">Sides where the list's end leaves the grid; at the other ends the item repeats.</param>
    internal static Move Plan(IReadOnlyList<Anchor> ordered, int current, int column, int top, Direction direction,
        Edges exits = Edges.Up)
    {
        Move End(Edges side) => new((exits & side) != 0 ? MoveKind.Exit : MoveKind.Stay);
        if (current >= 0) (column, top) = (ordered[current].Column, ordered[current].Top);
        int following = current + 1, preceding = current - 1;
        if (current < 0)
        {
            following = 0;
            while (following < ordered.Count && Before(ordered[following], column, top)) following++;
            preceding = following - 1;
        }
        switch (direction)
        {
            case Direction.RIGHT:
                return following < ordered.Count ? new Move(MoveKind.Select, following) : End(Edges.Right);
            case Direction.LEFT:
                return preceding >= 0 ? new Move(MoveKind.Select, preceding) : End(Edges.Left);
            case Direction.DOWN:
                for (int i = 0; i < ordered.Count; i++)
                    if (ordered[i].Top < top) return new Move(MoveKind.Select, i);
                return End(Edges.Down);
            case Direction.UP:
                int above = -1;
                for (int i = 0; i < ordered.Count; i++)
                    if (ordered[i].Top > top) above = ordered[i].Top;
                if (above < 0) return End(Edges.Up);
                for (int i = 0; i < ordered.Count; i++)
                    if (ordered[i].Top == above) return new Move(MoveKind.Select, i);
                break;
        }
        return new Move(MoveKind.Native);
    }

    private static bool Before(Anchor item, int column, int top) =>
        item.Top > top || item.Top == top && item.Column < column;
}
