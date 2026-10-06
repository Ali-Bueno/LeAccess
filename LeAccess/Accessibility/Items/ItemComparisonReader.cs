using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.Tooltips;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using IconSprite = LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite;

namespace LeAccess.Accessibility.Items;

/// <summary>Reads what the item tooltip shows while a tooltip key is held: the equipped-item comparison (L3/Ctrl),
/// the modifier explanations (R3/Alt) or the value ranges (both).</summary>
/// <remarks>See reference/item-comparison.md#controller-modes-2026-10-05.</remarks>
internal static class ItemComparisonReader
{
    /// <summary>What the last read saw of both tooltips, without item identities, for the runtime log.</summary>
    internal static string LastState { get; private set; }

    internal static bool TryRead(ItemData item, out string key, out string text)
    {
        key = text = null;
        var ui = UIBase.instance;
        var system = IsAlive(ui) ? ui.TooltipSystem : null;
        if (!IsAlive(system) || !system.isActiveAndEnabled) { LastState = "no tooltip system"; return false; }
        // The game draws its newer tooltip only behind a feature switch; otherwise the older one shows the item.
        if (TryReadModern(system, item, out key, out text, out string modern)) { LastState = $"v2 {modern}"; return true; }
        bool legacy = LegacyItemComparisonReader.TryRead(system._tooltipItemManager, item, out key, out text, out string older);
        // A tooltip still loading keeps the previous state, so moving between items does not flood the log.
        if (older != null) LastState = $"v2 {modern}; legacy {older}";
        return legacy;
    }

    private static bool TryReadModern(TooltipSystem system, ItemData item, out string key, out string text, out string state)
    {
        key = text = null;
        var main = system._itemTooltip;
        var mainInfo = Active(main) ? main._info : null;
        var activeInfo = system._activeItemTooltipInfo;
        if (mainInfo is null || mainInfo.WasCollected) { state = "hidden"; return false; }
        // The shown tooltip must be the game's current one, for the focused item.
        if (activeInfo is null || activeInfo.WasCollected || mainInfo.Pointer != activeInfo.Pointer) { state = "stale"; return false; }
        if (!SameItem(mainInfo._Item_k__BackingField, item)) { state = "other item"; return false; }
        var mode = mainInfo._TooltipMode_k__BackingField;
        state = $"mode={mode}";
        switch (mode)
        {
            case TooltipMode.CompareDefault:
            case TooltipMode.CompareModRanges:
                text = Comparison(system, main, out var comparedPointer);
                if (mode == TooltipMode.CompareModRanges) text = ScreenContentTracker.Combine(new[] { text, Ranges(main) });
                key = $"{mode}:{mainInfo.Pointer}:{comparedPointer}";
                return true;
            case TooltipMode.Explanation:
                text = Explanations(main);
                break;
            case TooltipMode.DefaultModRanges:
                text = Ranges(main);
                break;
            default:
                return false;
        }
        key = $"{mode}:{mainInfo.Pointer}";
        return true;
    }

    // With nothing equipped in that slot the game keeps the compare mode but hides the second tooltip.
    private static string Comparison(TooltipSystem system, ItemTooltip main, out System.IntPtr pointer)
    {
        pointer = System.IntPtr.Zero;
        var compared = system._itemTooltipComparison;
        var comparedInfo = Active(compared) ? compared._info : null;
        var active = system._activeComparisonItemTooltipInfo;
        if (comparedInfo is null || comparedInfo.WasCollected || active is null || active.WasCollected ||
            comparedInfo.Pointer != active.Pointer) return ModText.Get("item.compare_nothing");
        pointer = comparedInfo.Pointer;
        // The equipped item's name, then the game's "equipped in ..." tag.
        string equipped = ScreenContentTracker.Combine(new[] { Label(compared._name), Line(compared._secondaryNameSection),
            AnyText(compared._topTag) });
        var differences = new List<string>();
        AddLines(differences, main._combinedDifferencesSection);
        AddLines(differences, compared._combinedDifferencesSection);
        var changes = new List<string>();
        AddChanges(changes, main);
        AddChanges(changes, compared);
        return ScreenContentTracker.Combine(new[] {
            ComparedWith(equipped),
            differences.Count == 0 ? null : ModText.Format("item.compare_differences", string.Join("; ", differences)),
            changes.Count == 0 ? null : ModText.Format("item.compare_changes", string.Join("; ", changes)),
            differences.Count == 0 && changes.Count == 0 ? ModText.Get("item.compare_no_changes") : null });
    }

    // Explanation mode shows a sentence under each modifier line that has one.
    private static string Explanations(ItemTooltip tooltip)
    {
        var lines = new List<string>();
        foreach (var modifier in Modifiers(tooltip.gameObject))
        {
            string explanation = Label(modifier._explanationText);
            string line = Line(modifier);
            if (explanation != null) lines.Add(line == null ? explanation : ModText.Format("item.explained_line", line, explanation));
        }
        return ExplanationList(lines);
    }

    // Range modes show each rolled modifier's possible values beside it.
    private static string Ranges(ItemTooltip tooltip)
    {
        var lines = new List<string>();
        foreach (var modifier in Modifiers(tooltip.gameObject))
        {
            string range = Label(modifier._rangeText);
            string line = Line(modifier);
            if (range != null) lines.Add(line == null ? range : ModText.Format("item.ranged_line", line, range));
        }
        return RangeList(lines);
    }

    internal static string ComparedWith(string equipped) => string.IsNullOrWhiteSpace(equipped)
        ? ModText.Get("item.comparison") : ModText.Format("item.comparison_with", equipped);

    internal static string ExplanationList(List<string> lines) => lines.Count == 0
        ? ModText.Get("item.explanations_none") : ModText.Format("item.explanations", string.Join("; ", lines));

    internal static string RangeList(List<string> lines) => lines.Count == 0
        ? ModText.Get("item.ranges_none") : ModText.Format("item.ranges", string.Join("; ", lines));

    private static void AddLines(List<string> lines, ItemTooltipModifiersSection section)
    {
        if (!IsAlive(section) || !section.gameObject.activeInHierarchy) return;
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

    // Lines scrolled out of a long tooltip still count: the reader never depends on the visible screen area.
    private static IEnumerable<ItemTooltipModifier> Modifiers(GameObject root)
    {
        var modifiers = root.GetComponentsInChildren<ItemTooltipModifier>(false);
        if (modifiers is null || modifiers.WasCollected) yield break;
        foreach (var modifier in modifiers)
            if (IsAlive(modifier) && modifier.gameObject.activeInHierarchy) yield return modifier;
    }

    private static string Line(ItemTooltipModifier modifier) => IsAlive(modifier) && modifier.gameObject.activeInHierarchy
        ? Label(modifier._modifierText) : null;

    private static bool Active(ItemTooltip tooltip) => IsAlive(tooltip) && tooltip.isActiveAndEnabled;

    // Both tooltips keep the container entry's own object; ids can be empty and match unrelated items.
    internal static bool SameItem(ItemData actual, ItemData expected) =>
        actual is not null && !actual.WasCollected && expected is not null && !expected.WasCollected &&
        actual.Pointer == expected.Pointer;

    internal static string AnyText(Component owner)
    {
        if (!IsAlive(owner) || !owner.gameObject.activeInHierarchy) return null;
        return Label(owner.GetComponentInChildren<TMPro.TMP_Text>());
    }

    // Active, enabled, with text; position and clipping are ignored on purpose.
    internal static string Label(TMPro.TMP_Text label)
    {
        if (!IsAlive(label) || !label.isActiveAndEnabled) return null;
        string text = TextUtils.CleanText(label.text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
