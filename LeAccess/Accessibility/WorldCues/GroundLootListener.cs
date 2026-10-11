namespace LeAccess.Accessibility.WorldCues;

internal static class GroundLootListener
{
    /// <summary>Dropped gold, items, potions, tomes and ancient bones have their own loops; their listeners are never objects.</summary>
    internal static bool Owns(WorldObjectClickListener listener) =>
        listener.GetComponent<GroundGoldVisuals>() is not null || listener.GetComponent<GroundItemVisuals>() is not null ||
        listener.GetComponent<GroundPotionVisuals>() is not null || listener.GetComponent<GroundXPTomeVisuals>() is not null ||
        listener.GetComponent<GroundFavorTomeVisuals>() is not null || listener.GetComponent<GroundAncientBoneVisuals>() is not null;
}
