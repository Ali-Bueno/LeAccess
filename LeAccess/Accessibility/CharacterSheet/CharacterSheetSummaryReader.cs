using System;
using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterSheet;

/// <summary>A titled group of the sheet's stats as displayed.</summary>
internal readonly record struct SheetSection(string Title, IReadOnlyList<string> Entries);

/// <summary>Reads the character sheet in the game's own layout: main stats, resistances, additional defences, then the
/// open detail tab's lines in groups, each with the game's labels.</summary>
/// <remarks>See reference/character-sheet.md.</remarks>
internal static class CharacterSheetSummaryReader
{
    // A value's label sits in its row, or in the row group one or two containers up.
    private const int LabelSearchLevels = 3;
    // Detail lines the sheet fills itself (parry, endurance threshold...) have no stat display.
    private const int NoStatKind = -1;

    /// <summary>The sheet's sections in screen order; hasLife is false when no life line was found.</summary>
    internal static List<SheetSection> Sections(global::CharacterSheet sheet, out bool hasLife)
    {
        var sections = new List<SheetSection>();
        var content = CommonParent(sheet.PhysicalRes, sheet.armour) ?? sheet.transform;
        var values = ValueTexts(sheet, content);
        var main = new List<string>();
        AddField(main, sheet.Level);
        // Life, mana and their regeneration are bare stat displays in their own block, not line items.
        var life = BareDisplays(content).FirstOrDefault(display => !display.minionStat && display.property == SP.Health);
        hasLife = life is not null;
        if (hasLife && SectionOf(life.transform, content) is { } mainStats) main.AddRange(Rows(mainStats, values));
        AddAttributes(main, sheet);
        Add(sections, ModText.Get("sheet.section.main"), main);
        foreach (var anchor in new[] { sheet.PhysicalRes, sheet.armour })
            if (IsAlive(anchor) && SectionOf(anchor.transform, content) is { } section)
                Add(sections, Heading(section, values), Rows(section, values));
        AddDetailSections(sections, content);
        return sections;
    }

    private static void Add(List<SheetSection> sections, string title, List<string> entries)
    {
        if (entries.Count > 0) sections.Add(new SheetSection(title, entries));
    }

    // The sheet's plain value fields and every bare stat display shown outside the detail lines.
    private static HashSet<IntPtr> ValueTexts(global::CharacterSheet sheet, Transform content)
    {
        var values = new HashSet<IntPtr>();
        foreach (var field in new[]
                 {
                     sheet.PhysicalRes, sheet.LightningRes, sheet.ColdRes, sheet.FireRes, sheet.VoidRes, sheet.NecroticRes,
                     sheet.PoisonRes, sheet.armour, sheet.armourPercent, sheet.dodge, sheet.DodgeChance, sheet.blockChance,
                     sheet.blockEffectiveness, sheet.blockMitigation
                 })
            if (IsAlive(field)) values.Add(field.Pointer);
        foreach (var display in BareDisplays(content))
        {
            var text = IsAlive(display.statText) ? display.statText : display.GetComponent<TMP_Text>();
            if (IsAlive(text)) values.Add(text.Pointer);
        }
        return values;
    }

    private static IEnumerable<CharacterStatDisplay> BareDisplays(Transform content) =>
        content.GetComponentsInChildren<CharacterStatDisplay>()
            .Where(display => IsAlive(display) && !IsAlive(display.GetComponentInParent<CharacterStatDisplayLineItem>()));

    // The sheet's blocks (header, main stats, resistances, defences, detail tabs) share one parent.
    private static Transform CommonParent(Component first, Component second)
    {
        if (!IsAlive(first) || !IsAlive(second)) return null;
        var ancestors = new HashSet<IntPtr>();
        for (var t = first.transform.parent; t is not null; t = t.parent) ancestors.Add(t.Pointer);
        for (var t = second.transform.parent; t is not null; t = t.parent)
            if (ancestors.Contains(t.Pointer)) return t;
        return null;
    }

    private static Transform SectionOf(Transform item, Transform content)
    {
        for (var t = item; t is not null; t = t.parent)
            if (t.parent is not null && t.parent.Pointer == content.Pointer) return t;
        return null;
    }

    // Values in screen order; the values of one row are read under its label.
    private static List<string> Rows(Transform section, HashSet<IntPtr> values)
    {
        var rows = new List<string>();
        TMP_Text first = null;
        var shown = new List<string>();
        foreach (var text in section.GetComponentsInChildren<TMP_Text>())
        {
            if (!values.Contains(text.Pointer) || !TextUtils.IsVisibleLabel(text)) continue;
            string value = TextUtils.CleanText(text.text);
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (first is null || text.transform.parent.Pointer != first.transform.parent.Pointer)
            {
                AddRow(rows, first, shown);
                first = text;
            }
            shown.Add(value);
        }
        AddRow(rows, first, shown);
        return rows;
    }

    private static void AddRow(List<string> rows, TMP_Text first, List<string> shown)
    {
        if (first is not null && CharacterSheetSummaryText.Row(LabelFor(first), shown) is { } row) rows.Add(row);
        shown.Clear();
    }

    // The section's title: its first visible words before the values, outside their rows.
    private static string Heading(Transform section, HashSet<IntPtr> values)
    {
        var texts = section.GetComponentsInChildren<TMP_Text>();
        var rows = new HashSet<IntPtr>();
        foreach (var text in texts)
            if (values.Contains(text.Pointer)) rows.Add(text.transform.parent.Pointer);
        foreach (var text in texts)
        {
            if (values.Contains(text.Pointer)) return null;
            if (!TextUtils.IsVisibleLabel(text) || rows.Contains(text.transform.parent.Pointer)) continue;
            string heading = TextUtils.CleanText(text.text);
            if (!string.IsNullOrWhiteSpace(heading) && !HasDigit(heading)) return heading;
        }
        return null;
    }

    // Only the open tab is active; its lines are cut into groups by column, the game's spacers and stat kind.
    private static void AddDetailSections(List<SheetSection> sections, Transform content)
    {
        var lines = new List<CharacterStatDisplayLineItem>();
        var kinds = new List<(int Block, int Kind)>();
        int block = 0;
        foreach (var tab in content.GetComponentsInChildren<DetailedCharacterStatsTab>())
        {
            CharacterStatDisplayLineItem previous = null;
            foreach (var line in tab.GetComponentsInChildren<CharacterStatDisplayLineItem>())
            {
                if (!IsAlive(line) || !UiVisibility.IsVisible(line.gameObject)) continue;
                if (previous is not null && SplitsFrom(previous, line)) block++;
                previous = line;
                lines.Add(line);
                kinds.Add((block, Kind(line)));
            }
            block++;
        }
        var groups = CharacterSheetSummaryText.Groups(kinds);
        var entries = new List<string>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0 && groups[i] != groups[i - 1]) { Add(sections, null, entries); entries = new List<string>(); }
            if (CharacterStatLineReader.TryReadParts(lines[i].gameObject, out string name, out string value) &&
                CharacterSheetSummaryText.Entry(name, value) is { } entry) entries.Add(entry);
        }
        Add(sections, null, entries);
    }

    private static int Kind(CharacterStatDisplayLineItem line)
    {
        var display = line._characterStatDisplay;
        return IsAlive(display) ? (int)display.property : NoStatKind;
    }

    // The detail lines sit in two columns, with spacer objects between the game's groups.
    private static bool SplitsFrom(CharacterStatDisplayLineItem previous, CharacterStatDisplayLineItem line)
    {
        var column = line.transform.parent;
        if (column is null || previous.transform.parent is null || previous.transform.parent.Pointer != column.Pointer) return true;
        for (int i = previous.transform.GetSiblingIndex() + 1; i < line.transform.GetSiblingIndex(); i++)
        {
            var between = column.GetChild(i);
            if (between.gameObject.activeSelf && !IsAlive(between.GetComponent<CharacterStatDisplayLineItem>())) return true;
        }
        return false;
    }

    private static void AddField(List<string> entries, TMP_Text value)
    {
        string text = TextUtils.IsVisibleLabel(value) ? TextUtils.CleanText(value.text) : null;
        if (text is not null && CharacterSheetSummaryText.Row(LabelFor(value), new[] { text }) is { } entry) entries.Add(entry);
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
