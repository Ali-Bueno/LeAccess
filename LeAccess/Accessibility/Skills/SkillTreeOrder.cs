using System.Collections.Generic;

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

    /// <summary>Down/Up step through the list; Right/Left jump to the first node of the next/previous branch.</summary>
    internal static Move Plan(IReadOnlyList<int> branch, int position, Direction direction)
    {
        if (branch.Count == 0) return new Move(MoveKind.Native);
        if (position < 0 || position >= branch.Count) return new Move(MoveKind.Select, 0);
        switch (direction)
        {
            case Direction.DOWN:
                return position + 1 < branch.Count ? new Move(MoveKind.Select, position + 1) : new Move(MoveKind.Stay);
            case Direction.UP:
                return position > 0 ? new Move(MoveKind.Select, position - 1) : new Move(MoveKind.Stay);
            case Direction.RIGHT:
                for (int p = position + 1; p < branch.Count; p++)
                    if (branch[p] != branch[position]) return new Move(MoveKind.Select, p);
                return new Move(MoveKind.Stay);
            case Direction.LEFT:
                if (branch[position] == 0) return new Move(MoveKind.Stay);
                for (int p = 0; p < branch.Count; p++)
                    if (branch[p] == branch[position] - 1) return new Move(MoveKind.Select, p);
                return new Move(MoveKind.Stay);
        }
        return new Move(MoveKind.Native);
    }
}
