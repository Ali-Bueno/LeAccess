using System.Collections.Generic;

namespace LeAccess.Accessibility.CharacterCreation;

internal enum PhysicalTypingAction { Character, Backspace, Submit, Cancel }

internal readonly struct PhysicalTypingKey
{
    internal PhysicalTypingKey(PhysicalTypingAction action, char character = '\0')
    {
        Action = action;
        Character = character;
    }

    internal PhysicalTypingAction Action { get; }
    internal char Character { get; }
}

/// <summary>Turns one frame of physical keyboard text into edits for the screen keyboard.</summary>
internal static class PhysicalTypingKeys
{
    // Unity's Input.inputString reports Backspace as '\b' and Enter as '\n' or '\r'.
    private const char Backspace = '\b', LineFeed = '\n', CarriageReturn = '\r';

    /// <summary>Edits in typing order. Enter or Escape closes the keyboard, so nothing follows them.</summary>
    internal static List<PhysicalTypingKey> Translate(string typed, bool escapePressed)
    {
        var keys = new List<PhysicalTypingKey>();
        foreach (char character in typed ?? "")
        {
            if (character is LineFeed or CarriageReturn)
            {
                keys.Add(new PhysicalTypingKey(PhysicalTypingAction.Submit));
                return keys;
            }
            if (character == Backspace) keys.Add(new PhysicalTypingKey(PhysicalTypingAction.Backspace));
            // Ctrl shortcuts and Escape arrive as control characters; they never type.
            else if (!char.IsControl(character)) keys.Add(new PhysicalTypingKey(PhysicalTypingAction.Character, character));
        }
        if (escapePressed) keys.Add(new PhysicalTypingKey(PhysicalTypingAction.Cancel));
        return keys;
    }
}
