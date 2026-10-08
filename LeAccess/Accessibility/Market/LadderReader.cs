using System.Collections.Generic;
using LeAccess.Accessibility.Endgame;
using LeAccess.Accessibility.Panels;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads ladder rows (rank, characters, class, level, score), top placements, tabs and the empty list notice.</summary>
/// <remarks>Online data; every field is checked and a missing row reads nothing. See reference/endgame-panels.md#ladder.</remarks>
internal static class LadderReader
{
    internal static bool Owns(GameObject element) => IsAlive(element) &&
        (Row(element) != null || TopPlace(element) != null || IsAlive(element.GetComponentInParent<TabUIElement>()));

    internal static bool TryRead(GameObject element, out EndgameStop stop)
    {
        stop = default;
        var row = Row(element);
        if (row != null) return Make(element, RowText(row), out stop);
        var top = TopPlace(element);
        if (top != null) return Make(element, TopText(top), out stop);
        var tab = element.GetComponentInParent<TabUIElement>();
        return IsAlive(tab) && Make(element, TabText(tab), out stop);
    }

    /// <summary>Said once when the list turns out empty (offline, or a filter without entries).</summary>
    internal static string EmptyNotice(LadderPanel panel)
    {
        var manager = IsAlive(panel) ? panel.GetComponentInChildren<LadderUIManager>() : null;
        var empty = IsAlive(manager) ? manager.noEntriesElement : null;
        return IsAlive(empty) && empty.gameObject.activeInHierarchy ? MarketText.LadderEmpty() : null;
    }

    private static bool Make(GameObject element, string text, out EndgameStop stop)
    {
        stop = string.IsNullOrWhiteSpace(text) ? default : new EndgameStop($"{element.GetInstanceID()}:{text}", text);
        return stop.Text != null;
    }

    // Skill icons inside a row keep their own tooltip reading.
    private static LadderRowPlayerElement Row(GameObject element)
    {
        var member = element.GetComponentInParent<LadderRecordMemberElement>();
        if (!IsAlive(member) || InSkills(member.abilitySelectables, element)) return null;
        return member.GetComponentInParent<LadderRowPlayerElement>();
    }

    private static LadderUITopPlaceElement TopPlace(GameObject element)
    {
        var top = element.GetComponentInParent<LadderUITopPlaceElement>();
        return IsAlive(top) && !InSkills(top._skillSelectables, element) ? top : null;
    }

    private static bool InSkills(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Selectable> skills, GameObject element)
    {
        if (skills is null || skills.WasCollected) return false;
        foreach (var skill in skills)
            if (IsAlive(skill) && element.transform.IsChildOf(skill.transform)) return true;
        return false;
    }

    private static string RowText(LadderRowPlayerElement row)
    {
        var members = new List<LadderMember>();
        var subs = row.subElements;
        for (int i = 0; subs is not null && !subs.WasCollected && i < subs.Count; i++)
        {
            var sub = subs[i];
            if (!IsAlive(sub) || !sub.gameObject.activeInHierarchy) continue;
            string name = Visible(sub.nameTMP) ?? Visible(sub.selfNameTMP);
            members.Add(new LadderMember(name, Visible(sub.classTMP), Visible(sub.levelTMP),
                IsAlive(sub.isDeadIcon) && sub.isDeadIcon.activeInHierarchy));
        }
        bool own = IsAlive(row.localPlayerBackground) && row.localPlayerBackground.activeInHierarchy;
        return MarketText.LadderRow(Visible(row.rankTMP), Visible(row.scoreTMP), members, own, 0, 0);
    }

    private static string TopText(LadderUITopPlaceElement top)
    {
        var members = new List<LadderMember>();
        string account = Visible(top._playerName);
        if (account != null) members.Add(new LadderMember(account, null, null, false));
        var party = top._partyMembers;
        for (int i = 0; party is not null && !party.WasCollected && i < party.Count; i++)
        {
            var group = party[i];
            if (IsAlive(group) && group.gameObject.activeInHierarchy && Visible(group.NameTMP) is { } name)
                members.Add(new LadderMember(name, null, null, IsAlive(group.IsDeadIcon) && group.IsDeadIcon.activeInHierarchy));
        }
        if (members.Count <= (account != null ? 1 : 0) && Visible(top._characterName) is { } character)
            members.Add(new LadderMember(character, null, null, IsAlive(top._playerIsDeadIcon) && top._playerIsDeadIcon.activeInHierarchy));
        return MarketText.LadderRow(top._rank.ToString(), Visible(top._ladderScore), members, false, 0, 0);
    }

    // Tabs carry their caption in a child label; the active one and the place come from the tab controller.
    private static string TabText(TabUIElement tab)
    {
        var controller = tab.controller;
        string name = TextUtils.GetTextFromHierarchy(tab.gameObject);
        if (string.IsNullOrWhiteSpace(name) || !IsAlive(controller) || controller.tabElements is not { WasCollected: false } tabs) return null;
        int position = 0, count = 0;
        for (int i = 0; i < tabs.Count; i++)
        {
            var other = tabs[i];
            if (!IsAlive(other) || !other.gameObject.activeInHierarchy) continue;
            count++;
            if (other.Pointer == tab.Pointer) position = count;
        }
        return PanelText.Tab(name, controller.ActiveTabId == tab.elementID, position, count);
    }

    private static string Visible(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text) ? TextUtils.CleanText(label.text) : null;
}
