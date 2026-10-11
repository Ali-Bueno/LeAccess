using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using Box = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.Box;
using Cell = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.Cell;
using MoveKind = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.MoveKind;

internal static class NavigationTests
{
    internal static void Run(Action<bool, string> check)
    {
        RunPlan(check);

        ItemContainerEntry Item(int x, int y, int width = 1, int height = 1) =>
            new() { Position = new() { x = x, y = y }, size = new() { x = width, y = height } };
        var a = Item(0, 6, height: 2);
        var b = Item(3, 7);
        var c = Item(1, 4, 2, 3);
        var d = Item(5, 6);
        var e = Item(2, 0);
        var slots = new Dictionary<(int, int), ItemContainerEntry>();
        foreach (var entry in new[] { a, b, c, d, e })
            for (int x = entry.Position.x; x < entry.Position.x + entry.size.x; x++)
                for (int y = entry.Position.y; y < entry.Position.y + entry.size.y; y++) slots[(x, y)] = entry;
        var backpack = new ItemContainerUI
        {
            container = new() { Type = ContainerType.INVENTORY, Id = ContainerID.INVENTORY, Slots = slots },
            currentlySelectedItem = a, currentFocusSlot = new() { x = 0, y = 6 }
        };
        ItemContainersManager.Instance = new ItemContainersManager();
        bool Move(Direction direction) => InventoryNavigation.TryNavigate(backpack, direction);

        check(Move(Direction.RIGHT) && backpack.currentlySelectedItem == b && backpack.currentFocusSlot.x == 3 &&
            backpack.currentFocusSlot.y == 7, "A handled step focuses the next item at its top-left cell through the game's focus call");
        check(Move(Direction.DOWN) && backpack.currentlySelectedItem == c && backpack.currentFocusSlot.x == 1 &&
            backpack.currentFocusSlot.y == 6, "A tall item is reached once, from its top row");
        backpack.currentFocusSlot = new() { x = 0, y = 0 };
        check(Move(Direction.RIGHT) && backpack.currentlySelectedItem == d,
            "A hover refresh that moved only the logical slot does not change where the next step starts");
        int focused = backpack.Focused.Count;
        backpack.currentlySelectedItem = e; backpack.currentFocusSlot = new() { x = 2, y = 0 };
        check(Move(Direction.RIGHT) && backpack.Focused.Count == focused && backpack.currentlySelectedItem == e,
            "The last item repeats instead of wrapping to the first column");
        backpack.currentlySelectedItem = a; backpack.currentFocusSlot = new() { x = 0, y = 6 };
        check(!Move(Direction.UP) && backpack.currentFocusSlot.x == 0 && backpack.currentFocusSlot.y == 7,
            "Up from the top row lets the game leave from the item's column on the border row");
        check(InventoryNavigation.TryGetPosition(backpack, d, out int place, out int count) && place == 4 && count == 5,
            "Each item knows its place among the backpack's items");

        ItemContainersManager.Instance.cursor.Holding = true;
        check(!Move(Direction.RIGHT), "Moving a held item keeps the game's placement navigation");
        ItemContainersManager.Instance.cursor.Holding = false;
        backpack.singleSlotControllerNavigation = true;
        check(!Move(Direction.RIGHT), "Single-slot containers keep the game's navigation");
        backpack.singleSlotControllerNavigation = false;
        backpack.container.Id = ContainerID.UNDEFINED;
        check(!Move(Direction.RIGHT) && !InventoryNavigation.TryGetPosition(backpack, d, out _, out _),
            "Other containers keep the game's navigation and speak no position");
        foreach (var id in new[] { ContainerID.SHOP, ContainerID.SHOP_BUYBACK })
        {
            backpack.container.Id = id;
            backpack.currentlySelectedItem = a;
            check(Move(Direction.RIGHT) && backpack.currentlySelectedItem == b &&
                InventoryNavigation.TryGetPosition(backpack, d, out place, out count) && place == 4 && count == 5,
                $"The merchant's {id} grid uses the same item list and positions");
        }
        var faction = new LE.Factions.ShopContainerUI
        {
            container = new() { Type = ContainerType.SHOP, Id = ContainerID.SHOP, Slots = slots }, currentlySelectedItem = a
        };
        check(!InventoryNavigation.TryNavigate(faction, Direction.RIGHT), "Faction shops keep their own edge handling");
        backpack.container.Id = ContainerID.INVENTORY;

        var item = new ItemDataUnpacked { Id = "fourth", FullName = "Anillo", LocalisedBaseTypeName = "Anillo de oro" };
        d.data = item;
        backpack.currentlySelectedItem = d;
        NavigationManager.currentlySelected = backpack;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        string expected = "Anillo. Anillo de oro. " + LeAccess.I18n.ModText.Format("list.position", 4, 5);
        check(ScreenReader.Messages.LastOrDefault()?.StartsWith(expected) == true, "Backpack items end with their place: " +
            ScreenReader.Messages.LastOrDefault());
        backpack.currentlySelectedItem = e; e.data = new ItemDataUnpacked { Id = "fifth", FullName = "Llave" };
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        int spoken = ScreenReader.Messages.Count;
        check(Move(Direction.RIGHT), "A blocked step is handled");
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == spoken + 1 && ScreenReader.Messages.Last().StartsWith("Llave"),
            "A step that cannot go further repeats the current item");
        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Reset();
        ItemContainersManager.Instance = null;
    }

    // Native rows grow upward: row 0 is the bottom row.
    private static void RunPlan(Action<bool, string> check)
    {
        InventoryNavigationOrder.Move Plan(IReadOnlyList<Box> items, int current, Cell cursor, Direction direction,
            InventoryNavigationOrder.Edges exits = InventoryNavigationOrder.Edges.Up) =>
            InventoryNavigationOrder.Plan(items, current, cursor, direction, exits);
        bool Is(InventoryNavigationOrder.Move move, MoveKind kind, int index = -1) => move.Kind == kind && move.Index == index;

        // A bottom row of mixed heights, in reading order: spear 1x4, sword 1x3, shield 2x2, ring 1x1.
        var row = new List<Box> { new(1, 3, 1, 4), new(2, 2, 1, 3), new(3, 1, 2, 2), new(0, 0) };
        const int spear = 0, sword = 1, shield = 2, ring = 3;
        var bottom = new Cell(0, 0);
        // Left and right walk the reading order: every item once, the place moving by one (user report 2026-10-11).
        var visited = new List<int>();
        int at = spear;
        var move = Plan(row, at, new Cell(1, 3), Direction.RIGHT);
        for (int step = 0; step < 5 && move.Kind == MoveKind.Select; step++)
        {
            visited.Add(move.Index);
            at = move.Index;
            move = Plan(row, at, move.Cursor, Direction.RIGHT);
        }
        check(visited.SequenceEqual(new[] { sword, shield, ring }) && move.Kind == MoveKind.Stay,
            "Right walks the list in reading order, each item once, and stops at its end");
        visited.Clear();
        for (int step = 0; step < 5; step++)
        {
            move = Plan(row, at, row[at].TopLeft, Direction.LEFT);
            if (move.Kind != MoveKind.Select) break;
            visited.Add(move.Index);
            at = move.Index;
        }
        check(visited.SequenceEqual(new[] { shield, sword, spear }) && move.Kind == MoveKind.Stay,
            "Left walks the same list back to its start and stops there");
        move = Plan(row, spear, new Cell(1, 0), Direction.RIGHT);
        check(Is(move, MoveKind.Select, sword) && move.Cursor == new Cell(2, 2),
            "The cursor row inside a tall item does not change the next item; the cursor lands on its top-left cell");
        move = Plan(row, sword, new Cell(2, 0), Direction.LEFT);
        check(Is(move, MoveKind.Select, spear) && move.Cursor == new Cell(1, 3), "Left lands on the previous item's top-left cell");

        var tall = new List<Box> { new(0, 5, 1, 4), new(2, 4), new(2, 2), new(0, 1) };
        check(Is(Plan(tall, 0, new Cell(0, 2), Direction.RIGHT), MoveKind.Select, 1) &&
            Is(Plan(tall, 0, new Cell(0, 5), Direction.RIGHT), MoveKind.Select, 1),
            "A tall item's next item is the same from any of its rows");
        move = Plan(tall, 0, new Cell(0, 3), Direction.DOWN);
        check(Is(move, MoveKind.Select, 3) && move.Cursor == new Cell(0, 1), "Down keeps the column");
        move = Plan(tall, 3, new Cell(0, 1), Direction.UP);
        check(Is(move, MoveKind.Select, 0) && move.Cursor == new Cell(0, 2), "Up keeps the column and enters a tall item at its bottom");
        move = Plan(tall, 1, new Cell(2, 4), Direction.DOWN);
        check(Is(move, MoveKind.Select, 2) && move.Cursor == new Cell(2, 2), "Down scans the column to the next different item");
        move = Plan(row, ring, bottom, Direction.UP);
        check(Is(move, MoveKind.Select, spear) && move.Cursor == new Cell(1, 1),
            "With nothing above in the column, Up takes the nearest row's closest item");
        move = Plan(row, shield, new Cell(4, 1), Direction.UP);
        check(Is(move, MoveKind.Select, sword) && move.Cursor == new Cell(2, 2), "Up picks the smallest column distance");
        check(Is(Plan(row, sword, new Cell(2, 0), Direction.DOWN), MoveKind.Stay), "Down from the bottom row stays");
        check(Is(Plan(row, spear, new Cell(1, 3), Direction.UP), MoveKind.Exit), "Up from the top row leaves the grid");

        check(Is(Plan(row, -1, new Cell(4, 3), Direction.RIGHT), MoveKind.Select, sword) &&
            Is(Plan(row, -1, new Cell(2, 3), Direction.RIGHT), MoveKind.Select, sword) &&
            Is(Plan(row, -1, new Cell(2, 3), Direction.LEFT), MoveKind.Select, spear) &&
            Is(Plan(row, -1, new Cell(2, 3), Direction.DOWN), MoveKind.Select, sword),
            "An empty focused cell resumes from its own row and column");
        check(Is(Plan(row, spear, new Cell(1, 3), (Direction)9), MoveKind.Native), "Unknown directions fall back to the game");
        const InventoryNavigationOrder.Edges idolExits = InventoryNavigationOrder.Edges.Left | InventoryNavigationOrder.Edges.Up |
            InventoryNavigationOrder.Edges.Down;
        check(Is(Plan(row, spear, new Cell(1, 3), Direction.LEFT, idolExits), MoveKind.Exit) &&
            Is(Plan(row, ring, bottom, Direction.DOWN, idolExits), MoveKind.Exit) &&
            Is(Plan(row, ring, bottom, Direction.UP, idolExits), MoveKind.Select, spear),
            "A grid that the game leaves on several sides exits there when no item remains");
        check(Is(Plan(row, shield, new Cell(3, 1), Direction.RIGHT, idolExits), MoveKind.Select, ring) &&
            Is(Plan(row, ring, new Cell(0, 0), Direction.RIGHT, idolExits), MoveKind.Stay),
            "Sides that wrap natively step through the items and stay at the list end");
        var none = new List<Box>();
        check(Is(Plan(none, -1, new Cell(0, 4), Direction.RIGHT, idolExits), MoveKind.Stay) &&
            Is(Plan(none, -1, new Cell(0, 4), Direction.LEFT, idolExits), MoveKind.Exit),
            "An empty grid can still be left on its exit sides");
    }
}
