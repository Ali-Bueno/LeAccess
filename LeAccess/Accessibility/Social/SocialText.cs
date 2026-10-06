using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Social;

/// <summary>The three username fields of the Social panel's pop-up windows.</summary>
internal enum SocialField { PartyInvite, AddFriend, Block }

/// <summary>Spoken wording for the Social panel, built from the game's own labels.</summary>
/// <remarks>See reference/social-reading.md.</remarks>
internal static class SocialText
{
    private static string Join(params string[] parts) => ScreenContentTracker.Combine(parts);

    internal static string Row(string name, string character, string level, string location, string status,
        string section, bool loading) =>
        Join(loading ? ModText.Get("social.loading") : name, character, level, location, status, section);

    internal static string Member(string account, string character, string level, string location, string status,
        bool leader) =>
        Join(account, character, level, location, status, leader ? ModText.Get("social.leader") : null);

    internal static string Section(string caption, bool expanded, int players) =>
        Join(caption, SectionState(expanded, players));

    internal static string SectionState(bool expanded, int players) =>
        Join(ModText.Get(expanded ? "social.expanded" : "social.collapsed"), Players(players));

    private static string Players(int players) => players switch
    {
        0 => ModText.Get("social.players_none"),
        1 => ModText.Get("social.players_one"),
        _ => ModText.Format("social.players_many", players)
    };

    internal static string Tab(string caption, bool selected) =>
        ModText.Format(selected ? "social.tab_selected" : "social.tab", caption);

    internal static string Status(string value, string username) =>
        Join(string.IsNullOrWhiteSpace(value) ? null : ModText.Format("social.status", TrimDecoration(value)),
            string.IsNullOrWhiteSpace(username) ? null : ModText.Format("social.username", username));

    internal static string Region(string label, string region, string ping)
    {
        if (string.IsNullOrWhiteSpace(region)) return null;
        string value = string.IsNullOrWhiteSpace(ping) ? region : ModText.Format("social.region_ping", region, ping);
        return string.IsNullOrWhiteSpace(label) ? value : ModText.Format("control.field_value", label, value);
    }

    internal static string FieldLabel(SocialField field) => ModText.Get(field switch
    {
        SocialField.PartyInvite => "social.field_invite",
        SocialField.AddFriend => "social.field_add_friend",
        _ => "social.field_block"
    });

    internal static string Field(SocialField field, string value) =>
        Join(FieldLabel(field), ModText.Get("control.text_field"),
            string.IsNullOrEmpty(value) ? ModText.Get("social.field_empty") : value, ModText.Get("social.field_hint"));

    internal static string Button(string caption) => ModText.Format("social.button", caption);

    internal static string Unavailable(string caption, string reason) =>
        Join(ModText.Format("control.disabled", caption), reason);

    internal static string Options(string player) =>
        string.IsNullOrWhiteSpace(player) ? null : ModText.Format("social.options", player);

    /// <summary>The window's own labels, then the username format for the three username windows.</summary>
    internal static string Intro(IEnumerable<string> labels, bool usernameWindow)
    {
        var parts = new List<string>(labels);
        if (usernameWindow) parts.Add(ModText.Get("social.format_hint"));
        return ScreenContentTracker.Combine(parts);
    }

    /// <summary>The game's invitation prompt: inviter and message as one sentence, then its two buttons.</summary>
    internal static string Invite(string inviter, string message, string accept, string decline)
    {
        string sentence = string.IsNullOrWhiteSpace(inviter) ? message :
            string.IsNullOrWhiteSpace(message) ? inviter : inviter + " " + message;
        string choices = string.IsNullOrWhiteSpace(accept) || string.IsNullOrWhiteSpace(decline) ? accept ?? decline :
            ModText.Format("social.choices", accept, decline);
        return Join(sentence, choices);
    }

    /// <summary>What a change to a username field sounds like: the typed or erased characters, else the whole value.</summary>
    internal static string Edit(string before, string after)
    {
        before ??= "";
        after ??= "";
        if (before == after) return null;
        if (after.Length == 0) return ModText.Get("social.field_empty");
        if (after.Length > before.Length && after.StartsWith(before, System.StringComparison.Ordinal))
            return after.Substring(before.Length);
        if (before.Length > after.Length && before.StartsWith(after, System.StringComparison.Ordinal))
            return ModText.Format("social.erased", before.Substring(after.Length));
        return after;
    }

    /// <summary>Drops the status option's coloured dot and spacing, keeping its words.</summary>
    internal static string TrimDecoration(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        int start = 0, end = text.Length;
        while (start < end && !char.IsLetterOrDigit(text[start])) start++;
        while (end > start && !char.IsLetterOrDigit(text[end - 1]) && text[end - 1] != '.') end--;
        return text.Substring(start, end - start);
    }
}
