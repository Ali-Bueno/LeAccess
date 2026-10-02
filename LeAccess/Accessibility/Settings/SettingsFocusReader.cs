using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Settings;

internal readonly record struct SettingsFocusSnapshot(
    int OwnerId, int TabId, GameObject Selected, string TabName, string TabSummary);

internal static class SettingsFocusReader
{
    // The native SettingsPanelTabUIController.Select dispatch and installed tab IDs agree.
    // See reference/settings-reading.md; tabContents is empty in this prefab.
    private enum SettingsTab { Gameplay, Graphics, Sound, Social, Challenges }

    internal static bool TryRead(out SettingsFocusSnapshot snapshot)
    {
        snapshot = default;
        try
        {
            var navigation = NavigationManager.currentlySelected;
            var owner = navigation is null || navigation.WasCollected ? null : navigation.TryCast<SettingsPanelTabNavigable>();
            if (!IsAlive(owner) || !owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject)) return false;
            var declared = owner._tabUIController;
            var controller = IsAlive(declared) ? declared.TryCast<SettingsPanelTabUIController>() : null;
            if (!IsAlive(controller) || !UiVisibility.IsVisible(controller.gameObject)) return false;
            var manager = controller._settingsUIManager;
            if (!IsAlive(manager) || !UiVisibility.IsVisible(manager.gameObject) ||
                !controller.transform.IsChildOf(manager.transform)) return false;
            int tabId = controller.SelectedTabId;
            GameObject tabRoot = (SettingsTab)tabId switch
            {
                SettingsTab.Gameplay => manager.gameplayTab,
                SettingsTab.Graphics => manager.GraphicsTab,
                SettingsTab.Sound => manager.soundTab,
                SettingsTab.Social => manager.socialTab,
                SettingsTab.Challenges => manager.challengesTab,
                _ => null
            };
            if (!IsAlive(tabRoot) || tabRoot.Pointer != owner.gameObject.Pointer ||
                !tabRoot.transform.IsChildOf(manager.transform)) return false;
            var tabs = controller.tabElements;
            if (tabs is null || tabs.WasCollected) return false;
            TabUIElement tab = null;
            foreach (var candidate in tabs)
            {
                if (!IsAlive(candidate) || candidate.elementID != tabId) continue;
                if (tab != null) return false;
                tab = candidate;
            }
            if (!IsAlive(tab) || !IsAlive(tab.controller) || tab.controller.Pointer != controller.Pointer ||
                !tab.transform.IsChildOf(controller.transform) || !UiVisibility.IsVisible(tab.gameObject)) return false;
            string title = ReadUniqueLabel(tab.gameObject);
            string summary = (SettingsTab)tabId == SettingsTab.Challenges ? SettingsChallengeReader.Read(owner) : null;
            snapshot = new(owner.GetInstanceID(), tabId, ReadSelection(owner), title, summary);
            return true;
        }
        catch (Exception) { return false; }
    }

    private static GameObject ReadSelection(SettingsPanelTabNavigable owner)
    {
        var rects = owner._selectables;
        var components = owner._selectableComponents;
        int index = owner._currentlySelectedIdx;
        if (rects is null || rects.WasCollected || components is null || components.WasCollected ||
            rects.Length != components.Length || index < 0 || index >= rects.Length) return null;
        var rect = rects[index];
        var entry = components[index];
        var component = IsAlive(entry) ? entry.TryCast<Component>() : null;
        if (!IsAlive(rect) || !IsAlive(component) || component.gameObject.Pointer != rect.gameObject.Pointer ||
            !rect.IsChildOf(owner.transform) || !UiVisibility.IsVisible(rect.gameObject)) return null;
        return rect.gameObject;
    }

    internal static bool TryReadTabDetails(out string text)
    {
        text = null;
        if (!TryRead(out var current) || (SettingsTab)current.TabId != SettingsTab.Challenges) return false;
        var owner = NavigationManager.currentlySelected.TryCast<SettingsPanelTabNavigable>();
        string details = SettingsChallengeReader.Read(owner, includeDetails: true);
        if (string.IsNullOrWhiteSpace(details)) return false;
        text = ScreenContent.ScreenContentTracker.Combine(new[] { current.TabName, details });
        return true;
    }

    internal static string ReadUniqueLabel(GameObject root, bool firstLineOnly = false)
    {
        if (!IsAlive(root) || !UiVisibility.IsVisible(root)) return null;
        string result = null;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(false))
        {
            string text = ReadLabel(label, root, firstLineOnly);
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (result != null && result != text) return null;
            result = text;
        }
        return result;
    }

    internal static string ReadLabel(TMP_Text label, GameObject owner, bool firstLineOnly = false)
    {
        if (!IsAlive(owner) || !IsAlive(label) || !label.isActiveAndEnabled || label.alpha <= 0 ||
            !label.transform.IsChildOf(owner.transform) || !UiVisibility.IsVisible(label.gameObject) ||
            IsAlive(label.canvasRenderer) && label.canvasRenderer.cull) return null;
        string text = label.text;
        if (firstLineOnly && !string.IsNullOrEmpty(text))
        {
            int lineBreak = text.IndexOfAny(new[] { '\n', '\r' });
            int richBreak = text.IndexOf("<br", StringComparison.OrdinalIgnoreCase);
            if (richBreak >= 0 && (lineBreak < 0 || richBreak < lineBreak)) lineBreak = richBreak;
            if (lineBreak >= 0) text = text.Substring(0, lineBreak);
        }
        if (!firstLineOnly && text != null)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>", " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = TextUtils.CleanText(text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
