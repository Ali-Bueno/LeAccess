using HarmonyLib;
using LeAccess.Accessibility.Inventory;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(ItemContainerUI), nameof(ItemContainerUI.ProcessDropSubmit))]
internal static class InventoryDropPatch
{
    private static void Prefix(ItemContainerUI __instance)
    {
        InventoryDropFocus.BeforeDrop(__instance);
        Forge.ForgeInventoryNotices.BeforeDrop(__instance);
    }

    private static void Postfix(ItemContainerUI __instance)
    {
        InventoryDropFocus.AfterDrop(__instance);
        Forge.ForgeInventoryNotices.AfterDrop(__instance);
    }
}

// The native prologue has no relative branch, so the original may still run (reference/runtime-feedback.md).
[HarmonyPatch(typeof(ItemContainerUI), nameof(ItemContainerUI.HandleNavigateInput))]
internal static class InventoryNavigatePatch
{
    private static bool Prefix(ItemContainerUI __instance, Direction d)
    {
        InventoryDropFocus.CancelGuard();
        return !InventoryNavigation.TryNavigate(__instance, d);
    }
}
