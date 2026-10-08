using System;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>The mid-game mastery choice: each "Choose Mastery" button with its mastery's text, and the confirmation step.</summary>
/// <remarks>See reference/faction-vendor-panels.md#mastery-choice.</remarks>
internal static class MasterySelectionReader
{
    private static IntPtr _spokenGroup;

    internal static void Reset() => _spokenGroup = IntPtr.Zero;

    internal static bool TryRead(GameObject element, PanelKind kind, out string text)
    {
        text = null;
        if (kind != PanelKind.Mastery) return false;
        string label = TextUtils.GetTextFromHierarchy(element);
        var masteries = element.GetComponentInParent<MasteryPanel>();
        if (IsAlive(masteries))
        {
            var card = element.transform.parent;
            string content = IsAlive(card) ? Static(card.gameObject) : null;
            if (string.IsNullOrWhiteSpace(content)) return false;
            text = ScreenContentTracker.Combine(new[] { content, label, PanelText.Position(Place(masteries, element), Count(masteries)) });
            return true;
        }
        var step = element.GetComponentInParent<UnityDrivenNavigable>();
        if (!IsAlive(step) || string.IsNullOrWhiteSpace(label)) return false;
        bool entering = _spokenGroup != step.Pointer;
        _spokenGroup = step.Pointer;
        text = entering ? ScreenContentTracker.Combine(new[] { Static(step.gameObject), label }) : label;
        return true;
    }

    private static string Static(GameObject group) =>
        ScreenContentReader.Read(group, null, null, include: label => !IsAlive(label.GetComponentInParent<Selectable>()));

    private static int Place(MasteryPanel panel, GameObject element)
    {
        var buttons = panel.masteryButtons;
        if (buttons is null || buttons.WasCollected) return 0;
        for (int i = 0; i < buttons.Length; i++)
            if (IsAlive(buttons[i]) && buttons[i].gameObject.Pointer == element.Pointer) return i + 1;
        return 0;
    }

    private static int Count(MasteryPanel panel) =>
        panel.masteryButtons is { WasCollected: false } buttons ? buttons.Length : 0;
}
