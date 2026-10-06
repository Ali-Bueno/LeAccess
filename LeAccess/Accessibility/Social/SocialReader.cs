using System;
using LE.UI.Generic.UnityUI;
using LE.UI.Social.UnityUI;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using static LeAccess.Accessibility.Social.SocialWindows;

namespace LeAccess.Accessibility.Social;

/// <summary>Reads the Social panel's rows, sections, tabs, status, region, username windows and player options.</summary>
/// <remarks>See reference/social-reading.md.</remarks>
internal static class SocialReader
{
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        try
        {
            if (!IsAlive(element)) return false;
            if (IsAlive(element.GetComponentInParent<RightClickMenu>())) return TryReadOption(element, out text);
            var social = element.GetComponentInParent<FriendPostRequestsUI>();
            if (!IsAlive(social)) return false;
            SocialAccessibility.Observe(social);
            return TryReadButton(social, element, out text) || TryReadField(element, out text) ||
                TryReadSection(social, element, out text) || TryReadTab(element, out text) ||
                TryReadStatus(social, element, out text) || TryReadRegion(element, out text) ||
                TryReadMember(element, out text) || TryReadRow(social, element, out text);
        }
        catch (Exception) { text = null; return false; }
    }

    /// <summary>A part of a Social control that the cursor highlights (a row's card, the region's text field) is
    /// the same stop as the control that owns it.</summary>
    internal static GameObject Canonical(GameObject element)
    {
        try
        {
            if (!IsAlive(element) || IsAlive(element.GetComponent<Selectable>()) ||
                !IsAlive(element.GetComponentInParent<FriendPostRequestsUI>())) return element;
            var owner = element.GetComponentInParent<Selectable>();
            return IsAlive(owner) ? owner.gameObject : element;
        }
        catch (Exception) { return element; }
    }

    /// <summary>A section header's toggle says expanded or collapsed and its player count instead of on or off.</summary>
    internal static bool TryReadToggleState(Toggle toggle, bool value, out string text)
    {
        text = null;
        try
        {
            var social = IsAlive(toggle) ? toggle.GetComponentInParent<FriendPostRequestsUI>() : null;
            if (!IsAlive(social) || !TryGetSection(social, toggle, out var panel)) return false;
            text = SocialText.SectionState(value, CountRows(panel));
            return true;
        }
        catch (Exception) { text = null; return false; }
    }

    private static bool TryReadButton(FriendPostRequestsUI social, GameObject element, out string text)
    {
        text = null;
        var button = element.GetComponent<Button>();
        if (!IsAlive(button)) return false;
        string caption = Caption(button);
        foreach (var (window, _) in Fields(social))
            if (IsAlive(window) && element.transform.IsChildOf(window.transform))
            {
                text = SocialText.Button(caption ?? ModText.Get("social.close"));
                return true;
            }
        var row = element.GetComponentInParent<SocialPanelPlayerRef>();
        var requests = social.friendRequestsPanel;
        if (!IsAlive(row) || !IsAlive(requests) || !row.transform.IsChildOf(requests.transform)) return false;
        // The request's deny button is an icon without text.
        text = SocialText.Button(caption ?? ModText.Get("social.deny_request"));
        return true;
    }

    private static bool TryReadField(GameObject element, out string text)
    {
        text = null;
        if (!TryGetField(element, out var input, out var field)) return false;
        text = SocialText.Field(field, input.text);
        return true;
    }

    private static bool TryReadSection(FriendPostRequestsUI social, GameObject element, out string text)
    {
        text = null;
        var toggle = IsAlive(element.GetComponent<Button>()) ? element.GetComponentInChildren<Toggle>() :
            element.GetComponent<Toggle>();
        if (!IsAlive(toggle) || !TryGetSection(social, toggle, out var panel)) return false;
        text = SocialText.Section(HeaderCaption(toggle), toggle.isOn, CountRows(panel));
        return true;
    }

    private static bool TryReadTab(GameObject element, out string text)
    {
        text = null;
        var tab = element.GetComponent<TabUIElement>();
        if (!IsAlive(tab)) return false;
        string caption = TextUtils.GetTextFromHierarchy(element);
        if (caption == null) return false;
        text = SocialText.Tab(caption, IsAlive(tab.controller) && tab.controller.SelectedTabId == tab.elementID);
        return true;
    }

    private static bool TryReadStatus(FriendPostRequestsUI social, GameObject element, out string text)
    {
        text = null;
        var status = element.GetComponentInParent<ChangePlayerStatus>();
        var dropdown = IsAlive(status) ? status.statusDropdown : null;
        if (!IsAlive(dropdown) || !element.transform.IsChildOf(dropdown.transform)) return false;
        text = SocialText.Status(Label(dropdown.captionText), Label(social.socialTitle));
        return true;
    }

    private static bool TryReadRegion(GameObject element, out string text)
    {
        text = null;
        var region = element.GetComponent<RegionSelectionHelper>();
        if (!IsAlive(region)) return false;
        var party = region.GetComponentInParent<SocialPartyPanel>();
        string label = IsAlive(party) ? Label(party.regionCategoryLabel) : null;
        text = SocialText.Region(label, Label(region.textRegion), Label(region.textPing));
        return text != null;
    }

    private static bool TryReadMember(GameObject element, out string text)
    {
        text = null;
        var member = element.GetComponentInParent<SocialPartyMemberPanel>();
        if (!IsAlive(member) || !IsRowFocus(element, member)) return false;
        var leader = member.leaderIcon;
        text = SocialText.Member(Label(member.accountNameText), Label(member.characterNameText),
            Label(member.characterLevelText), Label(member.locationText), Label(member.statusText),
            IsAlive(leader) && leader.activeInHierarchy);
        return true;
    }

    private static bool TryReadRow(FriendPostRequestsUI social, GameObject element, out string text)
    {
        text = null;
        var row = element.GetComponentInParent<SocialPanelPlayerRef>();
        if (!IsAlive(row) || !IsRowFocus(element, row)) return false;
        string name = Label(row.socialPlayerName);
        string section = null;
        foreach (var (toggle, panel) in Sections(social))
            if (IsAlive(toggle) && IsAlive(panel) && row.transform.IsChildOf(panel.transform))
                section = HeaderCaption(toggle);
        // A row still loading hides its name; the game shows only a shimmer.
        text = SocialText.Row(name, Label(row.socialCharacterName), Label(row.socialCharacterLevel),
            Label(row.socialCharacterLocation), Label(row.socialStatusMsg), section, loading: name == null);
        return true;
    }

    private static bool TryReadOption(GameObject element, out string text)
    {
        text = null;
        var option = element.GetComponentInParent<RightClickBtnRef>();
        if (!IsAlive(option)) return false;
        string caption = Label(option.buttonText) ?? TextUtils.GetTextFromHierarchy(option.gameObject);
        if (caption == null) return false;
        var button = option.btnFunction;
        text = !IsAlive(button) || button.IsInteractable() ? caption :
            SocialText.Unavailable(caption, Reason(option.tooltip));
        return true;
    }

    // DisableButton enables the option's tooltip and stores the reason's localization key as its body.
    private static string Reason(GeneralTooltop tooltip) =>
        IsAlive(tooltip) && tooltip.enabled ? Localized(tooltip.bodyKey, tooltip.body) : null;

    private static string Localized(string key, string fallback)
    {
        string text = string.IsNullOrWhiteSpace(key) ? fallback : Localization.GetText(key, fallback ?? "");
        return string.IsNullOrWhiteSpace(text) ? null : TextUtils.CleanText(text);
    }

    private static bool IsRowFocus(GameObject element, Component row)
    {
        var selectable = element.GetComponent<Selectable>();
        return !IsAlive(selectable) || element.Pointer == row.gameObject.Pointer;
    }

    private static (Toggle Toggle, GameObject Panel)[] Sections(FriendPostRequestsUI social) => new[]
    {
        (social.friendRequestToggle, social.friendRequestsPanel), (social.onlineFriendToggle, social.onlinePlayersPanel),
        (social.offlineFriendToggle, social.offlinePlayersPanel), (social.sentRequestToggle, social.sentRequestsPanel),
        (social.blockedToggle, social.blockedPlayersPanel)
    };

    private static bool TryGetSection(FriendPostRequestsUI social, Toggle toggle, out GameObject panel)
    {
        panel = null;
        foreach (var section in Sections(social))
            if (Same(section.Toggle, toggle) && IsAlive(section.Panel)) { panel = section.Panel; return true; }
        return false;
    }

    // Collapsing deactivates the rows but keeps them, so every player row in the panel counts.
    private static int CountRows(GameObject panel)
    {
        int count = 0;
        var parent = panel.transform;
        for (int i = 0; i < parent.childCount; i++)
            if (IsAlive(parent.GetChild(i).GetComponent<SocialPanelPlayerRef>())) count++;
        return count;
    }

    // The header button holds the section's caption beside its toggle.
    private static string HeaderCaption(Toggle toggle)
    {
        var header = toggle.transform.parent;
        if (!IsAlive(header)) return null;
        foreach (var label in header.GetComponentsInChildren<TMP_Text>())
            if (!label.transform.IsChildOf(toggle.transform) && Label(label) is string caption) return caption;
        return null;
    }
}
