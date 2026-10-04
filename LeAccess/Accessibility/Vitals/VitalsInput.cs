using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Exploration;
using UnityEngine;

namespace LeAccess.Accessibility.Vitals;

internal static class VitalsInput
{
    private static string _keyboardFailure;

    internal static bool ReportPressed(KeyCode key, bool controllerEnabled)
    {
        try
        {
            if (MenuDetailInput.KeyPressed(key)) return true;
        }
        catch (Exception ex) { ReportFailure("Keyboard", ex, ref _keyboardFailure); }
        // Create+Square: ExplorationInput already ran this frame and owns the Create modifier.
        return controllerEnabled && ExplorationInput.Command == ExplorationCommand.Vitals;
    }

    private static void ReportFailure(string source, Exception exception, ref string previous)
    {
        string failure = $"{exception.GetType().Name}: {exception.Message}";
        if (previous == failure) return;
        previous = failure;
        Plugin.Log.LogWarning($"Vitals {source.ToLowerInvariant()} input unavailable: {failure}");
    }

    internal static void Reset() { _keyboardFailure = null; }
}
