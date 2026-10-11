namespace LeAccess.Accessibility.Exploration;

// Which breakable props the scanner lists and what it calls them.
internal static class BreakableSelection
{
    // A broken or unloaded prop drops out; a standing one is listed when seen.
    internal static bool Listed(bool standing, bool seen) => standing && seen;

    // The game's own name when it has one, else the generic label.
    internal static string Name(string gameName, string generic) =>
        string.IsNullOrWhiteSpace(gameName) ? generic : gameName.Trim();
}
