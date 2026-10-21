using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>Time beast evolution tiles and loot filter rules: a whole entry with its state and place in the list.</summary>
/// <remarks>See reference/faction-vendor-panels.md#time-beast-and-loot-filter.</remarks>
internal static class ListItemReader
{
    internal static bool TryRead(GameObject element, PanelKind kind, out string text)
    {
        text = null;
        return kind switch
        {
            PanelKind.TimeBeast => TryReadEvolution(element, out text) || TryReadEvolutionConfirm(element, out text),
            PanelKind.LootFilter => TryReadRule(element, out text),
            _ => false
        };
    }

    private static bool TryReadEvolution(GameObject element, out string text)
    {
        text = null;
        var tile = element.GetComponent<TimeBeastEvolutionTile>();
        if (!IsAlive(tile)) return false;
        string content = ScreenContentReader.Read(tile.gameObject, null, null);
        if (string.IsNullOrWhiteSpace(content)) return false;
        int place = 0, count = 0;
        var panel = element.GetComponentInParent<TimeBeastEvolutionPanelUI>();
        var options = IsAlive(panel) ? panel._evolutionOptions : null;
        if (options is not null && !options.WasCollected)
            for (int i = 0; i < options.Count; i++)
                if (IsAlive(options[i]) && options[i].Pointer == tile.Pointer) { place = i + 1; count = options.Count; }
        var toggle = EvolutionToggle(panel, tile, place - 1);
        text = PanelText.Evolution(content, IsAlive(toggle) && toggle.isOn, place, count);
        return true;
    }

    /// <summary>"Title, chosen" when one of the panel's evolution toggles turns on; null text for its other changes.</summary>
    internal static bool TryReadEvolutionChoice(Toggle toggle, bool value, out string text)
    {
        text = null;
        var panel = IsAlive(toggle) ? toggle.GetComponentInParent<TimeBeastEvolutionPanelUI>() : null;
        var toggles = IsAlive(panel) ? panel._evolutionToggles : null;
        if (toggles is null || toggles.WasCollected) return false;
        int index = -1;
        for (int i = 0; i < toggles.Count && index < 0; i++)
            if (IsAlive(toggles[i]) && toggles[i].Pointer == toggle.Pointer) index = i;
        if (index < 0) return false;
        if (value && panel.isActiveAndEnabled) text = PanelText.EvolutionChosen(EvolutionTitle(panel, index));
        return true;
    }

    // The game's confirm button: which evolution it will take, or that none is chosen yet.
    private static bool TryReadEvolutionConfirm(GameObject element, out string text)
    {
        text = null;
        var panel = element.GetComponentInParent<TimeBeastEvolutionPanelUI>();
        var button = IsAlive(panel) ? panel._confirmEvolutionButton : null;
        if (!IsAlive(button) || button.gameObject.Pointer != element.Pointer) return false;
        string caption = ScreenContentReader.Read(button.gameObject, null, null);
        if (string.IsNullOrWhiteSpace(caption)) return false;
        string chosen = null;
        var options = panel._evolutionOptions;
        if (options is not null && !options.WasCollected)
            for (int i = 0; i < options.Count && chosen is null; i++)
            {
                var toggle = EvolutionToggle(panel, options[i], i);
                if (IsAlive(toggle) && toggle.isOn) chosen = EvolutionTitle(panel, i);
            }
        text = PanelText.EvolutionConfirm(caption, chosen);
        return true;
    }

    // The panel keeps its toggles beside the tiles, in the same order; a tile's own toggle is the fallback.
    private static Toggle EvolutionToggle(TimeBeastEvolutionPanelUI panel, TimeBeastEvolutionTile tile, int index)
    {
        var toggles = IsAlive(panel) ? panel._evolutionToggles : null;
        var options = IsAlive(panel) ? panel._evolutionOptions : null;
        if (toggles is not null && !toggles.WasCollected && options is not null && !options.WasCollected &&
            toggles.Count == options.Count && index >= 0 && index < toggles.Count && IsAlive(toggles[index]))
            return toggles[index];
        return IsAlive(tile) ? tile.GetComponentInChildren<Toggle>(true) ?? tile.GetComponentInParent<Toggle>() : null;
    }

    private static string EvolutionTitle(TimeBeastEvolutionPanelUI panel, int index)
    {
        var options = panel._evolutionOptions;
        var tile = options is not null && !options.WasCollected && index >= 0 && index < options.Count ? options[index] : null;
        var header = IsAlive(tile) ? tile._headerText : null;
        return IsAlive(header) ? TextUtils.CleanText(header.text) : null;
    }

    private static bool TryReadRule(GameObject element, out string text)
    {
        text = null;
        var entry = element.GetComponentInParent<RuleEntryUI>();
        if (!IsAlive(entry) || entry.rule is null || entry.rule.WasCollected) return false;
        string description = Description(entry);
        if (string.IsNullOrWhiteSpace(description)) return false;
        bool enabled = entry.rule.isEnabled;
        if (Owns(entry.controllerLayoutRuleToggle, element) || Owns(entry.ruleToggle, element))
        {
            text = PanelText.RuleSwitch(description, enabled);
            return true;
        }
        var edit = entry.controllerLayoutEditRuleButton;
        if (!IsAlive(edit) || edit.gameObject.Pointer != element.Pointer) return false;
        var entries = IsAlive(entry.parent) ? entry.parent.entries : null;
        int place = 0, count = 0;
        if (entries is not null && !entries.WasCollected)
            for (int i = 0; i < entries.Count; i++)
                if (IsAlive(entries[i]) && entries[i].Pointer == entry.Pointer) { place = i + 1; count = entries.Count; }
        if (place == 0) return false;
        text = PanelText.Rule(place, count, description, enabled);
        return true;
    }

    private static string Description(RuleEntryUI entry)
    {
        foreach (var label in new TMPro.TMP_Text[] { entry.controllerLayoutText, entry.text })
            if (TextUtils.IsVisibleLabel(label) && TextUtils.CleanText(label.text) is { Length: > 0 } text) return text;
        return null;
    }

    private static bool Owns(GameObject root, GameObject element) =>
        IsAlive(root) && element.transform.IsChildOf(root.transform);
}
