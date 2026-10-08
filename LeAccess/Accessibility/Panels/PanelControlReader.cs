using System;
using UnityEngine;

namespace LeAccess.Accessibility.Panels;

/// <summary>Entry point of the menu reader for controls of the faction, vendor and item-service panels.</summary>
/// <remarks>See reference/faction-vendor-panels.md.</remarks>
internal static class PanelControlReader
{
    private static string _lastFailure;

    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        try
        {
            if (WeaverTreeReader.TryRead(element, out text) || TimelineReader.TryRead(element, out text)) return true;
            var kind = PanelIntro.KindOf(element);
            return kind != PanelKind.None && (VendorReader.TryReadControl(element, kind, out text) ||
                FactionReader.TryRead(element, kind, out text) || ListItemReader.TryRead(element, kind, out text) ||
                MasterySelectionReader.TryRead(element, kind, out text));
        }
        catch (Exception ex)
        {
            text = null;
            if (_lastFailure != ex.Message) Plugin.Log.LogWarning($"Panel control unavailable: {ex.Message}");
            _lastFailure = ex.Message;
            return false;
        }
    }

    /// <summary>Once per frame, from the menu update.</summary>
    internal static void Update()
    {
        PanelIntro.Update();
        VendorReader.Update();
        try { FactionReader.Poll(); WeaverTreeReader.Poll(); }
        catch (Exception ex)
        {
            if (_lastFailure != ex.Message) Plugin.Log.LogWarning($"Faction focus unavailable: {ex.Message}");
            _lastFailure = ex.Message;
        }
    }

    /// <summary>The held-detail key on a Weaver tree node.</summary>
    internal static bool ReadDetails()
    {
        try { return WeaverTreeReader.ReadDetails(); }
        catch (Exception ex)
        {
            if (_lastFailure != ex.Message) Plugin.Log.LogWarning($"Weaver node details unavailable: {ex.Message}");
            _lastFailure = ex.Message;
            return false;
        }
    }
}
