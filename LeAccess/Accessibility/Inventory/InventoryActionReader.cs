using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using LE.UI.Tooltips;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Inventory;

internal static class InventoryActionReader
{
    internal static IEnumerable<string> Read(ItemTooltip tooltip)
    {
        var result = new List<string>();
        var section = tooltip._prompts;
        if (!IsAlive(section) || !UiVisibility.IsVisible(section.gameObject)) return result;
        foreach (var prompt in section.gameObject.GetComponentsInChildren<PanelActionUIPrompt>(false))
        {
            try
            {
                var label = IsAlive(prompt) ? prompt._label : null;
                var action = IsAlive(prompt) ? prompt._panelAction : null;
                if (!IsAlive(prompt) || !prompt.isActiveAndEnabled || !IsAlive(label) || !label.isActiveAndEnabled ||
                    label.alpha <= 0 || !UiVisibility.IsVisible(label.gameObject) ||
                    IsAlive(label.canvasRenderer) && label.canvasRenderer.cull || action is null || action.WasCollected ||
                    !action.CanUse || IsAlive(prompt._cg) && prompt._cg.alpha <= 0) continue;
                string name = TextUtils.CleanText(label.text);
                if (string.IsNullOrWhiteSpace(name)) continue;
                bool controller = EpochInputManager.IsControllerActive;
                var overrides = controller ? action.GamepadButtonNamesOverride : action.MouseAndKeyboardButtonNamesOverride;
                string buttons = overrides is not null && !overrides.WasCollected && overrides.Length > 0
                    ? InputBindingText.ReadButtons(prompt.BuildOverrideButtonsInfo(overrides, controller), action.IsHoldAction)
                    : InputBindingText.ReadAction(action.GetActionName(), prompt.showKeyboardGylphs, action.IsHoldAction);
                string text = string.IsNullOrWhiteSpace(buttons) ? name : name + ": " + buttons;
                if (!result.Contains(text)) result.Add(text);
            }
            catch (Exception) { }
        }
        return result;
    }
}
