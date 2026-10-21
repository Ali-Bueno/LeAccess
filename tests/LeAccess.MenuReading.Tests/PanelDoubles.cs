namespace LeAccess.Accessibility.Panels
{
    // The faction, vendor and item-service readers need the game's panels; menu reading only needs them to own nothing.
    internal static class PanelControlReader
    {
        internal static bool TryRead(UnityEngine.GameObject element, out string text) { text = null; return false; }
    }

    internal static class PanelIntro
    {
        internal static string LeadFor(UnityEngine.GameObject element) => null;
    }

    internal static class ListItemReader
    {
        internal static bool TryReadEvolutionChoice(UnityEngine.UI.Toggle toggle, bool value, out string text) { text = null; return false; }
    }
}

namespace LeAccess.Accessibility.Market
{
    // The Bazaar and Observatory pickers need the game's UI; menu reading only needs them to own nothing.
    internal static class BazaarReader
    {
        internal static bool TryRead(UnityEngine.GameObject element, bool details, out string text) { text = null; return false; }
    }

    internal static class ProphecyPickerReader
    {
        internal static bool TryRead(UnityEngine.GameObject element, out string text) { text = null; return false; }
    }
}
