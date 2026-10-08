using System;
using System.Collections.Generic;
using LE.Factions;
using LE.UI.Tooltips;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>Reads the Weaver tree node the controller selected: name, points, lock, effect, and what a point changed.</summary>
/// <remarks>See reference/faction-vendor-panels.md#weaver-tree.</remarks>
internal static class WeaverTreeReader
{
    private static FactionTreeText.NodeState? _last;
    private static long _introducedTree;
    private static NodeTooltipInfo _info;
    private static string _infoKey;
    private const double ModelRetrySeconds = 0.2;
    private static double _nextRetry;

    internal static void Reset()
    {
        _last = null;
        _introducedTree = 0;
        _info = null;
        _infoKey = null;
        _nextRetry = 0;
    }

    /// <summary>Every frame: the game selects tree nodes without a focus cursor, so a new node or a point is spoken here.</summary>
    internal static void Poll()
    {
        if (!TryGetSelected(out var navigable, out var node)) { _last = null; _introducedTree = 0; return; }
        var state = StateOf(navigable, node);
        if (_last is not { } before || before.Node != state.Node)
        {
            if (Time.unscaledTimeAsDouble < _nextRetry) return;
            if (Describe(node) is null) { _nextRetry = Time.unscaledTimeAsDouble + ModelRetrySeconds; return; }
            _last = state;
            long tree = node.tree.Pointer.ToInt64();
            string intro = _introducedTree == tree ? null : FactionTreeText.TreeIntro(state.Unspent, state.Pending);
            _introducedTree = tree;
            ElementAnnouncer.AnnounceElement(node.gameObject, interrupt: true, context: intro);
            return;
        }
        if (before == state) return;
        _last = state;
        string changes = FactionTreeText.Changes(before, state);
        if (changes != null && UiFocusOwnership.HasForegroundAccess(node.gameObject)) ScreenReader.TrySpeak(changes, interrupt: true);
    }

    /// <summary>The generic focus path asks for a node's text: the same summary, or silence while the model is pending.</summary>
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        var node = element.GetComponentInParent<WeaverTreeNode>();
        if (!IsAlive(node)) return false;
        text = Describe(node) ?? "";
        return true;
    }

    /// <summary>Held-detail reading of the selected node, or false when the Weaver tree is not the selection.</summary>
    internal static bool ReadDetails()
    {
        if (!TryGetSelected(out _, out var node)) return false;
        var info = ModelOf(node);
        if (info is null) return false;
        var effect = EffectLines(info);
        effect.AddRange(BonusLines(info));
        var requirements = new List<string>();
        foreach (var requirement in Items(info.Requirements))
        {
            string text = TextUtils.CleanText(requirement.ToRequirementString());
            if (!string.IsNullOrWhiteSpace(text))
                requirements.Add(LeAccess.I18n.ModText.Format(requirement.IsMet ? "node.requirement_met" : "node.requirement_unmet", text));
        }
        string details = FactionTreeText.Details(effect, requirements, Neighbours(node));
        if (string.IsNullOrWhiteSpace(details)) return false;
        ScreenReader.Speak(details, true);
        return true;
    }

    private static bool TryGetSelected(out WeaverTreeNavigable navigable, out SkillTreeNode node)
    {
        navigable = null;
        node = null;
        var current = NavigationManager.currentlySelected;
        if (current is null || current.WasCollected) return false;
        navigable = current.TryCast<WeaverTreeNavigable>();
        if (!IsAlive(navigable) || !navigable.isActiveAndEnabled || !UiVisibility.IsVisible(navigable.gameObject)) return false;
        node = navigable.ActiveNode;
        return IsAlive(node) && node.isActiveAndEnabled && IsAlive(node.tree);
    }

    private static FactionTreeText.NodeState StateOf(WeaverTreeNavigable navigable, SkillTreeNode node)
    {
        var panel = navigable.GetComponentInParent<FactionRankUIWeaver>();
        var pending = IsAlive(panel) ? panel.unconfirmedPointsText : null;
        return new(node.Pointer.ToInt64(), node.pointsAllocated, node.maxPoints, node.tree.unspentPoints, node.requirementsMet,
            TextUtils.IsVisibleLabel(pending) ? TextUtils.CleanText(pending.text) : null);
    }

    private static string Describe(SkillTreeNode node)
    {
        var info = ModelOf(node);
        string name = info is null ? null : TextUtils.CleanText(info.Name);
        if (string.IsNullOrWhiteSpace(name)) return null;
        return FactionTreeText.Node(name, info.PointsAllocated, info.MaxPoints, node.requirementsMet,
            string.Join(". ", EffectLines(info)));
    }

    // The tooltip's own builder gives the localized name, effect and requirements; it is rebuilt when a point changes.
    private static NodeTooltipInfo ModelOf(SkillTreeNode node)
    {
        string key = $"{node.Pointer}:{node.pointsAllocated}:{node.requirementsMet}:{node.tree.unspentPoints}:{(global::Localization.Locale)}";
        if (_info is null || _info.WasCollected || _infoKey != key)
        {
            _info = NodeTooltipContentBuilder.GetNodeTooltipInfo(node, false);
            _infoKey = key;
        }
        return _info is null || _info.WasCollected ? null : _info;
    }

    private static List<string> EffectLines(NodeTooltipInfo info)
    {
        var lines = new List<string> { TextUtils.CleanText(info.Description) };
        foreach (var modifier in Items(info.Modifiers)) lines.Add(TextUtils.CleanText(modifier.ToModifierString()));
        lines.RemoveAll(string.IsNullOrWhiteSpace);
        return lines;
    }

    private static List<string> BonusLines(NodeTooltipInfo info)
    {
        var lines = new List<string> { TextUtils.CleanText(info.PointBonusDescription) };
        foreach (var modifier in Items(info.PointBonusModifiers)) lines.Add(TextUtils.CleanText(modifier.ToModifierString()));
        lines.RemoveAll(string.IsNullOrWhiteSpace);
        return lines;
    }

    private static IEnumerable<T> Items<T>(Il2CppSystem.Collections.Generic.List<T> list) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        if (list is null || list.WasCollected) yield break;
        for (int i = 0; i < list.Count; i++)
            if (list[i] is { WasCollected: false } item) yield return item;
    }

    // Nodes the tree draws a line to, clockwise from straight up, each with the way to it on screen.
    private static List<FactionTreeText.Neighbour> Neighbours(SkillTreeNode node)
    {
        var found = new List<(double Angle, FactionTreeText.Neighbour Neighbour)>();
        var seen = new HashSet<IntPtr>();
        foreach (var connection in Items(node.tree.connections))
        {
            var start = connection.startNode;
            var end = connection.endNode;
            if (!IsAlive(start) || !IsAlive(end)) continue;
            var other = start.Pointer == node.Pointer ? end : end.Pointer == node.Pointer ? start : null;
            if (!IsAlive(other) || !other.isActiveAndEnabled || !seen.Add(other.Pointer)) continue;
            var delta = other.transform.position - node.transform.position;
            string direction = GuidanceGeometry.Direction(new System.Numerics.Vector3(delta.x, 0, delta.y),
                System.Numerics.Vector3.UnitZ, System.Numerics.Vector3.UnitX);
            var info = ModelOf(other);
            string name = info is null ? null : TextUtils.CleanText(info.Name);
            if (direction is null || string.IsNullOrWhiteSpace(name)) continue;
            double angle = Math.Atan2(delta.x, delta.y);
            found.Add((angle < 0 ? angle + 2 * Math.PI : angle, new(name, direction)));
        }
        found.Sort((a, b) => a.Angle.CompareTo(b.Angle));
        return found.ConvertAll(entry => entry.Neighbour);
    }
}
