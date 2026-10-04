using System.Collections.Generic;
using LE.UI.PanelSystem;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

/// <summary>Reads the fullscreen passive panel's trees and their skill-unlock strip.</summary>
internal static class PassiveTreeReader
{
    /// <summary>The displayed tree's name, role and allocated points.</summary>
    internal static string ReadTree(FullScreenPanelPassiveTree panel, out string key)
    {
        key = null;
        if (!IsAlive(panel)) return null;
        int index = panel._currentMasteryIndex;
        key = $"{panel.GetInstanceID()}:{index}";
        var tab = FindTab(panel, index);
        string name = Label(panel._masteryNameText) ?? TreeName(panel, index);
        if (string.IsNullOrWhiteSpace(name)) return null;
        int? points = ParsePoints(panel._pointsAllocatedText) ?? TreePoints(panel, index, tab);
        return ScreenContentTracker.Combine(new[] {
            points is null ? name : ModText.Format("passive.tree", name, points.Value),
            TreeRole(panel, index, tab), Label(panel._pointsAvailableText) });
    }

    /// <summary>Every tree's points with the chosen mastery marked, then the unspent points.</summary>
    internal static string ReadOverview(FullScreenPanelPassiveTree panel)
    {
        var data = IsAlive(panel) ? panel._masteryDataList : null;
        if (data is null || data.WasCollected) return null;
        var trees = new List<string>();
        for (int i = 0; i < data.Count; i++)
        {
            var tab = FindTab(panel, i);
            string name = TreeName(panel, i);
            if (string.IsNullOrWhiteSpace(name)) continue;
            int? points = TreePoints(panel, i, tab);
            trees.Add(ScreenContentTracker.Combine(new[] {
                points is null ? name : ModText.Format("passive.tree", name, points.Value), TreeRole(panel, i, tab) }));
        }
        if (trees.Count == 0) return null;
        var parts = new List<string>();
        if (ChosenMastery(panel) == 0) parts.Add(ModText.Get("passive.no_mastery"));
        parts.Add(ModText.Format("passive.overview", string.Join("; ", trees)));
        return ScreenContentTracker.Combine(parts);
    }

    /// <summary>The tree-point threshold of a node on the displayed page while it is still unmet.</summary>
    internal static string ReadNodeThreshold(FullScreenPanelPassiveTree panel, SkillTreeNode node)
    {
        if (!IsAlive(panel) || !IsAlive(node) || node.mastery != panel._currentMasteryIndex) return null;
        if (!TryReadBar(panel, out int points, out _) || node.masteryRequirement <= points) return null;
        return ModText.Format("passive.node_threshold", node.masteryRequirement, points);
    }

    /// <summary>The selected skill of the strip that unlocks with this tree's points.</summary>
    internal static bool TryReadUnlock(FullScreenPanelPassiveTree panel, PassiveTreeUnlockPanelNavigable strip,
        out PassiveSelection selection)
    {
        selection = default;
        if (!IsAlive(strip) || !strip.isActiveAndEnabled || !UiVisibility.IsVisible(strip.gameObject)) return false;
        var tooltips = strip.ActiveAbilityTooltips;
        int index = strip.SelectedIndex;
        if (tooltips is null || tooltips.WasCollected || index < 0 || index >= tooltips.Count) return false;
        var tooltip = tooltips[index];
        if (!IsAlive(tooltip) || !UiVisibility.IsVisible(tooltip.gameObject)) return false;
        var icon = tooltip.GetComponentInParent<PassiveTreeUnlockableAbilityIcon>();
        var ability = tooltip.currentAbility;
        if (!IsAlive(ability) && IsAlive(icon)) ability = icon.getAbility();
        var localized = Localization.Abilities;
        if (!IsAlive(ability) || localized is null || localized.WasCollected) return false;
        string name = TextUtils.CleanText(localized.GetName(ability));
        if (string.IsNullOrWhiteSpace(name)) return false;
        string state = null;
        if (IsAlive(icon) && icon.requirement > 0 && TryReadBar(panel, out int points, out int maxPoints))
            // Native MasteryBarDisplay.ApplyUnlockedAbilityStates compares the requirement with these two values.
            state = points >= icon.requirement ? ModText.Get("passive.ability_unlocked") :
                icon.requirement <= maxPoints ? ModText.Format("passive.ability_unlockable", icon.requirement, points) :
                ModText.Format("passive.ability_unreachable", icon.requirement, maxPoints);
        string summary = ScreenContentTracker.Combine(new[] { name,
            ModText.Get(IsAlive(icon) && icon.isMasteryAbility ? "passive.mastery_ability" : "passive.ability"), state });
        selection = new PassiveSelection($"unlock:{tooltip.GetInstanceID()}", tooltip.gameObject, summary,
            TextUtils.CleanText(localized.GetDescription(ability)));
        return true;
    }

    private static bool TryReadBar(FullScreenPanelPassiveTree panel, out int points, out int maxPoints)
    {
        var bar = IsAlive(panel) ? panel._masteryBarDisplay : null;
        bool ready = IsAlive(bar) && bar._hasFillValues;
        points = ready ? bar._lastFillPoints : 0;
        maxPoints = ready ? bar._lastFillMaxPoints : 0;
        return ready;
    }

    private static string TreeRole(FullScreenPanelPassiveTree panel, int index, MasteryTabButton tab)
    {
        var data = panel._masteryDataList;
        var entry = data is not null && !data.WasCollected && index >= 0 && index < data.Count ? data[index] : null;
        if (entry is not null && !entry.WasCollected && !entry.isMastery) return ModText.Get("passive.tree_class");
        if (ChosenMastery(panel) == index) return ModText.Get("passive.tree_chosen");
        var button = IsAlive(tab) ? tab._tabButton : null;
        return IsAlive(button) && IsAlive(button._lockedPoints) && UiVisibility.IsVisible(button._lockedPoints)
            ? ModText.Get("passive.tree_locked") : null;
    }

    private static string TreeName(FullScreenPanelPassiveTree panel, int index)
    {
        var data = panel._masteryDataList;
        if (data is null || data.WasCollected || index < 0 || index >= data.Count) return null;
        var entry = data[index];
        return entry is null || entry.WasCollected ? null : MasteryReader.LocalizedName(entry.name, entry.isMastery);
    }

    private static int? TreePoints(FullScreenPanelPassiveTree panel, int index, MasteryTabButton tab)
    {
        var button = IsAlive(tab) ? tab._tabButton : null;
        int? shown = IsAlive(button) ? ParsePoints(button._assignedPoints) : null;
        if (shown is not null) return shown;
        var tree = panel._localTreeData;
        var levels = tree is null || tree.WasCollected ? null : tree.masteryLevels;
        return levels is not null && !levels.WasCollected && index >= 0 && index < levels.Length ? levels[index] : null;
    }

    private static int ChosenMastery(FullScreenPanelPassiveTree panel)
    {
        var tree = panel._localTreeData;
        return tree is null || tree.WasCollected ? -1 : tree.chosenMastery;
    }

    private static MasteryTabButton FindTab(FullScreenPanelPassiveTree panel, int index)
    {
        var tabs = panel._masteryTabButtons;
        if (tabs is null || tabs.WasCollected) return null;
        foreach (var tab in tabs)
            if (IsAlive(tab) && IsAlive(tab._tabButton) && tab._tabButton.SelectedIndex == index) return tab;
        return null;
    }

    private static int? ParsePoints(TMP_Text label) => int.TryParse(Label(label), out int points) ? points : null;

    private static string Label(TMP_Text label)
    {
        if (!TextUtils.IsVisibleLabel(label)) return null;
        string text = TextUtils.CleanText(label.text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
