using System;
using Rewired;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class MenuDetailInput
{
    // Match D2R's deliberate half-travel gesture on Rewired's normalized axes.
    internal const float PushThreshold = 0.5f;
    private static string _lastFailure;

    internal static bool IsUpPush(float x, float y, float previousX, float previousY) =>
        Math.Abs(previousX) < PushThreshold && Math.Abs(previousY) < PushThreshold &&
        y >= PushThreshold && y >= Math.Abs(x);

    internal static bool IsDownPush(float x, float y, float previousX, float previousY) =>
        IsUpPush(x, -y, previousX, -previousY);

    internal static bool KeyPressed(KeyCode key) => key != KeyCode.None &&
        !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
        !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) &&
        !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(key);

    internal static bool ReadRequested() => ReadStickRequested(up: true);

    private static bool ReadStickRequested(bool up)
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible) return false;
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
                var template = joystick.GetTemplate<IGamepadTemplate>();
                if (template is null || template.WasCollected) continue;
                var stick = template.rightStick;
                if (stick is null || stick.WasCollected) continue;
                var value = stick.value;
                var previous = stick.valuePrev;
                if (up ? IsUpPush(value.x, value.y, previous.x, previous.y)
                    : IsDownPush(value.x, value.y, previous.x, previous.y)) return true;
            }
        }
        catch (Exception ex)
        {
            if (_lastFailure != ex.Message)
            {
                _lastFailure = ex.Message;
                Plugin.Log.LogWarning($"Menu detail input unavailable: {ex.Message}");
            }
        }
        return false;
    }
}
