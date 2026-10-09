using UnityEngine;

internal enum Direction { LEFT, UP, RIGHT, DOWN }
internal enum NavigableID { NONE, PASSIVE_TREE, PASSIVE_TREE_TOP_BUTTONS, PASSIVE_TREE_UNLOCK_PANEL, PASSIVE_TREE_BANNER, PASSIVE_TREE_TOP_BUTTONS_LOWER }
internal static class NavigationManager
{
    internal static NativeObject currentlySelected;
    internal static NavigableID previousSelectedNavigableId;
}
internal static class EpochInputManager { internal static bool IsControllerActive = true; }
internal static class DirectionExtensions
{
    internal static Direction ToDirection(Vector3 input, float threshold = 0.6f) => Math.Abs(input.x) >= Math.Abs(input.y)
        ? input.x < 0 ? Direction.LEFT : Direction.RIGHT : input.y < 0 ? Direction.DOWN : Direction.UP;
}
internal sealed class PassiveTreeNavigable : Component
{
    internal enum PassiveSkillTreeNodeSearchDirection { Up, Down, Left, Right }
    internal readonly List<(Vector2Int Index, PassiveSkillTreeNodeSearchDirection Direction)> Entries = new();
    internal void selectSkillTreeWithIndexAndDirection(Vector2Int index, PassiveSkillTreeNodeSearchDirection direction) =>
        Entries.Add((index, direction));
    internal NativeObject _navigationElements;
    internal int _currentColumn, _currentRow;
    internal readonly List<(int Column, int Row)> Selected = new();
    internal readonly List<string> NativeSteps = new();
    internal void SelectNode() => Selected.Add((_currentColumn, _currentRow));
    internal void VerticalMoveToClosestNode(int direction) => NativeSteps.Add($"vertical {direction} from {_currentColumn},{_currentRow}");
    internal void HorizontalMoveToClosestNode(int direction) => NativeSteps.Add($"horizontal {direction} from {_currentColumn},{_currentRow}");
}
internal sealed class SkillnodeTooltip : Component
{
    internal SkillTreeNode node;
    internal int Exits;
    internal void EmulateOnPointerExit() => Exits++;
}
internal class Tree : Component { internal int unspentPoints; internal NativeArray<SkillTreeNode> nodeList; }
internal sealed class RequirementFromNode : NativeObject { internal SkillTreeNode node; internal int requirement; }
internal sealed class CharacterTree : Tree { internal NativeArray<GameObject> masteries; }
internal sealed class PassivePanelManager : Component
{
    internal static PassivePanelManager instance;
    internal CharacterTree activeTree;
    internal MasteryBanner openedBanner;
    internal byte activeMastery;
}
internal sealed class MasteryBanner : Component { internal NativeArray<MasteryButton> masteryButtons; }
internal sealed class MasteryButton : Component
{
    internal TMPro.TextMeshProUGUI levelTMP;
    internal UnityEngine.UI.Button button;
    internal bool isMasteryClass;
    internal GameObject lockIcon;
    internal MasteryBarDisplay masteryBarDisplay;
    internal string Name, Definition;
}
internal sealed class PassiveTreeBannerNavigable : Component
{
    internal UnityEngine.UI.Button _currentButton;
    internal bool isDescriptionSelected;
    internal UnityEngine.UI.Button CurrentButton { get => _currentButton; set { Guard(); _currentButton = value; Focused++; } }
    internal int Focused;
    internal PassiveTreeNavigable Tree;
    internal PassiveTreeNavigable GetPassiveTreeNavigable() => Tree;
}
internal sealed class Actor : Component { internal LocalTreeData localTreeData; }
internal static class PlayerFinder { internal static Actor Actor; internal static Actor getPlayerActor() => Actor; }
internal sealed class SkillTreeNode : Component
{
    internal Tree tree;
    internal int pointsAllocated, maxPoints = 5, mastery, masteryRequirement;
    internal bool requirementsMet, Passive = true;
    internal string Name = "Localized passive", Description = "Game description", nodeName = "Passive";
    internal NativeArray<RequirementFromNode> requirements;
    internal bool inPassiveTree() => Passive;
    internal bool RespecMode;
    internal bool relevantPanelIsInRespecMode() => RespecMode;
    internal int Clicks;
    internal void Clicked() { Guard(); Clicks++; }
}
internal enum SubmitType { A_EX, X_SQUARE, Y_TRIANGLE }
internal static class Localization
{
    internal static string Locale = "es";
    internal static LocalizationAbilities Abilities = new();
}
internal sealed class LocalizationAbilities : NativeObject
{
    internal string GetName(Ability ability) => ability.Name;
    internal string GetDescription(Ability ability) => ability.Description;
}
internal sealed class Ability : UnityEngine.Object { internal string Name, Description; }
internal sealed class AbilityTooltip : Component { internal Ability currentAbility; }
internal sealed class PassiveTreeUnlockableAbilityIcon : Component
{
    internal byte requirement;
    internal bool isMasteryAbility;
    internal Ability Ability;
    internal Ability getAbility() => Ability;
}
internal sealed class PassiveTreeUnlockPanelNavigable : Component
{
    internal NativeArray<AbilityTooltip> ActiveAbilityTooltips;
    internal int SelectedIndex;
}
internal sealed class TabButton : Component
{
    internal int SelectedIndex;
    internal TMPro.TMP_Text _assignedPoints;
    internal GameObject _lockedPoints;
}
internal sealed class MasteryTabButton : Component { internal TabButton _tabButton; }
internal sealed class MasteryBarDisplay : Component
{ internal bool _hasFillValues; internal int _lastFillPoints, _lastFillMaxPoints; }
internal sealed class LocalTreeData : UnityEngine.Object
{ internal NativeArray<byte> masteryLevels; internal byte chosenMastery; }
internal static class PassivePanelData
{ internal sealed class MasteryData : NativeObject { internal string name; internal bool isMastery; } }
internal sealed class UIBase : Component
{ internal static UIBase instance; internal LE.UI.Tooltips.TooltipSystem TooltipSystem; }
namespace Il2CppSystem
{
    internal sealed class Array : NativeObject
    {
        internal NativeObject[,] Cells;
        internal int Rank = 2;
        internal int GetLength(int dimension) => Cells.GetLength(dimension);
        internal NativeObject GetValue(int column, int row) => Cells[column, row];
    }
}
namespace LE.UI.Tooltips
{
    internal sealed class TooltipSystem : Component
    {
        internal static TooltipSystem Found;
        internal bool _nodeTooltipRequiresLayout;
        internal NodeTooltip _nodeTooltip;
        internal NodeTooltipInfo _activeNodeTooltipInfo;
        internal SkillTreeNode ActiveNodeTooltipNode;
    }
    internal sealed class NodeTooltip : Component { internal NodeTooltipInfo _info; }
    internal sealed class NodeTooltipInfo : NativeObject
    {
        internal static int Refreshes;
        internal SkillTreeNode Node;
        internal string Name, Description;
        internal int PointsAllocated => Node.pointsAllocated;
        internal int MaxPoints => Node.maxPoints;
        internal string PointBonusDescription;
        internal NativeArray<NodeTooltipModifierInfo> Modifiers = new(System.Array.Empty<NodeTooltipModifierInfo>());
        internal NativeArray<NodeTooltipModifierInfo> PointBonusModifiers = new(System.Array.Empty<NodeTooltipModifierInfo>());
        internal NativeArray<NodeTooltipRequirementInfo> Requirements = new(System.Array.Empty<NodeTooltipRequirementInfo>());
        internal NodeTooltipInfo(SkillTreeNode node, bool showAltText) => Node = node;
        internal void Refresh()
        {
            Refreshes++;
            Modifiers = new(new[] { new NodeTooltipModifierInfo() });
            Requirements = new(new[] { new NodeTooltipRequirementInfo() });
        }
    }
    internal static class NodeTooltipContentBuilder
    {
        internal static int Builds;
        internal static bool ReturnNull;
        internal static Action OnBuild;
        internal static NodeTooltipInfo GetNodeTooltipInfo(SkillTreeNode node, bool showAltText)
        {
            Builds++;
            if (ReturnNull) return null;
            var result = new NodeTooltipInfo(node, showAltText) { Name = node.Name, Description = node.Description };
            result.Refresh();
            OnBuild?.Invoke();
            return result;
        }
    }
    internal sealed class NodeTooltipModifierInfo : NativeObject
    { internal string ToModifierString() => "Localized modifier"; }
    internal sealed class NodeTooltipRequirementInfo : NativeObject
    { internal bool IsMet; internal string ToRequirementString() => "Localized requirement"; }
}
namespace LeAccess.Accessibility.ControllerMenus
{ internal static class ControllerMenuAccessibility { internal static bool HasActiveContent; } }
namespace LeAccess.I18n
{
    internal static class ModText
    {
        private static readonly Dictionary<string, string> Text = new()
        {
            ["node.locked"] = "Locked", ["node.full"] = "Full", ["node.no_points"] = "No points left",
            ["node.points"] = "{0} of {1} points allocated",
            ["node.requirement_met"] = "{0}, requirement met", ["node.requirement_unmet"] = "{0}, requirement not met",
            ["passive.tree"] = "{0}, {1} points", ["passive.tree_class"] = "Base class",
            ["passive.tree_chosen"] = "Your mastery", ["passive.tree_locked"] = "Tree locked",
            ["passive.no_mastery"] = "No mastery chosen", ["passive.overview"] = "Trees: {0}",
            ["passive.node_threshold"] = "Requires {0} tree points, you have {1}",
            ["passive.ability"] = "Tree skill", ["passive.mastery_ability"] = "Mastery skill",
            ["passive.ability_unlocked"] = "Unlocked", ["passive.ability_unlockable"] = "Unlocks at {0}, you have {1}",
            ["passive.ability_unreachable"] = "Needs {0}, tree allows {1}",
            ["list.position"] = "{0} of {1}", ["passive.column"] = "Column {0}", ["passive.unspent"] = "{0} unspent",
            ["passive.tree_shown"] = "Shown tree", ["passive.open_tree"] = "Confirm to open", ["passive.banner"] = "Banner", ["passive.column_threshold"] = "Column {0}, needs {1}",
            ["passive.column_locked"] = "Column {0} locked, needs {1}, you have {2}", ["passive.node_locked"] = "Locked node",
            ["passive.column_unlocked"] = "Column {0} unlocked", ["passive.column_relocked"] = "Column {0} relocked",
            ["skill.learned"] = "Learned", ["skill.not_learned"] = "Not learned",
            ["skill.requires"] = "requires {0}", ["skill.requires_point"] = "1 point in {1}, has {2}",
            ["skill.requires_points"] = "{0} points in {1}, has {2}", ["skill.path"] = "Path: {0}", ["skill.leads_to"] = "Leads to: {0}",
        };
        internal static string Get(string key) => Text[key];
        internal static string Format(string key, params object[] args) => string.Format(Text[key], args);
    }
}
namespace LeAccess.Accessibility.Skills
{
    internal static class SkillLearningReader
    {
        internal static readonly Dictionary<Ability, (bool Learned, string Level)> Known = new();
        internal static bool TryReadKnown(Ability ability, out bool learned, out string requiredLevel)
        {
            bool found = Known.TryGetValue(ability, out var state);
            learned = state.Learned;
            requiredLevel = state.Level;
            return found;
        }
    }
}
namespace LeAccess.Accessibility.CharacterCreation
{
    internal static class MasteryReader
    {
        internal static string LocalizedName(string name, bool mastery) => name is null ? null : "Localized " + name;
        internal static string ReadName(MasteryButton button) => button.Name;
        internal static string ReadDefinition(MasteryButton button) => button.Definition;
    }
}
namespace LE.UI.PanelSystem
{
    internal sealed class FullScreenPanelPassiveTree : Component
    {
        internal NativeObject ActiveChildNavigable;
        internal int _currentMasteryIndex;
        internal TMPro.TMP_Text _masteryNameText, _pointsAllocatedText, _pointsAvailableText;
        internal NativeArray<PassivePanelData.MasteryData> _masteryDataList;
        internal NativeArray<MasteryTabButton> _masteryTabButtons;
        internal LocalTreeData _localTreeData;
        internal MasteryBarDisplay _masteryBarDisplay;
    }
}
namespace UnityEngine
{
    internal struct Vector3 { internal float x, y, z; }
}
namespace UnityEngine.UI { internal sealed class Button : Selectable { } }
