using LeAccess.Accessibility;
using LeAccess.Accessibility.Forge;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using UnityEngine;

/// <summary>What the inventory says about the forge beside it, and the forge reader on its focused item slot.</summary>
internal static class ForgeNoticeTests
{
    internal static void Run(Action<bool, string> check)
    {
        string T(string key) => ModText.Get(key);
        string F(string key, params object[] args) => ModText.Format(key, args);
        string Join(params string[] parts) => ScreenContentTracker.Combine(parts);
        ItemData Item(string name) => new() { Unpacked = new() { FullName = name } };

        var panelGo = new GameObject();
        var forge = panelGo.Add(new CraftingSlotManager());
        var mainUi = panelGo.Child().Add(new CraftingMainUI());
        var modifierUi = panelGo.Child().Add(new ItemContainerUI());
        var supportUi = panelGo.Child().Add(new ItemContainerUI());
        var panel = panelGo.Add(new ForgePanel { CraftingSlotManager = forge, _main = mainUi, _modifier = modifierUi, _support = supportUi });
        var panels = new OpenPanels();
        var ui = new UIBase { InventoryOpen = true, PanelSystem = panels };
        UIBase.instance = ui;
        ItemContainersManager.Instance = new ItemContainersManager();
        var leads = InventoryAccessibility.Leads;
        ForgeInventoryNotices.Reset(); leads.Clear(); ScreenReader.Messages.Clear();

        // Opening: one lead with the forge, its item and the hint; closing clears it.
        ForgeInventoryNotices.Observe();
        check(leads.Count == 0, "Without a forge panel the inventory leads nothing");
        panels.Open = panel;
        ForgeInventoryNotices.Observe();
        check(leads.Count == 1 && leads[0] == Join(T("forge.opened"), T("forge.item_empty"), T("forge.grid_hint")),
            "Opening an empty forge leads with the forge, its empty item slot and the grid hint: " + string.Join(" | ", leads));
        ForgeInventoryNotices.Observe(); ForgeInventoryNotices.Observe();
        check(leads.Count == 1, "An open forge does not lead again");
        panelGo.Active = false;
        ForgeInventoryNotices.Observe();
        check(leads.Count == 2 && leads[1] is null, "Closing the forge clears the lead");
        ForgeInventoryNotices.Observe();
        check(leads.Count == 2, "A closed forge does not clear it twice");
        forge.main.Put("Espada de hueso");
        panelGo.Active = true;
        ForgeInventoryNotices.Observe();
        check(leads.Count == 3 && leads[2] == Join(T("forge.opened"), F("forge.item", "Espada de hueso"), T("forge.grid_hint")),
            "Opening a forge that holds an item names it: " + leads[2]);
        ui.InventoryOpen = false;
        ForgeInventoryNotices.Observe();
        check(leads.Count == 4 && leads[3] is null, "Closing the inventory ends the forge lead too");
        ui.InventoryOpen = true;
        forge.main.Clear();

        // Controller drops from the inventory into the forge.
        var inventory = new ItemContainerUI();
        var axe = Item("Hacha");
        var entry = new ItemContainerEntry { data = axe };
        inventory.Container.Slots[(2, 1)] = entry;
        inventory.currentFocusSlot = new Vector2Int(2, 1);
        ForgeInventoryNotices.Reset(); leads.Clear();
        ForgeInventoryNotices.Observe(); leads.Clear();

        ForgeInventoryNotices.BeforeDrop(inventory);
        forge.main.Put(axe);
        ForgeInventoryNotices.AfterDrop(inventory);
        check(leads.Single() == F("forge.moved_in", "Hacha") && ScreenReader.Messages.Count == 0,
            "An item that lands in the forge's item slot leads the next read with its name");
        forge.main.Clear(); leads.Clear();
        ForgeInventoryNotices.BeforeDrop(inventory);
        forge.modifier.Put(Item("Fragmento de vida"));
        ForgeInventoryNotices.AfterDrop(inventory);
        check(leads.Single() == F("forge.moved_in", "Fragmento de vida"), "A move into the shard slot is reported by what arrived");
        forge.modifier.Clear(); leads.Clear();
        ForgeInventoryNotices.BeforeDrop(inventory);
        forge.support.Put(Item("Glifo de esperanza"));
        ForgeInventoryNotices.AfterDrop(inventory);
        check(leads.Single() == F("forge.moved_in", "Glifo de esperanza"), "A move into the glyph slot is reported by what arrived");
        forge.support.Clear(); leads.Clear();
        ForgeInventoryNotices.AfterDrop(inventory);
        check(leads.Count == 0 && ScreenReader.Messages.Count == 0, "An after-drop without a before-drop says nothing");

        // The game refuses a move silently: the item stays, the cursor is empty and the forge is unchanged.
        ForgeInventoryNotices.BeforeDrop(inventory);
        ForgeInventoryNotices.AfterDrop(inventory);
        check(ScreenReader.Messages.Single() is { Text: var refused, Interrupt: true } && refused == T("forge.move_refused") && leads.Count == 0,
            "A refused move with an empty forge says so");
        ScreenReader.Messages.Clear();
        forge.main.Put("Espada de hueso");
        ForgeInventoryNotices.BeforeDrop(inventory);
        ForgeInventoryNotices.AfterDrop(inventory);
        check(ScreenReader.Messages.Single().Text == F("forge.move_refused_holding", "Espada de hueso"),
            "A refused move names the item the forge already holds");
        ScreenReader.Messages.Clear();
        ItemContainersManager.Instance.cursor.Holding = true;
        ForgeInventoryNotices.BeforeDrop(inventory);
        ForgeInventoryNotices.AfterDrop(inventory);
        check(ScreenReader.Messages.Count == 0, "An item picked up onto the cursor is not a refusal");
        ItemContainersManager.Instance.cursor.Holding = false;
        ForgeInventoryNotices.BeforeDrop(inventory);
        inventory.Container.Slots.Remove((2, 1));
        ForgeInventoryNotices.AfterDrop(inventory);
        check(ScreenReader.Messages.Count == 0 && leads.Count == 0, "An item that left its slot elsewhere is not a refusal");
        inventory.Container.Slots[(2, 1)] = entry;

        // The forge's own slots, a closed forge and an empty slot are ignored.
        foreach (var own in new[] { panel._main, panel._modifier, panel._support })
        {
            own.Container.Slots[(0, 0)] = new ItemContainerEntry { data = Item("Dentro") };
            ForgeInventoryNotices.BeforeDrop(own);
            ForgeInventoryNotices.AfterDrop(own);
        }
        check(ScreenReader.Messages.Count == 0 && leads.Count == 0, "Drops inside the forge's own slot containers are ignored");
        inventory.currentFocusSlot = new Vector2Int(5, 5);
        ForgeInventoryNotices.BeforeDrop(inventory);
        ForgeInventoryNotices.AfterDrop(inventory);
        check(ScreenReader.Messages.Count == 0 && leads.Count == 0, "A focused empty slot is ignored");
        inventory.currentFocusSlot = new Vector2Int(2, 1);
        panelGo.Active = false;
        ForgeInventoryNotices.BeforeDrop(inventory);
        forge.main.Clear(); forge.main.Put(axe);
        ForgeInventoryNotices.AfterDrop(inventory);
        check(ScreenReader.Messages.Count == 0 && leads.Count == 0, "With the forge closed drops are not reported");
        panelGo.Active = true;
        ForgeInventoryNotices.Reset(); forge.main.Clear(); leads.Clear();

        // The forge's item slot focused (L2): the forge reader owns it and reads the held item with the cursor hint.
        var previous = NavigationManager.currentlySelected;
        NavigationManager.currentlySelected = mainUi;
        check(ForgeCellReader.TryGetForge(out var found) && ReferenceEquals(found, forge), "A focused forge item slot finds its forge");
        check(ForgeCellReader.TryRead(forge, out var cell) && cell.Brief == Join(T("forge.item_empty"), T("forge.cursor_hint")) &&
            ReferenceEquals(cell.Root, mainUi.gameObject), "An empty focused item slot reads empty, then the cursor hint: " + cell.Brief);
        forge.main.Put("Espada de hueso");
        check(ForgeCellReader.TryRead(forge, out cell) && cell.Brief == Join(F("forge.item", "Espada de hueso"), T("forge.item_hint"), T("forge.cursor_hint")),
            "A filled focused item slot reads the item, then the cursor hint: " + cell.Brief);
        var stray = new GameObject().Add(new CraftingMainUI());
        NavigationManager.currentlySelected = stray;
        check(!ForgeCellReader.TryGetForge(out _), "An item slot outside any forge finds nothing");
        NavigationManager.currentlySelected = previous;
        check(ForgeCellReader.NameOf(ForgeCellReader.Content(forge.main)) == "Espada de hueso" &&
            ForgeCellReader.Content(forge.modifier) is null && ForgeCellReader.NameOf(null) is null,
            "Content and name of a forge slot");
        forge.main.Clear();
        UIBase.instance = null; ItemContainersManager.Instance = null; leads.Clear(); ScreenReader.Messages.Clear();
    }
}
