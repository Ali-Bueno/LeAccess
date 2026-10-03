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
            var selected = element.GetComponentInParent<Button>();
            if (!IsAlive(selected) || !selected.isActiveAndEnabled) return false;
            var slot = element.GetComponentInParent<AffixSlotForge>();
            if (IsAlive(slot) && slot.isActiveAndEnabled)
            {
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
            }
            else
            {
                var shard = element.GetComponentInParent<ShardAffixListElement>();
                if (!IsAlive(shard) || !shard.isActiveAndEnabled || !IsAlive(shard.button) ||
                    !Same(selected, shard.button.GetComponent<Button>()) ||
                    !Within(shard.button, shard.gameObject) || !Within(element, shard.button)) return false;
                string name = Read(shard.tmpName, shard.gameObject);
                if (name == null) return false;
                text = ScreenContentTracker.Combine(new[] { name, Read(shard.tmpAffixType, shard.gameObject),
                    Quantity(shard.tmpQuantity, shard.gameObject),
                    includeDetails ? Read(shard.improvementTMP, shard.gameObject) : null });
            }
            unavailable = !selected.IsInteractable();
            return !string.IsNullOrWhiteSpace(text);
        }
        catch (System.Exception) { text = null; unavailable = false; return false; }
    }

    private static bool Same(Button first, Button second) => IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;

    private static bool Within(GameObject element, GameObject owner) =>
        IsAlive(element) && IsAlive(owner) && UiVisibility.IsVisible(owner) && element.transform.IsChildOf(owner.transform);

    private static string Read(TMP_Text label, GameObject owner)
    {
        string value = TextUtils.IsVisibleLabel(label) && Within(label.gameObject, owner) ? TextUtils.CleanText(label.text) : null;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Quantity(TMP_Text label, GameObject owner)
    {
        string value = Read(label, owner);
        return string.IsNullOrWhiteSpace(value) ? null : ModText.Format("crafting.shards", value);
    }
}
