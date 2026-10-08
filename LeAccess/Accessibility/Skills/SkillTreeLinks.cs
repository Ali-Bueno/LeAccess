using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Skills;

/// <summary>Speech for how a skill tree node connects: what it requires, the way to it and what it leads to.</summary>
/// <remarks>See reference/skill-tree-navigation.md#node-connections-2026-10-08.</remarks>
internal static class SkillTreeLinks
{
    /// <summary>A node this one requires, the points the game asks there and the points it has.</summary>
    internal readonly record struct Requirement(string Name, int Points, int Allocated);

    /// <summary>"requiere X"; while the game reports the node's requirements unmet, each short one adds its points.</summary>
    internal static string Requires(IReadOnlyList<Requirement> requirements, bool met)
    {
        var parts = new List<string>();
        foreach (var requirement in requirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.Name)) continue;
            parts.Add(met || requirement.Allocated >= requirement.Points ? requirement.Name
                : ModText.Format(requirement.Points == 1 ? "skill.requires_point" : "skill.requires_points",
                    requirement.Points, requirement.Name, requirement.Allocated));
        }
        return parts.Count == 0 ? null : ModText.Format("skill.requires", string.Join(", ", parts));
    }

    /// <summary>"Ruta: A, B" from the branch's first node down to the one this requires, then "Lleva a: C, D".</summary>
    internal static string Details(IReadOnlyList<string> path, IReadOnlyList<string> leadsTo) => ScreenContentTracker.Combine(new[]
    {
        path.Count == 0 ? null : ModText.Format("skill.path", string.Join(", ", path)),
        leadsTo.Count == 0 ? null : ModText.Format("skill.leads_to", string.Join(", ", leadsTo))
    });
}
