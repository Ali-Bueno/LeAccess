using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using UnityEngine;
using SlotState = IdolsItemContainer.IdolSlotState;

internal static class IdolTests
{
    internal static void Run(Action<bool, string> check)
    {
        void Observe() { InventoryAccessibility.Update(); InventoryAccessibility.Update(); }
        string T(string key, params object[] args) => ModText.Format(key, args);
        string J(params string[] fragments) => ScreenContentTracker.Combine(fragments);
        string Last() => ScreenReader.Messages.LastOrDefault();
        ModText.SetLanguage("en");
        UIBase.instance = null;
        MerchantItemReader.Context = null;
        var savedActions = LE.UI.Tooltips.ItemTooltipPromptHelper.Actions.ToList();
        LE.UI.Tooltips.ItemTooltipPromptHelper.Actions.Clear();
        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Update();
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();

        // A 5x5 grid, y=4 on top; Position is an idol's bottom-left cell.
        ItemContainerEntry Idol(string name, string type, int x, int y, int width, int height) => new()
        {
            data = new ItemDataUnpacked { Id = name, FullName = name, LocalisedBaseTypeName = type },
            Position = new() { x = x, y = y }, size = new() { x = width, y = height }
        };
        var a = Idol("Small idol of haste", "Small idol", 0, 4, 1, 1);
        var b = Idol("Humble idol of fire", "Humble idol", 2, 4, 2, 1);
        var c = Idol("Stout idol of frost", "Stout idol", 1, 2, 1, 2);
        var d = Idol("Adorned idol of storms", "Adorned idol", 2, 1, 2, 2);
        var slots = new IdolsItemContainer { Id = ContainerID.IDOLS, Type = ContainerType.EQUIPPED, Size = new() { x = 5, y = 5 },
            Slots = new() };
        foreach (var idol in new[] { a, b, c, d })
            for (int x = idol.Position.x; x < idol.Position.x + idol.size.x; x++)
                for (int y = idol.Position.y; y < idol.Position.y + idol.size.y; y++) slots.Slots[(x, y)] = idol;
        foreach (var cell in new[] { (4, 4), (0, 0), (4, 0) }) slots.States[cell] = SlotState.Blocked;
        foreach (var cell in new[] { (1, 0), (2, 0), (3, 0) }) slots.States[cell] = SlotState.Locked;
        slots.Refracted.Add((3, 4));
        slots.Refracted.Add((4, 2));
        var grid = new IdolsContainerUI { container = slots, currentlySelectedItem = a, currentFocusSlot = new() { x = 0, y = 4 } };
        int Free() => Cells().Count(cell => slots.GetSlotState(cell.x, cell.y, out _) == SlotState.Unlocked &&
            !slots.Slots.ContainsKey(cell));
        int Locked() => Cells().Count(cell => slots.GetSlotState(cell.x, cell.y, out _) == SlotState.Locked);
        IEnumerable<(int x, int y)> Cells() =>
            Enumerable.Range(0, slots.Size.x).SelectMany(x => Enumerable.Range(0, slots.Size.y).Select(y => (x, y)));
        string Name(ItemContainerEntry idol) => J(((ItemDataUnpacked)idol.data).FullName, ((ItemDataUnpacked)idol.data).LocalisedBaseTypeName);

        // Item-list navigation over the idols, keeping the grid's native exits.
        ItemContainersManager.Instance = new ItemContainersManager();
        bool Move(Direction direction) => InventoryNavigation.TryNavigate(grid, direction);
        void Focus(ItemContainerEntry idol, int x, int y) { grid.currentlySelectedItem = idol; grid.currentFocusSlot = new() { x = x, y = y }; }
        check(Move(Direction.RIGHT) && grid.currentlySelectedItem == b && grid.currentFocusSlot.x == 2 && grid.currentFocusSlot.y == 4,
            "Right steps from one equipped idol to the next in reading order");
        check(Move(Direction.RIGHT) && grid.currentlySelectedItem == c && grid.currentFocusSlot.x == 1 && grid.currentFocusSlot.y == 3,
            "A tall idol is reached once, at its top cell");
        check(Move(Direction.DOWN) && grid.currentlySelectedItem == d && grid.currentFocusSlot.x == 2 && grid.currentFocusSlot.y == 2,
            "Down jumps to the first idol of the next row");
        int focused = grid.Focused.Count;
        check(Move(Direction.RIGHT) && grid.Focused.Count == focused && grid.currentlySelectedItem == d,
            "The last idol repeats instead of wrapping like the native grid");
        check(!Move(Direction.DOWN) && grid.currentFocusSlot.x == 2 && grid.currentFocusSlot.y == 0,
            "Down past the last row lets the game leave from the bottom row, toward the crafting buttons");
        Focus(a, 0, 4);
        check(!Move(Direction.UP) && grid.currentFocusSlot.x == 0 && grid.currentFocusSlot.y == slots.Size.y - 1,
            "Up from the top row lets the game leave to the idol altar");
        Focus(b, 2, 4);
        check(Move(Direction.LEFT) && grid.currentlySelectedItem == a, "Left steps back to the previous idol");
        check(!Move(Direction.LEFT) && grid.currentFocusSlot.x == 0 && grid.currentFocusSlot.y == 4,
            "Left from the first idol lets the game leave from the left column, back to the equipment");
        check(InventoryNavigation.TryGetPosition(grid, c, out int place, out int count) && place == 3 && count == 4,
            "Each idol knows its place among the equipped idols");
        ItemContainersManager.Instance.cursor.Holding = true;
        check(!Move(Direction.RIGHT), "A held idol keeps the game's own placement navigation");
        ItemContainersManager.Instance.cursor.Holding = false;
        var plain = new ItemContainerUI { container = slots };
        check(!InventoryNavigation.TryNavigate(plain, Direction.RIGHT), "Only the idol grid itself gets the idol list");

        // Entering the grid names it with its counts before the first idol.
        Focus(a, 0, 4);
        NavigationManager.currentlySelected = grid;
        Observe();
        string summary = J(T("idols.grid"), T("idols.equipped_many", 4), T("idols.free_many", Free()), T("idols.locked_many", Locked()));
        check(Last() == J(summary, Name(a), T("idols.size", 1, 1), T("list.position", 1, 4)),
            "Entering the grid speaks its name, idols, free and locked slots, then the first idol: " + Last());
        Focus(b, 2, 4);
        Observe();
        check(Last() == J(Name(b), T("idols.size", 2, 1), T("idols.on_refracted"), T("list.position", 2, 4)),
            "The next idol speaks its size, refracted slot and place without repeating the summary: " + Last());
        Focus(b, 0, 0);
        Observe();
        check(ScreenReader.Messages.Count == 2, "A hover refresh that moves only the logical cell stays silent");

        // Empty cells say what they are from the game's slot states.
        foreach (var (x, y, key) in new[] { (0, 3, "idols.free_slot"), (4, 2, "idols.free_refracted_slot"), (2, 0, "idols.locked_slot"),
            (4, 4, "idols.no_slot") })
        {
            Focus(null, x, y);
            Observe();
            check(Last() == T(key), $"Empty cell {x},{y} reads {key}: " + Last());
        }

        // Leaving and coming back names the grid again; R3 reads an equipped idol's full details.
        var other = new ItemContainerUI { container = new() { Id = ContainerID.INVENTORY, Type = ContainerType.INVENTORY } };
        NavigationManager.currentlySelected = other;
        Observe();
        Focus(c, 1, 3);
        NavigationManager.currentlySelected = grid;
        Observe();
        check(Last() == J(summary, Name(c), T("idols.size", 1, 2), T("list.position", 3, 4)), "Re-entering the grid repeats its summary: " + Last());
        ItemTooltipModelReader.Text = "Stout idol of frost. +12% frost resistance";
        InventoryAccessibility.Update(readDetails: true);
        check(Last() == ItemTooltipModelReader.Text && ItemTooltipModelReader.LastItem == c.data && ItemTooltipModelReader.LastContainer == grid,
            "The details gesture reads the equipped idol's full tooltip");
        ItemTooltipModelReader.Text = null;

        // Counts use singular forms and leave out locked slots once none remain.
        slots.Slots.Clear();
        slots.Slots[(0, 4)] = a;
        slots.States.Clear();
        slots.States[(0, 0)] = SlotState.Locked;
        Focus(a, 0, 4);
        NavigationManager.currentlySelected = other;
        Observe();
        NavigationManager.currentlySelected = grid;
        Observe();
        check(Last().StartsWith(J(T("idols.grid"), T("idols.equipped_one"), T("idols.free_many", Free()), T("idols.locked_one"))),
            "One idol and one locked slot use the singular forms: " + Last());
        slots.States.Clear();
        NavigationManager.currentlySelected = other;
        Observe();
        NavigationManager.currentlySelected = grid;
        Observe();
        check(Last() == J(T("idols.grid"), T("idols.equipped_one"), T("idols.free_many", Free()), Name(a), T("idols.size", 1, 1)),
            "A grid without locked slots does not mention them: " + Last());

        // The altar slot above the grid is named and read like any item slot.
        var altarEntry = new ItemContainerEntry { data = new ItemDataUnpacked { Id = "altar", FullName = "Pyramidal altar", LocalisedBaseTypeName = "Ancient altar" } };
        var altar = new IdolsInventoryContainerUI
        {
            container = new() { Id = ContainerID.IDOLS_INVENTORY, Type = ContainerType.EQUIPPED, Size = new() { x = 1, y = 1 },
                Slots = new() { [(0, 0)] = altarEntry } },
            currentlySelectedItem = altarEntry
        };
        NavigationManager.currentlySelected = altar;
        Observe();
        check(Last() == J(T("idols.altar"), "Pyramidal altar", "Ancient altar"), "The altar slot speaks its name and its altar: " + Last());
        altar.currentlySelectedItem = null; altar.container.Slots.Clear();
        Observe();
        check(Last() == J(T("idols.altar"), T("inventory.empty_slot")), "An empty altar slot is named");

        // The equipment slots from which Right reaches the altar or the grid say so.
        var manager = new NavigationManager();
        NavigationManager.instance = manager;
        manager.Registered[NavigableID.IDOLS_CONTAINER] = grid;
        manager.Registered[NavigableID.IDOL_ALTAR] = altar;
        ItemContainerUI Slot(ContainerID id, string name) => new()
        {
            identifier = id,
            container = new() { Id = id, Type = ContainerType.EQUIPPED, Entry = new() { data = new ItemDataUnpacked { Id = name, FullName = name } } }
        };
        var amulet = Slot(ContainerID.EQ_AMULET, "Amulet of light");
        var offhand = Slot(ContainerID.EQ_OFFHAND, "Oak shield");
        var gloves = Slot(ContainerID.EQ_GLOVES, "Leather gloves");
        var cells = new Il2CppSystem.Array { Cells = new Il2CppSystem.Object[3, 3] };
        var rects = new Il2CppSystem.Array { Cells = new Il2CppSystem.Object[3, 3] };
        void Place(ItemContainerUI slot, int x, int y) { cells.Cells[x, y] = slot; rects.Cells[x, y] = new RectTransform(slot.gameObject); }
        Place(amulet, 2, 0); Place(offhand, 2, 1); Place(gloves, 0, 1);
        var equipment = new EquippedItemsNavigable { navigables = cells, rects = rects };
        NavigationManager.currentlySelected = equipment;
        string Worn(ContainerID id, string name) => J(EquipmentSlotText.Read(id), name, T("inventory.equipped"));
        void Select(int x, int y) { equipment.currentlySelectedIdx = new() { x = x, y = y }; Observe(); }
        Select(2, 0);
        check(Last() == J(Worn(ContainerID.EQ_AMULET, "Amulet of light"), T("idols.route_altar")),
            "The top row's last slot says Right reaches the idol altar: " + Last());
        Select(2, 1);
        check(Last() == J(Worn(ContainerID.EQ_OFFHAND, "Oak shield"), T("idols.route_grid")),
            "The second row's last slot says Right reaches the idols: " + Last());
        Select(0, 1);
        check(Last() == Worn(ContainerID.EQ_GLOVES, "Leather gloves"), "A slot with another slot to its right gives no route");
        rects.Cells[1, 1] = rects.Cells[2, 1] = rects.Cells[0, 1];
        cells.Cells[2, 1] = null;
        Select(0, 2); Select(0, 1);
        check(Last() == J(Worn(ContainerID.EQ_GLOVES, "Leather gloves"), T("idols.route_grid")),
            "Cells of the same slot to the right are skipped, as the game's step does: " + Last());
        Place(offhand, 2, 1);
        grid.gameObject.Visible = false;
        Select(0, 2); Select(2, 1);
        check(Last() == Worn(ContainerID.EQ_OFFHAND, "Oak shield"), "A hidden idol grid is not offered");
        grid.gameObject.Visible = true;
        manager.Registered.Clear();
        Select(0, 2); Select(2, 0);
        check(Last() == Worn(ContainerID.EQ_AMULET, "Amulet of light"), "An unregistered altar is not offered");
        manager.Registered[NavigableID.IDOLS_CONTAINER] = grid;
        cells.Cells[0, 2] = gloves; rects.Cells[0, 2] = new RectTransform(new GameObject());
        Select(0, 2);
        check(!Last().Contains(T("idols.route_grid")) && !Last().Contains(T("idols.route_altar")),
            "Lower rows lead to the crafting buttons and give no idol route");

        NavigationManager.currentlySelected = null;
        NavigationManager.instance = null;
        InventoryAccessibility.Update();
        InventoryAccessibility.Reset();
        ItemContainersManager.Instance = null;
        LE.UI.Tooltips.ItemTooltipPromptHelper.Actions.AddRange(savedActions);
    }
}
