using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.ScreenContent;

internal sealed class ScreenContentTracker
{
    private readonly Dictionary<int, Entry> _entries = new();

    internal bool Observe(int id, string text, bool canSpeak, out string announcement)
    {
        announcement = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            Hide(id);
            return false;
        }
        if (!_entries.TryGetValue(id, out var entry)) _entries[id] = entry = new Entry();
        bool stable = entry.Candidate == text;
        entry.Stable = stable;
        entry.Candidate = text;
        if (entry.Spoken == text) return false;
        // Two matching observations allow text and localization to settle after the opening hook.
        if (stable && canSpeak)
        {
            entry.Spoken = text;
            announcement = text;
        }
        return true;
    }

    internal void Hide(int id) => _entries.Remove(id);
    internal bool HasStableContent(int id) => _entries.TryGetValue(id, out var entry) &&
        entry.Stable && entry.Spoken != entry.Candidate && !string.IsNullOrWhiteSpace(entry.Candidate);
    internal void Reset() => _entries.Clear();

    internal static string Combine(IEnumerable<string> fragments)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new System.Text.StringBuilder();
        foreach (string fragment in fragments)
        {
            string text = fragment?.Trim();
            if (string.IsNullOrWhiteSpace(text) || !seen.Add(text)) continue;
            if (result.Length != 0)
            {
                char last = result[result.Length - 1];
                result.Append(last == '.' || last == '!' || last == '?' || last == ':' ? " " : ". ");
            }
            result.Append(text);
        }
        return result.ToString();
    }

    private sealed class Entry
    {
        internal string Candidate;
        internal string Spoken;
        internal bool Stable;
    }
}
