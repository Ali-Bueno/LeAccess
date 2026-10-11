using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Keeps screen-reader keys (Ctrl silences speech) from arming the intro movie's two-press skip.</summary>
/// <remarks>See reference/runtime-feedback.md#intro-skipped-by-screen-reader-keys-2026-10-10.</remarks>
internal static class IntroSkipGuard
{
    private static readonly List<KeyCode> PressedKeys = new();
    private static bool _armed;
    private static string _lastFailure;
    private static bool _logged;

    /// <summary>Polled after the game's Update: disarms a first press that was only screen-reader keys.</summary>
    internal static void LateUpdate()
    {
        try
        {
            var video = CharacterSelectionVideoManager.instance;
            bool armed = video is not null && !video.WasCollected && video.firstPressDone;
            bool armedNow = armed && !_armed;
            _armed = armed;
            if (!armedNow || !OnlyScreenReaderKeysWentDown()) return;
            video.firstPressDone = false;
            _armed = false;
            var prompt = video.skipText;
            if (prompt is not null && !prompt.WasCollected) prompt.SetActive(false);
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

    private static bool OnlyScreenReaderKeysWentDown()
    {
        var controllers = ReInput.controllers;
        var keyboard = controllers?.Keyboard;
        var mouse = controllers?.Mouse;
        if (keyboard is null || keyboard.WasCollected || mouse is null || mouse.WasCollected) return false;
        if (mouse.GetAnyButtonDown() || AnyJoystickButtonDown(controllers)) return false;
        PressedKeys.Clear();
        for (int i = 0; i < keyboard.buttonCount; i++)
            if (keyboard.GetButtonDown(i)) PressedKeys.Add(keyboard.GetKeyCodeByButtonIndex(i));
        return ControllerModeKeys.OnlyScreenReaderKeys(PressedKeys);
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
