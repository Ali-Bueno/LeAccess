using System;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.PanelSystem;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Which endgame panels the screen-content reader speaks on opening, and what part of them it reads.</summary>
/// <remarks>See reference/endgame-panels.md.</remarks>
internal static class EndgameWindows
{
    /// <summary>Panels read as whole windows (their controls are read on focus, or by <see cref="EndgameAccessibility"/>).</summary>
    internal static bool Covers(Panel panel) =>
        IsAlive(panel.TryCast<AberrothEntrancePanel>()) || IsAlive(panel.TryCast<ArenaEntrancePanel>()) ||
        IsAlive(panel.TryCast<SanctuaryOfEterraPanel>()) || IsAlive(panel.TryCast<PinnacleBossVotingPanel>()) ||
        IsAlive(panel.TryCast<MonolithPanel>()) || IsAlive(panel.TryCast<ChooseBlessingPanel>()) ||
        IsAlive(panel.TryCast<ChooseEchoRewardPanel>()) || IsAlive(panel.TryCast<NemesisPanel>()) ||
        IsAlive(panel.TryCast<ObservatoryPanel>()) || IsAlive(panel.TryCast<LadderPanel>()) ||
        IsAlive(panel.TryCast<RageOfMorditasRewardPanel>());

    /// <summary>Every panel of this group, including the ones the dungeon and modal readers already cover.</summary>
    internal static bool IsFamily(Panel panel) =>
        Covers(panel) || IsAlive(panel.TryCast<TemporalSanctumEntrancePanel>()) ||
        IsAlive(panel.TryCast<SoulfireBastionEntrancePanel>()) || IsAlive(panel.TryCast<LightlessArborEntrancePanel>()) ||
        IsAlive(panel.TryCast<BossConfirmationPanel>()) || IsAlive(panel.TryCast<NemesisReplacementConfirmationModal>()) ||
        IsAlive(panel.TryCast<SootheMorditasModal>());

    /// <summary>Item slots in these panels (key, nemesis, reward) read like inventory slots; blessings have their own reader.</summary>
    internal static bool AllowsItemReading(GameObject slot)
    {
        var panel = slot.GetComponentInParent<Panel>();
        return IsAlive(panel) && IsFamily(panel) && !IsAlive(panel.TryCast<ChooseBlessingPanel>());
    }

    /// <summary>The reader for panels whose window text needs filtering; null leaves the default reading.</summary>
    internal static Func<string> Reader(Panel panel)
    {
        var root = panel.gameObject;
        var monolith = panel.TryCast<MonolithPanel>();
        if (IsAlive(monolith)) return () => IsAlive(monolith) ? MonolithReader.Intro(monolith) : null;
        var soothe = panel.TryCast<SootheMorditasModal>();
        if (IsAlive(soothe))
            return () => ScreenContentReader.Read(root, null, IsAlive(soothe) ? soothe._progressSliderText : null,
                include: label => IsStatic(root, label));
        // Votes are spoken per player by the endgame service, and the countdown would reread the window every second.
        if (VoteRosterReader.TryRead(panel, out _))
            return () => ScreenContentReader.Read(root, null, null, include: label => !VoteRosterReader.IsRowOrTimer(panel, label));
        // Options, rows and lists are spoken one by one as they get focus.
        if (IsAlive(panel.TryCast<SanctuaryOfEterraPanel>()) || IsAlive(panel.TryCast<ChooseEchoRewardPanel>()) ||
            IsAlive(panel.TryCast<LadderPanel>()) || IsAlive(panel.TryCast<ChooseBlessingPanel>()))
            return () => ScreenContentReader.Read(root, null, null, include: label => IsStatic(root, label));
        return null;
    }

    private static bool IsStatic(GameObject root, Component label) => !Inside<Selectable>(root, label) && !Inside<ScrollRect>(root, label);

    private static bool Inside<T>(GameObject root, Component label) where T : Component
    {
        var owner = label.GetComponentInParent<T>();
        return IsAlive(owner) && owner.transform.IsChildOf(root.transform);
    }
}
