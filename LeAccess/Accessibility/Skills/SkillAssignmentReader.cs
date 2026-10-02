using System;
using LeAccess.Accessibility.Announcements;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

internal enum SkillAssignmentStage { Hotbar, AvailableSkills }

internal sealed class SkillAssignmentContext
{
    internal SkillAssignmentStage Stage;
    internal int SlotNumber;
    internal string Binding;
    internal Ability CurrentAbility;
}

/// <summary>Identifies the assignment destination from the game's live slot and grid owner.</summary>
internal static class SkillAssignmentReader
{
    internal static bool TryReadContext(GameObject root, out SkillAssignmentContext context)
    {
        context = null;
        try
        {
            if (!IsAlive(root) || !UiVisibility.IsVisible(root)) return false;
            var bar = root.GetComponentInParent<AbilityBarIcon>();
            if (IsAlive(bar))
            {
                if (!SkillBindingReader.TryReadSlot(bar, out var ability)) return false;
                context = Create(SkillAssignmentStage.Hotbar, bar.abilityNumber, ability);
                return true;
            }

            var icon = root.GetComponentInParent<AbilitySelectionIcon>();
            var manager = AbilityBarSelectorManager.instance;
            var player = PlayerFinder.getPlayer();
            if (!IsAlive(icon) || !IsAlive(manager) || !manager.IsMenuOpened ||
                !IsAlive(player) || !IsAlive(manager._playerObject) || manager._playerObject.Pointer != player.Pointer)
                return false;
            var grid = manager._gridNavigable;
            if (!IsAlive(grid) || !grid.isActiveAndEnabled || !UiVisibility.IsVisible(grid.gameObject) ||
                !icon.transform.IsChildOf(grid.transform)) return false;
            if (!SkillBindingReader.TryReadSlot(manager.abilityClicked, out var current)) return false;
            context = Create(SkillAssignmentStage.AvailableSkills, manager.abilityClicked, current);
            return true;
        }
        catch (Exception) { return false; }
    }

    private static SkillAssignmentContext Create(SkillAssignmentStage stage, int slot, Ability ability) =>
        new SkillAssignmentContext { Stage = stage, SlotNumber = slot,
            Binding = SkillBindingReader.ReadSlot(slot), CurrentAbility = ability };
}
