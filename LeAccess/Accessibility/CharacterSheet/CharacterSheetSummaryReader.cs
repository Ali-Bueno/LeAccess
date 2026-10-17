using System;
using System.Collections.Generic;
using System.Linq;
using LE.UI.PanelSystem;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterSheet;

/// <summary>Reads the character sheet's plain value fields with the game's own labels beside them.</summary>
/// <remarks>See reference/character-sheet.md.</remarks>
internal static class CharacterSheetSummaryReader
{
    // A value's label sits in its row, or in the row group one or two containers up.
    private const int LabelSearchLevels = 3;
    // Life and mana are stat lines, not plain fields; they follow the level.
    private static readonly SP[] ResourceStats = { SP.Health, SP.HealthRegen, SP.Mana, SP.ManaRegen };

    /// <summary>The summary, with every other stat line on screen when asked, and the fields with nothing on screen.</summary>
    internal static string Build(global::CharacterSheet sheet, List<string> skipped, bool everyLine)
    {
        var fields = new (string Name, TMP_Text Value)[]
        {
            (nameof(sheet.Level), sheet.Level), (nameof(sheet.Experience), sheet.Experience),
            (nameof(sheet.experienceTillNextLevelText), sheet.experienceTillNextLevelText)
        };
        var entries = new List<string>();
        var read = new HashSet<IntPtr>();
        var lines = ActiveLines(sheet);
        AddAll(entries, skipped, fields);
        int before = entries.Count;
        AddLines(entries, read, lines.Where(IsResource));
        if (entries.Count == before) skipped.Add(nameof(ResourceStats));
        AddAttributes(entries, sheet);
        AddAll(entries, skipped, new (string, TMP_Text)[]
        {
            (nameof(sheet.PhysicalRes), sheet.PhysicalRes), (nameof(sheet.LightningRes), sheet.LightningRes),
            (nameof(sheet.ColdRes), sheet.ColdRes), (nameof(sheet.FireRes), sheet.FireRes), (nameof(sheet.VoidRes), sheet.VoidRes),
            (nameof(sheet.NecroticRes), sheet.NecroticRes), (nameof(sheet.PoisonRes), sheet.PoisonRes),
            (nameof(sheet.armour), sheet.armour), (nameof(sheet.armourPercent), sheet.armourPercent),
            (nameof(sheet.dodge), sheet.dodge), (nameof(sheet.DodgeChance), sheet.DodgeChance),
            (nameof(sheet.blockChance), sheet.blockChance), (nameof(sheet.blockEffectiveness), sheet.blockEffectiveness),
            (nameof(sheet.blockMitigation), sheet.blockMitigation), (nameof(sheet.dps), sheet.dps)
        });
        AddLines(entries, read, new[]
        {
            sheet.parryChance, sheet.glancingBlow, sheet.enduranceThreshold,
            sheet.PotionHealth, sheet.maximumCompanions, sheet.minionPower
        });
        if (everyLine) AddLines(entries, read, lines);
        return CharacterSheetSummaryText.Compose(entries);
    }

    // The detailed stat lists can sit beside the sheet in its panel.
    private static List<CharacterStatDisplayLineItem> ActiveLines(global::CharacterSheet sheet)
    {
        var panel = sheet.GetComponentInParent<Panel>();
        var root = IsAlive(panel) ? panel.transform : sheet.transform;
        return root.GetComponentsInChildren<CharacterStatDisplayLineItem>().ToList();
    }

    private static bool IsResource(CharacterStatDisplayLineItem line)
    {
        var display = line._characterStatDisplay;
        return IsAlive(display) && !display.minionStat && ResourceStats.Contains(display.property);
    }

    private static void AddLines(List<string> entries, HashSet<IntPtr> read, IEnumerable<CharacterStatDisplayLineItem> lines)
    {
        foreach (var line in lines)
            if (IsAlive(line) && read.Add(line.Pointer) && CharacterStatLineReader.TryRead(line.gameObject, out string text))
                entries.Add(text);
    }

    private static void AddAll(List<string> entries, List<string> skipped, IEnumerable<(string Name, TMP_Text Value)> fields)
    {
        foreach (var (name, value) in fields)
        {
            string text = TextUtils.IsVisibleLabel(value) ? TextUtils.CleanText(value.text) : null;
            // A field that already shows words ("Level 95") is its own label.
            string entry = CharacterSheetSummaryText.Entry(LabelFor(value), text) ??
                (text is not null && text.Any(char.IsLetter) ? text : null);
            if (entry is null) skipped.Add(name); else entries.Add(entry);
        }
    }

    // Only the corrupted or the uncorrupted variant of an attribute is shown at a time.
    private static void AddAttributes(List<string> entries, global::CharacterSheet sheet)
    {
        var displays = sheet.attributeDisplays;
        if (displays is null || displays.WasCollected) return;
        for (int i = 0; i < displays.Count; i++)
        {
            var display = displays[i];
            if (display is null || display.WasCollected) continue;
            var variant = IsAlive(display.corruptedAttribute) && display.corruptedAttribute.activeInHierarchy
                ? display.corruptedAttribute : display.uncorruptedAttribute;
            if (!IsAlive(variant) || !variant.activeInHierarchy) continue;
            var texts = new List<string>();
            foreach (var label in variant.GetComponentsInChildren<TMP_Text>())
                if (TextUtils.IsVisibleLabel(label)) texts.Add(TextUtils.CleanText(label.text));
            string entry = CharacterSheetSummaryText.Attribute(texts);
            if (entry is not null) entries.Add(entry);
        }
    }

    // The nearest visible text before the value in its row, else the first one after it.
    private static string LabelFor(TMP_Text value)
    {
        var container = value.transform.parent;
        for (int level = 0; level < LabelSearchLevels && container is not null; level++, container = container.parent)
        {
            string before = null, after = null;
            bool passed = false;
            foreach (var label in container.GetComponentsInChildren<TMP_Text>())
            {
                if (label.Pointer == value.Pointer) { passed = true; continue; }
                if (!TextUtils.IsVisibleLabel(label)) continue;
                string text = TextUtils.CleanText(label.text);
                if (string.IsNullOrWhiteSpace(text) || HasDigit(text)) continue;
                if (passed) after ??= text; else before = text;
            }
            if (before is not null || after is not null) return before ?? after;
        }
        return null;
    }

    private static bool HasDigit(string text)
    {
        foreach (char c in text) if (char.IsDigit(c)) return true;
        return false;
    }
}
