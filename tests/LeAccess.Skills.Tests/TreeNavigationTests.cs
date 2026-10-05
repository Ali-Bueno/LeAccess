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
        check(Plan(2, Direction.DOWN) == stay && Plan(0, Direction.DOWN) == stay && Plan(6, Direction.DOWN) == stay,
            "Down stops at the end of a branch instead of running into the next one");
        check(Plan(4, Direction.UP) == Select(0) && Plan(1, Direction.UP) == Select(0) && Plan(3, Direction.UP) == Select(0),
            "Up from a branch's first node reaches the root");
        check(Plan(0, Direction.UP) == new SkillTreeOrder.Move(MoveKind.Leave),
            "Up from the root leaves the tree for the buttons above it");
        check(Plan(0, Direction.LEFT) == stay && Plan(5, Direction.RIGHT) == stay, "The root and the outer branches stay put sideways");
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

        // Tree state: a new tree states it whole, later reads only what changed, and a selection keeps its notice.
        var state = new SkillTreeState.State((IntPtr)1, true, 3, false);
        check(SkillTreeState.Changes(null, state with { Specialised = false }) ==
            new SkillTreeState.Notice(Speech.T("skill.not_specialised"), null), "A new unspecialised tree says so first");
        check(SkillTreeState.Changes(null, state) == new SkillTreeState.Notice(null, Speech.F("skill.tree_unspent", 3)),
            "A new specialised tree states its unspent points");
        check(SkillTreeState.Changes(state, state with { Unspent = 2 }) == new SkillTreeState.Notice(null, Speech.F("skill.tree_unspent", 2)) &&
            SkillTreeState.Changes(state, state) == default, "Only a change of points is spoken again");
        check(SkillTreeState.Changes(state, state with { Respeccing = true }).Before == Speech.T("skill.respec_mode") &&
            SkillTreeState.Changes(state with { Respeccing = true }, state).Before == Speech.T("skill.respec_mode_ended"),
            "Entering and leaving point removal mode are spoken before the selection");
        check(SkillTreeState.Changes(state with { Specialised = false, Unspent = 0 }, state) ==
            new SkillTreeState.Notice(Speech.T("skill.now_specialised"), Speech.F("skill.tree_unspent", 3)) &&
            SkillTreeState.Changes(state, state with { Specialised = false }).Before == Speech.T("skill.despecialised"),
            "Specialising states the points to spend; despecialising is spoken");

        SkillAccessibility.Reset();
        var skill = new Ability { abilityName = "Desollar" };
        var data = new GameObject().Add(new LocalTreeData());
        PlayerFinder.treeData = data;
        tree.ability = skill;
        panel.currentlySelectedNode = a1;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) &&
            selection.Context == Speech.Join(Speech.T("skill.not_specialised"), Speech.F("skill.branch", 1)),
            "Opening an unspecialised tree says so before the branch: " + selection.Context);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) &&
            selection.Context == Speech.Join(Speech.T("skill.not_specialised"), Speech.F("skill.branch", 1)),
            "The notice stays while the same node is selected");
        panel.inTopArea = false;
        SkillTreeState.BeforeSubmit(panel, SubmitType.A_EX);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) &&
            selection.Status.Contains(Speech.T("skill.specialise_first")), "Cross on an unspecialised tree's node says to specialise first");
        panel.currentlySelectedNode = a2;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context is null &&
            !selection.Status.Contains(Speech.T("skill.specialise_first")), "The hint and notice end with the selection");
        data.Specialised.Add(skill); tree.unspentPoints = 0;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context == Speech.T("skill.now_specialised") &&
            selection.Status.Contains(Speech.F("skill.tree_unspent", 0)), "Specialising while a node is selected is spoken on it");
        SkillTreeState.BeforeSubmit(panel, SubmitType.A_EX);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Status.Contains(Speech.T("node.no_points")),
            "Cross without unspent points says none are left");
        SkillTreeState.BeforeSubmit(panel, SubmitType.Y_TRIANGLE);
        panel.SkillsSpecialisationUIManager = new GameObject().Add(new SkillsSpecialisationUIManager { Respeccing = true });
        a2.pointsAllocated = 2; a2.CouldBeRespecced = false;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context == Speech.T("skill.respec_mode") &&
            selection.Status.Contains(Speech.T("skill.cannot_refund")), "Point removal mode is spoken and marks a node the game will not refund");
        a2.CouldBeRespecced = true;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && !selection.Status.Contains(Speech.T("skill.cannot_refund")),
            "A refundable node carries no mark");
        panel.SkillsSpecialisationUIManager = null;
        root.maxPoints = 0;
        panel.currentlySelectedNode = root;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && !selection.Status.Contains(Speech.F("node.points", 0, 0)),
            "The root, which takes no points, does not read zero of zero");
        root.maxPoints = 5; a2.pointsAllocated = 0;

        // Up from the root leaves the tree the game's own way, on the button the tree needs now.
        var grid = new Component[11, 4];
        GameObject Cell(int x, string label = null)
        {
            var go = new GameObject();
            grid[x, 0] = go.Add(new UnityEngine.UI.Button());
            if (label != null) go.Child().Add(new TMPro.TMP_Text { text = label });
            return go;
        }
        panel.backButton = Cell(2).GetComponent<UnityEngine.UI.Button>();
        var specialisation = new GameObject().Add(new SkillsSpecialisationUIManager
            { respecButton = Cell(8, "Reasignar"), specialiseButton = Cell(9, "Especializar"), stopRespeccingButton = Cell(10) });
        panel.SkillsSpecialisationUIManager = specialisation;
        panel.selectables = new Il2CppSystem.Array(grid);
        data.Specialised.Remove(skill);
        panel.currentlySelectedNode = root; panel.currentlySelectedIdx = new Vector2Int(4, 0);
        int hidden = panel.HiddenTooltips, hiddenNodes = SkillsPanelManager.HiddenNodeTooltips;
        UnityEngine.EventSystems.EventSystem.current = new GameObject().Add(new UnityEngine.EventSystems.EventSystem
            { Selected = root.gameObject });
        check(Move(0, 1) && panel.currentlySelectedIdx == new Vector2Int(9, 0) && panel.FocusedCells.Last() == new Vector2Int(9, 0) &&
            panel.HiddenTooltips == hidden + 1 && SkillsPanelManager.HiddenNodeTooltips == hiddenNodes + 1 &&
            UnityEngine.EventSystems.EventSystem.current.Selected is null && panel.currentlySelectedNode == root,
            "Up from an unspecialised tree's root clears the selection and tooltips and focuses Specialise, keeping the node for Down");
        panel.inTopArea = true;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Name == "Especializar",
            "The top row reads the button's own label: " + selection.Name);
        panel.inTopArea = false;
        data.Specialised.Add(skill);
        panel.EmptyCells.Add(new Vector2Int(9, 0)); panel.EmptyCells.Add(new Vector2Int(10, 0));
        check(Move(0, 1) && panel.currentlySelectedIdx == new Vector2Int(8, 0), "A specialised tree lands on the respec button");
        panel.inTopArea = true;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Name == "Reasignar",
            "Hidden buttons are skipped like the game's empty cells");
        panel.currentlySelectedIdx = new Vector2Int(2, 0);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Name == Speech.T("skill.tree_back"),
            "An unlabelled Back button gets the mod's name");
        panel.inTopArea = false;
        panel.EmptyCells.Add(new Vector2Int(8, 0)); panel.EmptyCells.Add(new Vector2Int(2, 0));
        panel.currentlySelectedIdx = new Vector2Int(5, 0);
        int rightMoves = panel.RightMoves;
        check(Move(0, 1) && panel.RightMoves == rightMoves + 1 && panel.currentlySelectedIdx.y == 0,
            "Without a known button the game's own step right from the old column picks the cell");
        // Removing points hides Respec under the cursor; the last node is focused again, as Down would.
        panel.inTopArea = true; panel.currentlySelectedNode = a1; panel.currentlySelectedIdx = new Vector2Int(8, 0);
        SkillTreeNavigation.RecoverHiddenButton(panel);
        check(panel.currentlySelectedIdx.y == 1 && panel.Focused.Last() == a1, "A hidden button under the cursor returns focus to the last node");
        panel.currentlySelectedIdx = new Vector2Int(9, 0); panel.EmptyCells.Remove(new Vector2Int(9, 0));
        focused = panel.Focused.Count;
        SkillTreeNavigation.RecoverHiddenButton(panel);
        check(panel.Focused.Count == focused && panel.currentlySelectedIdx == new Vector2Int(9, 0), "A visible button keeps the focus");
        panel.inTopArea = false;
        panel.selectables = null;
        panel.currentlySelectedNode = root;
        check(Move(0, 1) && panel.currentlySelectedNode == root && panel.currentlySelectedIdx.y == 1,
            "Without the top row's cells the root stays focused");
        panel.SkillsSpecialisationUIManager = null; PlayerFinder.treeData = null; tree.ability = null;
        NavigationManager.currentlySelected = null;
        SkillAccessibility.Reset();
    }
}
