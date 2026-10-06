using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using LE.UI.Tooltips;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using static LeAccess.Accessibility.Items.ItemComparisonReader;
using Affixes = Il2CppSystem.Collections.Generic.List<UITooltipItemAffix>;

namespace LeAccess.Accessibility.Items;

/// <summary>The held tooltip modes on the older item tooltip, which the game draws while its newer one is switched off:
/// the comparison column and the affix rows, which carry explanations or ranges in those modes.</summary>
/// <remarks>See reference/item-comparison.md#older-tooltip-2026-10-05.</remarks>
internal static class LegacyItemComparisonReader
{
    /// <summary>State is null while the tooltip reopens in stages (one per frame) after a mode or item change.</summary>
    internal static bool TryRead(TooltipItemManager manager, ItemData item, out string key, out string text, out string state)
    {
        key = text = null;
        if (!IsAlive(manager) || !manager.active) { state = "inactive"; return false; }
        if (manager.activeInitializationStage != TooltipItemManager.TooltipInitializationStage.Completed) { state = null; return false; }
        var parameters = manager.activeParameters;
        var tooltip = manager.itemTooltip;
        if (parameters is null || parameters.WasCollected || !IsAlive(tooltip) || !tooltip.tooltipActive) { state = "hidden"; return false; }
        if (!SameItem(parameters._Item_k__BackingField, item)) { state = "other item"; return false; }
        var mode = parameters._TooltipMode_k__BackingField;
        state = $"mode={mode} autoCompare={manager.autoCompareEnabled}";
        switch (mode)
        {
            case TooltipMode.CompareDefault:
            case TooltipMode.CompareModRanges:
                bool equipped = IsAlive(tooltip.compareContent) && tooltip.compareContent.activeInHierarchy;
                state += equipped ? " compared" : " nothing equipped";
                text = equipped ? Comparison(tooltip) : ModText.Get("item.compare_nothing");
                if (mode == TooltipMode.CompareModRanges) text = ScreenContentTracker.Combine(new[] { text, RangeList(Rows(tooltip)) });
                break;
            case TooltipMode.Explanation:
                text = ExplanationList(Rows(tooltip));
                break;
            case TooltipMode.DefaultModRanges:
                text = RangeList(Rows(tooltip));
                break;
            default:
                return false;
        }
        key = $"legacy:{mode}:{item.Pointer}";
        return true;
    }

    // The game's summary of the swap (gained and lost lines); without one, the equipped item's own lines.
    private static string Comparison(UITooltipItem tooltip)
    {
        string equipped = ScreenContentTracker.Combine(new[] { Label(tooltip.compareNameText), Label(tooltip.compareTypeText) });
        string summary = ScreenContentTracker.Combine(new[] { Label(tooltip.comparisonText),
            Headed(tooltip.comparisonGainedHeader, tooltip.comparisonGainedText),
            Headed(tooltip.comparisonLostHeader, tooltip.comparisonLostText) });
        if (string.IsNullOrWhiteSpace(summary))
        {
            var lines = new List<string>();
            Add(lines, tooltip.compareImplicitText);
            Add(lines, tooltip.compareUniqueAffixesText);
            Add(lines, tooltip.comparePrefixes);
            Add(lines, tooltip.compareSuffixes);
            Add(lines, tooltip.compareSealedAffix, tooltip.compareSealedPrimordialAffix, tooltip.compareSealedCorruptedAffix);
            summary = string.Join("; ", lines);
        }
        return ScreenContentTracker.Combine(new[] { ComparedWith(equipped), summary });
    }

    // In the explanation and range modes the game rewrites these rows with that extra text.
    private static List<string> Rows(UITooltipItem tooltip)
    {
        var lines = new List<string>();
        Add(lines, tooltip.implicitText);
        Add(lines, tooltip.uniqueAffixesText);
        Add(lines, tooltip.prefixesText);
        Add(lines, tooltip.suffixesText);
        Add(lines, tooltip.sealedAffix, tooltip.sealedPrimordialAffix, tooltip.sealedCorruptedAffix);
        Add(lines, tooltip.forgingPotentialAltText);
        Add(lines, tooltip.legendaryPotentialAltText);
        Add(lines, tooltip.weaversWillAltText);
        Add(lines, tooltip.weaversTouchAltText);
        return lines;
    }

    private static string Headed(GameObject header, TMP_Text text)
    {
        string body = Label(text);
        if (body == null) return null;
        string title = IsAlive(header) && header.activeInHierarchy ? Label(header.GetComponentInChildren<TMP_Text>()) : null;
        return ScreenContentTracker.Combine(new[] { title, body });
    }

    private static void Add(List<string> lines, TMP_Text label)
    {
        if (IsAlive(label) && label.gameObject.activeInHierarchy && Label(label) is { } text) lines.Add(text);
    }

    private static void Add(List<string> lines, Affixes rows)
    {
        if (rows is null || rows.WasCollected) return;
        for (int i = 0; i < rows.Count; i++) Add(lines, rows[i]);
    }

    private static void Add(List<string> lines, params UITooltipItemAffix[] rows)
    {
        foreach (var row in rows)
            if (IsAlive(row) && row.gameObject.activeInHierarchy) Add(lines, row._text);
    }
}
