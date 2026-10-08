namespace LeAccess.Accessibility.Endgame
{
    // The endgame panels are read by a polled service; menu reading only needs it to own nothing.
    internal static class EndgameAccessibility
    {
        internal static bool TryHandleFocus(UnityEngine.GameObject element) => false;
    }
}
