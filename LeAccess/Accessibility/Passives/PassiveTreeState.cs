using System;
using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

/// <summary>After a node: the shown tree's unspent points and the columns its points opened or closed, when they change;
/// and why Cross did nothing. The tree's introduction states them when a tree is first shown.</summary>
/// <remarks>See reference/passive-reading.md#reading-like-the-skill-trees-2026-10-05.</remarks>
internal static class PassiveTreeState
{
    internal readonly record struct State(string Tree, int? Points, int Unspent);

    private static object _selection;
    private static State? _seen;
    private static string _notice;
    private static string _hint;
    private static string _lastFailure;

    /// <summary>What changed since the previous read, stable while the same selection stays.</summary>
    internal static string Describe(object selection, State state, IReadOnlyList<int> thresholds)
    {
        if (Equals(selection, _selection) && _seen == state) return WithHint(_notice);
        if (!Equals(selection, _selection)) _hint = null;
        var previous = _seen;
        _selection = selection;
        _seen = state;
        _notice = Changes(previous, state, thresholds);
        return WithHint(_notice);
    }

    internal static string Changes(State? previous, State state, IReadOnlyList<int> thresholds)
    {
        if (previous is not State before || before.Tree != state.Tree) return null;
        var parts = new List<string>();
        if (state.Unspent != before.Unspent) parts.Add(ModText.Format("passive.unspent", state.Unspent));
        if (state.Points is int now && before.Points is int then && now != then)
            for (int column = 0; column < thresholds.Count; column++)
            {
                int needed = thresholds[column];
                if (then < needed && needed <= now) parts.Add(ModText.Format("passive.column_unlocked", column + 1));
                else if (now < needed && needed <= then) parts.Add(ModText.Format("passive.column_relocked", column + 1));
            }
        return parts.Count == 0 ? null : ScreenContentTracker.Combine(parts);
    }

    /// <summary>Why Cross cannot add a point to this node, or null when it can (or the game is removing points).</summary>
    internal static string Refusal(SkillTreeNode node, int? treePoints, int treeIndex, int unspent)
    {
        if (!IsAlive(node) || node.relevantPanelIsInRespecMode()) return null;
        if (node.pointsAllocated >= node.maxPoints) return ModText.Get("node.full");
        string threshold = PassiveTreeReader.ReadNodeThreshold(node, treeIndex, treePoints);
        if (threshold != null) return threshold;
        if (!node.requirementsMet) return ModText.Get("node.locked");
        return unspent <= 0 ? ModText.Get("node.no_points") : null;
    }

    /// <summary>Cross on a node of the controller panel that the game will ignore says why, while that node stays selected.</summary>
    internal static void BeforeSubmit(PassiveTreeNavigable owner, SubmitType type)
    {
        try
        {
            if (type != SubmitType.A_EX || !PassivePanelReader.TryGetOpen(out var manager) || !PassivePanelReader.Owns(owner)) return;
            var cells = owner._navigationElements?.TryCast<Il2CppSystem.Array>();
            int column = owner._currentColumn, row = owner._currentRow;
            if (cells is null || cells.WasCollected || cells.Rank != 2 || column < 0 || row < 0 ||
                column >= cells.GetLength(0) || row >= cells.GetLength(1)) return;
            var root = PassiveTreeNavigation.CellRoot(cells, column, row);
            var node = IsAlive(root) ? root.GetComponent<SkillTreeNode>() : null;
            int index = PassivePanelReader.TreeIndex(manager, owner);
            string hint = Refusal(node, PassivePanelReader.Points(manager, index), index, manager.activeTree.unspentPoints);
            if (hint == null) return;
            _hint = hint;
            PassiveAccessibility.RepeatCurrent();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static string WithHint(string notice) =>
        _hint == null ? notice : ScreenContentTracker.Combine(new[] { notice, _hint });

    internal static void Reset()
    {
        _selection = null;
        _seen = null;
        _notice = null;
        _hint = null;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Passive tree state unavailable: {ex.Message}");
    }
}
