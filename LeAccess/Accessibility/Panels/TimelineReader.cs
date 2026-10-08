using System;
using System.Collections.Generic;
using LE.Factions;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>Reads the Forgotten Knights harbinger timelines: island, harbinger, defeated or not, and the corruption figures.</summary>
/// <remarks>See reference/faction-vendor-panels.md#forgotten-knights-timelines.</remarks>
internal static class TimelineReader
{
    private static long _view;
    private static bool _introduced;

    internal static void Reset()
    {
        _view = 0;
        _introduced = false;
    }

    /// <summary>Every frame while a rank UI is selected; true when the timelines view owns the selection and was handled.</summary>
    internal static bool Poll(FactionRankUITimelines ui)
    {
        if (!Owns(ui)) { Reset(); return false; }
        var view = FocusedView(ui);
        if (!IsAlive(view) || view.Pointer.ToInt64() == _view) return true;
        _view = view.Pointer.ToInt64();
        string intro = _introduced ? null : Intro(ui);
        _introduced = true;
        ElementAnnouncer.AnnounceElement(view.gameObject, interrupt: true, context: intro);
        return true;
    }

    /// <summary>The generic focus path asks for a timeline's text (focus on its icon or the view itself).</summary>
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        var view = element.GetComponentInParent<HarbingerDefeatedView>();
        if (!IsAlive(view)) return false;
        var ui = view.GetComponentInParent<FactionRankUITimelines>();
        var views = IsAlive(ui) ? ActiveViews(ui) : new List<HarbingerDefeatedView>();
        int place = views.FindIndex(other => other.Pointer == view.Pointer);
        text = FactionTreeText.Timeline(Text(view.islandName), Text(view.hoverText), IsOn(view.completedBox), place + 1, views.Count);
        return true;
    }

    // The timelines view owns the selection unless the game's gamepad focus sits on one of its other buttons.
    private static bool Owns(FactionRankUITimelines ui)
    {
        if (!IsAlive(ui) || !IsAlive(ui.timelinesRoot) || !ui.timelinesRoot.activeInHierarchy) return false;
        var button = ui.GamepadButton;
        return ui.CurrentKey == FactionRankUI.GamepadKey.Timelines || !IsAlive(button) ||
            IsAlive(button.GetComponentInParent<HarbingerDefeatedView>());
    }

    private static HarbingerDefeatedView FocusedView(FactionRankUITimelines ui)
    {
        var button = ui.GamepadButton;
        var owner = IsAlive(button) ? button.GetComponentInParent<HarbingerDefeatedView>() : null;
        if (IsAlive(owner)) return owner;
        var map = ui.timelinesMap;
        int key = (int)ui.focusedTimeline;
        return map is not null && !map.WasCollected && map.ContainsKey(key) ? map[key] : null;
    }

    private static List<HarbingerDefeatedView> ActiveViews(FactionRankUITimelines ui)
    {
        var views = new List<HarbingerDefeatedView>();
        var all = ui.harbingerDefeatedViews;
        if (all is null || all.WasCollected) return views;
        for (int i = 0; i < all.Count; i++)
            if (IsAlive(all[i]) && all[i].gameObject.activeInHierarchy) views.Add(all[i]);
        return views;
    }

    // The game's figures (corruption required, kills) and the Aberroth prompt, then how many timelines are done.
    private static string Intro(FactionRankUITimelines ui)
    {
        var lines = new List<string>();
        foreach (var label in new[] { ui.corruptionReqText, ui.highestCorruptionKillText, ui.harbingerKillsText, ui.aterrothsKillsText })
            if (TextUtils.IsVisibleLabel(label)) lines.Add(TextUtils.CleanText(label.text));
        foreach (var box in new[] { ui.aterrothEnabledBox1, ui.aterrothEnabledBox2, ui.aterrothDisabledBox1, ui.aterrothDisabledBox2 })
            if (IsAlive(box) && box.activeInHierarchy) lines.Add(ScreenContentReader.Read(box, null, null));
        var views = ActiveViews(ui);
        return FactionTreeText.TimelinesIntro(lines, views.FindAll(view => IsOn(view.completedBox)).Count, views.Count);
    }

    private static bool IsOn(GameObject box) => IsAlive(box) && box.activeInHierarchy;

    // A label the game localizes by key keeps its language even while its object is hidden.
    private static string Text(TMP_Text label)
    {
        if (!IsAlive(label)) return null;
        string text = label.text ?? "";
        var source = label.GetComponent<LocalizeStringEvent>();
        var entry = IsAlive(source) ? source.StringReference?.TableEntryReference : null;
        string key = entry is null || entry.WasCollected ? null : entry.Key;
        return TextUtils.CleanText(string.IsNullOrEmpty(key) ? text : global::Localization.GetText(key, text));
    }
}
