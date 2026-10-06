using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Forge;

/// <summary>Speaks the forge grid's selected cell, the forge on opening and each craft's result.</summary>
/// <remarks>Polled: most cells move the cursor without the hook other menus rely on.
/// See reference/crafting-reading.md#controller-forge-2026-10-05.</remarks>
internal static class ForgeAccessibility
{
    private static readonly ScreenContentTracker Tracker = new();
    private static CraftingSlotManager _open;
    private static bool _introduced;
    private static string _introKey;
    private static string _key;
    private static string _lastFailure;

    internal static bool HasActiveContent { get; private set; }

    /// <summary>The generic announcer stays silent for cells of the forge grid while it owns navigation.</summary>
    internal static bool TryHandleFocus(GameObject element)
    {
        try { return IsAlive(element) && ForgeCellReader.TryGetForge(out var forge) && element.transform.IsChildOf(forge.transform); }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update(bool readDetails = false)
    {
        try
        {
            TrackOpenPanel();
            HasActiveContent = false;
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || ScreenContentAnnouncer.HasActiveContent ||
                DialogueAnnouncer.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
                !ForgeCellReader.TryGetForge(out var forge) || !ForgeCellReader.TryRead(forge, out var cell))
            { Tracker.Reset(); _key = null; return; }
            HasActiveContent = true;
            _open = forge;
            if (_key != cell.Key) { Tracker.Reset(); _key = cell.Key; }
            if (_introKey != null && _introKey != cell.Key) _introKey = null;
            // The forge names itself and its item before the first cell after opening, for as long as that cell stays.
            string brief = _introduced && _introKey != cell.Key ? cell.Brief
                : ScreenContentTracker.Combine(new[] { ModText.Get("forge.opened"), ForgeCellReader.ItemSummary(forge), cell.Brief });
            bool pending = Tracker.Observe(1, brief, ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame,
                out string announcement, acknowledge: false);
            bool result = ForgeResultLog.HasResult;
            if (readDetails) announcement = ScreenContentTracker.Combine(new[] { brief, Details(cell) });
            else if (result)
            {
                // A craft can move focus; let the new cell settle so the result leads it in one utterance.
                if (pending && announcement == null) return;
                announcement = ScreenContentTracker.Combine(new[] { ForgeCellReader.ReadResult(forge), pending ? announcement : null });
            }
            if (string.IsNullOrWhiteSpace(announcement)) return;
            if (!ScreenReader.TrySpeak(announcement, interrupt: true)) return;
            if (result && !readDetails) ForgeResultLog.Clear();
            Tracker.Acknowledge(1, brief);
            if (!_introduced) _introKey = cell.Key;
            _introduced = true;
            Plugin.Log.LogInfo($"[{(readDetails ? "Forge details" : result ? "Forge result" : "Forge")}] {announcement}");
        }
        catch (Exception ex) { Tracker.Reset(); _key = null; ReportFailure(ex); }
    }

    // Manual details: the forge's chance, modifier and potential summary for the selected cell.
    private static string Details(ForgeCell cell) =>
        ForgeSummaryReader.TryRead(cell.Root, out string summary) ? summary : null;

    // Closing the forge (its panel hidden) makes the next opening introduce it again.
    private static void TrackOpenPanel()
    {
        if (_open is null) return;
        if (IsAlive(_open) && _open.isActiveAndEnabled && _open.gameObject.activeInHierarchy) return;
        _open = null;
        _introduced = false;
        _introKey = null;
        ForgeResultLog.Clear();
    }

    internal static void Reset()
    {
        Tracker.Reset();
        _key = null;
        _open = null;
        _introduced = false;
        _introKey = null;
        HasActiveContent = false;
        ForgeInventoryNotices.Reset();
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Forge feedback unavailable: {ex}");
    }
}
