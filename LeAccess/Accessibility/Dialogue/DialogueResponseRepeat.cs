namespace LeAccess.Accessibility.Dialogue;

/// <summary>Decides when a direction push re-reads the focused dialogue response.</summary>
internal static class DialogueResponseRepeat
{
    /// <summary>A push that moved focus, or after which something already spoke, is spoken by the game's own focus path.</summary>
    internal static bool ShouldRepeat(bool pushed, bool focusMoved, bool spokeThisFrame) =>
        pushed && !focusMoved && !spokeThisFrame;
}
