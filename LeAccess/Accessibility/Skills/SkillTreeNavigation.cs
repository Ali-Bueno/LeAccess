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
    private static readonly List<int> Parents = new();
    private static readonly List<List<int>> RequiredBy = new();
    private static readonly GroupEntryTracker Entry = new();
    private static List<int> _order = new();
    private static IntPtr _tree;
    private static int _root = -1;
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
                case SkillTreeOrder.MoveKind.Leave when LeaveForButtons(panel):
                    return true;
                case SkillTreeOrder.MoveKind.Stay:
                case SkillTreeOrder.MoveKind.Leave:
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

    /// <summary>A top-row button the game hid under the cursor (Respec, once point removal starts) leaves the last node
    /// focused again, as the game's own Down does; otherwise nothing could be read until the player moved.</summary>
    internal static void RecoverHiddenButton(SkillsPanelManager panel)
    {
        try
        {
            if (!IsAlive(panel) || !panel.IsSkillTreeOpen || !panel.inTopArea || panel.currentlySelectedIdx.y != 0 ||
                !panel.ArraySlotIsEmpty()) return;
            var node = ResolveNode(panel);
            if (IsAlive(node)) Focus(panel, node);
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

    /// <summary>What the node requires (the root aside) for its brief, and for its details the way to it from its branch's
    /// first node and the nodes that require it.</summary>
    internal static bool TryReadLinks(SkillsPanelManager panel, SkillTreeNode node, Func<SkillTreeNode, string> name,
        out string requires, out string details)
    {
        requires = details = null;
        if (!IsAlive(panel) || !IsAlive(node) || !TryReadOrder(panel)) return false;
        int index = Nodes.FindIndex(candidate => candidate.Pointer == node.Pointer);
        if (index < 0) return false;
        var requirements = new List<SkillTreeLinks.Requirement>();
        var list = node.requirements;
        if (list is not null && !list.WasCollected)
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                var required = entry?.node;
                if (!IsAlive(required) || IndexOf(required) == _root) continue;
                requirements.Add(new(name(required), entry.requirement, required.pointsAllocated));
            }
        requires = SkillTreeLinks.Requires(requirements, node.requirementsMet);
        var path = new List<string>();
        for (int parent = Parents[index], steps = 0; parent >= 0 && parent != _root && steps < Nodes.Count; parent = Parents[parent], steps++)
            path.Insert(0, name(Nodes[parent]));
        var leadsTo = new List<string>();
        foreach (int next in _order)
            if (RequiredBy[index].Contains(next)) leadsTo.Add(name(Nodes[next]));
        details = SkillTreeLinks.Details(path, leadsTo);
        return true;
    }

    /// <summary>The next selection names its branch again.</summary>
    internal static void ForgetBranch() => Entry.Reset();

    internal static void Reset()
    {
        Nodes.Clear();
        Branch.Clear();
        Parents.Clear();
        RequiredBy.Clear();
        _root = -1;
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
        foreach (var node in Nodes) RequiredBy.Add(new List<int>());
        for (int index = 0; index < Nodes.Count; index++)
        {
            var node = Nodes[index];
            var position = node.transform.position;
            int parent = RequiredIndex(node, index);
            Parents.Add(parent);
            described.Add(new SkillTreeOrder.Node(parent, position.x, position.y));
        }
        // The game treats the list's first node as the tree's root hexagon.
        int root = list.Count > 0 && IsAlive(list[0]) ? Nodes.FindIndex(node => node.Pointer == list[0].Pointer) : -1;
        _root = root;
        _order = SkillTreeOrder.Build(described, root, Branch);
        _tree = tree.Pointer;
        _listCount = list.Count;
        return _order.Count > 0;
    }

    // The first required node in the tree places this one; every required node lists it as leading on.
    private static int RequiredIndex(SkillTreeNode node, int self)
    {
        var requirements = node.requirements;
        if (requirements is null || requirements.WasCollected) return -1;
        int first = -1;
        for (int i = 0; i < requirements.Count; i++)
        {
            var required = requirements[i]?.node;
            int index = IndexOf(required);
            if (index < 0 || index == self) continue;
            if (first < 0) first = index;
            if (!RequiredBy[index].Contains(self)) RequiredBy[index].Add(self);
        }
        return first;
    }

    private static int IndexOf(SkillTreeNode node) =>
        IsAlive(node) ? Nodes.FindIndex(candidate => candidate.Pointer == node.Pointer) : -1;

    /// <summary>The game's own exit when nothing lies above, landing on the button the tree needs now.</summary>
    /// <remarks>Native order: clear the selection and both tooltips, set the top row, focus the cell; the last node stays
    /// stored, so Down returns to it. Without a known button, the game's step right from the old column picks the cell.</remarks>
    private static bool LeaveForButtons(SkillsPanelManager panel)
    {
        var cells = panel.selectables?.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || cells.Rank != 2 || cells.GetLength(1) == 0) return false;
        var specialisation = panel.SkillsSpecialisationUIManager;
        bool known = IsAlive(specialisation);
        var targets = new[]
        {
            known ? specialisation.specialiseButton : null, IsAlive(panel.specButton) ? panel.specButton.gameObject : null,
            known ? specialisation.stopRespeccingButton : null, IsAlive(panel.stopRespecButton) ? panel.stopRespecButton.gameObject : null,
            known ? specialisation.respecButton : null, IsAlive(panel.backButton) ? panel.backButton.gameObject : null
        };
        var start = panel.currentlySelectedIdx;
        int column = -1;
        foreach (var target in targets)
        {
            if (!IsAlive(target)) continue;
            for (int x = 0; x < cells.GetLength(0) && column < 0; x++)
            {
                var component = cells.GetValue(x, 0)?.TryCast<Component>();
                if (!IsAlive(component) || component.gameObject.GetInstanceID() != target.GetInstanceID()) continue;
                panel.currentlySelectedIdx = new Vector2Int(x, 0);
                if (!panel.ArraySlotIsEmpty()) column = x;
            }
            if (column >= 0) break;
        }
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (IsAlive(events)) events.SetSelectedGameObject(null);
        panel.HideIconTooltip();
        SkillsPanelManager.HideTreeNodeTooltip();
        if (column >= 0)
        {
            panel.currentlySelectedIdx = new Vector2Int(column, 0);
            panel.FocusSelectable();
        }
        else
        {
            panel.currentlySelectedIdx = new Vector2Int(start.x, 0);
            panel.MoveRight();
        }
        return true;
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
