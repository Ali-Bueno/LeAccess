using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class CraftingControlReader
{
    internal static bool TryRead(GameObject element, bool includeDetails, out string text, out bool unavailable)
    {
        text = null;
        unavailable = false;
        try
        {
            if (!UiVisibility.IsVisible(element)) return false;
            var shard = element.GetComponentInParent<ShardAffixListElement>();
            return IsAlive(shard) ? TryReadShard(element, shard, includeDetails, out text, out unavailable) :
                TryReadAffixSlot(element, out text, out unavailable);
        }
        catch (System.Exception) { text = null; unavailable = false; return false; }
    }

    private static bool TryReadAffixSlot(GameObject element, out string text, out bool unavailable)
    {
        text = null;
        unavailable = false;
        var selected = element.GetComponentInParent<Button>();
        if (!IsAlive(selected) || !selected.isActiveAndEnabled) return false;
        var slot = element.GetComponentInParent<AffixSlotForge>();
        if (!IsAlive(slot) || !slot.isActiveAndEnabled) return false;
        string action = null;
        if (Same(selected, slot.addAffixButton)) action = ModText.Get("crafting.add_affix");
        else
        {
            var upgrade = slot.upgradeButton;
            if (IsAlive(upgrade) && upgrade.isActiveAndEnabled && Same(selected, upgrade._upgradeButton) &&
                IsAlive(upgrade.affixSlotForge) && upgrade.affixSlotForge.Pointer == slot.Pointer)
                action = ModText.Get("crafting.upgrade_affix");
        }
        if (action == null || !Within(selected.gameObject, slot.gameObject)) return false;
        string name = Read(slot.nameTMP, slot.gameObject);
        text = ScreenContentTracker.Combine(new[] { action, name,
            name == null ? null : Read(slot.tierTMP, slot.gameObject),
            name == null ? null : Quantity(slot.countTMP, slot.gameObject) });
        unavailable = !selected.IsInteractable();
        return !string.IsNullOrWhiteSpace(text);
    }

    // The shard list highlights the row itself; its Button is a child of the row.
    private static bool TryReadShard(GameObject element, ShardAffixListElement shard, bool includeDetails,
        out string text, out bool unavailable)
    {
        text = null;
        unavailable = false;
        var button = IsAlive(shard.button) ? shard.button.GetComponent<Button>() : null;
        if (!shard.isActiveAndEnabled || !IsAlive(button) || !button.isActiveAndEnabled ||
            !Within(shard.button, shard.gameObject) ||
            element.Pointer != shard.gameObject.Pointer && !Within(element, shard.button)) return false;
        var nested = element.GetComponentInParent<Button>();
        if (IsAlive(nested) && !Same(nested, button)) return false;
        string name = Read(shard.tmpName, shard.gameObject);
        if (name == null) return false;
        string tooltip = null;
        // Selecting the row opens the game's affix tooltip with the row as its opener.
        if (includeDetails)
            Items.ItemTooltipReader.TryReadForOpener(shard.gameObject, TooltipItemManager.SlotType.AFFIX, out tooltip);
        text = ScreenContentTracker.Combine(new[] { name, Read(shard.tmpAffixType, shard.gameObject),
            Quantity(shard.tmpQuantity, shard.gameObject),
            includeDetails ? Read(shard.improvementTMP, shard.gameObject) : null, tooltip });
        unavailable = !button.IsInteractable();
        return !string.IsNullOrWhiteSpace(text);
    }

    private static bool Same(Button first, Button second) => IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;

    private static bool Within(GameObject element, GameObject owner) =>
        IsAlive(element) && IsAlive(owner) && UiVisibility.IsVisible(owner) && element.transform.IsChildOf(owner.transform);

    private static string Read(TMP_Text label, GameObject owner)
    {
        string value = TextUtils.IsVisibleLabel(label) && Within(label.gameObject, owner) ? TextUtils.CleanText(label.text) : null;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    // The shard list draws its count in brackets ("[12]", FormattingExtensions.WithBrackets).
    private static string Quantity(TMP_Text label, GameObject owner)
    {
        string value = Read(label, owner)?.Trim('[', ']', ' ');
        return string.IsNullOrWhiteSpace(value) ? null : ModText.Format("crafting.shards", value);
    }
}
