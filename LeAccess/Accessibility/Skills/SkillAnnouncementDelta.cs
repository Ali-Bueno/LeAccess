using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LeAccess.Accessibility.ScreenContent;

namespace LeAccess.Accessibility.Skills;

internal static class SkillAnnouncementDelta
{
    internal static string NewFragments(string previous, string current)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (string fragment in Split(previous)) known.Add(fragment.TrimEnd('.'));
        var added = new List<string>();
        foreach (string fragment in Split(current))
            if (known.Add(fragment.TrimEnd('.'))) added.Add(fragment);
        return ScreenContentTracker.Combine(added);
    }

    private static IEnumerable<string> Split(string text)
    {
        foreach (string fragment in Regex.Split(text ?? string.Empty, @"(?<=[.!?])\s+"))
            if (!string.IsNullOrWhiteSpace(fragment)) yield return fragment.Trim();
    }
}
