using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

namespace LeAccess.Accessibility.Settings;

internal static class SettingsFocusAnnouncer
{
    private static int _owner, _tab;
    private static string _heading;
    private static string _lastFailure;

    internal static bool Update()
    {
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible ||
                ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
                !SettingsFocusReader.TryRead(out var current))
            { Reset(); return false; }
            if (!ScreenReader.IsAvailable()) return true;

            string heading = ScreenContentTracker.Combine(new[] { current.TabName, current.TabSummary });
            bool changed = _owner != current.OwnerId || _tab != current.TabId || _heading != heading;
            if (UiVisibility.IsVisible(current.Selected))
                ElementAnnouncer.AnnounceElement(current.Selected, interrupt: true, context: changed ? heading : null);
            else if (changed && !string.IsNullOrWhiteSpace(heading))
                ScreenReader.Speak(heading, interrupt: true);
            if (changed && !string.IsNullOrWhiteSpace(heading))
                Plugin.Log.LogInfo($"[Settings] {heading}");
            _owner = current.OwnerId;
            _tab = current.TabId;
            _heading = heading;
            return true;
        }
        catch (System.Exception ex)
        {
            Reset();
            string failure = ex.GetType().Name + ": " + ex.Message;
            if (_lastFailure != failure)
            { _lastFailure = failure; Plugin.Log.LogWarning("Settings reading unavailable: " + failure); }
            return false;
        }
    }

    internal static bool ReadCurrentDetails()
    {
        if (!SettingsFocusReader.TryRead(out var current)) return false;
        if (SettingsFocusReader.TryReadTabDetails(out string details) ||
            UiVisibility.IsVisible(current.Selected) && ElementAnnouncer.TryReadDetails(current.Selected, out details))
            ScreenReader.Speak(details, interrupt: true);
        return true;
    }

    internal static void Reset() { _owner = _tab = 0; _heading = null; }
}
