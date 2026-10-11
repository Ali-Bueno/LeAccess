using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.Inventory;

/// <summary>Plans item-grid moves over the cells the items cover; items are listed in reading order, rows top to bottom.</summary>
internal static class InventoryNavigationOrder
{
    internal enum MoveKind { Select, Stay, Exit, Native }
    /// <summary>Grid sides where the game's own step leaves the grid instead of wrapping.</summary>
    [Flags] internal enum Edges { None = 0, Left = 1, Up = 2, Right = 4, Down = 8 }
    /// <summary>A grid cell; native rows grow upward, so a higher Row reads first.</summary>
    internal readonly record struct Cell(int Column, int Row);
    /// <summary>An item by its top-left cell and size.</summary>
    internal readonly record struct Box(int Column, int Top, int Width = 1, int Height = 1)
    {
        internal int Right => Column + Width - 1;
        internal int Bottom => Top - Height + 1;
        internal Cell TopLeft => new(Column, Top);
        internal bool Spans(int row) => row <= Top && row >= Bottom;
        internal bool Covers(Cell cell) => Spans(cell.Row) && cell.Column >= Column && cell.Column <= Right;
    }
    /// <summary>The move, and for Select the cell the cursor lands on.</summary>
    internal readonly record struct Move(MoveKind Kind, int Index = -1, Cell Cursor = default);

    internal static int Compare(Box a, Box b) =>
        a.Top != b.Top ? b.Top.CompareTo(a.Top) : a.Column.CompareTo(b.Column);

    /// <summary>Left and right walk the reading order one item at a time, so "N of M" always moves by one; up and down
    /// scan the cursor's column to the next different item, then the nearest row.</summary>
    /// <param name="current">Index of the focused item, or -1 when the focused cell is empty.</param>
    /// <param name="cursor">The cell inside the focused item that the step starts from.</param>
    /// <param name="exits">Sides where running out of items leaves the grid; at the other sides the item repeats.</param>
    internal static Move Plan(IReadOnlyList<Box> items, int current, Cell cursor, Direction direction, Edges exits = Edges.Up)
    {
        var from = current >= 0 ? items[current] : new Box(cursor.Column, cursor.Row);
        if (!from.Covers(cursor)) cursor = from.TopLeft;
        Move End(Edges side) => new((exits & side) != 0 ? MoveKind.Exit : MoveKind.Stay, -1, cursor);
        return direction switch
        {
            Direction.RIGHT => Horizontal(items, current, from, 1) ?? End(Edges.Right),
            Direction.LEFT => Horizontal(items, current, from, -1) ?? End(Edges.Left),
            Direction.DOWN => Vertical(items, current, from, cursor, -1) ?? End(Edges.Down),
            Direction.UP => Vertical(items, current, from, cursor, 1) ?? End(Edges.Up),
            _ => new Move(MoveKind.Native)
        };
    }

    // A row scan revisited tall items through their lower rows, so the place jumped back (user report 2026-10-11).
    private static Move? Horizontal(IReadOnlyList<Box> items, int current, Box from, int step)
    {
        int target = current >= 0 ? current + step : -1;
        if (current < 0)
            for (int i = step > 0 ? 0 : items.Count - 1; i >= 0 && i < items.Count; i += step)
                if (Math.Sign(Compare(items[i], from)) == step) { target = i; break; }
        return target >= 0 && target < items.Count ? new Move(MoveKind.Select, target, items[target].TopLeft) : null;
    }

    private static Move? Vertical(IReadOnlyList<Box> items, int current, Box from, Cell cursor, int step)
    {
        var (_, _, minRow, maxRow) = Extent(items);
        int first = step > 0 ? from.Top + 1 : from.Bottom - 1;
        for (int row = first; row >= minRow && row <= maxRow; row += step)
        {
            int hit = At(items, new Cell(cursor.Column, row));
            if (hit >= 0 && hit != current) return new Move(MoveKind.Select, hit, new Cell(cursor.Column, row));
        }
        // No item in the column: the nearest row with an item, taking the one closest to the column.
        for (int row = first; row >= minRow && row <= maxRow; row += step)
        {
            int best = -1, bestDistance = int.MaxValue;
            for (int i = 0; i < items.Count; i++)
            {
                if (i == current || !items[i].Spans(row)) continue;
                int distance = Math.Max(Math.Max(items[i].Column - cursor.Column, cursor.Column - items[i].Right), 0);
                if (distance < bestDistance) (best, bestDistance) = (i, distance);
            }
            if (best >= 0)
                return new Move(MoveKind.Select, best, new Cell(Math.Clamp(cursor.Column, items[best].Column, items[best].Right), row));
        }
        return null;
    }

    private static int At(IReadOnlyList<Box> items, Cell cell)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].Covers(cell)) return i;
        return -1;
    }

    private static (int MinColumn, int MaxColumn, int MinRow, int MaxRow) Extent(IReadOnlyList<Box> items)
    {
        var extent = (MinColumn: int.MaxValue, MaxColumn: int.MinValue, MinRow: int.MaxValue, MaxRow: int.MinValue);
        foreach (var item in items)
            extent = (Math.Min(extent.MinColumn, item.Column), Math.Max(extent.MaxColumn, item.Right),
                Math.Min(extent.MinRow, item.Bottom), Math.Max(extent.MaxRow, item.Top));
        return extent;
    }
}
