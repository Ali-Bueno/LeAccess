using System;
using BepInEx.Configuration;
using LE.UI.PanelSystem;
using LeAccess.Accessibility.Announcements;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterSheet;

/// <summary>Speaks the character sheet's main stats once on opening; right stick up and down move through its sections.</summary>
/// <remarks>See reference/character-sheet.md.</remarks>
internal static class CharacterSheetSummary
{
    private const string LogPrefix = "[Character sheet]";
    // Longest wait for the panel title and the first focus to finish before the opening summary is dropped.
    private const double OpeningWindowSeconds = 5;
    // No section read yet since the sheet opened: the first push reads the first.
    private const int NoSection = -1;
    // A step of none reads the first section, as on opening.
    private const int FirstSection = 0;
    private static ConfigEntry<bool> _readOnOpen;
    private static bool _open, _pending, _loggedNoLife;
    private static double _openedAt;
    private static int _section = NoSection;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config) =>
        _readOnOpen = config.Bind("Character sheet", "ReadSummaryOnOpen", true,
            "Read the sheet's level, life, mana and attributes once when it opens; right stick up and down move through its sections.");

    /// <summary>True when the right stick push was taken to move through the sheet's sections.</summary>
    internal static bool Update(bool next, bool previous)
    {
        if (_readOnOpen is null) return false;
        try
        {
            var sheet = OpenSheet();
            if (sheet is null) { _open = _pending = false; return false; }
            if (!_open)
            {
                _open = true;
                _pending = _readOnOpen.Value;
                _section = NoSection;
                _openedAt = Time.unscaledTimeAsDouble;
                return false;
            }
            if ((next || previous) && OwnsFocus(sheet))
            {
                _pending = false;
                return Speak(sheet, next ? 1 : -1, interrupt: true);
            }
            if (!_pending) return false;
            if (Time.unscaledTimeAsDouble - _openedAt > OpeningWindowSeconds) { _pending = false; return false; }
            // Queued behind the panel title and the first focus, never over them.
            if (ScreenReader.SpokeThisFrame || ScreenReader.IsSpeaking()) return false;
            _pending = false;
            Speak(sheet, FirstSection, interrupt: false);
        }
        catch (Exception ex) { ReportFailure(ex); }
        return false;
    }

    private static global::CharacterSheet OpenSheet()
    {
        var sheet = global::CharacterSheet.instance;
        return IsAlive(sheet) && sheet.isActiveAndEnabled && UiVisibility.IsVisible(sheet.gameObject) ? sheet : null;
    }

    private static bool OwnsFocus(global::CharacterSheet sheet)
    {
        if (!UiFocusOwnership.TryGetSelected(out var selected)) return false;
        if (selected.transform.IsChildOf(sheet.transform)) return true;
        var panel = sheet.GetComponentInParent<Panel>();
        return IsAlive(panel) && selected.transform.IsChildOf(panel.transform);
    }

    // The sections are read afresh each time, so values and the open tab are current.
    private static bool Speak(global::CharacterSheet sheet, int step, bool interrupt)
    {
        var sections = CharacterSheetSummaryReader.Sections(sheet, out bool hasLife);
        if (!hasLife && !_loggedNoLife)
        {
            _loggedNoLife = true;
            Plugin.Log.LogInfo($"{LogPrefix} no life line on screen");
        }
        if (sections.Count == 0) return false;
        int index = step == FirstSection ? 0 : CharacterSheetSummaryText.Step(_section, sections.Count, step);
        var section = sections[index];
        string text = CharacterSheetSummaryText.Section(section.Title, index + 1, sections.Count, section.Entries);
        if (string.IsNullOrWhiteSpace(text) || !ScreenReader.TrySpeak(text, interrupt)) return false;
        _section = index;
        Plugin.Log.LogInfo($"{LogPrefix} section {index + 1}/{sections.Count}: {text}");
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
