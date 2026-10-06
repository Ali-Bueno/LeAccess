namespace LeAccess.Accessibility.Forge
{
    // The forge grid service is tested in its own suite; menu reading only needs it to own nothing.
    internal static class ForgeAccessibility
    {
        internal static bool HasActiveContent;
        internal static bool TryHandleFocus(UnityEngine.GameObject element) => false;
    }
}
