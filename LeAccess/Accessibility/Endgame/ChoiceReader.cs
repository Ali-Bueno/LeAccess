using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>A focused control's spoken line; <paramref name="Details"/> builds the longer reading on request.</summary>
internal readonly record struct EndgameStop(string Key, string Text, Func<string> Details = null);

/// <summary>Reads the option rows of the Sanctuary of Eterra and echo reward panels: label, picked state, place, confirm.</summary>
/// <remarks>See reference/endgame-panels.md#choice-panels.</remarks>
internal static class ChoiceReader
{
    internal static bool TryRead(Panel panel, GameObject selected, out EndgameStop stop)
    {
        stop = default;
        var sanctuary = panel.TryCast<SanctuaryOfEterraPanel>();
        if (IsAlive(sanctuary) && IsAlive(sanctuary._sanctuaryOfEterraPanelManager))
        {
            var manager = sanctuary._sanctuaryOfEterraPanelManager;
            var rows = new List<(Button, TMP_Text)>();
            var options = manager.options;
            for (int i = 0; options is not null && !options.WasCollected && i < options.Length; i++)
                rows.Add((options[i]?.chooseOptionButton, options[i]?.optionText));
            return Read(rows, manager.chosenOption, manager.confirmButton, manager.confirmText, manager.closeButton, selected, out stop);
        }
        var reward = panel.TryCast<ChooseEchoRewardPanel>();
        var rewards = IsAlive(reward) ? reward._echoRewardPanelManager : null;
        if (!IsAlive(rewards)) return false;
        var list = new List<(Button, TMP_Text)>();
        var choices = rewards.rewardOptions;
        for (int i = 0; choices is not null && !choices.WasCollected && i < choices.Length; i++)
            list.Add((choices[i]?.chooseRewardButton, choices[i]?.rewardText));
        return Read(list, rewards.chosenOption, rewards.confirmButton, null, null, selected, out stop);
    }

    private static bool Read(List<(Button Button, TMP_Text Label)> rows, int chosen, Button confirm, TMP_Text confirmLabel,
        Button close, GameObject selected, out EndgameStop stop)
    {
        stop = default;
        for (int i = 0; i < rows.Count; i++)
        {
            if (!Is(rows[i].Button, selected)) continue;
            string label = Text(rows[i].Label) ?? TextUtils.GetTextFromHierarchy(selected);
            if (string.IsNullOrWhiteSpace(label)) return false;
            stop = new EndgameStop($"{selected.GetInstanceID()}:{i == chosen}",
                EndgameText.Option(label, i == chosen, rows[i].Button.interactable, i + 1, rows.Count));
            return true;
        }
        var control = Is(confirm, selected) ? confirm : Is(close, selected) ? close : null;
        if (control == null) return false;
        string caption = (control == confirm ? Text(confirmLabel) : null) ?? TextUtils.GetTextFromHierarchy(selected);
        if (string.IsNullOrWhiteSpace(caption)) return false;
        stop = new EndgameStop($"{selected.GetInstanceID()}:{control.interactable}", EndgameText.Button(caption, control.interactable));
        return true;
    }

    private static bool Is(Button button, GameObject selected) =>
        IsAlive(button) && (button.gameObject.Pointer == selected.Pointer || selected.transform.IsChildOf(button.transform));

    private static string Text(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text) ? TextUtils.CleanText(label.text) : null;
}
