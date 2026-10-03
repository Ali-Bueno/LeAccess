using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.Items;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using LE.UI.Tooltips;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Inventory;

internal static class InventoryActionReader
{
    private static string _lastFailure;
    // The same read-only presentation builder used by the native item tooltip.
    // This remains available when controller focus has not rendered a tooltip.
    internal static IEnumerable<string> ReadForItem(ItemData item, ItemContainerUI container)
    {
        var result = new List<string>();
        try
        {
            if (!ItemTooltipModelReader.OwnsSelection(item, container) ||
                !ItemTooltipModelReader.TryGetParameters(item, container, out var slot, out _)) return result;
            var unpacked = item.getAsUnpacked();
            if (unpacked is null || unpacked.WasCollected ||
                unpacked.Pointer != item.Pointer && !unpacked.IdEquals(item)) return result;
            var actions = ItemTooltipPromptHelper.GetActions(unpacked, slot);
            if (actions is null || actions.WasCollected) return result;
            var enumerator = actions.GetEnumerator();
            var cursor = enumerator?.TryCast<Il2CppSystem.Collections.IEnumerator>();
            if (cursor is null || cursor.WasCollected) return result;
            try
            {
                while (cursor.MoveNext())
                {
                    var action = enumerator.Current;
                    if (action is null || action.WasCollected || !action.CanUse) continue;
                    bool controller = EpochInputManager.IsControllerActive;
                    var device = controller ? PanelAction.ControlType.Gamepad : PanelAction.ControlType.MouseAndKeyboard;
                    if ((action.Controls & device) == 0) continue;
                    string label = TextUtils.CleanText(action.Label);
                    if (string.IsNullOrWhiteSpace(label)) continue;
                    var overrides = controller ? action.GamepadButtonNamesOverride : action.MouseAndKeyboardButtonNamesOverride;
                    string buttons;
                    if (overrides is not null && !overrides.WasCollected && overrides.Length > 0)
                    {
                        var names = new List<string>();
                        foreach (string value in overrides)
                        {
                            string name = InputBindingText.LocalizeButton(TextUtils.CleanText(value));
                            if (string.IsNullOrWhiteSpace(name)) { names.Clear(); break; }
                            if (!names.Contains(name)) names.Add(name);
                        }
                        buttons = names.Count == 0 ? null : string.Join(" + ", names);
                        if (buttons != null && action.IsHoldAction) buttons = ModText.Format("input.hold", buttons);
                    }
                    else buttons = InputBindingText.ReadAction(action.GetActionName(), holdOverride: action.IsHoldAction);
                    string text = Format(label, buttons);
                    if (!result.Contains(text)) result.Add(text);
                }
            }
            finally { enumerator.TryCast<Il2CppSystem.IDisposable>()?.Dispose(); }
            if (!ItemTooltipModelReader.OwnsSelection(item, container) ||
                !ItemTooltipModelReader.TryGetParameters(item, container, out var currentSlot, out _) ||
                currentSlot != slot) result.Clear();
        }
        catch (Exception ex)
        {
            result.Clear();
            string failure = $"{ex.GetType().Name}: {ex.Message}";
            if (_lastFailure != failure)
            {
                _lastFailure = failure;
                Plugin.Log.LogWarning($"Item action feedback unavailable: {failure}");
            }
        }
        return result;
    }

    private static string Format(string action, string buttons) =>
        string.IsNullOrWhiteSpace(buttons) ? action : buttons + ", " + action;

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
                string text = Format(name, buttons);
                if (!result.Contains(text)) result.Add(text);
            }
            catch (Exception) { }
        }
        return result;
    }
}
