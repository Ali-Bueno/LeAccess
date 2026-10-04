using HarmonyLib;
using LeAccess.Accessibility.Inventory;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(ItemContainerUI), nameof(ItemContainerUI.ProcessDropSubmit))]
internal static class InventoryDropPatch
{
    private static void Prefix(ItemContainerUI __instance) => InventoryDropFocus.BeforeDrop(__instance);
    private static void Postfix(ItemContainerUI __instance) => InventoryDropFocus.AfterDrop(__instance);
}

[HarmonyPatch(typeof(ItemContainerUI), nameof(ItemContainerUI.HandleNavigateInput))]
internal static class InventoryNavigatePatch
{
    private static void Prefix() => InventoryDropFocus.CancelGuard();
}
