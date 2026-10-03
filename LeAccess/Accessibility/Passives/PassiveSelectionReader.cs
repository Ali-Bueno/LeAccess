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

internal readonly record struct PassiveSelection(string Key, GameObject Root, string Summary, string Details)
{
    internal string Text => ScreenContentTracker.Combine(new[] { Summary, Details });
}

internal static class PassiveSelectionReader
{
    private static PassiveTreeNavigable _owner;
    private static SkillTreeNode _node;
    private static NodeTooltipInfo _fallback;
    private static string _revision;
    private static double _nextFallbackRetry;
    // Match the existing inventory/skill refresh rate while native text becomes available.
    private const double FallbackRetrySeconds = 0.2;
    internal static string LastStatus { get; private set; }
    internal static bool HasPassiveContext { get; private set; }
    internal static bool HasPendingModel { get; private set; }

    internal static void Reset()
    {
        _owner = null;
        _node = null;
        _fallback = null;
        _revision = null;
        _nextFallbackRetry = 0;
        HasPendingModel = false;
    }

    internal static bool TryRead(out PassiveSelection selection)
    {
        selection = default;
        HasPassiveContext = false;
        HasPendingModel = false;
        LastStatus = "blocked by focus, loading or foreground content";
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || ScreenContentAnnouncer.HasActiveContent ||
            DialogueAnnouncer.HasActiveContent || ControllerMenuAccessibility.HasActiveContent) return false;
        var current = NavigationManager.currentlySelected;
        if (current is null || current.WasCollected) return Fail("navigation missing or collected");
        var panel = current.TryCast<LE.UI.PanelSystem.FullScreenPanelPassiveTree>();
        if (IsAlive(panel))
        {
            HasPassiveContext = true;
            if (!panel.isActiveAndEnabled || !UiVisibility.IsVisible(panel.gameObject)) return Fail("fullscreen panel disabled or hidden");
            // The fullscreen panel forwards navigation to this exact active child.
            current = panel.ActiveChildNavigable;
            if (current is null || current.WasCollected) return Fail("fullscreen active child missing or collected");
        }
        var owner = current.TryCast<PassiveTreeNavigable>();
        if (!IsAlive(owner))
        {
            var component = current.TryCast<Component>();
            return Fail($"navigation is not PassiveTreeNavigable; object={(IsAlive(component) ? component.gameObject.name : "noncomponent")}");
        }
        HasPassiveContext = true;
        if (!owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject)) return Fail("passive navigable disabled or hidden");
        var raw = owner._navigationElements;
        if (raw is null || raw.WasCollected) return Fail("navigation array missing or collected");
        var cells = raw.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected) return Fail("navigation array cast unavailable");
        if (cells.Rank != 2) return Fail($"navigation array rank={cells.Rank}");
        int column = owner._currentColumn, row = owner._currentRow;
        // Native SelectNode and ShowTooltip both index the erased array as [column,row].
        if (column < 0 || row < 0 || column >= cells.GetLength(0) || row >= cells.GetLength(1))
            return Fail($"cell index invalid: [{column},{row}] shape=[{cells.GetLength(0)},{cells.GetLength(1)}]");
        var cell = cells.GetValue(column, row);
        if (cell is null || cell.WasCollected) return Fail($"selected cell missing: [{column},{row}]");
        var root = cell.TryCast<GameObject>();
        if (!IsAlive(root)) root = cell.TryCast<Component>()?.gameObject;
        if (!IsAlive(root)) return Fail("selected cell is not a live GameObject or Component");
        if (!UiVisibility.IsVisible(root)) return Fail($"selected cell hidden: {root.name}");
        var source = root.GetComponent<SkillnodeTooltip>();
        var node = IsAlive(source) ? source.node : null;
        // Native OnPointerEnter reacquires the same-object node when this cache is empty or foreign.
        if (!IsAlive(node) || node.gameObject.Pointer != root.Pointer) node = root.GetComponent<SkillTreeNode>();
        if (!IsAlive(node)) return Fail($"same-object SkillTreeNode missing: {root.name}; tooltip={IsAlive(source)}");
        if (!node.isActiveAndEnabled || !UiVisibility.IsVisible(node.gameObject)) return Fail("selected node disabled or hidden");
        if (!IsAlive(node.tree)) return Fail("selected node tree missing");
        if (!node.inPassiveTree()) return Fail("selected node is not in a passive tree");

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
            if (!IsAlive(_node) || _node.Pointer != node.Pointer || _fallback is null || _fallback.WasCollected || _revision != revision)
            {
                if (IsAlive(_node) && _node.Pointer == node.Pointer && _revision == revision &&
                    Time.unscaledTimeAsDouble < _nextFallbackRetry)
                {
                    HasPendingModel = true;
                    return Fail("native node tooltip name pending");
                }
                _node = node;
                // The bare constructor and Refresh leave Name/Description unset.
                _fallback = NodeTooltipContentBuilder.GetNodeTooltipInfo(node, false);
                _revision = revision;
            }
            info = _fallback;
            if (info is null || info.WasCollected)
            {
                HasPendingModel = true;
                _nextFallbackRetry = Time.unscaledTimeAsDouble + FallbackRetrySeconds;
                return Fail("native node tooltip builder returned no model");
            }
            LastStatus = "ready; native node tooltip builder";
        }
        else LastStatus = "ready; owned visible node tooltip";
        string name = TextUtils.CleanText(info.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            if (_fallback is not null && _fallback.Pointer == info.Pointer)
            {
                _fallback = null;
                _nextFallbackRetry = Time.unscaledTimeAsDouble + FallbackRetrySeconds;
                HasPendingModel = true;
            }
            return Fail($"node name empty; {LastStatus}");
        }
        string summary = ScreenContentTracker.Combine(new[] { name,
            ModText.Format("node.points", info.PointsAllocated, info.MaxPoints),
            ModText.Get(node.requirementsMet ? "node.available" : "node.locked") });
        var fragments = new List<string> { TextUtils.CleanText(info.Description) };
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
        selection = new PassiveSelection($"{node.tree.GetInstanceID()}:{node.GetInstanceID()}", root, summary,
            ScreenContentTracker.Combine(fragments));
        return true;
    }

    private static bool Fail(string status) { LastStatus = status; return false; }
}
