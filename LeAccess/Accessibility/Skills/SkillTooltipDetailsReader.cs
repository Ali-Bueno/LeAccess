using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.Tooltips;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

internal static class SkillTooltipDetailsReader
{
    internal static bool TryReadModernNode(SkillTreeNode node, out string name, out string details,
        out bool modernVisible)
    {
        name = details = null;
        modernVisible = false;
        var ui = UIBase.instance;
        var system = IsAlive(ui) ? ui.TooltipSystem : null;
        if (!IsAlive(system) || !system.isActiveAndEnabled) return false;
        var tooltip = system._nodeTooltip;
        modernVisible = IsAlive(tooltip) && tooltip.isActiveAndEnabled && UiVisibility.IsVisible(tooltip.gameObject);
        if (!modernVisible || system._nodeTooltipRequiresLayout) return false;
        var info = tooltip._info;
        var activeInfo = system._activeNodeTooltipInfo;
        var activeNode = system.ActiveNodeTooltipNode;
        if (info is null || info.WasCollected || activeInfo is null || activeInfo.WasCollected ||
            info.Pointer != activeInfo.Pointer || !IsAlive(activeNode) || activeNode.Pointer != node.Pointer ||
            !IsAlive(info.Node) || info.Node.Pointer != node.Pointer) return false;
        name = Read(tooltip._name);
        string requirements = ReadRequirements(info);
        details = Combine(ModText.Format("node.points", info.PointsAllocated, info.MaxPoints),
            tooltip._nodeType, tooltip._description, tooltip._altDescription,
            tooltip._modifiersSection, tooltip._pointBonusContainer, tooltip._pointBonusModifiersSection,
            string.IsNullOrWhiteSpace(requirements) ? Read(tooltip._requirementsSection) : requirements,
            tooltip._refundCostRoot);
        return !string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(details);
    }

    private static string ReadRequirements(NodeTooltipInfo info)
    {
        var requirements = info.Requirements;
        if (requirements is null || requirements.WasCollected) return null;
        var fragments = new List<string>();
        foreach (var requirement in requirements)
        {
            if (requirement is null || requirement.WasCollected) continue;
            string text = TextUtils.CleanText(requirement.ToRequirementString());
            if (!string.IsNullOrWhiteSpace(text)) fragments.Add(ModText.Format(requirement.IsMet
                ? "node.requirement_met" : "node.requirement_unmet", text));
        }
        return ScreenContentTracker.Combine(fragments);
    }

    internal static string ReadLegacyNode(NodeTooltipManager tooltip) => Combine(
        tooltip.points, tooltip.requirementPassiveText, tooltip.requirementBasePassiveText,
        tooltip.requirementQuestText, tooltip.altTextHeader, tooltip.altText, tooltip.loreText,
        tooltip.pointBonusContainer, ReadStats(tooltip.scalingStats), ReadStats(tooltip.nonScalingStats),
        ReadStats(tooltip.pointBonusStats));

    private static string ReadStats(Il2CppSystem.Collections.Generic.List<NodeTooltipStatDisplay> stats)
    {
        if (stats is null || stats.WasCollected) return null;
        var fragments = new List<string>();
        foreach (var stat in stats)
            if (IsAlive(stat)) fragments.Add(Read(stat.text));
        return ScreenContentTracker.Combine(fragments);
    }

    internal static string ReadAbility(UITooltipAbility tooltip)
    {
        var fragments = new List<string>();
        var stats = tooltip.statFields;
        if (stats is not null && !stats.WasCollected)
            foreach (var stat in stats)
                if (stat is not null && !stat.WasCollected && IsAlive(stat.obj) && UiVisibility.IsVisible(stat.obj))
                {
                    // The localized stat name is a sibling of the numeric text in the owned stat box.
                    if (!string.IsNullOrWhiteSpace(Read(stat.textField))) fragments.Add(Read(stat.obj));
                }
        fragments.Add(Read(tooltip.scalingTagsText));
        fragments.Add(Read(tooltip.minionTagsText));
        var scaling = tooltip.scalingInfoFields;
        if (scaling is not null && !scaling.WasCollected)
            foreach (var field in scaling)
                if (field is not null && !field.WasCollected)
                    fragments.Add(Read(field.scalingInfoFieldHolder));
        return ScreenContentTracker.Combine(fragments);
    }

    private static string Combine(params object[] sections)
    {
        var fragments = new List<string>();
        foreach (var section in sections)
            fragments.Add(section is string text ? text : section is Component component ? Read(component) :
                section is GameObject root ? Read(root) : null);
        return ScreenContentTracker.Combine(fragments);
    }

    private static string Read(Component section) => IsAlive(section) ? Read(section.gameObject) : null;
    private static string Read(GameObject root) => IsAlive(root) && UiVisibility.IsVisible(root)
        ? ScreenContentReader.Read(root, null, null) : null;
}
