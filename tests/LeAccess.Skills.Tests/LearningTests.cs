using LeAccess.Accessibility.Skills;
using UnityEngine;
using TMPro;

internal static class LearningTests
{
    internal static void Run(Action<bool, string> check)
    {
        BindingTests.Reset();
        var skill = new Ability();
        var icon = new GameObject().Add(new AbilityPanelIcon { ability = skill, locked = true });
        icon.UnlockReqTMP = icon.gameObject.Child().Add(new TMP_Text { text = "8" });
        check(SkillLearningReader.ReadPanel(icon) == "Not learned. Required level: 8",
            "Locked base class icon reads its real level with unlearned state");
        icon.locked = false; icon.UnlockReqTMP.gameObject.Active = false;
        check(SkillLearningReader.ReadPanel(icon) == "Learned. Required level: 8",
            "Learned skill preserves the requirement that Init filled before hiding its holder");
        icon.ablityUnlockType = AbilityPanelIcon.AbilityUnlockType.MasteryPoints;
        icon.UnlockReqTMP.text = "<b>15</b>";
        check(SkillLearningReader.ReadPanel(icon) == "Learned. Required passive points: 15",
            "Mastery passive points are never mislabeled as character level");
        icon.ablityUnlockType = AbilityPanelIcon.AbilityUnlockType.Mastering;
        icon.UnlockReqTMP.text = ""; icon.locked = true;
        check(SkillLearningReader.ReadPanel(icon) == "Not learned. Requires mastery",
            "Mastery selection requirement is announced without inventing a level");
        icon.ablityUnlockType = AbilityPanelIcon.AbilityUnlockType.BaseClass;
        icon.UnlockReqTMP.text = "";
        check(SkillLearningReader.ReadPanel(icon) == "Not learned", "Unavailable requirement does not invent a level");
        icon.UnlockReqTMP = new GameObject().Add(new TMP_Text { text = "40" });
        check(SkillLearningReader.ReadPanel(icon) == "Not learned", "Another icon's requirement cannot leak into selected skill");
        check(SkillLearningReader.ReadKnown(skill) is null, "Uninitialized known ability data does not claim an unlearned skill");
        var known = PlayerFinder.player.Add(new KnownAbilityList { treeData = PlayerFinder.player.Add(new LocalTreeData()),
            abilities = new NativeArray<Ability>(new[] { skill }) });
        var bow = new Ability { overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef { target = skill } };
        check(SkillLearningReader.ReadKnown(bow) == "Learned", "Known skill state recognizes its declared weapon variant");
        check(SkillLearningReader.ReadKnown(new Ability()) == "Not learned", "Initialized known list identifies an unlearned independent skill");
        check(SkillLearningReader.ReadKnown(new Ability(), provenLearned: true) == "Learned",
            "Current usable assignment takes precedence over a retained known-skill cache");
        known.treeData.characterClass = new CharacterClass { unlockableAbilities = new NativeArray<AbilityAndLevel>(
            new[] { new AbilityAndLevel { ability = skill, level = 5 } }) };
        check(SkillLearningReader.ReadKnown(bow) == "Learned. Required level: 5",
            "Known ability outside class panel reads its declared base-class unlock level through primary identity");
        known.abilities.WasCollected = true;
        check(SkillLearningReader.ReadKnown(skill) is null, "Collected known list cannot produce stale learned state");
        BindingTests.Reset();
    }
}
