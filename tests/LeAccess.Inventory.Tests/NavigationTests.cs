using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using Anchor = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.Anchor;
using MoveKind = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.MoveKind;

internal static class NavigationTests
{
    internal static void Run(Action<bool, string> check)
    {
        // Native rows grow upward: y=7 is the top row of the 14x8 backpack.
        var order = new List<Anchor> { new(0, 7), new(3, 7), new(1, 6), new(5, 6), new(2, 0) };
        InventoryNavigationOrder.Move Plan(int current, Direction direction, int column = -1, int top = -1) =>
            InventoryNavigationOrder.Plan(order, current, column, top, direction);
        InventoryNavigationOrder.Move Select(int index) => new(MoveKind.Select, index);
        var stay = new InventoryNavigationOrder.Move(MoveKind.Stay);
        check(Plan(0, Direction.RIGHT) == Select(1) && Plan(1, Direction.RIGHT) == Select(2) && Plan(2, Direction.LEFT) == Select(1),
            "Right and Left step item by item across rows in reading order");
        check(Plan(0, Direction.LEFT) == stay && Plan(4, Direction.RIGHT) == stay && Plan(4, Direction.DOWN) == stay,
            "The list ends stay put instead of wrapping around the grid");
        check(Plan(1, Direction.DOWN) == Select(2) && Plan(3, Direction.UP) == Select(0) && Plan(4, Direction.UP) == Select(2),
            "Down and Up jump to the first item of the next or previous row");
        check(Plan(1, Direction.UP) == new InventoryNavigationOrder.Move(MoveKind.Exit), "Up from the top row leaves the backpack");
        check(Plan(-1, Direction.RIGHT, 4, 7) == Select(2) && Plan(-1, Direction.LEFT, 4, 7) == Select(1) &&
            Plan(-1, Direction.DOWN, 4, 7) == Select(2) && Plan(-1, Direction.RIGHT, 9, 3) == Select(4),
            "An empty focused cell resumes from its place in the reading order");
        check(Plan(0, (Direction)9) == new InventoryNavigationOrder.Move(MoveKind.Native), "Unknown directions fall back to the game");
        var exit = new InventoryNavigationOrder.Move(MoveKind.Exit);
        const InventoryNavigationOrder.Edges idolExits = InventoryNavigationOrder.Edges.Left | InventoryNavigationOrder.Edges.Up |
            InventoryNavigationOrder.Edges.Down;
        InventoryNavigationOrder.Move Idol(int current, Direction direction) =>
            InventoryNavigationOrder.Plan(order, current, -1, -1, direction, idolExits);
        check(Idol(0, Direction.LEFT) == exit && Idol(4, Direction.DOWN) == exit && Idol(1, Direction.UP) == exit,
            "A grid that the game leaves on several sides exits there at the list's ends");
        check(Idol(4, Direction.RIGHT) == stay && Idol(1, Direction.LEFT) == Select(0), "Sides that wrap natively still repeat the item");
        check(InventoryNavigationOrder.Plan(new List<Anchor>(), -1, 0, 4, Direction.RIGHT, idolExits) == stay &&
            InventoryNavigationOrder.Plan(new List<Anchor>(), -1, 0, 4, Direction.LEFT, idolExits) == exit,
            "An empty grid can still be left on its exit sides");

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
}
