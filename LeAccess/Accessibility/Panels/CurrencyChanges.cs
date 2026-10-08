using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;

namespace LeAccess.Accessibility.Panels;

/// <summary>Reports a vendor's currency lines only when one changes after the opening reading.</summary>
internal sealed class CurrencyChanges
{
    private readonly Dictionary<string, string> _last = new();
    private bool _baselined;

    /// <summary>The first call records the amounts; later calls return the lines that differ, or null.</summary>
    internal string Observe(IReadOnlyList<KeyValuePair<string, string>> now)
    {
        var changed = new List<string>();
        foreach (var line in now)
        {
            if (_baselined && (!_last.TryGetValue(line.Key, out string before) || before != line.Value)) changed.Add(line.Value);
            _last[line.Key] = line.Value;
        }
        _baselined = true;
        return changed.Count == 0 ? null : ScreenContentTracker.Combine(changed);
    }

    internal void Reset()
    {
        _last.Clear();
        _baselined = false;
    }
}
