using System;
using Rewired;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

internal static class VitalsInput
{
    private static string _keyboardFailure;
    private static string _controllerFailure;

    internal static bool ReportPressed(KeyCode key, bool controllerEnabled)
    {
        try
        {
            if (key != KeyCode.None && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
                !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) &&
                !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(key))
                return true;
        }
        catch (Exception ex) { ReportFailure("Keyboard", ex, ref _keyboardFailure); }
        if (!controllerEnabled) return false;
        try
        {
            var manager = EpochInputManager.instance;
            var player = IsAlive(manager) ? manager.rewiredPlayer : null;
            if (player is null || player.WasCollected) return false;
            var controllers = player.controllers;
            if (controllers is null || controllers.WasCollected) return false;
            var joysticks = controllers.Joysticks;
            if (joysticks is null || joysticks.WasCollected) return false;
            var collection = joysticks.TryCast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>();
            if (collection is null || collection.WasCollected) return false;
            for (int index = 0; index < collection.Count; index++)
            {
                var joystick = joysticks[index];
                if (joystick is null || joystick.WasCollected) continue;
                try
                {
                    var template = joystick.GetTemplate<IGamepadTemplate>();
                    if (template is null || template.WasCollected) continue;
                    var back = template.back;
                    if (back is not null && !back.WasCollected && back.justPressed) return true;
                }
                catch (Exception ex) { ReportFailure("Controller", ex, ref _controllerFailure); }
            }
        }
        catch (Exception ex) { ReportFailure("Controller", ex, ref _controllerFailure); }
        return false;
    }

    private static void ReportFailure(string source, Exception exception, ref string previous)
    {
        string failure = $"{exception.GetType().Name}: {exception.Message}";
        if (previous == failure) return;
        previous = failure;
        Plugin.Log.LogWarning($"Vitals {source.ToLowerInvariant()} input unavailable: {failure}");
    }

    internal static void Reset() { _keyboardFailure = _controllerFailure = null; }
}
