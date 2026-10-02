using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;
using Rewired;
using LeAccess.Accessibility.Announcements;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Skills;

internal static class SkillBindingReader
{
    internal static string LastStatus { get; private set; }

    internal static string ReadAssignment(Ability ability)
    {
        if (!TryFindSlots(ability, out var slots, out bool complete)) return null;
        if (slots.Count == 0) return complete ? ModText.Get("skill.not_equipped") : null;
        string binding = ReadSlots(slots);
        return string.IsNullOrWhiteSpace(binding) ? ModText.Get("skill.equipped") : ModText.Format("skill.equipped_binding", binding);
    }

    internal static string ReadAssignment(AbilityBarIcon icon)
    {
        if (!TryReadSlot(icon, out var ability) || !NavigationObjectLifetime.IsAlive(ability)) return null;
        string binding = Read(icon);
        return string.IsNullOrWhiteSpace(binding) ? ModText.Get("skill.equipped") : ModText.Format("skill.equipped_binding", binding);
    }
    internal static string Read(Ability ability)
    {
        return TryFindSlots(ability, out var slots, out _) ? ReadSlots(slots) : null;
    }

    private static bool TryFindSlots(Ability ability, out List<int> slots, out bool complete)
    {
        slots = new List<int>();
        complete = false;
        LastStatus = "selected ability unavailable";
        if (!NavigationObjectLifetime.IsAlive(ability)) return false;
        try
        {
            if (!TryReadEquipped(out var equipped)) return false;
            complete = SkillIdentity.TryReadPrimary(ability, out _);
            for (int index = 0; index < equipped.Count; index++)
            {
                var candidate = equipped[index];
                if (candidate is null) continue;
                if (Matches(candidate, ability)) slots.Add(index + 1);
                else if (!SkillIdentity.TryReadPrimary(candidate, out _)) complete = false;
            }
            // A pending variant in another slot cannot erase a proven positive assignment.
            LastStatus = slots.Count > 0 || complete ? "ready" : "primary identity pending";
            return true;
        }
        catch (Exception ex) { LastStatus = "identity read failed: " + ex.GetType().Name; return false; }
    }

    private static string ReadSlots(List<int> slots)
    {
        var bindings = new List<string>();
        foreach (int slot in slots) AddUnique(bindings, ReadSlot(slot));
        return bindings.Count == 0 ? null : string.Join(", ", bindings);
    }

    internal static string Read(AbilityBarIcon icon)
    {
        var bindings = new List<string>();
        ReadBindings(icon, bindings);
        return bindings.Count == 0 ? null : string.Join(", ", bindings);
    }

    internal static bool TryReadSlot(AbilityBarIcon icon, out Ability ability)
    {
        ability = null;
        LastStatus = "hotbar owner unavailable";
        return BelongsToPlayer(icon, PlayerFinder.getPlayer()) && TryReadSlot(icon.abilityNumber, out ability);
    }

    internal static bool TryReadSlot(int abilityNumber, out Ability ability)
    {
        ability = null;
        LastStatus = "slot unavailable";
        if (!CastAction(abilityNumber).HasValue || !TryReadEquipped(out var equipped) || abilityNumber > equipped.Count)
            return false;
        ability = equipped[abilityNumber - 1];
        LastStatus = ability is null || NavigationObjectLifetime.IsAlive(ability) ? "ready" : "slot ability unavailable";
        return ability is null || NavigationObjectLifetime.IsAlive(ability);
    }

    internal static string ReadSlot(int abilityNumber)
    {
        try
        {
            if (!CastAction(abilityNumber).HasValue) return null;
            var bindings = new List<string>();
            var icons = AbilityBarIcon.all;
            var player = PlayerFinder.getPlayer();
            if (!NavigationObjectLifetime.IsAlive(player)) return null;
            if (icons is not null && !icons.WasCollected)
                foreach (var icon in icons)
                    if (BelongsToPlayer(icon, player) && icon.abilityNumber == abilityNumber) ReadBindings(icon, bindings);
            if (bindings.Count == 0) ReadAction(CastAction(abilityNumber), bindings);
            return bindings.Count == 0 ? null : string.Join(", ", bindings);
        }
        catch (Exception) { return null; }
    }

    private static bool TryReadEquipped(out List<Ability> equipped)
    {
        equipped = null;
        LastStatus = "player unavailable";
        try
        {
            var player = PlayerFinder.getPlayer();
            var actor = PlayerFinder.getPlayerActor();
            if (!NavigationObjectLifetime.IsAlive(player) || !NavigationObjectLifetime.IsAlive(actor)) return false;
            LastStatus = "player actor owner mismatch";
            if (actor.gameObject.Pointer != player.Pointer) return false;
            var abilities = actor.GetAbilityList();
            LastStatus = "ability list unavailable";
            if (!NavigationObjectLifetime.IsAlive(abilities)) return false;
            var combo = abilities.TryCast<ComboAbilityList>();
            LastStatus = "combo initialization pending";
            if (NavigationObjectLifetime.IsAlive(combo) && !combo.HasInitialised) return false;
            // PlayerAbilityList.getEquippedAbilities also appends the inactive human-form bar.
            var entries = NavigationObjectLifetime.IsAlive(combo) ? combo.equippedAbilities : abilities.getEquippedAbilities();
            LastStatus = "equipped slots unavailable";
            if (entries is null || entries.WasCollected) return false;
            equipped = new List<Ability>();
            foreach (var entry in entries) equipped.Add(entry);
            LastStatus = equipped.Count > 0 ? "ready" : "equipped slots empty";
            return equipped.Count > 0;
        }
        catch (Exception ex) { LastStatus = "equipped slots read failed: " + ex.GetType().Name; return false; }
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
                EpochInputManager.InputActions? castAction = CastAction(icon.abilityNumber);
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

    private static void ReadAction(EpochInputManager.InputActions? actionId, List<string> bindings)
    {
        if (!actionId.HasValue || !ReInput.isReady || ReInput.mapping is null || ReInput.mapping.WasCollected ||
            !NavigationObjectLifetime.IsAlive(PromptsInfoProvider.Instance)) return;
        var action = ReInput.mapping.GetAction((int)actionId.Value);
        if (action is not null && !action.WasCollected) AddUnique(bindings, InputBindingText.ReadAction(action.name));
    }

    private static EpochInputManager.InputActions? CastAction(int abilityNumber) => abilityNumber switch
    {
        // Native AbilityBarIcon.clicked passes this one-based slot to the assignment manager.
        1 => EpochInputManager.InputActions.Ability1,
        2 => EpochInputManager.InputActions.Ability2,
        3 => EpochInputManager.InputActions.Ability3,
        4 => EpochInputManager.InputActions.Ability4,
        5 => EpochInputManager.InputActions.Ability5,
        _ => null
    };

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

    private static bool Matches(Ability candidate, Ability expected) => SkillIdentity.Matches(candidate, expected);

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
