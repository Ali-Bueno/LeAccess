using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using UnityEngine;

internal static class SpeechAcceptanceTests
{
    internal static void Run(Action<bool, string> check)
    {
        var item = new ItemDataUnpacked { Id = "acceptance-item", FullName = "Current sword" };
        var container = new ItemContainerUI { currentlySelectedItem = new() { data = item } };
        NavigationManager.currentlySelected = container;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        ScreenReader.OutputAccepted = false;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == 0 && InventoryAccessibility.HasPendingContent,
            "Rejected inventory output remains pending instead of acknowledging speech");
        ScreenReader.OutputAccepted = true;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Single() == "Current sword" && !InventoryAccessibility.HasPendingContent,
            "Recovered inventory output reads current selection exactly once");

        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        ItemTooltipModelReader.Text = "Current full properties";
        ScreenReader.OutputAccepted = false;
        InventoryAccessibility.Update(readDetails: true);
        check(InventoryAccessibility.HasPendingContent, "Rejected manual inventory details remain pending");
        ItemTooltipModelReader.Text = "Refreshed full properties";
        ScreenReader.OutputAccepted = true;
        ScreenReader.SpokeThisFrame = true;
        InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == 0 && InventoryAccessibility.HasPendingContent,
            "Automatic inventory details retry yields to output already accepted this frame");
        ScreenReader.SpokeThisFrame = false;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Single() == "Refreshed full properties",
            "Current manual inventory intent retries freshly read details after recovery");
        ScreenReader.Available = false;
        InventoryAccessibility.Update(readDetails: true);
        check(InventoryAccessibility.HasPendingContent, "Unavailable output keeps new manual intent pending after a previously accepted inventory brief");
        ScreenReader.Available = true;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last() == "Refreshed full properties",
            "Late inventory reader accepts a still-current manual request once");

        foreach (var gate in new[] { "changed", "hidden", "collected", "unready", "unavailable" })
        {
            InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
            ScreenReader.OutputAccepted = false;
            ScreenReader.Available = gate != "unavailable";
            InventoryAccessibility.Update(readDetails: true);
            if (gate == "changed") container.currentlySelectedItem = new() { data = new ItemDataUnpacked { Id = "next", FullName = "Next shield" } };
            else if (gate == "collected") container.WasCollected = true;
            else if (gate == "unready") container.container = null;
            else container.gameObject.Visible = false;
            ScreenReader.OutputAccepted = ScreenReader.Available = true;
            InventoryAccessibility.Update(); InventoryAccessibility.Update();
            check(!ScreenReader.Messages.Contains("Refreshed full properties") &&
                (gate == "changed" ? ScreenReader.Messages.Single() == "Next shield" :
                    gate == "unready" ? ScreenReader.Messages.Single() == "Item information is unavailable" : ScreenReader.Messages.Count == 0),
                "Failed manual inventory intent cannot replay after ownership loss: " + gate);
            container.gameObject.Visible = true;
            container.WasCollected = false;
            container.container = new();
            container.currentlySelectedItem = new() { data = item };
        }
        ItemTooltipModelReader.Text = null;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); NavigationManager.currentlySelected = null;
    }
}
