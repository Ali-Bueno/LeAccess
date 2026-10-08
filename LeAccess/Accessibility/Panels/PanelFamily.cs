using Il2CppInterop.Runtime.InteropTypes;
using LE.UI.PanelSystem;

namespace LeAccess.Accessibility.Panels;

internal enum PanelKind { None, Faction, FactionRank, Gambling, Vendor, Crafting, KeysTrader, Eternity, TimeBeast, LootFilter, Mastery }

/// <summary>Which of the faction, vendor and item-service panels a game panel is.</summary>
/// <remarks>See reference/faction-vendor-panels.md.</remarks>
internal static class PanelFamily
{
    internal static PanelKind Identify(Panel panel)
    {
        bool Is<T>() where T : Il2CppObjectBase => panel.TryCast<T>() is not null;
        if (Is<FactionsPanel>() || Is<ChooseItemFactionPanel>()) return PanelKind.Faction;
        if (Is<CircleOfFortuneRankPanel>() || Is<MerchantsGuildRankPanel>() || Is<ForgottenKnightsRankPanel>() ||
            Is<WeaverRankPanel>()) return PanelKind.FactionRank;
        if (Is<GoldGamblingPanel>() || Is<SoulGamblingPanel>() || Is<CircleOfFortuneGamblingPanel>() ||
            Is<MerchantsGuildGamblingPanel>()) return PanelKind.Gambling;
        if (Is<HunterVendorPanel>() || Is<WeaverVendorPanel>() || Is<TimeglassVendorPanel>()) return PanelKind.Vendor;
        if (Is<ExchangeWovenEchoesPanel>() || Is<GlyphOfInsightReplacementPanel>() || Is<RandomizeLegendaryPotentialPanel>() ||
            Is<RerollUniqueAffixesPanel>() || Is<ShatterSetItemsPanel>() || Is<SwapAttributesPanel>() ||
            Is<IdolEnchantmentPanel>()) return PanelKind.Crafting;
        if (Is<DungeonKeysTraderPanel>()) return PanelKind.KeysTrader;
        if (Is<EternityCachePanel>()) return PanelKind.Eternity;
        if (Is<TimeBeastEvolutionPanel>()) return PanelKind.TimeBeast;
        if (Is<LootFilterPanel>()) return PanelKind.LootFilter;
        if (Is<ChooseMasteryPanel>()) return PanelKind.Mastery;
        return PanelKind.None;
    }

    /// <summary>A vendor or gambler panel's stock and buyback containers, with the slot type its tooltips use.</summary>
    internal static bool TryVendor(Panel panel, out ItemContainerUI stock, out ItemContainerUI buyback,
        out TooltipItemManager.SlotType slot)
    {
        stock = buyback = null;
        slot = TooltipItemManager.SlotType.UNDEFINED;
        if (panel.TryCast<GoldGamblingPanel>() is { } gold)
        { stock = gold._gambling; buyback = gold._buyback; slot = TooltipItemManager.SlotType.GAMBLING; }
        else if (panel.TryCast<SoulGamblingPanel>() is { } soul)
        { stock = soul._soulGambling; buyback = soul._buyback; slot = TooltipItemManager.SlotType.SOUL_GAMBLING; }
        else if (panel.TryCast<CircleOfFortuneGamblingPanel>() is { } fortune)
        { stock = fortune._factionGamblingCoF; buyback = fortune._buyback; slot = TooltipItemManager.SlotType.FACTION_GAMBLING; }
        else if (panel.TryCast<MerchantsGuildGamblingPanel>() is { } guild)
        { stock = guild._factionGamblingMG; buyback = guild._buyback; slot = TooltipItemManager.SlotType.FACTION_GAMBLING; }
        else if (panel.TryCast<HunterVendorPanel>() is { } hunter)
        { stock = hunter._hunterVendor; buyback = hunter._buyback; slot = TooltipItemManager.SlotType.HUNTER_VENDOR; }
        else if (panel.TryCast<WeaverVendorPanel>() is { } weaver)
        { stock = weaver._weaverVendor; buyback = weaver._buyback; slot = TooltipItemManager.SlotType.WEAVER_VENDOR; }
        else if (panel.TryCast<TimeglassVendorPanel>() is { } timeglass)
        { stock = timeglass._shopContainerUI; buyback = timeglass._buyback; slot = TooltipItemManager.SlotType.TIMEGLASS_VENDOR; }
        return slot != TooltipItemManager.SlotType.UNDEFINED;
    }
}
