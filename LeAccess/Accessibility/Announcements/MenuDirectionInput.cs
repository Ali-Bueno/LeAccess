using Rewired;
using UnityEngine;

namespace LeAccess.Accessibility.Announcements;

/// <summary>Detects the frame a menu direction is pushed: d-pad, left stick or an arrow key.</summary>
internal static class MenuDirectionInput
{
    private static readonly KeyCode[] ArrowKeys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

    internal static bool PushedThisFrame() => ArrowPressed() || MenuDetailInput.AnyGamepad(DirectionPushed);

    private static bool ArrowPressed()
    {
        foreach (var key in ArrowKeys)
            if (Input.GetKeyDown(key)) return true;
        return false;
    }

    private static bool DirectionPushed(IGamepadTemplate template)
    {
        var pad = template.dPad;
        if (pad is not null && !pad.WasCollected &&
            (Pressed(pad.up) || Pressed(pad.down) || Pressed(pad.left) || Pressed(pad.right))) return true;
        var stick = template.leftStick;
        if (stick is null || stick.WasCollected) return false;
        var value = stick.value;
        var previous = stick.valuePrev;
        return MenuDetailInput.IsPush(value.x, value.y, previous.x, previous.y);
    }

    private static bool Pressed(IControllerTemplateButton button) =>
        button is not null && !button.WasCollected && button.justPressed;
}
