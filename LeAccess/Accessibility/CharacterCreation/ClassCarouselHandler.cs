using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Announces creation choices briefly and exposes their descriptions for manual reading.</summary>
public static class ClassCarouselHandler
{
    private static readonly ScreenContentTracker Tracker = new();
    private static string _key;
    private static string _manualName;
    private static string _lastFailure;
    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent { get; private set; }

    internal static bool TryHandleFocus(GameObject go)
    {
        try
        {
            return CanRead() && ClassSelectionReader.TryRead(out var selection) &&
                IsAlive(go) && go.transform.IsChildOf(selection.Root.transform);
        }
        catch (System.Exception ex) { ReportFailure(ex); return false; }
    }

    public static void CheckPendingClassAnnouncement()
    {
        try
        {
            if (!CanRead() || !ClassSelectionReader.TryRead(out var selection) ||
                ScreenContentAnnouncer.HasActiveContent && !ScreenContentAnnouncer.OwnsFocus(selection.Root))
            { Reset(); return; }
            if (_key != selection.Key) { Tracker.Reset(); _key = selection.Key; _manualName = null; }
            HasActiveContent = true;
            string text = selection.Name;
            if (text != null && text == _manualName) { HasPendingContent = false; return; }
            HasPendingContent = Tracker.Observe(1, text, ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame &&
                !ScreenContentAnnouncer.HasPendingContent, out string announcement);
            if (announcement == null) return;
            HasPendingContent = false;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[Class] {announcement}");
            _lastFailure = null;
        }
        catch (System.Exception ex) { Reset(); ReportFailure(ex); }
    }

    internal static bool TryReadDetails(out string text)
    {
        text = null;
        try
        {
            if (!CanRead() || !ClassSelectionReader.TryRead(out var selection) ||
                ScreenContentAnnouncer.HasActiveContent && !ScreenContentAnnouncer.OwnsFocus(selection.Root) ||
                string.IsNullOrWhiteSpace(selection.Name) || string.IsNullOrWhiteSpace(selection.Description)) return false;
            text = ScreenContentTracker.Combine(new[] { selection.Name, selection.Description });
            if (_key != selection.Key) { Tracker.Reset(); _key = selection.Key; }
            _manualName = selection.Name;
            HasPendingContent = false;
            return true;
        }
        catch (System.Exception ex) { ReportFailure(ex); return false; }
    }

    private static bool CanRead() => Application.isFocused && !LE.UI.LoadingScreen.IsVisible &&
        !DialogueAnnouncer.HasActiveContent && !ControllerMenuAccessibility.HasActiveContent;

    public static void Reset()
    {
        Tracker.Reset();
        _key = null;
        _manualName = null;
        HasActiveContent = HasPendingContent = false;
        ClassSelectionReader.Reset();
    }

    private static void ReportFailure(System.Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Class feedback unavailable: {ex}");
    }
}
