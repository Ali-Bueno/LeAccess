using System;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Skills;

/// <summary>A specialised skill's level and its progress to the next, as the gauge above the tree shows them.</summary>
/// <remarks>See reference/skill-tree-navigation.md (2026-10-10, user report).</remarks>
internal static class SkillLevel
{
    /// <summary>Whole percent of the way from this level's experience to the next one's, never 100 before the level is gained.</summary>
    internal static int Percent(int xp, int levelStart, int levelEnd)
    {
        long span = (long)levelEnd - levelStart;
        if (span <= 0) return 0;
        return (int)Math.Clamp((xp - (long)levelStart) * 100 / span, 0, 99);
    }

    internal static string Describe(int level, int xp, int maxLevel, Func<int, int> experienceFor) =>
        level >= maxLevel ? ModText.Format("skill.level_max", level)
            : ModText.Format("skill.level", level, Percent(xp, experienceFor(level), experienceFor(level + 1)));

    /// <summary>Null when the ability is not specialised or the game cannot say.</summary>
    internal static string Read(LocalTreeData data, Ability ability)
    {
        if (!data.isSpecialisedGetSlotAndXp(ability.playerAbilityID, out _, out int xp)) return null;
        return Describe(data.getAbilityLevel(ability), xp, SpecialisedAbilityManager.maxAbilityLevel,
            SpecialisedAbilityManager.getAbilityXPLevelRequirement);
    }
}
