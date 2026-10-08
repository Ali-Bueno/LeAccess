using System;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Cosmetics;

/// <summary>Reads a focused cosmetics slot, skill row, class filter or category tab from the game's own components.</summary>
/// <remarks>See reference/cosmetics-reading.md.</remarks>
internal static class CosmeticsReader
{
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        try
        {
            if (!IsAlive(element)) return false;
            var skill = element.GetComponentInParent<CosmeticSkillSlot>();
            if (IsAlive(skill)) { text = SkillRow(skill); return true; }
            var slot = element.GetComponentInParent<CosmeticItemSlot>();
            if (IsAlive(slot)) { text = CosmeticsText.Slot(SlotName(slot), Equipped(slot), slot.locked); return true; }
            var classFilter = element.GetComponentInParent<MtxClassToggle>();
            if (IsAlive(classFilter)) { text = CosmeticsText.ClassFilter(ClassName(classFilter.Class), classFilter.Toggled); return true; }
            var tab = element.GetComponentInParent<MTXCategorySelection>();
            if (!IsAlive(tab) || !IsAlive(tab.categoryName)) return false;
            string name = TextUtils.CleanText(tab.categoryName.text);
            if (string.IsNullOrWhiteSpace(name)) return false;
            text = CosmeticsText.Tab(name, tab.isSelected);
            return true;
        }
        catch (Exception) { text = null; return false; }
    }

    private static string SkillRow(CosmeticSkillSlot slot)
    {
        string name = AbilityName(slot.ability);
        return CosmeticsText.Slot(string.IsNullOrWhiteSpace(name) ? SlotName(slot) : name, Equipped(slot), slot.locked);
    }

    // The slot's own localization key; its designer name is only the fallback.
    private static string SlotName(CosmeticItemSlot slot)
    {
        string fallback = TextUtils.CleanText(slot.cosmeticSlotName);
        string key = slot.cosmeticLocalizedKey;
        string shown = string.IsNullOrWhiteSpace(key) ? null : TextUtils.CleanText(Localization.GetText(key, fallback));
        if (!string.IsNullOrWhiteSpace(shown) && shown != key) return shown;
        return string.IsNullOrWhiteSpace(fallback) ? ModText.Get("cosmetics.slot") : fallback;
    }

    private static string Equipped(CosmeticItemSlot slot)
    {
        var cosmetic = slot._storedCosmetic;
        return IsAlive(cosmetic) ? TextUtils.CleanText(cosmetic.DisplayName) : null;
    }

    private static string AbilityName(Ability ability)
    {
        if (!IsAlive(ability)) return null;
        string raw = ability.abilityName;
        string key = LocalizationSystem.Conventions.GetAbilityNameKey(raw);
        return TextUtils.CleanText(string.IsNullOrWhiteSpace(key) ? raw : Localization.GetText(key, raw));
    }

    // The game localizes classes by their enum name.
    private static string ClassName(CharacterClassID id)
    {
        string name = id.ToString();
        var common = Localization.Common;
        return TextUtils.CleanText(common is null || common.WasCollected ? name : common.GetClass(name));
    }
}
