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
    internal int pointsAllocated, maxPoints = 5;
    internal bool requirementsMet, Passive = true;
    internal string Name = "Localized passive", Description = "Game description";
    internal bool inPassiveTree() => Passive;
}
internal static class Localization { internal static string Locale = "es"; }
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
        internal static string Get(string key) => key == "node.available" ? "Available" : "Locked";
        internal static string Format(string key, params object[] args) =>
            string.Format(key == "node.points" ? "{0} of {1} points allocated" :
                key == "node.requirement_met" ? "{0}, requirement met" : "{0}, requirement not met", args);
    }
}
namespace LE.UI.PanelSystem
{
    internal sealed class FullScreenPanelPassiveTree : Component
    { internal NativeObject ActiveChildNavigable; }
}
