using LeAccess.Accessibility.Inventory;
using UnityEngine;

internal static class InventoryPromptTests
{
    internal static void Run(Action<bool, string> check)
    {
        var previousUi = UIBase.instance;
        var previousSelected = NavigationManager.currentlySelected;
        var item = new ItemDataUnpacked { Id = "current-item" };
        var container = new ItemContainerUI { currentlySelectedItem = new ItemContainerEntry { data = item } };
        var opener = new GameObject();
        opener.transform.parent = container.transform;
        var parameters = new TooltipItemManager.ItemTooltipParameters
        {
            _Item_k__BackingField = item,
            _Opener_k__BackingField = opener,
            _SlotType_k__BackingField = TooltipItemManager.SlotType.INVENTORY
        };
        NavigationManager.currentlySelected = container;
        var owner = new ItemTooltipPrompts();
        var prompt = new MultiButtonsActionPrompt();
        var tooltip = new UITooltipItem { _itemTooltipPrompts = owner };
        var manager = new TooltipItemManager { itemTooltip = tooltip, activeParameters = parameters };
        var system = new LE.UI.Tooltips.TooltipSystem
        {
            _tooltipItemManager = manager,
            ActiveItemTooltipRect = manager.ActiveTooltipRect
        };
        UIBase.instance = new UIBase { TooltipSystem = system };
        check(InventoryPromptReader.CanRead(prompt), "Ordinary menu prompts retain their existing eligibility");
        prompt.transform.parent = owner.transform;
        check(InventoryPromptReader.CanRead(prompt), "Ready visible primary tooltip owns its legacy prompts");

        // Installed prefab starts every action active; the native constructor starts CanUse=true.
        manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.SetContent;
        check(prompt.CanUse && !InventoryPromptReader.CanRead(prompt),
            "Usable-looking prefab actions stay silent before the native SetPrompts stage finishes");
        manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.Completed;
        system.IsItemTooltipReady = false;
        check(!InventoryPromptReader.CanRead(prompt), "Outer tooltip readiness also gates legacy action hints");
        system.IsItemTooltipReady = true;
        manager.active = false;
        check(!InventoryPromptReader.CanRead(prompt), "Inactive manager cannot expose its pooled legacy prompts");
        manager.active = true;
        tooltip.tooltipActive = false;
        check(!InventoryPromptReader.CanRead(prompt), "Inactive tooltip cannot expose active prefab action children");
        tooltip.tooltipActive = true;
        tooltip.contentVisible = false;
        check(!InventoryPromptReader.CanRead(prompt), "Hidden item content also hides its action hints");
        tooltip.contentVisible = true;
        tooltip.content.Visible = false;
        check(!InventoryPromptReader.CanRead(prompt), "Native content flag cannot override hidden content hierarchy");
        tooltip.content.Visible = true;

        var pooledOwner = new ItemTooltipPrompts();
        prompt.transform.parent = pooledOwner.transform;
        check(!InventoryPromptReader.CanRead(prompt), "Active pooled or comparison prompt owner cannot claim current tooltip");
        prompt.transform.parent = owner.transform;
        system.ActiveItemTooltipRect = new GameObject().transform;
        check(!InventoryPromptReader.CanRead(prompt), "Modern or other active tooltip rectangle excludes legacy prompts");
        system.ActiveItemTooltipRect = manager.ActiveTooltipRect;
        owner.isActiveAndEnabled = false;
        check(!InventoryPromptReader.CanRead(prompt), "Disabled legacy prompt owner is rejected");
        owner.isActiveAndEnabled = true;
        owner.WasCollected = true;
        check(!InventoryPromptReader.CanRead(prompt), "Collected prompt owner is rejected before Unity null comparison");
        owner.WasCollected = false;
        UIBase.instance.WasCollected = true;
        check(!InventoryPromptReader.CanRead(prompt), "Collected UI base cannot grant ownership to surviving prompts");
        UIBase.instance.WasCollected = false;
        prompt.WasCollected = true;
        check(!InventoryPromptReader.CanRead(prompt), "Collected prompt is rejected before native component lookup");
        prompt.WasCollected = false;
        check(InventoryPromptReader.CanRead(prompt), "Current ready prompt resumes after transient lifecycle gates clear");
        container.currentlySelectedItem = null;
        check(!InventoryPromptReader.CanRead(prompt), "Empty focused slot cannot replay the prior ready item's actions");
        container.currentlySelectedItem = new ItemContainerEntry { data = new ItemDataUnpacked { Id = "new-selection" } };
        check(!InventoryPromptReader.CanRead(prompt), "A new item cannot inherit the prior item's still-ready action hints");
        container.currentlySelectedItem.data = item;
        parameters._Opener_k__BackingField = new GameObject();
        check(!InventoryPromptReader.CanRead(prompt), "A matching item with an opener in another container cannot own hints");
        parameters._Opener_k__BackingField = opener;
        parameters._SlotType_k__BackingField = TooltipItemManager.SlotType.GROUND;
        check(!InventoryPromptReader.CanRead(prompt), "Ground item parameters cannot supply inventory action hints");
        parameters._SlotType_k__BackingField = TooltipItemManager.SlotType.INVENTORY;
        container.gameObject.Visible = false;
        check(!InventoryPromptReader.CanRead(prompt), "Hidden selected inventory container cannot supply action hints");
        container.gameObject.Visible = true;
        NavigationManager.currentlySelected = new ItemContainerUI();
        check(!InventoryPromptReader.CanRead(prompt), "Switching focus to another container invalidates old action hints");
        UIBase.instance = previousUi;
        NavigationManager.currentlySelected = previousSelected;
    }
}
