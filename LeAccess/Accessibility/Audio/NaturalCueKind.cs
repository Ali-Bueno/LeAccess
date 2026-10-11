namespace LeAccess.Accessibility.Audio;

internal enum NaturalCueKind
{
    Gold, Equipment, Chest, Stash, Portal, ZoneTransition, Discovery, UnexploredRoom, InsideUnexploredRoom,
    // Loop-only kinds: no short excerpt exists for them.
    Potion, Tome, MagicEquipment, RareEquipment, LegendaryEquipment, Rune, Gem, Scroll, QuestItem, Shrine, Nemesis
}
