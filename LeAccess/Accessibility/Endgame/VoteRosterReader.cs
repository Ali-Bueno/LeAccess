using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Reads who accepted or declined on the boss confirmation and pinnacle voting panels, from their status rows.</summary>
/// <remarks>See reference/endgame-panels.md#vote-panels.</remarks>
internal static class VoteRosterReader
{
    /// <summary>The panel whose rows are being read, with the lines that are not part of its spoken body.</summary>
    internal static bool TryRead(Panel panel, out List<VoteEntry> votes)
    {
        votes = null;
        var boss = panel.TryCast<BossConfirmationPanel>();
        if (IsAlive(boss) && IsAlive(boss._bossConfirmationPanelUI))
        {
            votes = new List<VoteEntry>();
            var rows = boss._bossConfirmationPanelUI.playerStatuses;
            for (int i = 0; rows is not null && !rows.WasCollected && i < rows.Length; i++)
                Add(votes, rows[i]?.gameObject, rows[i]?.playerName, rows[i]?.acceptedIcon, rows[i]?.declinedIcon);
            return true;
        }
        var pinnacle = panel.TryCast<PinnacleBossVotingPanel>();
        if (!IsAlive(pinnacle) || !IsAlive(pinnacle._pinnacleBossVotingPanelUI)) return false;
        votes = new List<VoteEntry>();
        var voteRows = pinnacle._pinnacleBossVotingPanelUI.playerStatuses;
        for (int i = 0; voteRows is not null && !voteRows.WasCollected && i < voteRows.Length; i++)
            Add(votes, voteRows[i]?.gameObject, voteRows[i]?.playerName, voteRows[i]?.acceptedIcon, voteRows[i]?.declinedIcon);
        return true;
    }

    /// <summary>Labels that belong to the rows or the countdown, so the panel text does not repeat them.</summary>
    internal static bool IsRowOrTimer(Panel panel, Component label)
    {
        var boss = panel.TryCast<BossConfirmationPanel>();
        if (IsAlive(boss) && IsAlive(boss._bossConfirmationPanelUI))
            return InsideAny(label, boss._bossConfirmationPanelUI.playerStatuses);
        var pinnacle = panel.TryCast<PinnacleBossVotingPanel>();
        var ui = IsAlive(pinnacle) ? pinnacle._pinnacleBossVotingPanelUI : null;
        if (!IsAlive(ui)) return false;
        return IsAlive(ui.TimerObject) && label.transform.IsChildOf(ui.TimerObject.transform) || InsideAny(label, ui.playerStatuses);
    }

    private static void Add(List<VoteEntry> votes, GameObject row, TMP_Text name, GameObject accepted, GameObject declined)
    {
        if (!IsAlive(row) || !row.activeInHierarchy) return;
        string player = IsAlive(name) ? TextUtils.CleanText(name.text) : null;
        if (string.IsNullOrWhiteSpace(player)) return;
        var state = IsAlive(accepted) && accepted.activeInHierarchy ? VoteState.Accepted :
            IsAlive(declined) && declined.activeInHierarchy ? VoteState.Declined : VoteState.Pending;
        votes.Add(new VoteEntry(player, state));
    }

    private static bool InsideAny(Component label, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<LE.UI.Dungeons.PlayerStatus> rows)
    {
        for (int i = 0; rows is not null && !rows.WasCollected && i < rows.Length; i++)
            if (IsAlive(rows[i]) && label.transform.IsChildOf(rows[i].transform)) return true;
        return false;
    }

    private static bool InsideAny(Component label, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<LE.UI.Voting.PlayerVoteStatus> rows)
    {
        for (int i = 0; rows is not null && !rows.WasCollected && i < rows.Length; i++)
            if (IsAlive(rows[i]) && label.transform.IsChildOf(rows[i].transform)) return true;
        return false;
    }
}
