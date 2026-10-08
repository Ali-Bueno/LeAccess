namespace LeAccess.Accessibility.Gifting
{
    // The gifting panel is read by its own polled service; menu reading only needs it to own nothing.
    internal static class GiftAccessibility
    {
        internal static bool TryHandleFocus(UnityEngine.GameObject element) => false;
    }

    internal static class GiftPanelReader
    {
        internal static bool TryReadResonanceRow(UnityEngine.GameObject element, out string text) { text = null; return false; }
    }
}
