using System;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

/// <summary>The open tree's specialisation, unspent points and respec mode, spoken when a tree opens and when they change.</summary>
/// <remarks>See reference/skill-tree-navigation.md#modifying-a-skill-with-a-controller-2026-10-04-static-analysis.</remarks>
internal static class SkillTreeState
{
    internal readonly record struct State(IntPtr Tree, bool Specialised, int Unspent, bool Respeccing);
    /// <summary>What to say before the selection (a new tree or mode) and after it (points left, a refused press).</summary>
    internal readonly record struct Notice(string Before, string After);

    private static object _selection;
    private static State? _seen;
    private static Notice _notice;
    private static string _hint;

    internal static bool TryRead(SkillsPanelManager panel, out State state)
    {
        state = default;
        var tree = IsAlive(panel) && panel.IsSkillTreeOpen ? panel.CurrentlyOpenTree : null;
        if (!IsAlive(tree)) return false;
        // The game's own test before spending a point; the tree's specialised flag is never written.
        var data = PlayerFinder.getLocalTreeData();
        var ability = tree.ability;
        if (!IsAlive(data) || !IsAlive(ability)) return false;
        var specialisation = panel.SkillsSpecialisationUIManager;
        state = new State(tree.Pointer, data.isSpecialised(ability), Math.Max(0, tree.unspentPoints),
            IsAlive(specialisation) && specialisation.Respeccing);
        return true;
    }

    /// <summary>The whole state on a new tree, otherwise only what changed; stable while the same selection stays.</summary>
    internal static Notice Describe(object selection, State state)
    {
        if (Equals(selection, _selection) && _seen == state) return WithHint(_notice);
        if (!Equals(selection, _selection)) _hint = null;
        var previous = _seen;
        _selection = selection;
        _seen = state;
        _notice = Changes(previous, state);
        return WithHint(_notice);
    }

    internal static Notice Changes(State? previous, State state)
    {
        string unspent = ModText.Format("skill.tree_unspent", state.Unspent);
        if (previous is not State before || before.Tree != state.Tree)
            return state.Specialised
                ? new Notice(state.Respeccing ? ModText.Get("skill.respec_mode") : null, unspent)
                : new Notice(ModText.Get("skill.not_specialised"), null);
        string mode = null, after = null;
        if (state.Specialised != before.Specialised)
            mode = ModText.Get(state.Specialised ? "skill.now_specialised" : "skill.despecialised");
        if (state.Respeccing != before.Respeccing)
            mode = ScreenContentTracker.Combine(new[] { mode, ModText.Get(state.Respeccing ? "skill.respec_mode" : "skill.respec_mode_ended") });
        if (state.Specialised && (state.Unspent != before.Unspent || state.Specialised != before.Specialised)) after = unspent;
        return new Notice(mode, after);
    }

    /// <summary>A on a node that the game ignores says why, as long as that node stays selected.</summary>
    internal static void BeforeSubmit(SkillsPanelManager panel, SubmitType type)
    {
        try
        {
            if (type != SubmitType.A_EX || !IsAlive(panel) || panel.inTopArea || !IsAlive(SkillTreeNavigation.ResolveNode(panel)) ||
                !TryRead(panel, out var state)) return;
            string hint = !state.Specialised ? ModText.Get("skill.specialise_first")
                : !state.Respeccing && state.Unspent == 0 ? ModText.Get("skill.no_points") : null;
            if (hint == null) return;
            _hint = hint;
            SkillAccessibility.RepeatCurrent();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static string _lastFailure;

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Skill tree state unavailable: {ex.Message}");
    }

    private static Notice WithHint(Notice notice) =>
        _hint == null ? notice : notice with { After = ScreenContentTracker.Combine(new[] { notice.After, _hint }) };

    internal static void Reset()
    {
        _selection = null;
        _seen = null;
        _notice = default;
        _hint = null;
    }
}
