using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Reads the echo the Monolith web cursor rests on: the game's own echo tooltip plus the island's state.</summary>
/// <remarks>See reference/endgame-panels.md#monolith.</remarks>
internal static class MonolithReader
{
    internal static bool TryRead(MonolithPanel panel, out EndgameStop stop)
    {
        stop = default;
        var manager = panel.TimelineManager;
        if (!IsAlive(manager) || !manager._echoTooltipOpen) return false;
        var ui = manager._currentEchoTooltipIsland;
        var tooltip = manager._echoTooltip;
        if (!IsAlive(ui) || !IsAlive(tooltip) || !UiVisibility.IsVisible(tooltip.gameObject)) return false;
        var island = ui.island;
        if (island is null || island.WasCollected) return false;
        string text = ScreenContentReader.Read(tooltip.gameObject, tooltip._echoTitle, tooltip._echoSubtitle);
        if (string.IsNullOrWhiteSpace(text)) return false;
        stop = new EndgameStop($"{island.hexIndex}", EndgameText.Island(text, State(manager, island), Rewarding(manager, island)));
        return true;
    }

    /// <summary>The panel's own header: title, corruption and gaze of Orobyss as the game shows them.</summary>
    internal static string Intro(MonolithPanel panel)
    {
        var manager = panel.TimelineManager;
        if (!IsAlive(manager)) return Clean(panel.Title);
        return ScreenContentTracker.Combine(new[] { Clean(panel.Title), Visible(manager._corruptionTMP), Visible(manager._gazeOfOrobyssTMP) });
    }

    private static IslandState State(MonolithTimelinePanelManager manager, EchoWebIsland island)
    {
        if (island.completed) return IslandState.Completed;
        var web = manager.web;
        return web is not null && !web.WasCollected && !web.islandCanBeRun(island) ? IslandState.Unreachable : IslandState.Available;
    }

    private static bool Rewarding(MonolithTimelinePanelManager manager, EchoWebIsland island) =>
        manager.web is { WasCollected: false } web && island.hasClaimableDroppableReward(web);

    private static string Visible(TMPro.TMP_Text label) => TextUtils.IsVisibleLabel(label) ? Clean(label.text) : null;

    private static string Clean(string text) => string.IsNullOrWhiteSpace(text) ? null : TextUtils.CleanText(text);
}
