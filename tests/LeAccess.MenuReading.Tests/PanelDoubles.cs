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
}
