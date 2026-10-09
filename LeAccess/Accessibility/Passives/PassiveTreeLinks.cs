using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Skills;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

/// <summary>What a passive node requires, the way to it and what it leads to, worded as in the specialization trees.</summary>
/// <remarks>See reference/passive-reading.md#node-requirements-2026-10-08.</remarks>
internal static class PassiveTreeLinks
{
    internal static void Read(SkillTreeNode node, Func<SkillTreeNode, string> name, out string requires, out string details)
    {
        requires = details = null;
        var tree = IsAlive(node) ? node.tree : null;
        if (!IsAlive(tree)) return;
        var requirements = new List<SkillTreeLinks.Requirement>();
        foreach (var (required, points) in Requirements(node))
            requirements.Add(new(name(required), points, required.pointsAllocated));
        requires = SkillTreeLinks.Requires(requirements, node.requirementsMet);
        // The chain of first requirements, nearest last, as the skill trees' "Ruta".
        var path = new List<string>();
        var seen = new HashSet<IntPtr> { node.Pointer };
        for (var parent = First(node); parent is not null && seen.Add(parent.Pointer); parent = First(parent))
            path.Insert(0, name(parent));
        var leadsTo = new List<string>();
        var list = tree.nodeList;
        if (list is not null && !list.WasCollected)
            for (int i = 0; i < list.Count; i++)
            {
                var other = list[i];
                if (!IsAlive(other) || other.Pointer == node.Pointer) continue;
                foreach (var (required, _) in Requirements(other))
                    if (required.Pointer == node.Pointer) { leadsTo.Add(name(other)); break; }
            }
        details = SkillTreeLinks.Details(path, leadsTo);
    }

    // A node without points (a hidden anchor) is never a requirement worth naming.
    private static IEnumerable<(SkillTreeNode Node, int Points)> Requirements(SkillTreeNode node)
    {
        var list = node.requirements;
        if (list is null || list.WasCollected) yield break;
        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            var required = entry is null || entry.WasCollected ? null : entry.node;
            if (IsAlive(required) && required.maxPoints > 0) yield return (required, entry.requirement);
        }
    }

    private static SkillTreeNode First(SkillTreeNode node)
    {
        foreach (var (required, _) in Requirements(node)) return required;
        return null;
    }
}
