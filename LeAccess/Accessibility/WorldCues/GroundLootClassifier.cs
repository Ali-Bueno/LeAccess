using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.WorldCues;

/// <summary>The game's item predicates that decide a dropped item's loop.</summary>
internal readonly record struct LootTraits(bool Equipment, bool Idol, bool UniqueSetOrLegendary, bool Rare,
    bool Exalted, bool Magic, bool Rune, bool Glyph, bool AffixShard);

// Equipment and idols sound by rarity as in D2RAccess; other loot by type (reference/world-object-sounds.md).
internal static class GroundLootClassifier
{
    internal static NaturalCueKind Classify(LootTraits item)
    {
        if (item.Equipment || item.Idol)
            return item.UniqueSetOrLegendary ? NaturalCueKind.LegendaryEquipment
                : item.Rare || item.Exalted ? NaturalCueKind.RareEquipment
                : item.Magic ? NaturalCueKind.MagicEquipment
                : NaturalCueKind.Equipment;
        if (item.Rune) return NaturalCueKind.Rune;
        if (item.Glyph) return NaturalCueKind.Gem;
        if (item.AffixShard) return NaturalCueKind.Scroll;
        // Keys, lenses, woven echoes and other crafting or quest materials.
        return NaturalCueKind.QuestItem;
    }

    internal static bool IsGear(NaturalCueKind kind) => kind is NaturalCueKind.Equipment or
        NaturalCueKind.MagicEquipment or NaturalCueKind.RareEquipment or NaturalCueKind.LegendaryEquipment;

    internal static NaturalCueKind Classify(ItemData data) => Classify(new LootTraits(
        data.isEquipment(), data.isIdol(), data.isUniqueSetOrLegendary(), data.isRare(), data.isExaltedItem(),
        data.isMagic(), data.IsRune(), data.IsGlyph(), data.IsAffixShard()));
}
