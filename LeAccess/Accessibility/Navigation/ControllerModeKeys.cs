using System.Collections.Generic;
using UnityEngine;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Decides whether keyboard activity came only from screen-reader keys, which must not end controller mode.</summary>
internal static class ControllerModeKeys
{
    // Screen readers use these alone or as modifiers: Ctrl silences, Shift pauses, Insert/CapsLock are the reader key.
    private static readonly HashSet<KeyCode> ScreenReaderKeys = new()
    {
        KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift,
        KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.AltGr, KeyCode.Insert, KeyCode.CapsLock,
        KeyCode.LeftWindows, KeyCode.RightWindows, KeyCode.LeftCommand, KeyCode.RightCommand
    };

    /// <summary>True when at least one key was active and every active key is a screen-reader key.</summary>
    internal static bool OnlyScreenReaderKeys(IEnumerable<KeyCode> active)
    {
        bool any = false;
        foreach (var key in active)
        {
            if (!ScreenReaderKeys.Contains(key)) return false;
            any = true;
        }
        return any;
    }
}
