namespace LeAccess.Accessibility.Guide
{
    // The guide panel reader needs the game's panel types; menu reading only needs it to own nothing.
    internal static class GuideAccessibility
    {
        internal static bool TryReadCategory(UnityEngine.GameObject element, out string text) { text = null; return false; }
    }
}

namespace LeAccess.Accessibility.Items
{
    // The stash tab reader needs the game's stash types; menu reading only needs it to own nothing.
    internal static class StashTabReader
    {
        internal static bool TryRead(UnityEngine.GameObject element, out string text) { text = null; return false; }
    }
}
