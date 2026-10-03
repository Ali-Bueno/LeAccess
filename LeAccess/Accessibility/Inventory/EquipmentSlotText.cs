using System;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Inventory;

internal static class EquipmentSlotText
{
    internal static string Read(ContainerID id)
    {
        var (gameKey, fallbackKey) = id switch
        {
            ContainerID.EQ_HELMET => ("EquipmentType_HELMET", "inventory.slot_helmet"),
            ContainerID.EQ_ARMOR => ("EquipmentType_BODY_ARMOR", "inventory.slot_armor"),
            ContainerID.EQ_WEAPON => ("Cosmetics_Flyout_Title_Mainhand", "inventory.slot_weapon"),
            ContainerID.EQ_OFFHAND => ("Bazaar_MainPanel_EquippedPreview_OffHand", "inventory.slot_offhand"),
            ContainerID.EQ_GLOVES => ("EquipmentType_GLOVES", "inventory.slot_gloves"),
            ContainerID.EQ_BELT => ("EquipmentType_BELT", "inventory.slot_belt"),
            ContainerID.EQ_BOOTS => ("EquipmentType_BOOTS", "inventory.slot_boots"),
            ContainerID.EQ_RIGHTRING or ContainerID.EQ_LEFTRING => ("EquipmentType_RING", "inventory.slot_ring"),
            ContainerID.EQ_AMULET => ("EquipmentType_AMULET", "inventory.slot_amulet"),
            ContainerID.EQ_RELIC => ("EquipmentType_RELIC", "inventory.slot_relic"),
            _ => ((string)null, null)
        };
        if (gameKey is null) return null;
        string fallback = ModText.Get(fallbackKey);
        string text;
        try { text = TextUtils.CleanText(Localization.GetText(gameKey, fallback)); }
        catch (Exception) { text = fallback; }
        if (string.IsNullOrWhiteSpace(text) || text == gameKey) text = fallback;
        return id switch
        {
            ContainerID.EQ_LEFTRING => ModText.Format("inventory.left_slot", text),
            ContainerID.EQ_RIGHTRING => ModText.Format("inventory.right_slot", text),
            _ => text
        };
    }
}
