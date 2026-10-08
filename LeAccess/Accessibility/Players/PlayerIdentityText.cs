using LeAccess.I18n;

namespace LeAccess.Accessibility.Players;

/// <summary>Speech for another player: public username, then character name and level when known.</summary>
internal static class PlayerIdentityText
{
    internal static string Compose(string username, string character, int level) =>
        Join(username, character, level > 0 ? ModText.Format("progression.level", level) : null);

    internal static string Join(string username, string character, string levelText)
    {
        var parts = new System.Collections.Generic.List<string>(3);
        foreach (var part in new[] { username, character, levelText })
            if (!string.IsNullOrWhiteSpace(part)) parts.Add(part.Trim());
        return string.Join(", ", parts);
    }
}
