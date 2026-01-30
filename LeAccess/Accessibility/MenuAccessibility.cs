using LeAccess.Accessibility.CharacterCreation;
using UnityEngine;

namespace LeAccess.Accessibility;

/// <summary>
/// Main component for menu accessibility.
/// </summary>
public class MenuAccessibility : MonoBehaviour
{
    void Awake()
    {
        Plugin.Log.LogInfo("MenuAccessibility component initialized");
    }

    void Update()
    {
        ClassCarouselHandler.CheckPendingClassAnnouncement();
    }

    /// <summary>
    /// Force announce a specific text.
    /// </summary>
    public static void Announce(string text, bool interrupt = true)
    {
        ScreenReader.Speak(text, interrupt);
    }
}
