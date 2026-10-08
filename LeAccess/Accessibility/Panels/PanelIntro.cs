using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>Introduces a faction, vendor or item-service panel once per opening, ahead of the first control read in it.</summary>
/// <remarks>See reference/faction-vendor-panels.md.</remarks>
internal static class PanelIntro
{
    private static Panel _current;
    private static PanelKind _kind;
    private static bool _introduced;
    private static int _lastElement;
    private static Panel _lastPanel;
    private static PanelKind _lastKind;
    private static string _lastFailure;

    internal static bool TryGetCurrent(out Panel panel, out PanelKind kind)
    {
        panel = _current;
        kind = _kind;
        return IsAlive(_current);
    }

    /// <summary>Every frame: an introduced panel that closed or hid is forgotten, so reopening introduces it again.</summary>
    internal static void Update()
    {
        if (_current is null) return;
        try
        {
            if (!IsAlive(_current) || !_current.isActiveAndEnabled || !UiVisibility.IsVisible(_current.gameObject)) Reset();
        }
        catch (Exception) { Reset(); }
    }

    internal static PanelKind KindOf(GameObject element)
    {
        Locate(element, out _, out var kind);
        return kind;
    }

    /// <summary>The panel's introduction when this element is the first one read in a newly opened family panel.</summary>
    internal static string LeadFor(GameObject element)
    {
        try
        {
            if (!Locate(element, out var panel, out var kind) || kind == PanelKind.None) return null;
            if (IsAlive(_current) && _current.Pointer == panel.Pointer && _introduced) return null;
            _current = panel;
            _kind = kind;
            _introduced = true;
            string text = Compose(panel, kind);
            Plugin.Log.LogInfo($"[Panel] {kind}: {text}");
            return text;
        }
        catch (Exception ex)
        {
            if (_lastFailure != ex.Message) Plugin.Log.LogWarning($"Panel introduction unavailable: {ex.Message}");
            _lastFailure = ex.Message;
            return null;
        }
    }

    internal static void Reset()
    {
        _current = null;
        _kind = PanelKind.None;
        _introduced = false;
        _lastElement = 0;
        _lastPanel = null;
        VendorReader.Reset();
        FactionReader.Reset();
        MasterySelectionReader.Reset();
        Market.BazaarReader.Reset();
    }

    // The nearest panel of an element and its family; the last answer is kept because focus reads repeat per frame.
    private static bool Locate(GameObject element, out Panel panel, out PanelKind kind)
    {
        panel = null;
        kind = PanelKind.None;
        try
        {
            if (!IsAlive(element)) return false;
            int id = element.GetInstanceID();
            if (id != _lastElement || _lastPanel is null || !IsAlive(_lastPanel) && _lastKind != PanelKind.None)
            {
                _lastElement = id;
                var nearest = element.GetComponentInParent<Panel>();
                bool same = IsAlive(nearest) && IsAlive(_lastPanel) && nearest.Pointer == _lastPanel.Pointer;
                _lastPanel = nearest;
                if (!same) _lastKind = IsAlive(nearest) ? PanelFamily.Identify(nearest) : PanelKind.None;
            }
            panel = _lastPanel;
            kind = _lastKind;
            return IsAlive(panel);
        }
        catch (Exception) { return false; }
    }

    private static string Compose(Panel panel, PanelKind kind)
    {
        string title = kind is PanelKind.Gambling or PanelKind.Vendor or PanelKind.TimeBeast or PanelKind.LootFilter or PanelKind.Mastery or PanelKind.Bazaar
            ? Title(panel) : null;
        string body = kind is PanelKind.LootFilter or PanelKind.Mastery ? null : PanelStaticText.Read(panel);
        string extra = kind switch
        {
            PanelKind.Gambling or PanelKind.Vendor => VendorReader.IntroLine(panel),
            PanelKind.FactionRank => FactionReader.StandingLine(panel),
            _ => null
        };
        return ScreenContentTracker.Combine(new[] { title, body, extra });
    }

    // A raw localization key would be worse than silence.
    private static string Title(Panel panel)
    {
        string title = TextUtils.CleanText(panel.Title);
        return string.IsNullOrWhiteSpace(title) || title.Contains('_') ? null : title;
    }
}
