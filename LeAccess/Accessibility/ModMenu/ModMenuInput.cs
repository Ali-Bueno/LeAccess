using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Exploration;
using Rewired;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.ModMenu;

/// <summary>The menu chord and the menu's own commands, read from the pad templates and the keyboard.</summary>
internal static class ModMenuInput
{
    private const double RepeatDelaySeconds = 0.4, RepeatIntervalSeconds = 0.12;
    private static MenuCommand _heldDirection;
    private static double _nextRepeat;

    /// <summary>The player the game reads; null before the game's input manager exists.</summary>
    internal static Player CurrentPlayer()
    {
        var manager = EpochInputManager.instance;
        var player = IsAlive(manager) ? manager.rewiredPlayer : null;
        return player is null || player.WasCollected ? null : player;
    }

    /// <summary>L1 pressed while Create, L3 or the mod button is held, or Ctrl + the open key.</summary>
    internal static bool ChordPressed(bool controller, KeyCode openKey, bool leftStickOnOtherPads)
    {
        bool pressed = openKey != KeyCode.None && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
            Input.GetKeyDown(openKey);
        if (!controller) { ShoulderGuard.Release(); return pressed; }
        var player = CurrentPlayer();
        var joysticks = player?.controllers?.Joysticks;
        var collection = joysticks?.TryCast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>();
        bool anyModifier = false;
        for (int index = 0; collection is not null && index < collection.Count; index++)
        {
            var joystick = joysticks[index];
            if (joystick is null || joystick.WasCollected) continue;
            var template = joystick.GetTemplate<IGamepadTemplate>();
            if (template is null || template.WasCollected) continue;
            bool backIsModifier = ModifierChoice.For(LeftStickModifier.IsPlayStation(joystick), leftStickOnOtherPads) ==
                ModifierButton.Back;
            var stick = template.leftStick;
            bool modifier = stick is not null && !stick.WasCollected && Held(stick.press) || backIsModifier && Held(template.back);
            if (!modifier) continue;
            anyModifier = true;
            ShoulderGuard.Hold(player, joystick, template);
            var shoulder = template.leftShoulder1;
            if (shoulder is not null && !shoulder.WasCollected && shoulder.justPressed) pressed = true;
        }
        if (!anyModifier) ShoulderGuard.ReleaseWhenUp();
        return pressed;
    }

    /// <summary>One menu command per frame; held directions repeat.</summary>
    internal static MenuCommand ReadCommand(double now)
    {
        var button = ButtonCommand();
        if (button != MenuCommand.None) { _heldDirection = MenuCommand.None; return button; }
        var direction = HeldDirection();
        if (direction != _heldDirection)
        {
            _heldDirection = direction;
            _nextRepeat = now + RepeatDelaySeconds;
            return direction;
        }
        if (direction == MenuCommand.None || now < _nextRepeat) return MenuCommand.None;
        _nextRepeat = now + RepeatIntervalSeconds;
        return direction;
    }

    internal static void Reset() => _heldDirection = MenuCommand.None;

    /// <summary>A key pressed for capture; Escape or Circle cancel (true with KeyCode.None).</summary>
    internal static bool TryReadKey(out KeyCode key)
    {
        key = KeyCode.None;
        if (Input.GetKeyDown(KeyCode.Escape) || AnyPad(template => Pressed(template.actionBottomRow2))) return true;
        if (!Input.anyKeyDown) return false;
        foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if (code >= KeyCode.Mouse0 || code == KeyCode.None || IsModifier(code) || !Input.GetKeyDown(code)) continue;
            key = code;
            return true;
        }
        return false;
    }

    private static MenuCommand ButtonCommand()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            return MenuCommand.Activate;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) return MenuCommand.Back;
        if (Input.GetKeyDown(KeyCode.Delete)) return MenuCommand.Reset;
        if (Input.GetKeyDown(KeyCode.P)) return MenuCommand.Preview;
        if (Input.GetKeyDown(KeyCode.I)) return MenuCommand.Details;
        if (Input.GetKeyDown(KeyCode.Home)) return MenuCommand.First;
        if (Input.GetKeyDown(KeyCode.End)) return MenuCommand.Last;
        var command = MenuCommand.None;
        AnyPad(template =>
        {
            command = Pressed(template.actionBottomRow1) ? MenuCommand.Activate
                : Pressed(template.actionBottomRow2) ? MenuCommand.Back
                : Pressed(template.actionTopRow2) ? MenuCommand.Reset
                : Pressed(template.actionTopRow1) ? MenuCommand.Preview
                : Pressed(template.start) ? MenuCommand.Close
                : RightStickUp(template) ? MenuCommand.Details : MenuCommand.None;
            return command != MenuCommand.None;
        });
        return command;
    }

    private static MenuCommand HeldDirection()
    {
        if (Input.GetKey(KeyCode.UpArrow)) return MenuCommand.Previous;
        if (Input.GetKey(KeyCode.DownArrow)) return MenuCommand.Next;
        if (Input.GetKey(KeyCode.LeftArrow)) return MenuCommand.Decrease;
        if (Input.GetKey(KeyCode.RightArrow)) return MenuCommand.Increase;
        var direction = MenuCommand.None;
        AnyPad(template =>
        {
            direction = PadDirection(template);
            return direction != MenuCommand.None;
        });
        return direction;
    }

    private static MenuCommand PadDirection(IGamepadTemplate template)
    {
        var pad = template.dPad;
        if (pad is not null && !pad.WasCollected)
        {
            if (Held(pad.up)) return MenuCommand.Previous;
            if (Held(pad.down)) return MenuCommand.Next;
            if (Held(pad.left)) return MenuCommand.Decrease;
            if (Held(pad.right)) return MenuCommand.Increase;
        }
        var stick = template.leftStick;
        if (stick is null || stick.WasCollected) return MenuCommand.None;
        var value = stick.value;
        float x = value.x, y = value.y;
        if (Math.Max(Math.Abs(x), Math.Abs(y)) < MenuDetailInput.PushThreshold) return MenuCommand.None;
        if (Math.Abs(y) >= Math.Abs(x)) return y > 0 ? MenuCommand.Previous : MenuCommand.Next;
        return x > 0 ? MenuCommand.Increase : MenuCommand.Decrease;
    }

    private static bool RightStickUp(IGamepadTemplate template)
    {
        var stick = template.rightStick;
        if (stick is null || stick.WasCollected) return false;
        var value = stick.value;
        var previous = stick.valuePrev;
        return MenuDetailInput.IsUpPush(value.x, value.y, previous.x, previous.y);
    }

    private static bool AnyPad(Func<IGamepadTemplate, bool> test) => MenuDetailInput.AnyGamepad(test);

    private static bool Held(IControllerTemplateButton button) => button is not null && !button.WasCollected && button.value;
    private static bool Pressed(IControllerTemplateButton button) => button is not null && !button.WasCollected && button.justPressed;

    private static bool IsModifier(KeyCode code) => code is KeyCode.LeftControl or KeyCode.RightControl or KeyCode.LeftShift or
        KeyCode.RightShift or KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.LeftWindows or KeyCode.RightWindows or
        KeyCode.LeftCommand or KeyCode.RightCommand or KeyCode.AltGr;
}
