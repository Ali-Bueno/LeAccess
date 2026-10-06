using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LeAccess.Accessibility.Social;

/// <summary>Recognises the game's system chat lines about parties, friends and blocking by their localized templates.</summary>
/// <remarks>Keys come from the game's UI and Common string tables; see reference/social-reading.md.</remarks>
internal static class SocialChatLines
{
    private static readonly Regex Placeholder = new(@"\{\d+\}");

    // A template without placeholders is posted alone or after the player's name and a space.
    internal static readonly string[] Keys =
    {
        "Chat_SuccessMessage_InviteSend", "Chat_SuccessMessage_FriendRequest", "Chat_SuccessMessage_FriendRemoved",
        "Chat_SuccessMessage_UserBlocked", "Chat_SuccessMessage_Added_To_ Block_List",
        "Chat_SuccessMessage_Removed_From_Block_List", "Chat_ErrorMessage_FailedInvite", "Chat_ErrorMessage_FriendRequest",
        "Chat_ErrorMessage_Block", "Chat_ErrorMessage_Unblock", "Chat_ErrorMessage_AlreadyAddedToBlockList",
        "Chat_ErrorMessage_NotRecordedInBlockList", "Chat_ErrorMessage_BlockNotValid", "Chat_ErrorMessage_UnblockNotValid",
        "Chat_ErrorMessage_BlockYourself", "Chat_ErrorMessage_UnblockYourself", "Chat_ErrorMessage_FindPlayer",
        "Chat_ErrorMessage_PartyMember", "Chat_Message_SetPartyRegion", "ErrorCode_PartyJoinError", "ErrorCode_PartyInvite",
        "ErrorCode_PartyMaxMembers", "ErrorCode_PartyFull", "ErrorCode_PartyRejected", "ErrorCode_PartyCycleMismatch",
        "Common_CycleMissmatch_invite_Legacy", "Common_CycleMissmatch_invite_Cycle", "ErrorCode_PartyInviteExistingDungeonRun",
        "ErrorCode_PartyServerProfileMismatch", "ErrorCode_PartySoloChallenge", "ErrorCode_PartyModeMismatch",
        "ErrorCode_PartyLeave", "ErrorCode_PartyPromote", "ErrorCode_PartyCreate", "ErrorCode_PartyFetch",
        "ErrorCode_PartySetRegion", "ErrorCode_KickedForInactivity"
    };

    /// <summary>Whether a system line is one of the social templates, given the game's current text for a key.</summary>
    internal static bool IsSocial(string content, Func<string, string> localize)
    {
        string line = Clean(content);
        if (string.IsNullOrEmpty(line) || localize == null) return false;
        foreach (string key in Keys)
        {
            string template = Clean(localize(key));
            if (!string.IsNullOrEmpty(template) && Matches(line, template)) return true;
        }
        return false;
    }

    private static bool Matches(string line, string template)
    {
        if (!Placeholder.IsMatch(template))
            return line == template || line.EndsWith(" " + template, StringComparison.Ordinal);
        string pattern = "^" + string.Join(".+", Placeholder.Split(template).Select(Regex.Escape)) + "$";
        return Regex.IsMatch(line, pattern, RegexOptions.Singleline);
    }

    private static string Clean(string text) =>
        text == null ? null : Regex.Replace(Regex.Replace(text, "<[^>]+>", ""), @"\s+", " ").Trim();
}
