using System;

namespace LeAccess.Accessibility.Dialogue;

/// <summary>Whether NPC speech text (conversation lines, barks, cinematic subtitles) is spoken at all.</summary>
internal static class NpcLines
{
    private static Func<bool> _speak = () => true;

    internal static bool Speak => _speak();

    /// <summary>Reads the player's choice each time, so a config edit applies without a restart.</summary>
    internal static void Use(Func<bool> speak) => _speak = speak;
}
