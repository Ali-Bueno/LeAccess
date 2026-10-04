using System.Collections.Generic;
using LE.UI.PanelSystem;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Skills;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

/// <summary>Reads the fullscreen passive panel's trees, and the skill-unlock strip of either passive panel.</summary>
internal static class PassiveTreeReader
{
    /// <summary>The displayed tree's name, role and allocated points.</summary>
    internal static string ReadTree(FullScreenPanelPassiveTree panel)
    {
        if (!IsAlive(panel)) return null;
        int index = panel._currentMasteryIndex;
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
    internal static string ReadNodeThreshold(SkillTreeNode node, int treeIndex, int? points)
    {
        if (!IsAlive(node) || node.mastery != treeIndex || points is null || node.masteryRequirement <= points) return null;
        return ModText.Format("passive.node_threshold", node.masteryRequirement, points.Value);
    }

    /// <summary>The selected skill of the strip that unlocks with this tree's points.</summary>
    internal static bool TryReadUnlock(PassiveTreeUnlockPanelNavigable strip, int? points, int? maxPoints,
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
        // The player's known skills decide learned skills; level-gated class skills ignore this tree's points.
        bool knownState = SkillLearningReader.TryReadKnown(ability, out bool learned, out string requiredLevel);
        if (knownState && (learned || requiredLevel != null))
            state = ScreenContentTracker.Combine(new[] { ModText.Get(learned ? "skill.learned" : "skill.not_learned"), requiredLevel });
        else if (IsAlive(icon) && icon.requirement > 0 && points is int have)
            // Native MasteryBarDisplay.ApplyUnlockedAbilityStates compares the requirement with these two values.
            state = ScreenContentTracker.Combine(new[] { knownState ? ModText.Get("skill.not_learned") : null,
                have >= icon.requirement ? knownState ? null : ModText.Get("passive.ability_unlocked") :
                maxPoints is int max && icon.requirement > max ? ModText.Format("passive.ability_unreachable", icon.requirement, max) :
                ModText.Format("passive.ability_unlockable", icon.requirement, have) });
        else if (knownState) state = ModText.Get("skill.not_learned");
        string summary = ScreenContentTracker.Combine(new[] { name,
            ModText.Get(IsAlive(icon) && icon.isMasteryAbility ? "passive.mastery_ability" : "passive.ability"), state,
            ModText.Format("list.position", index + 1, tooltips.Count) });
        selection = new PassiveSelection($"unlock:{tooltip.GetInstanceID()}", tooltip.gameObject, summary,
            TextUtils.CleanText(localized.GetDescription(ability)));
        return true;
    }

    internal static bool TryReadBar(MasteryBarDisplay bar, out int points, out int maxPoints)
    {
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
