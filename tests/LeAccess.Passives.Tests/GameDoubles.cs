using UnityEngine;

internal static class NavigationManager { internal static NativeObject currentlySelected; }
internal sealed class PassiveTreeNavigable : Component
{
    internal NativeObject _navigationElements;
    internal int _currentColumn, _currentRow;
}
internal sealed class SkillnodeTooltip : Component { internal SkillTreeNode node; }
internal sealed class Tree : Component { internal int unspentPoints; }
internal sealed class SkillTreeNode : Component
{
    internal Tree tree;
    internal int pointsAllocated, maxPoints = 5, mastery, masteryRequirement;
    internal bool requirementsMet, Passive = true;
    internal string Name = "Localized passive", Description = "Game description";
    internal bool inPassiveTree() => Passive;
}
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
internal sealed class LocalTreeData : NativeObject
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
            ["node.available"] = "Available", ["node.locked"] = "Locked",
            ["node.points"] = "{0} of {1} points allocated",
            ["node.requirement_met"] = "{0}, requirement met", ["node.requirement_unmet"] = "{0}, requirement not met",
            ["passive.tree"] = "{0}, {1} points", ["passive.tree_class"] = "Base class",
            ["passive.tree_chosen"] = "Your mastery", ["passive.tree_locked"] = "Tree locked",
            ["passive.no_mastery"] = "No mastery chosen", ["passive.overview"] = "Trees: {0}",
            ["passive.node_threshold"] = "Requires {0} tree points, you have {1}",
            ["passive.ability"] = "Tree skill", ["passive.mastery_ability"] = "Mastery skill",
            ["passive.ability_unlocked"] = "Unlocked", ["passive.ability_unlockable"] = "Unlocks at {0}, you have {1}",
            ["passive.ability_unreachable"] = "Needs {0}, tree allows {1}",
        };
        internal static string Get(string key) => Text[key];
        internal static string Format(string key, params object[] args) => string.Format(Text[key], args);
    }
}
namespace LeAccess.Accessibility.CharacterCreation
{
    internal static class MasteryReader
    { internal static string LocalizedName(string name, bool mastery) => name is null ? null : "Localized " + name; }
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
