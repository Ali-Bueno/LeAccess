using System;
using System.Globalization;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

internal static class SkillLearningReader
{
    internal static string ReadPanel(AbilityPanelIcon icon)
    {
        if (!IsAlive(icon) || !IsAlive(icon.ability)) return null;
        string state = ModText.Get(icon.locked ? "skill.not_learned" : "skill.learned");
        string requirement = null;
        var text = icon.UnlockReqTMP;
        // Init fills this value even when the learned skill's requirement holder is hidden.
        string value = IsAlive(text) && text.transform.IsChildOf(icon.transform) ? TextUtils.CleanText(text.text) : null;
        if (icon.ablityUnlockType == AbilityPanelIcon.AbilityUnlockType.Mastering)
            requirement = ModText.Get("skill.requires_mastery");
        else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) && amount > 0)
            requirement = ModText.Format(icon.ablityUnlockType == AbilityPanelIcon.AbilityUnlockType.BaseClass
                ? "skill.required_level" : "skill.required_passive_points", amount);
        else if (!string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out _)) requirement = value;
        return ScreenContentTracker.Combine(new[] { state, requirement });
    }

    internal static string ReadKnown(Ability ability, bool provenLearned = false)
    {
        try
        {
            if (!SkillIdentity.TryReadPrimary(ability, out _)) return null;
            var player = PlayerFinder.getPlayer();
            if (!IsAlive(player)) return null;
            var known = player.GetComponent<KnownAbilityList>();
            if (!IsAlive(known) || !IsAlive(known.treeData) || known.abilities is null || known.abilities.WasCollected)
                return null;
            bool any = false;
            foreach (var candidate in known.abilities)
            {
                if (!SkillIdentity.TryReadPrimary(candidate, out _)) return null;
                any = true;
                if (SkillIdentity.Matches(candidate, ability))
                    return ScreenContentTracker.Combine(new[] { ModText.Get("skill.learned"), ReadBaseLevel(known, ability) });
            }
            return any ? ScreenContentTracker.Combine(new[] { ModText.Get(provenLearned ? "skill.learned" : "skill.not_learned"),
                ReadBaseLevel(known, ability) }) : null;
        }
        catch (Exception) { return null; }
    }

    private static string ReadBaseLevel(KnownAbilityList known, Ability ability)
    {
        var characterClass = known.treeData.characterClass;
        if (!IsAlive(characterClass)) return null;
        var unlocks = characterClass.unlockableAbilities;
        if (unlocks is null || unlocks.WasCollected) return null;
        foreach (var unlock in unlocks)
            if (unlock is not null && !unlock.WasCollected && unlock.level > 0 && SkillIdentity.Matches(unlock.ability, ability))
                return ModText.Format("skill.required_level", unlock.level);
        return null;
    }
}
