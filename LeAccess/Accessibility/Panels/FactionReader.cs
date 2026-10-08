using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.Factions;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>Faction tiles (choose or overview panels) and the rank rewards of a faction's rank panel.</summary>
/// <remarks>See reference/faction-vendor-panels.md#factions.</remarks>
internal static class FactionReader
{
    private static IntPtr _spokenTile;
    private static int _polled;

    internal static void Reset()
    {
        _spokenTile = IntPtr.Zero;
        _polled = 0;
    }

    /// <summary>Every frame: the faction panels keep their own gamepad selection, so a change of it is announced here.</summary>
    internal static void Poll()
    {
        var navigation = NavigationManager.currentlySelected;
        if (navigation is null || navigation.WasCollected) { _polled = 0; return; }
        Selectable selected = null;
        var rank = navigation.TryCast<FactionRankUI>();
        if (IsAlive(rank)) selected = rank.GamepadButton;
        else if (navigation.TryCast<FactionUI>() is { } overview && IsAlive(overview)) selected = overview.GamepadButton;
        if (!IsAlive(selected) || !selected.isActiveAndEnabled) { _polled = 0; return; }
        int id = selected.GetInstanceID();
        if (id == _polled) return;
        _polled = id;
        ElementAnnouncer.AnnounceElement(selected.gameObject, interrupt: true);
    }

    internal static bool TryRead(GameObject element, PanelKind kind, out string text)
    {
        text = null;
        if (kind == PanelKind.FactionRank)
        {
            var ui = element.GetComponentInParent<FactionRankUI>();
            return IsAlive(ui) && TryReadRank(ui, element, out text);
        }
        if (kind != PanelKind.Faction) return false;
        var tile = element.GetComponentInParent<FactionTile>();
        if (!IsAlive(tile)) return false;
        text = ReadTileControl(tile, element);
        return !string.IsNullOrWhiteSpace(text);
    }

    /// <summary>"Rank N, favor M" for the faction of an open rank panel, or null.</summary>
    internal static string StandingLine(Panel panel)
    {
        var ui = panel.GetComponentInChildren<FactionRankUI>();
        return IsAlive(ui) && TryFaction(ui, out var faction) ? PanelText.Standing(faction.Rank, faction.Favor) : null;
    }

    private static bool TryReadRank(FactionRankUI ui, GameObject element, out string text)
    {
        text = null;
        var buttons = ui.rankButtons;
        var rewards = ui.rankRewards;
        if (buttons is null || buttons.WasCollected || rewards is null || rewards.WasCollected) return false;
        for (int i = 0; i < buttons.Length && i < rewards.Length; i++)
        {
            var button = buttons[i];
            if (!IsAlive(button) || button.gameObject.Pointer != element.Pointer) continue;
            var reward = rewards[i];
            if (!IsAlive(reward) || !TryFaction(ui, out var faction)) return false;
            int rank = RankNumber(reward, i + 1);
            text = PanelText.RankReward(rank, buttons.Length, Read(reward.titleText), Read(reward.descriptionText),
                rank <= faction.Rank, rank == faction.Rank, Read(reward.nextUnlockText));
            return true;
        }
        return false;
    }

    // The tile's name and description once on entering it; moving between its controls says only the control.
    private static string ReadTileControl(FactionTile tile, GameObject element)
    {
        string label = TextUtils.GetTextFromHierarchy(element);
        bool entering = _spokenTile != tile.Pointer;
        _spokenTile = tile.Pointer;
        if (!entering) return label ?? PanelText.Banner(null);
        string summary = ScreenContentReader.Read(tile.gameObject, null, null, include: l =>
            !Inside<Selectable>(l, tile) && !Inside<GeneralTooltop>(l, tile));
        var faction = tile.currentFaction;
        string standing = IsAlive(tile.aligned) && tile.aligned.activeInHierarchy && faction is not null && !faction.WasCollected
            ? PanelText.Standing(faction.Rank, faction.Favor) : null;
        summary = ScreenContentTracker.Combine(new[] { summary, standing });
        return label == null ? PanelText.Banner(summary) : ScreenContentTracker.Combine(new[] { summary, label });
    }

    private static bool TryFaction(FactionRankUI ui, out Faction faction)
    {
        faction = null;
        var provider = ui.FactionInfoProvider;
        return provider is not null && !provider.WasCollected && provider.TryGetFaction(ui.factionID, out faction) &&
            faction is not null && !faction.WasCollected;
    }

    // The number the game prints on the reward, from whichever frame (open or locked) is showing.
    private static int RankNumber(FactionReward reward, int fallback)
    {
        foreach (var label in new[] { reward.rankText, reward.rankTextLocked })
            if (IsAlive(label) && int.TryParse(TextUtils.CleanText(label.text), out int rank)) return rank;
        return fallback;
    }

    private static string Read(TMP_Text label)
    {
        string text = IsAlive(label) ? TextUtils.CleanText(label.text) : null;
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static bool Inside<T>(Component label, FactionTile tile) where T : Component
    {
        var owner = label.GetComponentInParent<T>();
        return IsAlive(owner) && owner.transform.IsChildOf(tile.transform);
    }
}
