using HarmonyLib;
using LeAccess.Accessibility.Items;
using LE.UI.Tooltips;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(ItemTooltipModifier), nameof(ItemTooltipModifier.SetIcon))]
internal static class ItemComparisonIconPatch
{
    private static void Postfix(ItemTooltipModifier __instance, ItemTooltipAssetList.ItemTooltipModifierIconSprite iconSprite) =>
        ItemComparisonIcons.Set(__instance, iconSprite);
}

[HarmonyPatch(typeof(ItemTooltipModifier), nameof(ItemTooltipModifier.OnDataUpdate))]
internal static class ItemModifierDataPatch
{
    private static void Postfix(ItemTooltipModifier __instance) => ItemComparisonIcons.Clear(__instance);
}
