using LeAccess.Accessibility.Inventory;

internal static class PointerHoverTests
{
    internal static void Run(Action<bool, string> check)
    {
        var container = new ItemContainerUI { identifier = ContainerID.INVENTORY };
        bool controller = EpochInputManager.IsControllerActive;

        EpochInputManager.IsControllerActive = true;
        check(!InventoryPointerHover.Filter(container, true),
            "A hidden mouse entering the backpack in controller mode cannot mark it hovered");
        check(!InventoryPointerHover.Filter(container, false), "Clearing the hover stays allowed in controller mode");
        container.isHovered = true;
        check(!InventoryPointerHover.Filter(container, true), "A hover left from before is dropped on the next write");
        check(!InventoryPointerHover.Filter(null, true), "A missing container still cannot be hovered by the controller");

        EpochInputManager.IsControllerActive = false;
        check(InventoryPointerHover.Filter(container, true), "Mouse and keyboard keep the game's own hover");
        check(!InventoryPointerHover.Filter(container, false), "Mouse and keyboard keep the game's own unhover");

        EpochInputManager.IsControllerActive = controller;
    }
}
