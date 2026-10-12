using System;
using LE.Services.Models;
using LeAccess.Accessibility.Utils;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Social;

/// <summary>Character name of a friend, from the social manager's presence cache (see reference/nearby-player-names.md).</summary>
internal static class FriendCharacterNames
{
    internal static string Read(UserIdentity identity)
    {
        try
        {
            var manager = SocialManager.instance;
            if (!IsAlive(manager) || identity is null || identity.WasCollected) return null;
            var cache = manager._friendStatusCache;
            string key = identity.MasterAccountID;
            if (cache is null || cache.WasCollected || string.IsNullOrEmpty(key) || !cache.ContainsKey(key)) return null;
            // Never TryGetValue: its interop out slot is pointer-sized and the native struct overruns it (reference/nearby-player-names.md).
            FriendStatus status = cache[key];
            if (status is null || status.WasCollected) return null;
            // The cache entry must belong to this exact player.
            if (!string.Equals(status.UserIdentity?.MasterAccountID, key, StringComparison.Ordinal)) return null;
            var summary = status.CharacterSummary;
            string name = summary is null || summary.WasCollected ? null : TextUtils.CleanText(summary.CharacterName);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch (Exception) { return null; }
    }
}
