using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal static class QuestAccessibility
{
    private static readonly QuestAnnouncementState Announcements = new();
    private static QuestButtonGenerator _generator;
    private static string _lastFailure;

    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent => HasActiveContent && Announcements.HasPendingContent;

    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            if (IsBlocked() || !IsAlive(element)) return false;
            RememberGenerator(element);
            var generator = GetGenerator();
            if (!QuestSelectionReader.TryRead(generator, out _)) return false;
            var selected = generator._selectedButton_k__BackingField;
            var button = element.GetComponentInParent<QuestButton>();
            // Let ordinary tab/filter/pin controls retain their own focus labels.
            return IsAlive(button) && button.Pointer == selected.Pointer &&
                element.Pointer == selected.gameObject.Pointer;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update()
    {
        HasActiveContent = false;
        try
        {
            if (!QuestSelectionReader.TryRead(GetGenerator(), out var selection))
            { Announcements.Reset(); return; }
            HasActiveContent = true;
            if (!Announcements.Observe(selection.Key, selection.Summary, selection.Details,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame,
                out string announcement)) return;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[Quest] {announcement}");
        }
        catch (Exception ex) { HasActiveContent = false; Announcements.Reset(); ReportFailure(ex); }
    }

    private static bool IsBlocked() => !Application.isFocused || LE.UI.LoadingScreen.IsVisible ||
        ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent || ControllerMenuAccessibility.HasActiveContent;

    private static QuestButtonGenerator GetGenerator()
    {
        if (IsBlocked()) return null;
        var navigation = NavigationManager.currentlySelected;
        if (navigation is null || navigation.WasCollected) return null;
        var component = navigation.TryCast<Behaviour>();
        if (!IsAlive(component) || !component.isActiveAndEnabled || !UiVisibility.IsVisible(component.gameObject)) return null;
        RememberGenerator(component.gameObject);
        var prioritized = navigation.TryCast<PrioritizedNavigable>();
        if (IsAlive(prioritized) && IsAlive(prioritized.ActiveSelectable))
            RememberGenerator(prioritized.ActiveSelectable.gameObject);
        if (!IsAlive(_generator)) return null;
        var panel = _generator.questPanel;
        var info = _generator.questInformation;
        bool ownsNavigation = QuestSelectionReader.IsOwned(component.gameObject, _generator.transform) ||
            IsAlive(panel) && !panel.questsHidden && QuestSelectionReader.IsOwned(component.gameObject, panel.transform) ||
            IsAlive(info) && QuestSelectionReader.IsOwned(component.gameObject, info.transform);
        return ownsNavigation ? _generator : null;
    }

    private static void RememberGenerator(GameObject element)
    {
        var button = element.GetComponentInParent<QuestButton>();
        if (IsAlive(button) && IsAlive(button.buttonGenerator)) { _generator = button.buttonGenerator; return; }
        var generator = element.GetComponentInParent<QuestButtonGenerator>();
        if (IsAlive(generator)) { _generator = generator; return; }
        var panel = element.GetComponentInParent<QuestPanel>();
        if (IsAlive(panel) && IsAlive(panel.buttonGenerator)) _generator = panel.buttonGenerator;
    }

    internal static void Reset() { Announcements.Reset(); _generator = null; _lastFailure = null; HasActiveContent = false; }
    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Quest journal feedback unavailable: {ex}");
    }
}
