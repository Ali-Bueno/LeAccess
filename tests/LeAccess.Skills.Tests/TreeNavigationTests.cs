using LeAccess.Accessibility.Skills;
using UnityEngine;
using MoveKind = LeAccess.Accessibility.Skills.SkillTreeOrder.MoveKind;

internal static class TreeNavigationTests
{
    internal static void Run(Action<bool, string> check)
    {
        SkillAccessibility.Reset();
        var treeObject = new GameObject();
        var tree = treeObject.Add(new SkillTree { treeID = 9 });
        var all = new Dictionary<string, SkillTreeNode>();
        SkillTreeNode Node(string name, int id, float x, float y, SkillTreeNode requires = null)
        {
            var go = treeObject.Child();
            go.transform.position = new Vector3 { x = x, y = y };
            var node = go.Add(new SkillTreeNode { tree = tree, id = id, nodeName = name,
                requirements = new(requires is null ? Array.Empty<SkillTreeNode.RequirementFromNode>() :
                    new[] { new SkillTreeNode.RequirementFromNode { node = requires } }) });
            all[name] = node;
            return node;
        }
        var root = Node("Raíz", 0, 0, 0);
        var a1 = Node("A1", 1, 100, 100, root);
        var a2 = Node("A2", 2, 200, 100, a1);
        var b1 = Node("B1", 3, 100, -100, root);
        var b2 = Node("B2", 4, 200, -50, b1);
        var b3 = Node("B3", 5, 200, -150, b1);
        var loose = Node("Suelto", 6, 100, 0);
        // The game's list keeps authoring order; only its first entry is the root.
        tree.nodeList = new(new[] { root, b2, a1, loose, b1, a2, b3 });

        var described = new List<SkillTreeOrder.Node>();
        var listed = tree.nodeList.ToList();
        foreach (var node in listed)
        {
            var required = node.requirements.Count == 0 ? null : node.requirements[0].node;
            described.Add(new SkillTreeOrder.Node(required is null ? -1 : listed.IndexOf(required),
                node.transform.position.x, node.transform.position.y));
        }
        var branch = new List<int>();
        var order = SkillTreeOrder.Build(described, 0, branch).Select(index => listed[index].nodeName).ToList();
        check(string.Join(",", order) == "Raíz,A1,A2,Suelto,B1,B2,B3" && string.Join(",", branch) == "0,1,1,2,3,3,3",
            "Each branch reads whole after the root, top branch first, and a node follows the node it requires: " + string.Join(",", order));
        SkillTreeOrder.Move Plan(int position, Direction direction) => SkillTreeOrder.Plan(branch, position, direction);
        SkillTreeOrder.Move Select(int position) => new(MoveKind.Select, position);
        var stay = new SkillTreeOrder.Move(MoveKind.Stay);
        check(Plan(1, Direction.DOWN) == Select(2) && Plan(5, Direction.UP) == Select(4) && Plan(4, Direction.DOWN) == Select(5),
            "Down and Up step inside the branch");
        check(Plan(2, Direction.DOWN) == stay && Plan(4, Direction.UP) == stay && Plan(0, Direction.DOWN) == stay &&
            Plan(6, Direction.DOWN) == stay, "Down and Up stop at the ends of a branch instead of running into the next one");
        check(Plan(0, Direction.UP) == stay && Plan(0, Direction.LEFT) == stay && Plan(5, Direction.RIGHT) == stay,
            "The root and the outer branches stay put");
        check(Plan(0, Direction.RIGHT) == Select(1) && Plan(2, Direction.RIGHT) == Select(3) && Plan(3, Direction.RIGHT) == Select(4) &&
            Plan(5, Direction.LEFT) == Select(3) && Plan(1, Direction.LEFT) == Select(0),
            "Right and Left reach the neighbouring branch at the same place, clamped to its length");
        check(SkillTreeOrder.PlaceInBranch(branch, 5) == (2, 3) && SkillTreeOrder.PlaceInBranch(branch, 0) == (1, 1),
            "A node's place is counted inside its branch");
        check(Plan(-1, Direction.UP) == Select(0) && Plan(0, (Direction)9) == new SkillTreeOrder.Move(MoveKind.Native),
            "An unknown selection starts at the root; unknown directions fall back to the game");
        var cycle = new List<SkillTreeOrder.Node> { new(-1, 0, 0), new(2, 1, 0), new(1, 2, 0) };
        check(SkillTreeOrder.Build(cycle, 0, branch).Count == 3, "Nodes caught in a requirement cycle are still listed");

        var panel = new GameObject().Add(new SkillsPanelManager
        { IsSkillTreeOpen = true, CurrentlyOpenTree = tree, currentlySelectedNode = root });
        bool Move(float x, float y) => SkillTreeNavigation.TryNavigate(panel, new Vector3 { x = x, y = y });
        check(Move(1, 0) && panel.currentlySelectedNode == a1 && panel.Focused.Last() == a1 && panel.Coroutines == 1 &&
            panel.currentlySelectedIdx.y == 1, "A move stores the node, places the cursor on the tree row and opens its tooltip");
        check(Move(1, 0.3f) && panel.currentlySelectedNode == loose, "A diagonal flick follows its dominant direction");
        panel.currentlySelectedNode = b3; panel.currentlySelectedIdx.y = 0;
        int focused = panel.Focused.Count;
        check(Move(0, -1) && panel.currentlySelectedNode == b3 && panel.Focused.Count == focused + 1 && panel.currentlySelectedIdx.y == 1,
            "The last node stays focused and restores the tree row the native input cleared, so A spends a point there");
        panel.inTopArea = true;
        check(!Move(0, -1), "The specialization row at the top keeps the game's own movement");
        panel.inTopArea = false; panel.IsSkillTreeOpen = false;
        check(!Move(0, -1), "A closed tree keeps the game's own movement");
        panel.IsSkillTreeOpen = true;

        var prefabRoot = new GameObject().Add(new SkillTreeNode { id = 0, nodeName = "Raíz del prefab" });
        panel.currentlySelectedNode = prefabRoot;
        check(SkillTreeNavigation.ResolveNode(panel) == root, "The prefab's root stored after opening maps to the opened tree's root");
        EpochInputManager.IsControllerActive = false;
        SkillTreeNavigation.AfterTreeOpened(panel);
        check(panel.currentlySelectedNode == prefabRoot, "Without a controller the game's opening focus is left alone");
        EpochInputManager.IsControllerActive = true;
        SkillTreeNavigation.AfterTreeOpened(panel);
        check(panel.currentlySelectedNode == root && panel.Focused.Last() == root, "Opening a tree focuses its own root node");

        NavigationManager.currentlySelected = panel;
        panel.currentlySelectedNode = b1;
        SkillTreeNavigation.ForgetBranch();
        check(SkillSelectionReader.TryRead(out var selection, includeDetails: false) && selection.Name == "B1" &&
            selection.Status.EndsWith(Speech.F("list.position", 1, 3)) && selection.Context == Speech.F("skill.branch", 3),
            "A node entering a branch names it and states its place in the branch: " + selection.Status);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context == Speech.F("skill.branch", 3),
            "The branch name stays while the same node is selected, so the brief does not change");
        panel.currentlySelectedNode = b2;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context is null &&
            selection.Status.EndsWith(Speech.F("list.position", 2, 3)), "A move inside the branch does not repeat its name");
        panel.currentlySelectedNode = root;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context is null,
            "The root is the skill itself and has no branch name");
        NavigationManager.currentlySelected = null;
        SkillAccessibility.Reset();
    }
}
