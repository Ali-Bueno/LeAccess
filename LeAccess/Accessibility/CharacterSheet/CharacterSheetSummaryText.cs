using System;
using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.CharacterSheet;

/// <summary>Pure text assembly for the character-sheet sections; the labels and values are the game's own.</summary>
internal static class CharacterSheetSummaryText
{
    // Lines of one stat kind start their own section from this many in a row; a shorter run joins the lines before it.
    internal const int MinGroupRun = 2;

    internal static string Entry(string label, string value) =>
        string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value) ? null : label.Trim() + ": " + value.Trim();

    /// <summary>A row's values under its one label ("Armour: 92, 17%"); a value with words of its own needs no label.</summary>
    internal static string Row(string label, IEnumerable<string> values)
    {
        string shown = string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
        if (shown.Length == 0) return null;
        return Entry(label, shown) ?? (shown.Any(char.IsLetter) ? shown : null);
    }

    /// <summary>An attribute's displayed texts: the first text without digits names it, the first with digits is its value.</summary>
    internal static string Attribute(IEnumerable<string> texts)
    {
        var shown = texts.Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
        return Entry(shown.FirstOrDefault(text => !text.Any(char.IsDigit)), shown.FirstOrDefault(text => text.Any(char.IsDigit)));
    }

    internal static string Compose(IEnumerable<string> entries) => ScreenContentTracker.Combine(entries);

    /// <summary>A section as spoken: its title, its place among the sections, then its entries.</summary>
    internal static string Section(string title, int position, int count, IEnumerable<string> entries) =>
        Compose(new[] { title, ListPosition.Text(position, count) }.Concat(entries));

    /// <summary>The section next to the current one, staying at either end; -1 (none yet) moves to the first.</summary>
    internal static int Step(int current, int count, int direction) =>
        count <= 0 ? -1 : current < 0 ? 0 : Math.Clamp(current + direction, 0, count - 1);

    /// <summary>The section of each detail line: a new block (column or game spacer) starts one, and so does a run of
    /// <see cref="MinGroupRun"/> or more lines of one kind.</summary>
    internal static int[] Groups(IReadOnlyList<(int Block, int Kind)> lines)
    {
        var groups = new int[lines.Count];
        int group = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            bool newBlock = i == 0 || lines[i].Block != lines[i - 1].Block;
            if (newBlock || lines[i].Kind != lines[i - 1].Kind)
            {
                int run = 1;
                while (i + run < lines.Count && lines[i + run].Block == lines[i].Block && lines[i + run].Kind == lines[i].Kind) run++;
                if (newBlock || run >= MinGroupRun) group++;
            }
            groups[i] = group;
        }
        return groups;
    }
}
