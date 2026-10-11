using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Keeps screen-reader keys (Ctrl silences speech) from counting as "any key" that skips the intro movie.</summary>
/// <remarks>See reference/runtime-feedback.md#intro-skipped-by-screen-reader-keys-2026-10-10.</remarks>
internal static class IntroSkipGuard
{
    private static readonly List<KeyCode> PressedKeys = new();
    private static string _lastFailure;
    private static bool _logged;

    /// <summary>Turns the game's "a key went down" back when the only new input was screen-reader keys.</summary>
    internal static void Review(ref bool anyKeyDown)
    {
        if (!anyKeyDown) return;
        try
        {
            var controllers = ReInput.controllers;
            var keyboard = controllers?.Keyboard;
            var mouse = controllers?.Mouse;
            if (keyboard is null || keyboard.WasCollected || mouse is null || mouse.WasCollected) return;
            if (mouse.GetAnyButtonDown() || AnyJoystickButtonDown(controllers)) return;
            PressedKeys.Clear();
            for (int i = 0; i < keyboard.buttonCount; i++)
                if (keyboard.GetButtonDown(i)) PressedKeys.Add(keyboard.GetKeyCodeByButtonIndex(i));
            if (!ControllerModeKeys.OnlyScreenReaderKeys(PressedKeys)) return;
            anyKeyDown = false;
            if (_logged) return;
            _logged = true;
            Plugin.Log.LogInfo("[Intro] skip ignored: screen-reader key");
        }
        catch (Exception ex)
        {
            string failure = $"{ex.GetType().Name}: {ex.Message}";
            if (_lastFailure == failure) return;
            _lastFailure = failure;
            Plugin.Log.LogWarning($"Intro skip guard unavailable: {ex}");
        }
    }

    private static bool AnyJoystickButtonDown(ReInput.ControllerHelper controllers)
    {
        var joysticks = controllers.Joysticks;
        if (joysticks is null || joysticks.WasCollected) return false;
        for (int i = 0; i < controllers.joystickCount; i++)
        {
            var joystick = joysticks[i];
            if (joystick is not null && !joystick.WasCollected && joystick.GetAnyButtonDown()) return true;
        }
        return false;
    }
}
