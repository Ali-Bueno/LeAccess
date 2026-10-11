using System;
using System.Collections.Generic;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Utils;

/// <summary>Tracks text fields and turns each change into what typing or erasing in it sounds like.</summary>
internal sealed class FieldEcho
{
    private readonly Dictionary<int, string> _values = new();

    /// <summary>The first sight of a field sets its baseline silently; later changes return their echo, else null.</summary>
    internal string Observe(int id, string value)
    {
        value ??= "";
        if (!_values.TryGetValue(id, out string before)) { _values[id] = value; return null; }
        if (before == value) return null;
        _values[id] = value;
        return Describe(before, value);
    }

    internal void Remove(int id) => _values.Remove(id);

    internal void Clear() => _values.Clear();

    /// <summary>The typed or erased characters, else the whole new value.</summary>
    internal static string Describe(string before, string after)
    {
        before ??= "";
        after ??= "";
        if (before == after) return null;
        if (after.Length == 0) return ModText.Get("social.field_empty");
        if (after.Length > before.Length && after.StartsWith(before, StringComparison.Ordinal))
            return after.Substring(before.Length);
        if (before.Length > after.Length && before.StartsWith(after, StringComparison.Ordinal))
            return ModText.Format("social.erased", before.Substring(after.Length));
        return after;
    }
}
