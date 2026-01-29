using UnityEngine;

namespace LeAccess.Accessibility;

/// <summary>
/// Main component for menu accessibility.
/// Provides utility methods for announcements.
/// The main announcement logic is now in UIPatches via ControllerFocusCursor.Show patches.
/// </summary>
public class MenuAccessibility : MonoBehaviour
{
    void Awake()
    {
        Plugin.Log.LogInfo("MenuAccessibility component initialized");
    }

    /// <summary>
    /// Force announce a specific text.
    /// </summary>
    public static void Announce(string text, bool interrupt = true)
    {
        ScreenReader.Speak(text, interrupt);
    }
}
