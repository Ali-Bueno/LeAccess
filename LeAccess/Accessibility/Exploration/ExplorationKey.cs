using System;
using System.IO;
using System.Text;

namespace LeAccess.Accessibility.Exploration;

// File key of one character's saved exploration. Offline it is the character name and class; online the
// character data can lack either, so the key falls back to the name alone, then to the account name.
internal static class ExplorationKey
{
    private const int MaxNameLength = 64;
    private const string OfflinePrefix = "offline-";
    private const string OnlinePrefix = "online-";
    private const string AccountPrefix = "online-account-";

    internal static string Compose(bool online, string characterName, int? classId, string accountName)
    {
        string name = Sanitize(characterName);
        if (name.Length > 0)
        {
            if (!online) return classId is int offlineClass ? $"{OfflinePrefix}{name}-{offlineClass}" : null;
            return classId is int onlineClass ? $"{OnlinePrefix}{name}-{onlineClass}" : $"{OnlinePrefix}{name}";
        }
        string account = online ? Sanitize(accountName) : "";
        if (account.Length == 0) return null;
        return classId is int accountClass ? $"{AccountPrefix}{account}-{accountClass}" : $"{AccountPrefix}{account}";
    }

    internal static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var invalid = Path.GetInvalidFileNameChars();
        var text = new StringBuilder();
        foreach (char c in name.Trim())
            text.Append(char.IsControl(c) || Array.IndexOf(invalid, c) >= 0 || c == ' ' ? '_' : c);
        return text.Length > MaxNameLength ? text.ToString(0, MaxNameLength) : text.ToString();
    }
}
