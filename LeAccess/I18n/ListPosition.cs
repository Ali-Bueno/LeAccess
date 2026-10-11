namespace LeAccess.I18n;

/// <summary>The spoken "N of M" place of an entry in a list, switchable as a whole.</summary>
internal static class ListPosition
{
    internal static bool Enabled { get; set; } = true;

    /// <summary>Null when disabled or meaningless (a single entry or no place), so combined speech drops it.</summary>
    internal static string Text(int position, int count) =>
        Enabled && count > 1 && position > 0 ? ModText.Format("list.position", position, count) : null;
}
