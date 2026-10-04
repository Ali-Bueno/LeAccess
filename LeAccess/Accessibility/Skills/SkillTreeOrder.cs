using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;

namespace LeAccess.Accessibility.Skills;

/// <summary>Orders a skill tree depth-first from its root, so each node follows the node it requires.</summary>
internal static class SkillTreeOrder
{
    internal enum MoveKind { Select, Stay, Native }
    internal readonly record struct Move(MoveKind Kind, int Position = -1);
    /// <summary>A node with the index of the node it requires (-1 for the root) and its screen position.</summary>
    internal readonly record struct Node(int Parent, float X, float Y);

    /// <summary>Node indices in reading order; branch[p] numbers the root's subtree holding position p (0 = root).</summary>
    internal static List<int> Build(IReadOnlyList<Node> nodes, int root, List<int> branch)
    {
        var order = new List<int>();
        branch.Clear();
        if (root < 0 || root >= nodes.Count) return order;
        var children = new List<int>[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) children[i] = new List<int>();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (i == root) continue;
            int parent = nodes[i].Parent;
            children[parent >= 0 && parent < nodes.Count && parent != i ? parent : root].Add(i);
        }
        // Siblings read top to bottom, then left to right.
        foreach (var list in children)
            list.Sort((a, b) => nodes[a].Y != nodes[b].Y ? nodes[b].Y.CompareTo(nodes[a].Y) : nodes[a].X.CompareTo(nodes[b].X));
        var visited = new bool[nodes.Count];
        Visit(root, 0);
        int next = 1;
        foreach (int child in children[root]) Visit(child, next++);
        // A requirement cycle cannot be reached from the root; such nodes still form their own branches.
        for (int i = 0; i < nodes.Count; i++) if (!visited[i]) Visit(i, next++);
        return order;

        void Visit(int node, int number)
        {
            if (visited[node]) return;
            visited[node] = true;
            order.Add(node);
            branch.Add(number);
            if (node == root) return;
            foreach (int child in children[node]) Visit(child, number);
        }
    }

    /// <summary>Down/Up step inside the branch and stop at its ends; Right/Left go to the neighbouring branch at the same place.</summary>
    internal static Move Plan(IReadOnlyList<int> branch, int position, Direction direction)
    {
        if (branch.Count == 0) return new Move(MoveKind.Native);
        if (position < 0 || position >= branch.Count) return new Move(MoveKind.Select, 0);
        GroupedGridOrder.Step? step = direction switch
        {
            Direction.DOWN => GroupedGridOrder.Step.NextInGroup,
            Direction.UP => GroupedGridOrder.Step.PreviousInGroup,
            Direction.RIGHT => GroupedGridOrder.Step.NextGroup,
            Direction.LEFT => GroupedGridOrder.Step.PreviousGroup,
            _ => null
        };
        if (step is null) return new Move(MoveKind.Native);
        var sizes = BranchSizes(branch);
        int group = branch[position];
        var move = GroupedGridOrder.Plan(sizes, group, position - Start(branch, group), step.Value);
        return move.Kind == GroupedGridOrder.MoveKind.Select
            ? new Move(MoveKind.Select, Start(branch, move.Group) + move.Index) : new Move(MoveKind.Stay);
    }

    /// <summary>The 1-based place of a position inside its branch, and the branch's size.</summary>
    internal static (int Place, int Count) PlaceInBranch(IReadOnlyList<int> branch, int position)
    {
        int group = branch[position];
        return (position - Start(branch, group) + 1, BranchSizes(branch)[group]);
    }

    private static int Start(IReadOnlyList<int> branch, int group)
    {
        for (int p = 0; p < branch.Count; p++) if (branch[p] == group) return p;
        return -1;
    }

    // Build numbers branches in list order, so each branch is one contiguous run of positions.
    private static int[] BranchSizes(IReadOnlyList<int> branch)
    {
        int highest = 0;
        foreach (int number in branch) highest = System.Math.Max(highest, number);
        var sizes = new int[highest + 1];
        foreach (int number in branch) sizes[number]++;
        return sizes;
    }
}
