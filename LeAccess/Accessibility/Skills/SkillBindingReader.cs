using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;
using Rewired;
using LeAccess.Accessibility.Announcements;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Skills;

internal static class SkillBindingReader
{
    internal static string ReadAssignment(Ability ability)
    {
        if (!NavigationObjectLifetime.IsAlive(ability)) return null;
        try
        {
            var icons = AbilityBarIcon.all;
            var player = PlayerFinder.getPlayer();
            if (icons is null || icons.WasCollected || !NavigationObjectLifetime.IsAlive(player)) return null;
            bool equipped = false, hasOwnedIcon = false;
            foreach (var icon in icons)
                if (BelongsToPlayer(icon, player))
                {
                    hasOwnedIcon = true;
                    if (Matches(icon.ability, ability) || Matches(icon.correspondingAbility, ability)) equipped = true;
                }
            if (!hasOwnedIcon) return null;
            if (!equipped) return ModText.Get("skill.not_equipped");
            string binding = Read(ability);
            return string.IsNullOrWhiteSpace(binding) ? ModText.Get("skill.equipped") : ModText.Format("skill.equipped_binding", binding);
        }
        catch (Exception) { return null; }
    }

    internal static string ReadAssignment(AbilityBarIcon icon)
    {
        if (!BelongsToPlayer(icon, PlayerFinder.getPlayer()) ||
            !NavigationObjectLifetime.IsAlive(icon.ability) && !NavigationObjectLifetime.IsAlive(icon.correspondingAbility)) return null;
        string binding = Read(icon);
        return string.IsNullOrWhiteSpace(binding) ? ModText.Get("skill.equipped") : ModText.Format("skill.equipped_binding", binding);
    }
    internal static string Read(Ability ability)
    {
        if (!NavigationObjectLifetime.IsAlive(ability)) return null;
        try
        {
            var icons = AbilityBarIcon.all;
            if (icons is null || icons.WasCollected) return null;
            var player = PlayerFinder.getPlayer();
            if (!NavigationObjectLifetime.IsAlive(player)) return null;
            var bindings = new List<string>();
            for (int index = 0; index < icons.Count; index++)
            {
                var icon = icons[index];
                if (!BelongsToPlayer(icon, player)) continue;
                if (Matches(icon.ability, ability) || Matches(icon.correspondingAbility, ability))
                    ReadBindings(icon, bindings);
            }
            return bindings.Count == 0 ? null : string.Join(", ", bindings);
        }
        catch (Exception) { return null; }
    }

    internal static string Read(AbilityBarIcon icon)
    {
        var bindings = new List<string>();
        ReadBindings(icon, bindings);
        return bindings.Count == 0 ? null : string.Join(", ", bindings);
    }

    private static void ReadBindings(AbilityBarIcon icon, List<string> bindings)
    {
        if (!NavigationObjectLifetime.IsAlive(icon)) return;
        try
        {
            if (!BelongsToPlayer(icon, PlayerFinder.getPlayer()) || !ReInput.isReady) return;
            var mapping = ReInput.mapping;
            var prompts = PromptsInfoProvider.Instance;
            if (mapping is null || mapping.WasCollected || !NavigationObjectLifetime.IsAlive(prompts)) return;
            var actions = new List<string>();
            CollectActions(icon, actions, static (InputCharacterForAction label) => label.actionName);
            CollectActions(icon, actions, static (InputDependantPrompt prompt) => prompt._rewiredActionName);
            CollectActions(icon, actions, static (MultiButtonsActionPrompt prompt) => prompt._rewiredActionName);
            actions.RemoveAll(name =>
            {
                var action = mapping.GetAction(name);
                return action is null || action.WasCollected || !IsCastAction(action.id);
            });
            if (actions.Count == 0)
            {
                // Native UpdateIcon reads equippedAbilities[abilityNumber - 1].
                EpochInputManager.InputActions? castAction = icon.abilityNumber switch
                {
                    1 => EpochInputManager.InputActions.Ability1,
                    2 => EpochInputManager.InputActions.Ability2,
                    3 => EpochInputManager.InputActions.Ability3,
                    4 => EpochInputManager.InputActions.Ability4,
                    5 => EpochInputManager.InputActions.Ability5,
                    _ => null
                };
                var slotAction = castAction.HasValue ? mapping.GetAction((int)castAction.Value) : null;
                if (slotAction is not null && !slotAction.WasCollected) AddUnique(actions, slotAction.name);
            }
            foreach (string actionName in actions)
            {
                var action = mapping.GetAction(actionName);
                if (action is null || action.WasCollected || !IsCastAction(action.id)) continue;
                // The icon supplies its action; the game's prompt provider supplies the current remapped buttons.
                AddUnique(bindings, InputBindingText.ReadAction(actionName));
            }
        }
        catch (Exception) { }
    }

    private static void CollectActions<T>(AbilityBarIcon icon, List<string> actions, Func<T, string> actionName)
        where T : UnityEngine.Component
    {
        foreach (var label in icon.GetComponentsInChildren<T>(true))
        {
            if (!NavigationObjectLifetime.IsAlive(label)) continue;
            var owner = label.GetComponentInParent<AbilityBarIcon>(true);
            if (NavigationObjectLifetime.IsAlive(owner) && owner.Pointer == icon.Pointer)
                AddUnique(actions, actionName(label));
        }
    }

    private static bool Matches(Ability candidate, Ability expected) =>
        NavigationObjectLifetime.IsAlive(candidate) && candidate.Pointer == expected.Pointer;

    private static bool BelongsToPlayer(AbilityBarIcon icon, UnityEngine.GameObject player) =>
        NavigationObjectLifetime.IsAlive(icon) && NavigationObjectLifetime.IsAlive(player) &&
        NavigationObjectLifetime.IsAlive(icon.player) && icon.player.Pointer == player.Pointer;

    private static bool IsCastAction(int action) => (EpochInputManager.InputActions)action is
        EpochInputManager.InputActions.Ability1 or EpochInputManager.InputActions.Ability2 or
        EpochInputManager.InputActions.Ability3 or EpochInputManager.InputActions.Ability4 or
        EpochInputManager.InputActions.Ability5;

    private static void AddUnique(List<string> values, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value)) values.Add(value);
    }
}
