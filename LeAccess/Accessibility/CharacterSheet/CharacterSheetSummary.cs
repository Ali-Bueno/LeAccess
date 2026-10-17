using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LE.UI.PanelSystem;
using LeAccess.Accessibility.Announcements;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterSheet;

/// <summary>Speaks the character sheet's upper half once on opening, and every stat on screen on the menu details input.</summary>
/// <remarks>See reference/character-sheet.md.</remarks>
internal static class CharacterSheetSummary
{
    private const string LogPrefix = "[Character sheet]";
    // Longest wait for the panel title and the first focus to finish before the opening summary is dropped.
    private const double OpeningWindowSeconds = 5;
    private static ConfigEntry<bool> _readOnOpen;
    private static bool _open, _pending;
    private static double _openedAt;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config) =>
        _readOnOpen = config.Bind("Character sheet", "ReadSummaryOnOpen", true,
            "Read the sheet's level, life, mana, attributes, resistances, armour, dodge and block once when it opens.");

    internal static void Update()
    {
        if (_readOnOpen is null) return;
        try
        {
            var sheet = OpenSheet();
            if (sheet is null) { _open = _pending = false; return; }
            if (!_open) { _open = true; _pending = _readOnOpen.Value; _openedAt = Time.unscaledTimeAsDouble; return; }
            if (!_pending) return;
            if (Time.unscaledTimeAsDouble - _openedAt > OpeningWindowSeconds) { _pending = false; return; }
            // Queued behind the panel title and the first focus, never over them.
            if (ScreenReader.SpokeThisFrame || ScreenReader.IsSpeaking()) return;
            _pending = false;
            Speak(sheet, interrupt: false, everyLine: false);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    /// <summary>The menu details input while focus is on the open sheet.</summary>
    internal static bool TryReadOnRequest()
    {
        try
        {
            var sheet = OpenSheet();
            if (sheet is null || !UiFocusOwnership.TryGetSelected(out var selected) || !Owns(sheet, selected)) return false;
            return Speak(sheet, interrupt: true, everyLine: true);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    private static global::CharacterSheet OpenSheet()
    {
        var sheet = global::CharacterSheet.instance;
        return IsAlive(sheet) && sheet.isActiveAndEnabled && UiVisibility.IsVisible(sheet.gameObject) ? sheet : null;
    }

    private static bool Owns(global::CharacterSheet sheet, GameObject selected)
    {
        if (selected.transform.IsChildOf(sheet.transform)) return true;
        var panel = sheet.GetComponentInParent<Panel>();
        return IsAlive(panel) && selected.transform.IsChildOf(panel.transform);
    }

    private static bool Speak(global::CharacterSheet sheet, bool interrupt, bool everyLine)
    {
        var skipped = new List<string>();
        string text = CharacterSheetSummaryReader.Build(sheet, skipped, everyLine);
        if (skipped.Count > 0) Plugin.Log.LogInfo($"{LogPrefix} no label or value on screen for: {string.Join(", ", skipped)}");
        if (string.IsNullOrWhiteSpace(text) || !ScreenReader.TrySpeak(text, interrupt)) return false;
        Plugin.Log.LogInfo($"{LogPrefix} {text}");
        return true;
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"{LogPrefix} summary unavailable: {failure}");
    }
}
