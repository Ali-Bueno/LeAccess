using UnityEngine;

namespace LeAccess.Accessibility.Navigation;

internal static class NavigationObjectLifetime
{
    // Unity's null operator reads the native pointer, which throws for a collected IL2CPP wrapper.
    public static bool IsAlive(UnityEngine.Object value) =>
        value is not null && !value.WasCollected && value != null;
}
