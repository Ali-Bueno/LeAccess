using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

/// <summary>Moves through an open skill tree branch by branch instead of the native nearest-node search.</summary>
/// <remarks>See reference/skill-tree-navigation.md.</remarks>
internal static class SkillTreeNavigation
{
    private static readonly List<SkillTreeNode> Nodes = new();
    private static readonly List<int> Branch = new();
    private static readonly GroupEntryTracker Entry = new();
    private static List<int> _order = new();
    private static IntPtr _tree;
    private static int _listCount = -1;
    private static string _lastFailure;

    /// <summary>Handles a d-pad or stick move inside the open tree; false lets the game's own move run.</summary>
    internal static bool TryNavigate(SkillsPanelManager panel, Vector3 input)
    {
        try
        {
            // While the cursor is on the specialization row at the top, the game's own handling applies.
            if (!IsAlive(panel) || !panel.IsSkillTreeOpen || panel.inTopArea || !TryReadOrder(panel)) return false;
            int position = PositionOf(ResolveNode(panel));
            var move = SkillTreeOrder.Plan(Branch, position, DirectionExtensions.ToDirection(input));
            switch (move.Kind)
            {
                case SkillTreeOrder.MoveKind.Select:
                    Focus(panel, Nodes[_order[move.Position]]);
                    return true;
                case SkillTreeOrder.MoveKind.Stay:
                    // Native d-pad input cleared the tree row before this call; refocusing restores it for A.
                    Focus(panel, Nodes[_order[position]]);
                    SkillAccessibility.RepeatCurrent();
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    /// <summary>After opening, the game focuses the tree prefab's own root; this focuses the opened tree's node.</summary>
    internal static void AfterTreeOpened(SkillsPanelManager panel)
    {
        try
        {
            // Native OnOpenSkillTree focuses a node only with a controller.
            if (!IsAlive(panel) || !EpochInputManager.IsControllerActive || !TryReadOrder(panel)) return;
            var node = ResolveNode(panel);
            var selected = panel.currentlySelectedNode;
            if (IsAlive(node) && (!IsAlive(selected) || selected.Pointer != node.Pointer)) Focus(panel, node);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    /// <summary>The open tree's node for the game's selection, which can still be the prefab's copy after opening.</summary>
    internal static SkillTreeNode ResolveNode(SkillsPanelManager panel)
    {
        var selected = panel.currentlySelectedNode;
        var tree = panel.CurrentlyOpenTree;
        if (!IsAlive(selected) || !IsAlive(tree)) return null;
        if (IsAlive(selected.tree) && selected.tree.Pointer == tree.Pointer) return selected;
        var nodes = tree.nodeList;
        if (nodes is null || nodes.WasCollected) return null;
        for (int i = 0; i < nodes.Count; i++)
            if (IsAlive(nodes[i]) && nodes[i].id == selected.id) return nodes[i];
        return null;
    }

    /// <summary>The node's place in its branch, with the branch's name when the selection just entered it.</summary>
    internal static bool TryDescribe(SkillsPanelManager panel, SkillTreeNode node, out string label, out int position, out int count)
    {
        label = null;
        position = count = 0;
        if (!IsAlive(panel) || !TryReadOrder(panel)) return false;
        int place = PositionOf(node);
        if (place < 0) return false;
        (position, count) = SkillTreeOrder.PlaceInBranch(Branch, place);
        // The root is the skill itself; its branches are numbered from 1.
        int branch = Branch[place];
        if (Entry.Entered(node.Pointer, (_tree, branch)) && branch > 0) label = ModText.Format("skill.branch", branch);
        return true;
    }

    /// <summary>The next selection names its branch again.</summary>
    internal static void ForgetBranch() => Entry.Reset();

    internal static void Reset()
    {
        Nodes.Clear();
        Branch.Clear();
        _order = new List<int>();
        _tree = IntPtr.Zero;
        _listCount = -1;
    }

    private static int PositionOf(SkillTreeNode node)
    {
        if (!IsAlive(node)) return -1;
        for (int p = 0; p < _order.Count; p++)
            if (Nodes[_order[p]].Pointer == node.Pointer) return p;
        return -1;
    }

    private static bool TryReadOrder(SkillsPanelManager panel)
    {
        var tree = panel.CurrentlyOpenTree;
        var list = IsAlive(tree) ? tree.nodeList : null;
        if (list is null || list.WasCollected) return false;
        if (_tree == tree.Pointer && _listCount == list.Count && Nodes.TrueForAll(IsAlive)) return _order.Count > 0;
        Reset();
        for (int i = 0; i < list.Count; i++)
            if (IsAlive(list[i]) && list[i].gameObject.activeInHierarchy) Nodes.Add(list[i]);
        var described = new List<SkillTreeOrder.Node>(Nodes.Count);
        foreach (var node in Nodes)
        {
            var position = node.transform.position;
            described.Add(new SkillTreeOrder.Node(RequiredIndex(node), position.x, position.y));
        }
        // The game treats the list's first node as the tree's root hexagon.
        int root = list.Count > 0 && IsAlive(list[0]) ? Nodes.FindIndex(node => node.Pointer == list[0].Pointer) : -1;
        _order = SkillTreeOrder.Build(described, root, Branch);
        _tree = tree.Pointer;
        _listCount = list.Count;
        return _order.Count > 0;
    }

    private static int RequiredIndex(SkillTreeNode node)
    {
        var requirements = node.requirements;
        if (requirements is null || requirements.WasCollected) return -1;
        for (int i = 0; i < requirements.Count; i++)
        {
            var required = requirements[i]?.node;
            if (!IsAlive(required)) continue;
            int index = Nodes.FindIndex(candidate => candidate.Pointer == required.Pointer);
            if (index >= 0) return index;
        }
        return -1;
    }

    // The same steps as the native move: store the node, place the cursor (marking the tree row), show its tooltip.
    private static void Focus(SkillsPanelManager panel, SkillTreeNode node)
    {
        panel.currentlySelectedNode = node;
        panel.FocusTreeNode();
        panel.StartCoroutine(panel.ShowTreeNodeTooltip());
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Skill tree navigation fell back to the game: {ex}");
    }
}
