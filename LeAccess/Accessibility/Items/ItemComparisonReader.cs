using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.Tooltips;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using IconSprite = LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite;

namespace LeAccess.Accessibility.Items;

/// <summary>Reads the equipped-item comparison the game shows while its compare action is held.</summary>
internal static class ItemComparisonReader
{
    internal static bool TryRead(ItemData item, out string key, out string text)
    {
        key = text = null;
        var ui = UIBase.instance;
        var system = IsAlive(ui) ? ui.TooltipSystem : null;
        if (!IsAlive(system) || !system.isActiveAndEnabled) return false;
        var main = system._itemTooltip;
        var compared = system._itemTooltipComparison;
        if (!Visible(main) || !Visible(compared)) return false;
        var mainInfo = main._info;
        var comparedInfo = compared._info;
        var active = system._activeComparisonItemTooltipInfo;
        if (mainInfo is null || mainInfo.WasCollected || comparedInfo is null || comparedInfo.WasCollected ||
            active is null || active.WasCollected || comparedInfo.Pointer != active.Pointer ||
            !SameItem(mainInfo._Item_k__BackingField, item)) return false;

        string equipped = ScreenContentTracker.Combine(new[] { Label(compared._name), Line(compared._secondaryNameSection) });
        var differences = new List<string>();
        AddLines(differences, main._combinedDifferencesSection);
        AddLines(differences, compared._combinedDifferencesSection);
        var changes = new List<string>();
        AddChanges(changes, main);
        AddChanges(changes, compared);
        key = $"{mainInfo.Pointer}:{comparedInfo.Pointer}";
        text = ScreenContentTracker.Combine(new[] {
            string.IsNullOrWhiteSpace(equipped) ? ModText.Get("item.comparison") : ModText.Format("item.comparison_with", equipped),
            differences.Count == 0 ? null : ModText.Format("item.compare_differences", string.Join("; ", differences)),
            changes.Count == 0 ? null : ModText.Format("item.compare_changes", string.Join("; ", changes)),
            differences.Count == 0 && changes.Count == 0 ? ModText.Get("item.compare_no_changes") : null });
        return true;
    }

    private static void AddLines(List<string> lines, ItemTooltipModifiersSection section)
    {
        if (!IsAlive(section) || !UiVisibility.IsVisible(section.gameObject)) return;
        foreach (var modifier in Modifiers(section.gameObject))
        {
            string line = Line(modifier);
            if (line is not null) lines.Add(Describe(ItemComparisonIcons.Get(modifier), line) ?? line);
        }
    }

    // Per-line icons mark what the swap adds, removes, raises or lowers; unchanged lines stay out.
    private static void AddChanges(List<string> lines, ItemTooltip tooltip)
    {
        var combined = tooltip._combinedDifferencesSection;
        foreach (var modifier in Modifiers(tooltip.gameObject))
        {
            if (IsAlive(combined) && modifier.transform.IsChildOf(combined.transform)) continue;
            string line = Line(modifier);
            string change = line is null ? null : Describe(ItemComparisonIcons.Get(modifier), line);
            if (change is not null) lines.Add(change);
        }
    }

    private static string Describe(IconSprite? icon, string line) => icon switch
    {
        IconSprite.Compare_Added => ModText.Format("item.compare_added", line),
        IconSprite.Compare_Removed => ModText.Format("item.compare_removed", line),
        IconSprite.Compare_Increase or IconSprite.Compare_CombinedIncrease => ModText.Format("item.compare_increase", line),
        IconSprite.Compare_Decrease or IconSprite.Compare_CombinedDecrease => ModText.Format("item.compare_decrease", line),
        _ => null
    };

    private static IEnumerable<ItemTooltipModifier> Modifiers(GameObject root)
    {
        var modifiers = root.GetComponentsInChildren<ItemTooltipModifier>(false);
        if (modifiers is null || modifiers.WasCollected) yield break;
        foreach (var modifier in modifiers)
            if (IsAlive(modifier) && UiVisibility.IsVisible(modifier.gameObject)) yield return modifier;
    }

    private static string Line(ItemTooltipModifier modifier) => IsAlive(modifier) && UiVisibility.IsVisible(modifier.gameObject)
        ? Label(modifier._modifierText) : null;

    private static bool Visible(ItemTooltip tooltip) =>
        IsAlive(tooltip) && tooltip.isActiveAndEnabled && UiVisibility.IsVisible(tooltip.gameObject);

    private static bool SameItem(ItemData actual, ItemData expected) =>
        actual is not null && !actual.WasCollected && expected is not null && !expected.WasCollected &&
        (actual.Pointer == expected.Pointer || actual.IdEquals(expected));

    private static string Label(TMPro.TMP_Text label)
    {
        if (!TextUtils.IsVisibleLabel(label)) return null;
        string text = TextUtils.CleanText(label.text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
