using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Keeps controller mode while only screen-reader keys are pressed, so menus keep their focus.</summary>
/// <remarks>See reference/runtime-feedback.md#controller-mode-lost-to-screen-reader-keys-2026-10-04.</remarks>
internal static class ControllerModeGuard
{
    private static readonly List<KeyCode> ActiveKeys = new();
    private static string _lastFailure;
    private static bool _loggedKeep;

    /// <summary>Turns the game's "leave controller mode" verdict back when the keyboard activity was only screen-reader keys.</summary>
    internal static void Review(EpochInputManager manager, ref bool shouldBeActive)
    {
        if (shouldBeActive) return;
        try
        {
            // The game's other conditions still apply: a connected, assigned gamepad and no mouse activity this frame.
            var assigned = EpochInputManager.AssignedController;
            if (!EpochInputManager.IsControllerActive || !IsAlive(manager) || assigned is null || assigned.WasCollected) return;
            var player = manager.rewiredPlayer;
            if (player is null || player.WasCollected || player.controllers is null || player.controllers.joystickCount <= 0) return;
            var controllers = ReInput.controllers;
            var mouse = controllers?.Mouse;
            var keyboard = controllers?.Keyboard;
            if (mouse is null || mouse.WasCollected || keyboard is null || keyboard.WasCollected) return;
            double now = ReInput.time.unscaledTime;
            if (mouse.GetAnyButton() || mouse.GetAnyButtonUp() || mouse.GetLastTimeAnyAxisActive() == now ||
                mouse.GetLastTimeAnyAxisChanged() == now) return;

            ActiveKeys.Clear();
            // A released key still counts on its release frame, so released screen-reader keys are kept too.
            for (int i = 0; i < keyboard.buttonCount; i++)
                if (keyboard.GetButton(i) || keyboard.GetButtonUp(i)) ActiveKeys.Add(keyboard.GetKeyCodeByButtonIndex(i));
            if (!ControllerModeKeys.OnlyScreenReaderKeys(ActiveKeys)) return;
            shouldBeActive = true;
            if (!_loggedKeep)
            {
                _loggedKeep = true;
                Plugin.Log.LogInfo($"[Controller mode] kept while screen-reader keys were pressed ({string.Join(", ", ActiveKeys)})");
            }
        }
        catch (Exception ex)
        {
            string failure = $"{ex.GetType().Name}: {ex.Message}";
            if (_lastFailure == failure) return;
            _lastFailure = failure;
            Plugin.Log.LogWarning($"Controller mode guard unavailable: {ex}");
        }
    }
}
