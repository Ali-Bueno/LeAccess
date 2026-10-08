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
            PanelKind.TimeBeast => TryReadEvolution(element, out text),
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
        var toggle = tile.GetComponent<Toggle>();
        int place = 0, count = 0;
        var options = element.GetComponentInParent<TimeBeastEvolutionPanelUI>()?._evolutionOptions;
        if (options is not null && !options.WasCollected)
            for (int i = 0; i < options.Count; i++)
                if (IsAlive(options[i]) && options[i].Pointer == tile.Pointer) { place = i + 1; count = options.Count; }
        text = PanelText.Evolution(content, IsAlive(toggle) && toggle.isOn, place, count);
        return true;
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
