using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.Morditas;

/// <summary>While the Morditas reader follows an encounter in the current scene, the generic notification and objective
/// readers leave its pop-ups, widgets and instructions to it (reference/rage-of-morditas.md#one-speaker).</summary>
internal static class MorditasSpeechOwner
{
    private static readonly List<string> Owned = new();

    internal static bool Active { get; private set; }

    /// <summary>Where the encounter stands, for the on-demand report; null outside one.</summary>
    internal static string Status { get; set; }

    /// <summary>Claims the encounter; texts are the game's pop-up sentences the reader speaks itself.</summary>
    internal static void Begin(IEnumerable<string> texts)
    {
        Owned.Clear();
        foreach (string text in texts)
            if (!string.IsNullOrWhiteSpace(text)) Owned.Add(text.Trim());
        Active = true;
    }

    internal static bool OwnsText(string text)
    {
        if (!Active || string.IsNullOrWhiteSpace(text)) return false;
        foreach (string owned in Owned)
            if (text.Contains(owned, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static void End()
    {
        Owned.Clear();
        Active = false;
        Status = null;
    }
}
