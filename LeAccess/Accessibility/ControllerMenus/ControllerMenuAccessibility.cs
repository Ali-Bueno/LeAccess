using System;
using LeAccess.I18n;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.ControllerMenus;

internal static class ControllerMenuAccessibility
{
    private static readonly ScreenContentTracker Tracker = new();
    private static int _selectedSector;
    private static int _menuId;
    private static bool _openingPending;
    private static string MenuTitle => ModText.Get("controller.menu");
    private static string _lastFailure;
    internal static bool HasPendingContent { get; private set; }

    internal static bool HasActiveContent
    {
        get { try { return IsAlive(FindMenu()); } catch (Exception ex) { ReportFailure(ex); return false; } }
    }

    internal static bool TryHandleFocus(GameObject element) => HasActiveContent;

    internal static void Update()
    {
        HasPendingContent = false;
        try
        {
            var menu = FindMenu();
            if (!IsAlive(menu)) { ClearMenu(); return; }
            int menuId = menu.GetInstanceID();
            if (menuId != _menuId)
            {
                ClearSelection();
                _menuId = menuId;
                _openingPending = true;
            }
            var radial = menu.FocusedRadialMenu;
            if (!IsAlive(radial) || !radial._enabled || !UiVisibility.HasVisiblePresentation(radial.gameObject))
            { ClearSelection(); return; }
            var sector = radial._selectedSector;
            if (!IsAlive(sector) || !sector.isActiveAndEnabled || !sector._selected ||
                !UiVisibility.HasVisiblePresentation(sector.gameObject)) { ClearSelection(); AnnounceOpening(); return; }
            int id = sector.GetInstanceID();
            if (id != _selectedSector) { ClearSelection(); _selectedSector = id; }

            string text = null;
            var label = sector._optionText;
            if (TextUtils.IsVisibleLabel(label))
                text = TextUtils.CleanText(label.text);
            if (string.IsNullOrWhiteSpace(text))
            {
                var option = sector._assignedOption;
                if (option is not null && !option.WasCollected && !string.IsNullOrWhiteSpace(option.m_TextKey))
                    text = TextUtils.CleanText(Localization.GetText(option.m_TextKey));
            }
            if (string.IsNullOrWhiteSpace(text)) { AnnounceOpening(); return; }
            HasPendingContent = Tracker.Observe(id, text,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement);
            if (announcement == null) return;
            HasPendingContent = false;
            if (_openingPending) announcement = MenuTitle + ". " + announcement;
            _openingPending = false;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[ControllerMenu] {announcement}");
        }
        catch (Exception ex) { ClearMenu(); ReportFailure(ex); }
    }

    private static ControllerMenu FindMenu()
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || ScreenContentAnnouncer.HasActiveContent ||
            DialogueAnnouncer.HasActiveContent) return null;
        var manager = ControllerManager.instance;
        if (!IsAlive(manager) || !manager.isActiveAndEnabled) return null;
        var map = manager._CurrentMap_k__BackingField;
        if (map is null || map.WasCollected) return null;
        var controllerMap = map.TryCast<ControllerMenuMap>();
        if (controllerMap is null || controllerMap.WasCollected) return null;
        var menu = controllerMap.controllerMenu;
        return IsAlive(menu) && menu.isActiveAndEnabled && menu._enabled && UiVisibility.HasVisiblePresentation(menu.gameObject)
            ? menu : null;
    }

    private static void ClearSelection() { Tracker.Reset(); _selectedSector = 0; HasPendingContent = false; }
    private static void ClearMenu() { ClearSelection(); _menuId = 0; _openingPending = false; }
    private static void AnnounceOpening()
    {
        HasPendingContent = _openingPending;
        if (!_openingPending || !ScreenReader.IsAvailable() || ScreenReader.SpokeThisFrame) return;
        _openingPending = HasPendingContent = false;
        ScreenReader.Speak(MenuTitle, interrupt: true);
        Plugin.Log.LogInfo("[ControllerMenu] opened; waiting for a sector selection");
    }
    internal static void Reset() { ClearMenu(); _lastFailure = null; }
    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Controller menu feedback unavailable: {failure}");
    }
}
