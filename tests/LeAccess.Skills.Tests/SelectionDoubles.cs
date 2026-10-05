namespace Il2CppSystem
{
    internal sealed class Array : NativeObject
    {
        private readonly System.Array _values;
        internal Array(System.Array values) => _values = values;
        internal int Rank { get { Guard(); return _values.Rank; } }
        internal int GetLength(int dimension) { Guard(); return _values.GetLength(dimension); }
        internal NativeObject GetValue(int x, int y) { Guard(); return _values.GetValue(x, y) as NativeObject; }
    }
}
internal static class NavigationManager { internal static NativeObject currentlySelected; }
internal class PrioritizedNavigable : UnityEngine.Component { internal UnityEngine.UI.Selectable ActiveSelectable { get; set; } }
internal sealed class SkillsPanelManager : UnityEngine.Component
{
    internal NativeObject selectables;
    internal UnityEngine.Vector2Int currentlySelectedIdx;
    internal bool IsSkillTreeOpen;
    internal bool inTopArea;
    internal SkillTreeNode currentlySelectedNode;
    internal SkillTree CurrentlyOpenTree;
    internal SkillsSpecialisationUIManager SkillsSpecialisationUIManager;
    internal UnityEngine.UI.Button backButton, specButton, stopRespecButton;
    internal readonly List<SkillTreeNode> Focused = new();
    internal int Coroutines;
    internal void FocusTreeNode() { Focused.Add(currentlySelectedNode); currentlySelectedIdx.y = 1; }
    internal Il2CppSystem.Collections.Generic.List<AbilityPanelIcon> baseClassPanelIcons, baseClassTreePanelIcons,
        masteryClass1PanelIcons, masteryClass2PanelIcons, masteryClass3PanelIcons;
    internal AbilityPanelIcon masteryAbilityPanelIcon1, masteryAbilityPanelIcon2, masteryAbilityPanelIcon3;
    internal TMPro.TMP_Text baseClassName, mastery1ClassName, mastery2ClassName, mastery3ClassName;
    internal readonly List<UnityEngine.Vector2Int> FocusedCells = new();
    internal int HiddenTooltips;
    internal void HideIconTooltip() => HiddenTooltips++;
    internal void FocusSelectable() => FocusedCells.Add(currentlySelectedIdx);
    internal readonly HashSet<UnityEngine.Vector2Int> EmptyCells = new();
    internal bool ArraySlotIsEmpty() => EmptyCells.Contains(currentlySelectedIdx);
    internal int RightMoves;
    internal void MoveRight() { RightMoves++; currentlySelectedIdx.x++; FocusSelectable(); }
    internal static int HiddenNodeTooltips;
    internal static void HideTreeNodeTooltip() => HiddenNodeTooltips++;
    internal string ShowTreeNodeTooltip() => "tooltip";
    internal void StartCoroutine(string routine) => Coroutines++;
}
internal enum Direction { LEFT, UP, RIGHT, DOWN }
internal static class DirectionExtensions
{
    internal static Direction ToDirection(UnityEngine.Vector3 input, float threshold = 0.6f) => Math.Abs(input.x) >= Math.Abs(input.y)
        ? input.x < 0 ? Direction.LEFT : Direction.RIGHT : input.y < 0 ? Direction.DOWN : Direction.UP;
}
internal sealed class AbilityPanelIcon : UnityEngine.Component
{
    internal enum AbilityUnlockType { BaseClass, MasteryPoints, Mastering }
    internal AbilityUnlockType ablityUnlockType;
    internal bool locked;
    internal bool specMode;
    internal int slotToSpec = -1;
    internal Ability ability;
    internal AbilityTooltip tooltipAbility;
    internal TMPro.TMP_Text skillName { get; set; }
    internal TMPro.TMP_Text UnlockReqTMP { get; set; }
}
internal sealed class LocalTreeData : UnityEngine.Component
{
    internal CharacterClass characterClass;
    internal readonly HashSet<Ability> Specialised = new();
    internal bool isSpecialised(Ability ability) { Guard(); return Specialised.Contains(ability); }
}
internal sealed class CharacterClass : UnityEngine.Object { internal NativeArray<AbilityAndLevel> unlockableAbilities; }
internal sealed class AbilityAndLevel : NativeObject { internal Ability ability; internal int level; }
internal sealed class KnownAbilityList : UnityEngine.Component
{
    internal LocalTreeData treeData;
    internal NativeArray<Ability> abilities;
}
internal sealed class AbilitySelectionIcon : UnityEngine.Component
{ internal Ability ability; internal AbilityTooltip tooltipAbility { get; set; } }
internal sealed class SpecialisedAbilityIcon : UnityEngine.Component
{ internal Ability ability { get; set; } internal bool locked; internal TMPro.TMP_Text levelLockedText; }
namespace UnityEngine.EventSystems
{
    internal sealed class EventSystem : UnityEngine.Component
    {
        internal static EventSystem current;
        internal UnityEngine.GameObject Selected;
        internal void SetSelectedGameObject(UnityEngine.GameObject selected) => Selected = selected;
    }
}
internal sealed class AbilityTooltipSelectableAdapter : UnityEngine.UI.Selectable
{ internal AbilityTooltip _abilityTooltip { get; set; } }
internal sealed class AbilityTooltip : UnityEngine.Component
{
    internal UITooltipAbility toolTip;
    internal Ability currentAbility;
    internal bool activeAbilityTooltip = true;
}
internal sealed class UITooltipAbility : UnityEngine.Component
{
    internal sealed class BigStatField : NativeObject { internal UnityEngine.GameObject obj; internal TMPro.TMP_Text textField; }
    internal sealed class ScalingInfo : NativeObject { internal UnityEngine.GameObject scalingInfoFieldHolder; }
    internal Il2CppSystem.Collections.Generic.List<BigStatField> statFields;
    internal Il2CppSystem.Collections.Generic.List<ScalingInfo> scalingInfoFields;
    internal TMPro.TMP_Text scalingTagsText, minionTagsText;
    internal enum TooltipInitializationStage { None, SetContent, Completed }
    internal Ability currentAbility;
    internal bool tooltipActive = true;
    internal TooltipInitializationStage activeInitializationStage = TooltipInitializationStage.Completed;
    internal UnityEngine.GameObject content;
    internal TMPro.TMP_Text nameText, descriptionText, skillDefinition, moreInfoDetailsText;
    internal UnityEngine.GameObject moreInfoObj;
}
internal sealed class SkillTree : UnityEngine.Component
{
    internal int treeID; internal Ability ability; internal NativeArray<SkillTreeNode> nodeList;
    internal int unspentPoints;
}
internal sealed class SkillsSpecialisationUIManager : UnityEngine.Component
{
    internal bool Respeccing;
    internal UnityEngine.GameObject specialiseButton, respecButton, stopRespeccingButton;
    internal TMPro.TextMeshProUGUI respecMinLevelText;
}
internal sealed class SkillTreeNode : UnityEngine.Component
{
    internal int pointsAllocated;
    internal byte maxPoints = 5;
    internal bool CouldBeRespecced = true;
    internal bool getCouldBeRespecced() { Guard(); return CouldBeRespecced; }
    internal SkillTree tree;
    internal int id;
    internal string nodeName, description;
    internal NativeArray<RequirementFromNode> requirements;
    internal sealed class RequirementFromNode : NativeObject { internal SkillTreeNode node; }
}
internal sealed class NodeTooltipManager : UnityEngine.Component
{
    internal TMPro.TMP_Text requirementPassiveText, requirementBasePassiveText, requirementQuestText, altTextHeader, altText, loreText;
    internal UnityEngine.GameObject pointBonusContainer;
    internal Il2CppSystem.Collections.Generic.List<NodeTooltipStatDisplay> scalingStats, nonScalingStats, pointBonusStats;
    internal static NodeTooltipManager instance;
    internal bool tooltipActive = true;
    internal bool requireLayoutUpdate { get; set; }
    internal SkillTreeNode previousNode { get; set; }
    internal UnityEngine.GameObject content { get; set; }
    internal TMPro.TMP_Text title { get; set; }
    internal TMPro.TMP_Text description { get; set; }
    internal TMPro.TMP_Text points { get; set; }
}
internal sealed class NodeTooltipStatDisplay : UnityEngine.Component { internal TMPro.TMP_Text text; }
internal sealed class UIBase : UnityEngine.Component
{ internal static UIBase instance; internal LE.UI.Tooltips.TooltipSystem TooltipSystem; }
namespace Il2CppSystem.Collections.Generic
{
    internal sealed class List<T> : NativeObject, System.Collections.Generic.IEnumerable<T>
    {
        internal readonly System.Collections.Generic.List<T> Items = new();
        internal void Add(T value) => Items.Add(value);
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { Guard(); return Items.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
namespace LE.UI.Tooltips
{
    internal sealed class TooltipSystem : UnityEngine.Component
    {
        internal NodeTooltip _nodeTooltip;
        internal NodeTooltipInfo _activeNodeTooltipInfo;
        internal SkillTreeNode ActiveNodeTooltipNode;
        internal bool _nodeTooltipRequiresLayout;
    }
    internal sealed class NodeTooltipInfo : NativeObject
    {
        internal SkillTreeNode Node;
        internal int PointsAllocated = 0, MaxPoints = 5;
        internal Il2CppSystem.Collections.Generic.List<NodeTooltipRequirementInfo> Requirements;
    }
    internal sealed class NodeTooltipRequirementInfo : NativeObject
    {
        internal bool IsMet;
        internal string Text;
        internal string ToRequirementString() { Guard(); return Text; }
    }
    internal sealed class NodeTooltip : UnityEngine.Component
    {
        internal TMPro.TMP_Text _name, _nodeType, _description, _altDescription;
        internal UnityEngine.Component _modifiersSection, _pointBonusModifiersSection, _requirementsSection;
        internal UnityEngine.GameObject _pointBonusContainer, _refundCostRoot;
        internal NodeTooltipInfo _info;
    }
}
internal static class Localization
{
    internal static AbilityLocalization Abilities = new();
    internal static SkillLocalization Skills = new();
}
internal sealed class AbilityLocalization : NativeObject
{
    internal readonly Dictionary<Ability, string> Names = new();
    internal readonly Dictionary<Ability, string> Descriptions = new();
    internal string GetName(Ability ability) { Guard(); return Names.GetValueOrDefault(ability, ability.abilityName); }
    internal Action OnDescription;
    internal string GetDescription(Ability ability) { Guard(); OnDescription?.Invoke(); return Descriptions.GetValueOrDefault(ability); }
}
internal sealed class SkillLocalization : NativeObject
{
    internal string GetNodeName(int tree, int node, string fallback) { Guard(); return fallback; }
    internal string GetNodeDescription(int tree, int node, string fallback) { Guard(); return fallback; }
}
namespace LeAccess.Accessibility.ControllerMenus
{ internal static class ControllerMenuAccessibility { internal static bool HasActiveContent; } }
