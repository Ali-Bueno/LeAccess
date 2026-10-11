using System;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Skills;

/// <summary>A specialised skill's level and its progress to the next, as the gauge above the tree shows them.</summary>
/// <remarks>See reference/skill-tree-navigation.md (2026-10-10, user report).</remarks>
internal static class SkillLevel
{
    private static string _logged;

    /// <summary>Whole percent of the game's 0-1 progress, never 100 before the level is gained.</summary>
    internal static int Percent(float progress) =>
        float.IsFinite(progress) ? (int)Math.Clamp(Math.Floor(progress * 100f), 0, 99) : 0;

    internal static string Describe(int level, float progress, int maxLevel) =>
        level >= maxLevel ? ModText.Format("skill.level_max", level)
            : ModText.Format("skill.level", level, Percent(progress));

    /// <summary>Null when the ability is not specialised or the game cannot say.</summary>
    internal static string Read(LocalTreeData data, Ability ability)
    {
        if (!data.isSpecialisedGetSlotAndXp(ability.playerAbilityID, out _, out int xp)) return null;
        int level = data.getAbilityLevel(ability);
        float progress = SpecialisedAbilityManager.getProgressToNextLevel(xp);
        string text = Describe(level, progress, SpecialisedAbilityManager.maxAbilityLevel);
        if (text != _logged)
        {
            _logged = text;
            Plugin.Log.LogInfo($"[Skill level] {ability.abilityName}: level={level} xp={xp} progress={progress}");
        }
        return text;
    }
}
