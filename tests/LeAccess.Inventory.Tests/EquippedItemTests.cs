using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using UnityEngine;

internal static class EquippedItemTests
{
    internal static void Run(Action<bool, string> check)
    {
        void Observe() { InventoryAccessibility.Update(); InventoryAccessibility.Update(); }
        void Reset() { InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); }
        string T(string key, params object[] args) => ModText.Format(key, args);
        string J(params string[] fragments) => ScreenContentTracker.Combine(fragments);
        ModText.SetLanguage("en");
        UIBase.instance = null;
        var helmetItem = new ItemDataUnpacked { Id = "helmet", FullName = "Crown of fire", LocalisedBaseTypeName = "Iron helmet" };
        var glovesItem = new ItemDataUnpacked { Id = "gloves", FullName = "Leather gloves" };
        var helmet = new ItemContainerUI
        {
            identifier = ContainerID.EQ_HELMET,
            container = new() { Id = ContainerID.EQ_HELMET, Type = ContainerType.EQUIPPED, Entry = new() { data = helmetItem } }
        };
        var gloves = new ItemContainerUI
        {
            identifier = ContainerID.EQ_GLOVES,
            container = new() { Id = ContainerID.EQ_GLOVES, Type = ContainerType.EQUIPPED, Entry = new() { data = glovesItem } }
        };
        var grid = new Il2CppSystem.Array { Cells = new Il2CppSystem.Object[3, 2] };
        grid.Cells[2, 0] = helmet;
        grid.Cells[0, 1] = gloves;
        var highlight = new GameObject();
        var rects = new Il2CppSystem.Array { Cells = new Il2CppSystem.Object[3, 2] };
        rects.Cells[2, 0] = new RectTransform(highlight);
        var equipment = new EquippedItemsNavigable
        { navigables = grid, rects = rects, currentlySelectedIdx = new() { x = 2, y = 0 } };
        NavigationManager.currentlySelected = equipment;
        Reset();
        check(InventoryAccessibility.HasActiveContent && InventorySelectionReader.GetFocusedContainer() == helmet,
            "Native [x,y] equipment array resolves the selected slot even when its local hover entry is absent");
        check(InventoryAccessibility.TryHandleFocus(highlight) && InventoryAccessibility.TryHandleFocus(helmet.gameObject),
            "Equipment reader owns the exact native focus rectangle and slot container");
        check(!InventoryAccessibility.TryHandleFocus(gloves.gameObject) && !InventoryAccessibility.TryHandleFocus(new GameObject()),
            "Other equipment slots and unrelated controls cannot claim selected equipment focus");
        Observe();
        check(ScreenReader.Messages.Single() == J(T("inventory.slot_helmet"), helmetItem.FullName, helmetItem.LocalisedBaseTypeName, T("inventory.equipped")),
            "Equipped navigation reads localized slot, actual worn item and equipped status");
        Observe();
        check(ScreenReader.Messages.Count == 1, "Stable equipment focus remains silent");
        equipment.currentlySelectedIdx = new() { x = 0, y = 1 };
        Observe();
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_gloves"), glovesItem.FullName, T("inventory.equipped")), "Navigation follows the next native selected cell");
        equipment.currentlySelectedIdx = new() { x = 2, y = 0 };
        Observe();
        check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Contains(helmetItem.FullName),
            "Returning to a worn slot rereads its current item");
        var replacement = new ItemDataUnpacked { Id = "replacement", FullName = "Oak crown" };
        helmet.currentlySelectedItem = new() { data = helmetItem };
        helmet.container.Entry = new() { data = replacement };
        Observe();
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), replacement.FullName, T("inventory.equipped")),
            "Changing equipment in place reads live slot contents instead of the retained old hover item");
        check(!InventorySelectionReader.OwnsSelection(helmetItem, helmet) && InventorySelectionReader.OwnsSelection(replacement, helmet),
            "Item action and detail ownership follows the current live equipment entry");
        ItemTooltipModelReader.Text = "Oak crown. Armor +12";
        InventoryAccessibility.Update(readDetails: true);
        check(ItemTooltipModelReader.LastItem == replacement && ItemTooltipModelReader.LastContainer == helmet &&
            ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), ItemTooltipModelReader.Text), "Manual equipped details use the actual selected slot item");
        helmet.container.Entry = null;
        Observe();
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), T("inventory.empty_slot")), "Removed equipment announces the named empty slot despite old hover data");
        InventoryAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), T("inventory.empty_slot")), "Empty manual equipment reading cannot duplicate its slot label");
        helmet.container = null;
        Observe();
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), T("inventory.selection_unavailable")), "Unavailable equipment data is never called empty");
        helmet.container = new() { Id = ContainerID.EQ_GLOVES, Type = ContainerType.EQUIPPED, Entry = new() { data = glovesItem } };
        check(!InventorySelectionReader.TryGetEntry(helmet, out _), "A foreign backing container cannot masquerade as the selected slot");
        helmet.container.Id = ContainerID.EQ_HELMET;
        helmet.container.Type = ContainerType.INVENTORY;
        check(!InventorySelectionReader.TryGetEntry(helmet, out _), "Equipment slot cannot read a backing inventory container");
        helmet.container.Type = ContainerType.EQUIPPED;
        helmet.container.Entry = new() { data = helmetItem };
        Observe();
        check(ScreenReader.Messages.Last().Contains(helmetItem.FullName), "Same slot recovers when authoritative equipment data becomes available");
        helmet.container.Entry.data = null;
        Observe();
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), T("inventory.selection_unavailable")), "An existing entry with missing item data is unknown rather than empty");
        helmet.container.Entry.data = new ItemData { Id = "packed", Unpacked = new ItemDataUnpacked { Id = "foreign", FullName = "Foreign crown" } };
        Observe();
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), T("inventory.selection_unavailable")), "Unpacked data with a different native identity is never spoken");
        helmet.container.Entry.data = new ItemDataUnpacked { Id = "pending name" };
        Observe();
        check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), T("inventory.selection_unavailable")), "A not-yet-named item does not announce only an equipped marker");
        helmet.container.Entry.data = helmetItem;
        var priorGrid = equipment.navigables;
        equipment.navigables = null;
        check(!InventoryAccessibility.HasActiveContent, "Missing native equipment grid cannot reuse an old selected slot");
        equipment.navigables = priorGrid;
        highlight.Visible = false;
        check(!InventoryAccessibility.TryHandleFocus(highlight), "Hidden equipment highlight does not claim visible focus");
        highlight.Visible = true;
        foreach (string gate in new[] { "owner hidden", "owner disabled", "owner collected", "container hidden", "container disabled",
            "container collected", "array collected", "cell collected", "bad rank", "negative x", "negative y", "past x", "past y", "empty cell", "wrong cell type" })
        {
            equipment.gameObject.Visible = gate != "owner hidden";
            equipment.isActiveAndEnabled = gate != "owner disabled";
            equipment.WasCollected = gate == "owner collected";
            helmet.gameObject.Visible = gate != "container hidden";
            helmet.isActiveAndEnabled = gate != "container disabled";
            helmet.WasCollected = gate == "container collected" || gate == "cell collected";
            grid.WasCollected = gate == "array collected";
            grid.Rank = gate == "bad rank" ? 1 : 2;
            equipment.currentlySelectedIdx = new()
            {
                x = gate == "negative x" ? -1 : gate == "past x" ? 3 : 2,
                y = gate == "negative y" ? -1 : gate == "past y" ? 2 : 0
            };
            grid.Cells[2, 0] = gate == "empty cell" ? null : gate == "wrong cell type" ? new GameObject() : helmet;
            Reset(); Observe();
            check(!InventoryAccessibility.HasActiveContent && ScreenReader.Messages.Count == 0, "Equipment visibility/lifetime/bounds guard: " + gate);
        }
        equipment.currentlySelectedIdx = new() { x = 2, y = 0 };
        grid.Cells[2, 0] = helmet;
        Reset();
        foreach (string gate in new[] { "entry", "data", "source" })
        {
            helmet.container.Entry.WasCollected = gate == "entry";
            helmetItem.WasCollected = gate == "data";
            helmet.container.WasCollected = gate == "source";
            Observe();
            check(ScreenReader.Messages.Last() == J(T("inventory.slot_helmet"), T("inventory.selection_unavailable")), "Collected equipment data remains unknown: " + gate);
        }
        helmet.container.WasCollected = false;
        helmet.container.Entry.WasCollected = false;
        helmetItem.WasCollected = false;
        foreach (string gate in new[] { "loading", "unfocused", "tutorial", "dialogue", "controller menu" })
        {
            LE.UI.LoadingScreen.IsVisible = gate == "loading";
            Application.isFocused = gate != "unfocused";
            LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = gate == "tutorial";
            LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = gate == "dialogue";
            LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = gate == "controller menu";
            Reset(); InventoryAccessibility.Update(readDetails: true);
            check(!InventoryAccessibility.HasActiveContent && ScreenReader.Messages.Count == 0, "Equipped automatic/manual foreground guard: " + gate);
        }
        LE.UI.LoadingScreen.IsVisible = false; Application.isFocused = true;
        LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = false;
        LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
        LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = false;
        foreach (ContainerID id in Enum.GetValues<ContainerID>().Where(InventorySelectionReader.IsEquipmentSlot))
            check(!string.IsNullOrWhiteSpace(EquipmentSlotText.Read(id)), "Named equipment fallback exists for " + id);
        check(EquipmentSlotText.Read(ContainerID.INVENTORY) == null, "Ordinary inventory never acquires an equipment slot label");
        check(EquipmentSlotText.Read(ContainerID.EQ_LEFTRING) == T("inventory.left_slot", T("inventory.slot_ring")) &&
            EquipmentSlotText.Read(ContainerID.EQ_RIGHTRING) == T("inventory.right_slot", T("inventory.slot_ring")), "Two worn ring slots have distinct identities");
        ModText.SetLanguage("es");
        check(EquipmentSlotText.Read(ContainerID.EQ_HELMET) == T("inventory.slot_helmet") &&
            EquipmentSlotText.Read(ContainerID.EQ_LEFTRING) == T("inventory.left_slot", T("inventory.slot_ring")),
            "Equipment fallback slot and ring side follow Spanish localization");
        Localization.Texts["EquipmentType_HELMET"] = "YELMO DEL JUEGO";
        check(EquipmentSlotText.Read(ContainerID.EQ_HELMET) == Localization.Texts["EquipmentType_HELMET"], "Native game translation takes priority over mod fallback");
        Localization.Texts["EquipmentType_HELMET"] = "EquipmentType_HELMET";
        check(EquipmentSlotText.Read(ContainerID.EQ_HELMET) == T("inventory.slot_helmet"), "Unresolved game localization keys never leak into speech");
        Localization.Texts.Clear(); ModText.SetLanguage("en");
        Reset();
        var action = new LE.UI.PanelSystem.PanelActionPrompt { Label = "Unequip", ActionName = "unequip" };
        LE.UI.Tooltips.ItemTooltipPromptHelper.Actions.Add(action);
        LeAccess.Accessibility.Announcements.InputBindingText.Bindings["unequip"] = "A";
        Observe();
        check(ScreenReader.Messages.Single().EndsWith("A, " + action.Label), "Native item actions work while navigation owner is equipment grid");
        LE.UI.Tooltips.ItemTooltipPromptHelper.OnRead = () => equipment.currentlySelectedIdx = new() { x = 0, y = 1 };
        Time.unscaledTime += 1;
        Reset(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == 0, "Selection changing during action construction cannot speak stale equipped item");
        LE.UI.Tooltips.ItemTooltipPromptHelper.OnRead = null;
        LE.UI.Tooltips.ItemTooltipPromptHelper.Actions.Clear();
        equipment.currentlySelectedIdx = new() {x=2,y=0};
        ItemTooltipModelReader.Text = "Crown of fire. Armor +12";
        foreach (string gate in new[] {"modal", "dialogue", "controller", "loading", "unfocused"})
        {
            Reset();
            ItemTooltipModelReader.OnRead = () =>
            {
                LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = gate == "modal";
                LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = gate == "dialogue";
                LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = gate == "controller";
                LE.UI.LoadingScreen.IsVisible = gate == "loading";
                Application.isFocused = gate != "unfocused";
            };
            InventoryAccessibility.Update(readDetails:true);
            check(ScreenReader.Messages.Count == 0 && !InventoryAccessibility.HasPendingContent,
                "Foreground changing during manual equipment model construction cancels stale output: " + gate);
            ItemTooltipModelReader.OnRead = null;
            LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = false;
            LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
            LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = false;
            LE.UI.LoadingScreen.IsVisible = false;Application.isFocused = true;
        }
        ItemTooltipModelReader.Text = null;
        NavigationManager.currentlySelected = null;
        Reset();
    }
}
