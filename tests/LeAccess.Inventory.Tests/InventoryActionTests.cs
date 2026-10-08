using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LE.UI.PanelSystem;
using LE.UI.Tooltips;
using UnityEngine;

internal static class InventoryActionTests
{
    internal static void Run(Action<bool, string> check)
    {
        var item = new ItemDataUnpacked { Id = "test-actions", FullName = "Lanza", LocalisedBaseTypeName = "Lanza a dos manos" };
        var container = new ItemContainerUI { currentlySelectedItem = new() { data = item } };
        NavigationManager.currentlySelected = container;
        UIBase.instance = null;
        var equip = new PanelActionPrompt { Label = "Equipar", ActionName = "equip" };
        var drop = new PanelActionPrompt { Label = "Tirar", ActionName = "drop" };
        ItemTooltipPromptHelper.Actions = new() { equip, drop };
        InputBindingText.Bindings["equip"] = "Triángulo";
        InputBindingText.Bindings["drop"] = "Cuadrado";
        EpochInputManager.IsControllerActive = true;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Single() == "Lanza. Lanza a dos manos. Triángulo, Equipar. Cuadrado, Tirar",
            "Ordinary item navigation announces button first without a visible tooltip");
        int reads = ItemTooltipPromptHelper.Reads;
        for (int i = 0; i < 10; i++) InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == 1 && ItemTooltipPromptHelper.Reads == reads,
            "Steady focus neither repeats speech nor rebuilds presentation every frame");
        container.currentlySelectedItem = new() { data = new ItemDataUnpacked { Id = "test-actions", FullName = "Lanza", LocalisedBaseTypeName = "Lanza a dos manos" } };
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == 1, "An online sync replacing the focused item's wrappers does not read it again");
        container.currentlySelectedItem = new() { data = new ItemDataUnpacked { Id = "next", FullName = "Escudo" } };
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Last() == "Escudo. Triángulo, Equipar. Cuadrado, Tirar",
            "Moving to the next item repeats its available controls even when actions are unchanged");
        drop.CanUse = false; Time.unscaledTime += 1;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Last() == "Escudo. Triángulo, Equipar", "Unavailable actions disappear from item navigation");
        MerchantItemReader.Context = "Precio de compra: 100 oro"; Time.unscaledTime += 1;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Last() == "Escudo. Precio de compra: 100 oro. Triángulo, Equipar",
            "Item merchant context and native action are integrated into one focus announcement");
        container.currentlySelectedItem = null;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Last() == LeAccess.I18n.ModText.Get("inventory.empty_slot"), "Empty cells clear previous merchant prices and buttons");
        container.currentlySelectedItem = new() { data = item };
        equip.Controls = PanelAction.ControlType.MouseAndKeyboard;
        check(!InventoryActionReader.ReadForItem(item, container).Any(), "Other device actions cannot leak through model fallback");
        EpochInputManager.IsControllerActive = false;
        check(InventoryActionReader.ReadForItem(item, container).Single() == "Triángulo, Equipar", "Matching device action uses provider binding");
        equip.MouseAndKeyboardButtonNamesOverride = new() { Values = new[] { "Shift", "Right click" } };
        equip.IsHoldAction = true;
        check(InventoryActionReader.ReadForItem(item, container).Single() == LeAccess.I18n.ModText.Format("input.hold", "Shift + Right click") + ", Equipar",
            "Explicit native overrides and hold are retained without invoking actions");
        equip.MouseAndKeyboardButtonNamesOverride.Values = new[] { "Shift", "" };
        check(InventoryActionReader.ReadForItem(item, container).Single() == "Equipar", "Incomplete chord never fabricates a binding");
        ItemTooltipModelReader.ParametersAvailable = false;
        check(!InventoryActionReader.ReadForItem(item, container).Any(), "Unknown slot context remains silent");
        ItemTooltipModelReader.ParametersAvailable = true;
        check(!InventoryActionReader.ReadForItem(new ItemDataUnpacked(), container).Any(), "Foreign item cannot supply prompts");
        ItemTooltipPromptHelper.OnRead = () => ItemTooltipModelReader.ParametersAvailable = false;
        check(!InventoryActionReader.ReadForItem(item, container).Any(), "Slot context lost during model reading discards action hints");
        ItemTooltipModelReader.ParametersAvailable = true;
        ItemTooltipPromptHelper.OnRead = () => NavigationManager.currentlySelected = null;
        check(!InventoryActionReader.ReadForItem(item, container).Any(), "Selection change while reading discards stale prompts");
        ItemTooltipPromptHelper.OnRead = null; ItemTooltipPromptHelper.Actions.Clear();
        MerchantItemReader.Context = null; EpochInputManager.IsControllerActive = true;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
    }
}
