using System;
using System.Collections.Generic;
using EHG.Multiplayer;
using LeAccess.Accessibility.Players;
using LeAccess.Accessibility.Utils;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Progression;

/// <summary>Reads level, experience, unspent passive points, HUD skill buttons and zone; see reference/progression.md.</summary>
internal static class ProgressionProbe
{
    internal static string LastFailure { get; private set; }

    internal static bool TryRead(out ProgressionSnapshot snapshot)
    {
        snapshot = default;
        try
        {
            var tracker = Tracker();
            if (!IsAlive(tracker)) return false;
            int level = tracker.CurrentLevel;
            if (level <= 0) return false;
            bool hud = ReadSkills(out var skills, out bool newSkill);
            snapshot = new ProgressionSnapshot(tracker.Pointer.ToInt64(), level, ExperienceTracker.maxLevel,
                tracker.CurrentExperience, tracker.NextLevelExperience, PassivePoints(tracker),
                hud ? skills : Array.Empty<SkillPoint>(), hud && newSkill, Zone());
            LastFailure = null;
            return true;
        }
        catch (Exception ex)
        {
            LastFailure = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    // Online the root has no Actor, so the tracker comes from LocalPlayer; offline from PlayerFinder.
    private static ExperienceTracker Tracker()
    {
        if (LocalPlayerSource.FromLocalPlayer) return LocalPlayer.instance.experienceTracker;
        var tracker = PlayerFinder.playerExperienceTracker;
        if (IsAlive(tracker)) return tracker;
        return IsAlive(PlayerFinder.player) ? PlayerFinder.getExperienceTracker() : null;
    }

    private static LocalTreeData Tree(ExperienceTracker tracker)
    {
        var tree = tracker.localTreeData;
        if (IsAlive(tree)) return tree;
        if (LocalPlayerSource.FromLocalPlayer) return LocalPlayer.instance.localTreeData;
        return PlayerFinder.getLocalTreeData();
    }

    // The passive tree's own count; the HUD button is the fallback when the tree cannot be read.
    private static int PassivePoints(ExperienceTracker tracker)
    {
        try
        {
            var tree = Tree(tracker);
            var passives = IsAlive(tree) ? tree.passiveTree : null;
            if (passives is not null && !passives.WasCollected) return passives.getUnspentPoints();
        }
        catch (Exception) { /* fall back to the HUD button below */ }
        var button = LevelUpPassivesButton.instance;
        return IsAlive(button) && button.gameObject.activeSelf ? 1 : 0;
    }

    // False when the HUD component does not exist yet (loading, menus), so nothing is inferred.
    private static bool ReadSkills(out List<SkillPoint> skills, out bool newSkill)
    {
        skills = new List<SkillPoint>();
        newSkill = false;
        var buttons = LevelUpSkillsButton.instance;
        if (!IsAlive(buttons)) return false;
        var abilities = buttons.abilitiesWithLevelUpButtons;
        int count = abilities is null || abilities.WasCollected ? 0 : abilities.Count;
        for (int index = 0; index < count; index++)
        {
            var ability = abilities[index];
            if (IsAlive(ability)) skills.Add(new SkillPoint(ability.Pointer.ToString(), AbilityName(ability)));
        }
        var unlock = buttons.newUnlockButton;
        newSkill = IsAlive(unlock) && unlock.activeSelf;
        return true;
    }

    private static string AbilityName(Ability ability)
    {
        string raw = ability.abilityName;
        try
        {
            string key = LocalizationSystem.Conventions.GetAbilityNameKey(raw);
            return TextUtils.CleanText(string.IsNullOrWhiteSpace(key) ? raw : Localization.GetText(key, raw));
        }
        catch (Exception) { return TextUtils.CleanText(raw); }
    }

    // Same scene lookup as the zone announcement.
    private static string Zone()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded || !SceneList.TryGetNonDefaultSceneDetails(scene.name, out var details) ||
            details is null || details.WasCollected) return null;
        return ScreenContent.ScreenContentTracker.Combine(new[] { TextUtils.CleanText(details.LocalizedName), Zones.ZoneEra.Name(details.era) });
    }
}
