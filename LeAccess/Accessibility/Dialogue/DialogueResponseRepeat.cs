namespace LeAccess.Accessibility.Dialogue;

/// <summary>Decides when a direction push re-reads the focused dialogue response.</summary>
internal static class DialogueResponseRepeat
{
    /// <summary>Only a lone response: with more, the game moves focus a frame after the push and speaks it itself.</summary>
    internal static bool ShouldRepeat(bool pushed, int responses, bool focusMoved, bool spokeThisFrame) =>
        pushed && responses == 1 && !focusMoved && !spokeThisFrame;
}
