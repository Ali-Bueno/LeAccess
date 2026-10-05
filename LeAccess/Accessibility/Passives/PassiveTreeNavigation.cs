using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

/// <summary>Moves the passive cursor through the columns instead of the native same-row/column search.</summary>
internal static class PassiveTreeNavigation
{
    private static readonly List<(int Column, int Row)> Order = new();
    private static readonly List<IntPtr> Nodes = new();
    private static readonly List<int> Thresholds = new();
    private static IntPtr _orderOwner;
    private static int _orderColumn = -1, _orderRow = -1;
    private static string _lastFailure;

    /// <summary>Moves inside the column grid; leaving it, or an unreadable grid, takes the game's own step.</summary>
    internal static void Navigate(PassiveTreeNavigable owner, Direction direction)
    {
        bool handled = false;
        try { handled = TryMove(owner, direction); }
        catch (Exception ex) { ReportFailure(ex); }
        if (handled) return;
        try { NativeStep(owner, direction); }
        catch (Exception ex) { ReportFailure(ex); }
    }

    /// <summary>Handles a stick flick like the native handler, which converts it with the same default threshold.</summary>
    internal static void Navigate(PassiveTreeNavigable owner, Vector3 input)
    {
        Direction direction;
        try { direction = DirectionExtensions.ToDirection(input); }
        catch (Exception ex) { ReportFailure(ex); return; }
        Navigate(owner, direction);
    }

    private static bool TryMove(PassiveTreeNavigable owner, Direction direction)
    {
        if (!TryReadOrder(owner, out var cells) || Order.Count == 0) return false;
        var move = PassiveNavigationOrder.Plan(Order, owner._currentColumn, owner._currentRow, direction);
        switch (move.Kind)
        {
            case PassiveNavigationOrder.MoveKind.Select:
                MoveTo(owner, cells, move.Column, move.Row);
                return true;
            case PassiveNavigationOrder.MoveKind.Stay:
                PassiveAccessibility.RepeatCurrent();
                return true;
            case PassiveNavigationOrder.MoveKind.Exit:
                Leave(owner, cells);
                // Native IsOutBorder leaves the grid through its own exit when the step starts on this border.
                if (direction == Direction.UP) owner._currentRow = cells.GetLength(1) - 1;
                else if (direction == Direction.DOWN) owner._currentRow = 0;
                else if (direction == Direction.LEFT) owner._currentColumn = 0;
                return false;
            default:
                return false;
        }
    }

    /// <summary>Native HandleSubmitInput (0x105c9d0, build 25711439): only Cross acts, clicking the selected cell's node.</summary>
    /// <remarks>Replayed because its prologue's short branches crash through a detour; an empty cell, where the game
    /// would throw, does nothing.</remarks>
    internal static void Submit(PassiveTreeNavigable owner, SubmitType type)
    {
        try
        {
            if (type != SubmitType.A_EX || !IsAlive(owner)) return;
            var cells = owner._navigationElements?.TryCast<Il2CppSystem.Array>();
            int column = owner._currentColumn, row = owner._currentRow;
            if (cells is null || cells.WasCollected || cells.Rank != 2 || column < 0 || row < 0 ||
                column >= cells.GetLength(0) || row >= cells.GetLength(1)) return;
            var cell = cells.GetValue(column, row);
            var node = cell is null || cell.WasCollected ? null : cell.TryCast<SkillTreeNode>();
            if (!IsAlive(node)) node = NodeAt(cells, column, row);
            if (IsAlive(node)) node.Clicked();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    // The same dispatch as native HandleNavigateInput, whose short-branch prologue crashes when called through
    // its detour; the hook therefore never runs the original (see reference/passive-reading.md).
    private static void NativeStep(PassiveTreeNavigable owner, Direction direction)
    {
        if (direction == Direction.UP) owner.VerticalMoveToClosestNode(1);
        else if (direction == Direction.DOWN) owner.VerticalMoveToClosestNode(-1);
        else if (direction == Direction.LEFT) owner.HorizontalMoveToClosestNode(-1);
        else if (direction == Direction.RIGHT) owner.HorizontalMoveToClosestNode(1);
    }

    /// <summary>Coming back from the buttons above, the banner or the skill strip below lands on the top (bottom for the
    /// strip) of the first column, where the grid is left for them.</summary>
    internal static void AfterSelected(PassiveTreeNavigable owner)
    {
        try
        {
            var previous = NavigationManager.previousSelectedNavigableId;
            // The banner, left of the first column, returns there too.
            bool fromTop = previous is NavigableID.PASSIVE_TREE_TOP_BUTTONS or NavigableID.PASSIVE_TREE_TOP_BUTTONS_LOWER or
                NavigableID.PASSIVE_TREE_BANNER;
            if (!fromTop && previous != NavigableID.PASSIVE_TREE_UNLOCK_PANEL) return;
            // Native OnSelected ignores selection without a controller; the fullscreen panel arranges these exits differently.
            if (!EpochInputManager.IsControllerActive || !PassivePanelReader.Owns(owner)) return;
            if (!TryReadOrder(owner, out var cells) || Order.Count == 0) return;
            int last = 0;
            while (last + 1 < Order.Count && Order[last + 1].Column == Order[0].Column) last++;
            var target = Order[fromTop ? 0 : last];
            if (owner._currentColumn != target.Column || owner._currentRow != target.Row)
                MoveTo(owner, cells, target.Column, target.Row);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    /// <summary>The node's place in its column, the column's ordinal and the tree points its nodes need at least,
    /// or false when it is not in the grid.</summary>
    internal static bool TryGetPosition(PassiveTreeNavigable owner, SkillTreeNode node, out int position, out int count,
        out int column, out int threshold)
    {
        position = count = column = threshold = 0;
        if (!IsAlive(node)) return false;
        if (!IsAlive(owner) || owner.Pointer != _orderOwner || owner._currentColumn != _orderColumn ||
            owner._currentRow != _orderRow)
        {
            if (!TryReadOrder(owner, out _)) return false;
        }
        int index = Nodes.IndexOf(node.Pointer);
        if (index < 0) return false;
        var cell = Order[index];
        if (PassiveNavigationOrder.PlaceOf(Order, cell.Column, cell.Row) is not { } place) return false;
        position = place.Index + 1;
        count = place.Size;
        column = place.Group + 1;
        threshold = ColumnThresholds()[place.Group];
        return true;
    }

    /// <summary>Per column (in order), the fewest tree points any of its nodes needs; the game sets columns by these.</summary>
    internal static List<int> ColumnThresholds()
    {
        var thresholds = new List<int>();
        for (int i = 0; i < Order.Count; i++)
            if (i > 0 && Order[i].Column == Order[i - 1].Column)
                thresholds[thresholds.Count - 1] = Math.Min(thresholds[thresholds.Count - 1], Thresholds[i]);
            else thresholds.Add(Thresholds[i]);
        return thresholds;
    }

    /// <summary>The GameObject of a grid cell, which may hold a node component or its GameObject.</summary>
    internal static GameObject CellRoot(Il2CppSystem.Array cells, int column, int row)
    {
        var cell = cells.GetValue(column, row);
        if (cell is null || cell.WasCollected) return null;
        var root = cell.TryCast<GameObject>();
        return IsAlive(root) ? root : cell.TryCast<Component>()?.gameObject;
    }

    internal static void Reset()
    {
        Order.Clear();
        Nodes.Clear();
        Thresholds.Clear();
        _orderOwner = IntPtr.Zero;
        _orderColumn = _orderRow = -1;
    }

    private static bool TryReadOrder(PassiveTreeNavigable owner, out Il2CppSystem.Array cells)
    {
        Reset();
        cells = null;
        if (!IsAlive(owner) || !owner.isActiveAndEnabled) return false;
        var raw = owner._navigationElements;
        cells = raw is null || raw.WasCollected ? null : raw.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || cells.Rank != 2) return false;
        for (int column = 0; column < cells.GetLength(0); column++)
            for (int row = cells.GetLength(1) - 1; row >= 0; row--)
            {
                var node = NodeAt(cells, column, row);
                // A node listed in two cells is read once, at its first place.
                if (!IsAlive(node) || !node.isActiveAndEnabled || !UiVisibility.IsVisible(node.gameObject) ||
                    Nodes.Contains(node.Pointer)) continue;
                Order.Add((column, row));
                Nodes.Add(node.Pointer);
                Thresholds.Add(node.masteryRequirement);
            }
        _orderOwner = owner.Pointer;
        _orderColumn = owner._currentColumn;
        _orderRow = owner._currentRow;
        return true;
    }

    private static SkillTreeNode NodeAt(Il2CppSystem.Array cells, int column, int row)
    {
        var root = CellRoot(cells, column, row);
        return IsAlive(root) ? root.GetComponent<SkillTreeNode>() : null;
    }

    private static void MoveTo(PassiveTreeNavigable owner, Il2CppSystem.Array cells, int column, int row)
    {
        Leave(owner, cells);
        owner._currentColumn = column;
        owner._currentRow = row;
        owner.SelectNode();
    }

    // Native moves end the previous node's hover before selecting the next one.
    private static void Leave(PassiveTreeNavigable owner, Il2CppSystem.Array cells)
    {
        int column = owner._currentColumn, row = owner._currentRow;
        if (column < 0 || row < 0 || column >= cells.GetLength(0) || row >= cells.GetLength(1)) return;
        var root = CellRoot(cells, column, row);
        var tooltip = IsAlive(root) ? root.GetComponent<SkillnodeTooltip>() : null;
        if (IsAlive(tooltip)) tooltip.EmulateOnPointerExit();
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Passive navigation fell back to the game: {ex}");
    }
}
