using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using Rewired;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class InputBindingText
{
    internal static string ReadAction(string actionName, bool useKeyboardIcons = false, bool? holdOverride = null)
    {
        if (string.IsNullOrWhiteSpace(actionName) || !ReInput.isReady) return null;
        try
        {
            var provider = PromptsInfoProvider.Instance;
            return IsAlive(provider) ? ReadButtons(provider.GetButtonsInfo(actionName, useKeyboardIcons), holdOverride) : null;
        }
        catch (Exception) { return null; }
    }

    internal static string Read(ButtonInfo button)
    {
        string name = ReadName(button);
        return name is null ? null : button.IsHoldAction ? ModText.Format("input.hold", name) : name;
    }

    private static string ReadName(ButtonInfo button)
    {
        if (button is null || button.WasCollected) return null;
        string name = TextUtils.CleanText(button.ButtonName);
        if (string.IsNullOrWhiteSpace(name)) return null;
        return name;
    }

    internal static string ReadButtons(Il2CppSystem.Collections.Generic.List<ButtonInfo> buttons, bool? holdOverride = null)
    {
        if (buttons is null || buttons.WasCollected) return null;
        var names = new List<string>();
        bool hold = holdOverride ?? false;
        foreach (var button in buttons)
        {
            string name = ReadName(button);
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (!names.Contains(name)) names.Add(name);
            if (!holdOverride.HasValue) hold |= button.IsHoldAction;
        }
        if (names.Count == 0) return null;
        string chord = string.Join(" + ", names);
        return hold ? ModText.Format("input.hold", chord) : chord;
    }

    internal static string ReadPrompt(MultiButtonsActionPrompt prompt)
    {
        if (!IsAlive(prompt) || !TextUtils.IsVisibleLabel(prompt._promptDescription)) return null;
        string action = TextUtils.CleanText(prompt._promptDescription.text);
        if (string.IsNullOrWhiteSpace(action)) return null;
        string buttons = null;
        try { buttons = ReadButtons(prompt.GetButtonsInfo()); }
        catch (Exception) { }
        return string.IsNullOrWhiteSpace(buttons) ? action : buttons + ", " + action;
    }
}
