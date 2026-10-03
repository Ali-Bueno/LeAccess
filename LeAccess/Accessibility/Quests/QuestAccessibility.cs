using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal static class QuestAccessibility
{
    private static readonly QuestAnnouncementState Announcements = new();
    private static QuestButtonGenerator _generator;
    private static string _lastFailure;
    private static string _detailsKey;

    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent => HasActiveContent && (Announcements.HasPendingContent || _detailsKey != null);

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
                (element.Pointer == selected.gameObject.Pointer ||
                 IsAlive(selected.buttonRectTransform) && element.Pointer == selected.buttonRectTransform.gameObject.Pointer);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update()
    {
        HasActiveContent = false;
        try
        {
            if (!QuestSelectionReader.TryRead(GetGenerator(), out var selection))
            { ClearSelection(); return; }
            HasActiveContent = true;
            if (_detailsKey != selection.Key) _detailsKey = null;
            bool prepared = Announcements.Observe(selection.Key, selection.Summary, selection.Objectives,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame,
                out string announcement, acknowledge: false);
            bool requestDetails = _detailsKey == selection.Key && ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame;
            if (requestDetails)
            {
                Announcements.Observe(selection.Key, selection.Summary, selection.Objectives, false, out _, acknowledge: false);
                announcement = selection.Text;
            }
            else if (!prepared) return;
            if (!QuestSelectionReader.TryRead(GetGenerator(), out var current) || current.Key != selection.Key)
            { ClearSelection(); return; }
            if (!ScreenReader.TrySpeak(announcement, interrupt: true)) return;
            Announcements.Acknowledge(selection.Key, selection.Summary, selection.Objectives);
            _detailsKey = null;
            Plugin.Log.LogInfo($"[{(requestDetails ? "Quest details" : "Quest")}] {announcement}");
        }
        catch (Exception ex) { ClearSelection(); ReportFailure(ex); }
    }

    internal static bool ReadCurrentDetails()
    {
        try
        {
            if (!QuestSelectionReader.TryRead(GetGenerator(), out var selection)) { ClearSelection(); return false; }
            HasActiveContent = true;
            _detailsKey = selection.Key;
            Announcements.Observe(selection.Key, selection.Summary, selection.Objectives, false, out _, acknowledge: false);
            Announcements.Observe(selection.Key, selection.Summary, selection.Objectives, false, out _, acknowledge: false);
            if (!QuestSelectionReader.TryRead(GetGenerator(), out var current) || current.Key != selection.Key)
            { ClearSelection(); return false; }
            if (!ScreenReader.TrySpeak(selection.Text, interrupt: true)) return true;
            Announcements.Acknowledge(selection.Key, selection.Summary, selection.Objectives);
            _detailsKey = null;
            Plugin.Log.LogInfo($"[Quest details] {selection.Text}");
            return true;
        }
        catch (Exception ex) { ClearSelection(); ReportFailure(ex); return false; }
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
        var mapGenerator = GetMapGenerator(component.gameObject);
        if (IsAlive(mapGenerator)) _generator = mapGenerator;
        // QuestInformation can receive focus before a quest button does.
        var currentInfo = navigation.TryCast<QuestInformation>();
        var manager = NavigationManager.instance;
        if (IsAlive(currentInfo) && IsAlive(manager) &&
            manager.GetNavigableFromID(NavigableID.WORLD_MAP_PANEL, out var mapNavigation) &&
            mapNavigation is not null && !mapNavigation.WasCollected)
        {
            var mapComponent = mapNavigation.TryCast<Behaviour>();
            var owner = IsAlive(mapComponent) ? GetMapGenerator(mapComponent.gameObject) : null;
            if (IsAlive(owner) && IsAlive(owner.questInformation) && owner.questInformation.Pointer == currentInfo.Pointer)
                _generator = owner;
        }
        if (!IsAlive(_generator)) return null;
        var panel = _generator.questPanel;
        var info = _generator.questInformation;
        bool ownsNavigation = IsAlive(mapGenerator) && mapGenerator.Pointer == _generator.Pointer ||
            QuestSelectionReader.IsOwned(component.gameObject, _generator.transform) ||
            IsAlive(panel) && !panel.questsHidden && QuestSelectionReader.IsOwned(component.gameObject, panel.transform) ||
            IsAlive(info) && QuestSelectionReader.IsOwned(component.gameObject, info.transform);
        return ownsNavigation ? _generator : null;
    }

    private static QuestButtonGenerator GetMapGenerator(GameObject element)
    {
        var worldMap = element.GetComponentInParent<WorldMapPanel>();
        var map = IsAlive(worldMap) ? worldMap._mapPanel : element.GetComponentInParent<MapPanel>();
        if (IsAlive(worldMap) && (!worldMap.isActiveAndEnabled || !UiVisibility.IsVisible(worldMap.gameObject))) return null;
        if (!IsAlive(map) || !map.isActiveAndEnabled || !UiVisibility.IsVisible(map.gameObject)) return null;
        var panel = map.questPanel;
        if (!IsAlive(panel)) return null;
        var generator = panel.buttonGenerator;
        return IsAlive(generator) && IsAlive(generator.questPanel) && generator.questPanel.Pointer == panel.Pointer
            ? generator : null;
    }

    private static void RememberGenerator(GameObject element)
    {
        var button = element.GetComponentInParent<QuestButton>();
        if (IsAlive(button) && IsAlive(button.buttonGenerator)) { _generator = button.buttonGenerator; return; }
        var generator = element.GetComponentInParent<QuestButtonGenerator>();
        if (IsAlive(generator)) { _generator = generator; return; }
        var panel = element.GetComponentInParent<QuestPanel>();
        if (IsAlive(panel) && IsAlive(panel.buttonGenerator)) { _generator = panel.buttonGenerator; return; }
    }

    private static void ClearSelection() { Announcements.Reset(); _detailsKey = null; HasActiveContent = false; }
    internal static void Reset() { ClearSelection(); _generator = null; _lastFailure = null; }
    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Quest journal feedback unavailable: {ex}");
    }
}
