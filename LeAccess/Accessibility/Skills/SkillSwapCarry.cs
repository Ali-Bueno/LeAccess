using System;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

/// <summary>Carries the skill highlighted when Square swaps to the action bar into the slot's skill list.</summary>
/// <remarks>The game keeps no pending skill; see reference/skill-assignment.md#square-carries-the-highlighted-skill-2026-10-04.</remarks>
internal static class SkillSwapCarry
{
    private static Ability _pending;
    private static string _lastFailure;
    /// <summary>The carried skill's spoken name while the action bar waits for a slot.</summary>
    internal static string PendingName { get; private set; }

    /// <summary>Square on the Skills panel closes it and focuses the action bar; remember the highlighted skill.</summary>
    internal static void BeforeSubmit(SubmitType type)
    {
        if (type != SubmitType.X_SQUARE) return;
        try
        {
            Clear();
            if (!SkillSelectionReader.TryRead(out var selection, includeDetails: false) || selection.IsNode ||
                !IsAlive(selection.Ability) || string.IsNullOrWhiteSpace(selection.Name)) return;
            _pending = selection.Ability;
            PendingName = selection.Name;
        }
        catch (Exception ex) { Clear(); ReportFailure(ex); }
    }

    /// <summary>Waits on the action bar, focuses the carried skill when a slot's list opens, and drops it elsewhere.</summary>
    internal static void Update()
    {
        if (!IsAlive(_pending)) { PendingName = null; return; }
        try
        {
            var selected = NavigationManager.currentlySelected;
            var id = selected is null || selected.WasCollected ? NavigableID.NONE : selected.GetNavigableID();
            if (id == NavigableID.ABILITY_PANEL) return;
            // One attempt once the opened list has its entries; a skill missing from it leaves the game's focus.
            if (id == NavigableID.ABILITY_PANEL_FLYOUT && !TrySelectInList(selected.TryCast<ListNavigable>())) return;
            Clear();
        }
        catch (Exception ex) { Clear(); ReportFailure(ex); }
    }

    // The list opens on its first skill; this runs before that entry has been spoken. False while the list is empty.
    private static bool TrySelectInList(ListNavigable list)
    {
        var children = IsAlive(list) ? list._selectableChildren : null;
        if (children is null || children.WasCollected || children.Count == 0) return false;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var icon = IsAlive(child) ? child.GetComponent<AbilitySelectionIcon>() : null;
            if (!IsAlive(icon) || !SkillIdentity.Matches(icon.ability, _pending)) continue;
            list.TrySelectElementWithIndex(i);
            break;
        }
        return true;
    }

    internal static void Clear()
    {
        _pending = null;
        PendingName = null;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Skill swap carry unavailable: {ex.Message}");
    }
}
