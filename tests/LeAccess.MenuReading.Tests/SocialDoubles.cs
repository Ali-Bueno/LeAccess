namespace LeAccess.Accessibility.Social
{
    // The Social readers are tested in their own suite; menu reading only needs them to own nothing.
    internal static class SocialReader
    {
        internal static bool TryRead(UnityEngine.GameObject element, out string text) { text = null; return false; }
        internal static UnityEngine.GameObject Canonical(UnityEngine.GameObject element) => element;
        internal static bool TryReadToggleState(UnityEngine.UI.Toggle toggle, bool value, out string text) { text = null; return false; }
    }
    internal static class SocialAccessibility
    {
        internal static string LeadFor(UnityEngine.GameObject element) => null;
        internal static void LeadSpoken(UnityEngine.GameObject element) { }
    }
}
