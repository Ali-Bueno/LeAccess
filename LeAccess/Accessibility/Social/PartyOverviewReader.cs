using System;
using LE.Services.Models;
using LE.UI.Party.UnityUI;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Social;

/// <summary>Reads the party overview panel (radial menu "Group"): member cards and their portal buttons.</summary>
/// <remarks>See reference/social-reading.md. The card (Member N) is the Selectable; the cursor highlights its
/// "Class" child, so both are one stop.</remarks>
internal static class PartyOverviewReader
{
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        try
        {
            if (!IsAlive(element)) return false;
            var member = element.GetComponentInParent<PartyOverviewMemberPanel>();
            if (!IsAlive(member)) return false;
            string name = MemberName(member);
            var portal = member.requestPortalButton;
            text = IsAlive(portal) && element.transform.IsChildOf(portal.transform) ?
                PartyText.Portal(name, portal.IsInteractable()) : ReadMember(member, name);
            return true;
        }
        catch (Exception) { text = null; return false; }
    }

    /// <summary>Whether the focus is inside a party member card, whose non-selectable parts belong to the card.</summary>
    internal static bool Owns(GameObject element) =>
        IsAlive(element) && IsAlive(element.GetComponentInParent<PartyOverviewMemberPanel>());

    private static string ReadMember(PartyOverviewMemberPanel member, string name)
    {
        var party = member.partyMember;
        var leader = member.leaderTransform;
        bool isLeader = IsAlive(leader) ? leader.activeInHierarchy : party != null && party.IsLeader;
        return PartyText.Member(name, ClassName(party), isLeader, member.isInDeadState, IsAlive(member.playerActorSync),
            Percent(member.healthBar), Percent(member.wardBar));
    }

    private static string MemberName(PartyOverviewMemberPanel member)
    {
        var label = member.accountNameText;
        string shown = IsAlive(label) ? TextUtils.CleanText(label.text) : null;
        if (!string.IsNullOrWhiteSpace(shown)) return shown;
        var identity = member.partyMember?.Identity;
        return identity is null ? null : TextUtils.CleanText(identity.Username);
    }

    // The party record keeps the class as an enum; its name is the key the game localizes.
    private static string ClassName(PartyMember_Full party)
    {
        var id = party?.characterClass;
        if (id is null || !id.HasValue) return null;
        string name = id.Value.ToString();
        var common = Localization.Common;
        return TextUtils.CleanText(common is null || common.WasCollected ? name : common.GetClass(name));
    }

    private static int Percent(Image bar) => IsAlive(bar) ? PartyText.Percent(bar.fillAmount) : 0;
}
