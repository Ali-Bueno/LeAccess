using System.Collections.Generic;
using LE.UI.Tooltips;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

internal readonly record struct PassiveSelection(string Key, GameObject Root, string Text);

internal static class PassiveSelectionReader
{
    private static PassiveTreeNavigable _owner;
    private static SkillTreeNode _node;
    private static NodeTooltipInfo _fallback;
    private static string _revision;

    internal static void Reset()
    {
        _owner = null;
        _node = null;
        _fallback = null;
        _revision = null;
    }

    internal static bool TryRead(out PassiveSelection selection)
    {
        selection = default;
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || ScreenContentAnnouncer.HasActiveContent ||
            DialogueAnnouncer.HasActiveContent || ControllerMenuAccessibility.HasActiveContent) return false;
        var current = NavigationManager.currentlySelected;
        if (current is null || current.WasCollected) return false;
        var owner = current.TryCast<PassiveTreeNavigable>();
        if (!IsAlive(owner) || !owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject)) return false;
        var raw = owner._navigationElements;
        if (raw is null || raw.WasCollected) return false;
        var cells = raw.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || cells.Rank != 2) return false;
        int column = owner._currentColumn, row = owner._currentRow;
        // Native SelectNode and ShowTooltip both index the erased array as [column,row].
        if (column < 0 || row < 0 || column >= cells.GetLength(0) || row >= cells.GetLength(1)) return false;
        var cell = cells.GetValue(column, row);
        if (cell is null || cell.WasCollected) return false;
        var root = cell.TryCast<GameObject>();
        if (!IsAlive(root)) root = cell.TryCast<Component>()?.gameObject;
        if (!IsAlive(root) || !UiVisibility.IsVisible(root)) return false;
        var source = root.GetComponent<SkillnodeTooltip>();
        var node = IsAlive(source) ? source.node : root.GetComponent<SkillTreeNode>();
        if (!IsAlive(node) || !node.isActiveAndEnabled || !UiVisibility.IsVisible(node.gameObject) ||
            !IsAlive(node.tree) || !node.inPassiveTree()) return false;

        if (!IsAlive(_owner) || _owner.Pointer != owner.Pointer)
        {
            Reset();
            _owner = owner;
        }

        NodeTooltipInfo info = null;
        var ui = UIBase.instance;
        var system = IsAlive(ui) ? ui.TooltipSystem : null;
        if (IsAlive(system) && system.isActiveAndEnabled && !system._nodeTooltipRequiresLayout &&
            IsAlive(system._nodeTooltip) && system._nodeTooltip.isActiveAndEnabled &&
            UiVisibility.IsVisible(system._nodeTooltip.gameObject))
        {
            var displayed = system._nodeTooltip._info;
            var active = system._activeNodeTooltipInfo;
            var activeNode = system.ActiveNodeTooltipNode;
            if (displayed is not null && !displayed.WasCollected && active is not null && !active.WasCollected &&
                displayed.Pointer == active.Pointer && IsAlive(activeNode) && activeNode.Pointer == node.Pointer &&
                IsAlive(displayed.Node) && displayed.Node.Pointer == node.Pointer) info = displayed;
        }
        if (info is null)
        {
            string locale = global::Localization.Locale;
            string revision = $"{node.pointsAllocated}:{node.maxPoints}:{node.requirementsMet}:{node.tree.unspentPoints}:{locale}";
            if (!IsAlive(_node) || _node.Pointer != node.Pointer || _fallback is null || _fallback.WasCollected)
            {
                _node = node;
                _fallback = new NodeTooltipInfo(node, false);
                _revision = null;
            }
            if (_revision != revision)
            {
                _fallback.Refresh();
                _revision = revision;
            }
            info = _fallback;
        }
        string name = TextUtils.CleanText(info.Name);
        if (string.IsNullOrWhiteSpace(name)) return false;
        var fragments = new List<string> { name, ModText.Format("node.points", info.PointsAllocated, info.MaxPoints),
            TextUtils.CleanText(info.Description) };
        if (info.Modifiers is not null && !info.Modifiers.WasCollected)
            foreach (var modifier in info.Modifiers)
                if (modifier is not null && !modifier.WasCollected) fragments.Add(TextUtils.CleanText(modifier.ToModifierString()));
        fragments.Add(TextUtils.CleanText(info.PointBonusDescription));
        if (info.PointBonusModifiers is not null && !info.PointBonusModifiers.WasCollected)
            foreach (var modifier in info.PointBonusModifiers)
                if (modifier is not null && !modifier.WasCollected) fragments.Add(TextUtils.CleanText(modifier.ToModifierString()));
        if (info.Requirements is not null && !info.Requirements.WasCollected)
            foreach (var requirement in info.Requirements)
                if (requirement is not null && !requirement.WasCollected)
                {
                    string text = TextUtils.CleanText(requirement.ToRequirementString());
                    if (!string.IsNullOrWhiteSpace(text)) fragments.Add(ModText.Format(requirement.IsMet ?
                        "node.requirement_met" : "node.requirement_unmet", text));
                }
        selection = new PassiveSelection($"{node.tree.GetInstanceID()}:{node.GetInstanceID()}", root,
            ScreenContentTracker.Combine(fragments));
        return true;
    }
}
